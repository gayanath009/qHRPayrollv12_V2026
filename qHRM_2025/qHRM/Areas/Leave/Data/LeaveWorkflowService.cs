using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using qHRM.Area.Config;
using qHRM.Areas.Leave.Models;
using qHRM.Models;

using System.IO;
using qHRM.Areas.Notify.Data;
using qHRM.Areas.Notify.Models;

namespace qHRM.Areas.Leave.Data
{
    // =========================================================================
    // DTOs for Workflow Engine API communication
    // =========================================================================

    public class WfStartRequest
    {
        public string WorkflowCode { get; set; }
        public Dictionary<string, string> InputData { get; set; }
        public Guid? RequesterId { get; set; }
    }

    public class WfStartResponse
    {
        public string Message { get; set; }
        public Guid InstanceId { get; set; }
        public string InstanceNumber { get; set; }
    }

    public class WfApproveRequest
    {
        public Guid ApprovalId { get; set; }
        public string Comment { get; set; }
        public string DigitalSignature { get; set; }
    }

    public class WfRejectRequest
    {
        public Guid ApprovalId { get; set; }
        public string Comment { get; set; }
    }

    public class WfDelegateRequest
    {
        public Guid ApprovalId { get; set; }
        public Guid DelegateUserId { get; set; }
        public string Comment { get; set; }
    }

    public class WfInstanceDto
    {
        public Guid Id { get; set; }
        public string InstanceNumber { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public List<WfInstanceStepDto> Steps { get; set; } = new();
        public List<WfHistoryDto> Histories { get; set; } = new();
    }

    public class WfInstanceStepDto
    {
        public Guid Id { get; set; }
        public int StepNumber { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<WfApprovalDto> Approvals { get; set; } = new();
    }

    public class WfApprovalDto
    {
        public Guid Id { get; set; }
        public Guid ApproverId { get; set; }
        public string ApproverName { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public DateTime? ActionDate { get; set; }
        public string Comment { get; set; }
        public string DelegatedFromUserName { get; set; }
    }

    public class WfHistoryDto
    {
        public string StepName { get; set; }
        public string Action { get; set; }
        public string PerformedByName { get; set; }
        public DateTime ActionDate { get; set; }
        public string Comment { get; set; }
    }

    public class WfAuthResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
    }

    public class WfSyncUserRequest
    {
        public int? EmpSysId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
    }

    public class LvApproverResolveResult
    {
        public bool Found { get; set; }
        public int? EmpSysId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    // =========================================================================
    // Notification Event Type constants
    // =========================================================================
    public static class LvWfNotifyEvent
    {
        public const string Submitted     = "Submitted";
        public const string Approved      = "Approved";
        public const string Rejected      = "Rejected";
        public const string FinalApproved = "FinalApproved";
        public const string Delegated     = "Delegated";
        public const string Reminder      = "Reminder";
    }

    // =========================================================================
    // LeaveWorkflowService
    // Central service for all leave-workflow integration logic.
    // =========================================================================

    public class LeaveWorkflowService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly qHRMasterContext   _hrmContext;
        private readonly SysConfigContext   _sysConfigContext;
        private readonly IConfiguration     _configuration;
        private readonly WorkflowNotificationService _notificationService;
        private readonly LeaveBalanceService _balanceService;
        private readonly IServiceScopeFactory _scopeFactory;

        // ── JWT token cache ──────────────────────────────────────────────────
        // Reusing a valid token eliminates one HTTP roundtrip (~200-500 ms) on
        // every workflow submission. Tokens are refreshed 60 s before expiry.
        private string   _cachedToken;
        private DateTime _tokenExpiresAt = DateTime.MinValue;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);
        // ────────────────────────────────────────────────────────────────────

        private string WfEngineBaseUrl
        {
            get
            {
                var url = _configuration["WorkflowEngine:BaseUrl"] ?? "http://localhost:5001";
                if (string.IsNullOrWhiteSpace(url)) url = "http://localhost:5001";
                // Normalize: remove trailing slash and trim any trailing '/api' segment to avoid double '/api/api' when endpoints append '/api/...'
                url = url.TrimEnd('/');
                if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                    url = url.Substring(0, url.Length - 4);
                return url;
            }
        }
        private string WfAdminUser     => _configuration["WorkflowEngine:AdminUser"] ?? "admin";
        private string WfAdminPassword => _configuration["WorkflowEngine:AdminPassword"] ?? "Password123!";

        public LeaveWorkflowService(
            IHttpClientFactory httpClientFactory,
            qHRMasterContext   hrmContext,
            SysConfigContext   sysConfigContext,
            IConfiguration     configuration,
            WorkflowNotificationService notificationService = null,
            LeaveBalanceService balanceService = null,
            IServiceScopeFactory scopeFactory = null)
        {
            _httpClientFactory   = httpClientFactory;
            _hrmContext          = hrmContext;
            _sysConfigContext    = sysConfigContext;
            _configuration       = configuration;
            _notificationService = notificationService;
            _balanceService      = balanceService;
            _scopeFactory        = scopeFactory;
        }

        // =====================================================================
        // WORKFLOW ENGINE AUTHENTICATION
        // Gets a JWT token from the Workflow Engine using admin credentials.
        // Token is cached in memory and reused until 60 s before its expiry
        // to avoid an HTTP roundtrip on every workflow submission.
        // ── Fast-fail circuit breaker on connection failure ──────────────────
        private static DateTime _lastConnectionFailureTime = DateTime.MinValue;
        private static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(5);

        private async Task<string> GetWfTokenAsync(CancellationToken cancellationToken = default)
        {
            // Fast path: return cached token if still valid (with 60 s buffer).
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            // Circuit breaker: If the workflow engine recently failed/refused connection, fail fast
            if (DateTime.UtcNow - _lastConnectionFailureTime < FailureCooldown)
                throw new HttpRequestException("Workflow Engine is currently offline (cooldown active).");

            // Slow path: acquire lock so only one thread refreshes the token.
            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring the lock.
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAt)
                    return _cachedToken;

                if (DateTime.UtcNow - _lastConnectionFailureTime < FailureCooldown)
                    throw new HttpRequestException("Workflow Engine is currently offline (cooldown active).");

                var client = _httpClientFactory.CreateClient("WorkflowEngine");
                client.Timeout = TimeSpan.FromSeconds(15);

                var payload = new { Username = WfAdminUser, Password = WfAdminPassword };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8, "application/json");

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var response = await client.PostAsync($"{WfEngineBaseUrl}/api/auth/login", content, linkedCts.Token);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<WfAuthResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _cachedToken   = result?.Token ?? throw new Exception("Failed to obtain Workflow Engine token.");
                // Assume tokens are valid for 30 minutes; refresh 60 s before expiry.
                _tokenExpiresAt = DateTime.UtcNow.AddMinutes(29);
                _lastConnectionFailureTime = DateTime.MinValue;
                return _cachedToken;
            }
            catch (Exception ex)
            {
                // Only trip circuit breaker on actual network connection failures (e.g. host unreachable, port closed, connection refused)
                if (ex is System.Net.Sockets.SocketException ||
                    (ex is HttpRequestException httpEx && (httpEx.InnerException is System.Net.Sockets.SocketException || httpEx.Message.Contains("No connection could be made") || httpEx.Message.Contains("actively refused"))))
                {
                    _lastConnectionFailureTime = DateTime.UtcNow;
                }
                throw;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private HttpClient GetWfClient(string token)
        {
            var client = _httpClientFactory.CreateClient("WorkflowEngine");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // =====================================================================
        // PARSE NOTE & ATTACHMENT HELPER
        // Extracts clean reason text and attachment metadata (e.g. [MedCert: filename.ext])
        // =====================================================================
        public static (string cleanReason, bool hasAttachment, string attachmentUrl, string attachmentFileName, string attachmentType) ParseNoteAndAttachment(string note, string defaultReason = "—")
        {
            if (string.IsNullOrWhiteSpace(note))
                return (defaultReason ?? "—", false, string.Empty, string.Empty, string.Empty);

            string text = note.Trim();
            string attachmentFile = string.Empty;
            string attachmentType = string.Empty;
            string attachmentUrl = string.Empty;

            var match = System.Text.RegularExpressions.Regex.Match(text, @"\[(?:MedCert|Attachment|Doc):\s*([^\]]+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                attachmentFile = match.Groups[1].Value.Trim();
                text = text.Replace(match.Value, "").Trim();
            }

            bool hasAttachment = !string.IsNullOrEmpty(attachmentFile);
            if (hasAttachment)
            {
                attachmentUrl = $"/attach/medCert/{attachmentFile}";
                var ext = System.IO.Path.GetExtension(attachmentFile).ToLowerInvariant();
                attachmentType = (ext == ".pdf") ? "pdf" : (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(ext) ? "image" : "document");
            }

            string cleanReason = !string.IsNullOrWhiteSpace(text) ? text : (defaultReason ?? "—");
            return (cleanReason, hasAttachment, attachmentUrl, attachmentFile, attachmentType);
        }

        // =====================================================================
        // USER SYNCHRONISATION
        // Ensures an HR employee exists as a User in the Workflow Engine.
        // Maps EmpSysId (int) ↔ WF User GUID via HrWfUserMapping table.
        // =====================================================================

        public async Task<Guid> GetOrCreateWfUserAsync(int empSysId, string token)
        {
            // Check mapping table first
            var mapping = await _hrmContext.HrWfUserMapping
                .FirstOrDefaultAsync(m => m.EmpSysId == empSysId);

            if (mapping != null)
                return mapping.WfUserId;

            // Fetch employee details
            var emp = await _hrmContext.HrEmpGenInfo
                .FirstOrDefaultAsync(e => e.EmpSysId == empSysId);

            if (emp == null)
                throw new Exception($"Employee {empSysId} not found in HR Master.");

            // Get office email from job assignment (strictly Ofc_Email from HR_EmpJobAssign)
            var jobAssign = await _hrmContext.HrEmpJobAssign
                .Where(j => j.EmpSysId == empSysId && j.WkStat == 1)
                .OrderByDescending(j => j.StDate)
                .FirstOrDefaultAsync();
            if (jobAssign == null)
            {
                jobAssign = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == empSysId)
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();
            }

            string email = jobAssign?.OfcEmail ?? "";

            string username = emp.EmpId?.ToLower().Replace(" ", "_")
                ?? $"emp_{empSysId}";

            // Create user in Workflow Engine
            var client = GetWfClient(token);
            var payload = new WfSyncUserRequest
            {
                EmpSysId = empSysId,
                Username = username,
                Email    = email,
                Password = "WfSync@" + empSysId,  // Temp password; WF engine users use JWT
                FullName = emp.FullName
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{WfEngineBaseUrl}/api/admin/users", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create WF Engine user for {emp.FullName}: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var created = JsonSerializer.Deserialize<JsonElement>(json, opts);
            var wfUserId = created.GetProperty("id").GetGuid();

            // Save mapping
            _hrmContext.HrWfUserMapping.Add(new HrWfUserMapping
            {
                EmpSysId    = empSysId,
                WfUserId    = wfUserId,
                WfUsername  = username,
                CreatedDate = DateTime.Now
            });
            await _hrmContext.SaveChangesAsync();

            return wfUserId;
        }

        // =====================================================================
        // RESOLVE WORKFLOW CODE
        // Resolves the concrete Workflow Engine definition Code (e.g. INH-SICK-CSL,
        // OUS-SICK-CSL, INH-ANUL, OUS-ANUL) matching the leave type and employee work type.
        // =====================================================================
        public async Task<string> ResolveWorkflowCodeAsync(string configuredWfCode, string lvTypCode, int wrktypId)
        {
            string reqWorkType = wrktypId == 2 ? "OUTSOURCED" : "IN_HOUSE";
            string targetCode = !string.IsNullOrWhiteSpace(configuredWfCode) ? configuredWfCode.Trim() : (lvTypCode ?? "LV_ANL").Trim();
            string leaveCode = !string.IsNullOrWhiteSpace(lvTypCode) ? lvTypCode.Trim() : targetCode;

            string wfConStr = GetWorkflowEngineDbConnectionString();
            if (!string.IsNullOrEmpty(wfConStr))
            {
                try
                {
                    using var conn = new Microsoft.Data.SqlClient.SqlConnection(wfConStr);
                    await conn.OpenAsync();

                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                        SELECT RTRIM(Code) AS Code, RTRIM(ISNULL(TargetWorkType, '')) AS TargetWorkType, RTRIM(ISNULL(AssociatedLeaveTypes, '')) AS AssociatedLeaveTypes
                        FROM WorkflowDefinitions
                        WHERE IsActive = 1 AND IsDeleted = 0", conn);

                    using var reader = await cmd.ExecuteReaderAsync();
                    var defs = new List<(string Code, string TargetWorkType, string AssociatedLeaveTypes)>();
                    while (await reader.ReadAsync())
                    {
                        defs.Add((
                            reader["Code"]?.ToString() ?? "",
                            reader["TargetWorkType"]?.ToString() ?? "",
                            reader["AssociatedLeaveTypes"]?.ToString() ?? ""
                        ));
                    }

                    bool MatchesWorkType((string Code, string TargetWorkType, string AssociatedLeaveTypes) d, bool exactOnly)
                    {
                        string defWorkType = d.TargetWorkType.Trim().ToUpperInvariant();
                        if (exactOnly)
                        {
                            return defWorkType == reqWorkType || (reqWorkType == "OUTSOURCED" && defWorkType == "OUTSRC");
                        }
                        return string.IsNullOrEmpty(defWorkType) || defWorkType == "ALL" || defWorkType == reqWorkType || (reqWorkType == "OUTSOURCED" && defWorkType == "OUTSRC");
                    }

                    bool MatchesLeaveCode((string Code, string TargetWorkType, string AssociatedLeaveTypes) d)
                    {
                        if (string.Equals(d.Code, targetCode, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(d.Code, leaveCode, StringComparison.OrdinalIgnoreCase))
                            return true;

                        if (!string.IsNullOrWhiteSpace(d.AssociatedLeaveTypes))
                        {
                            var tokens = d.AssociatedLeaveTypes.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return tokens.Any(code => string.Equals(code.Trim(), targetCode, StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(code.Trim(), leaveCode, StringComparison.OrdinalIgnoreCase));
                        }
                        return false;
                    }

                    // Check exact code match first
                    var exact = defs.FirstOrDefault(d => string.Equals(d.Code, targetCode, StringComparison.OrdinalIgnoreCase) && MatchesWorkType(d, exactOnly: true));
                    if (!string.IsNullOrEmpty(exact.Code)) return exact.Code;

                    exact = defs.FirstOrDefault(d => string.Equals(d.Code, targetCode, StringComparison.OrdinalIgnoreCase) && MatchesWorkType(d, exactOnly: false));
                    if (!string.IsNullOrEmpty(exact.Code)) return exact.Code;

                    // Priority 1: Specific work-type match + matching leave type / associated leave type
                    var matched = defs.FirstOrDefault(d => MatchesLeaveCode(d) && MatchesWorkType(d, exactOnly: true));
                    if (!string.IsNullOrEmpty(matched.Code)) return matched.Code;

                    // Priority 2: Generic work-type match (ALL or unrestricted) + matching leave type
                    matched = defs.FirstOrDefault(d => MatchesLeaveCode(d) && MatchesWorkType(d, exactOnly: false));
                    if (!string.IsNullOrEmpty(matched.Code)) return matched.Code;
                }
                catch
                {
                    // If DB query fails, fall through to deterministic fallback
                }
            }

            // Fallback deterministic resolution based on leave type code and work type:
            string check = (!string.IsNullOrEmpty(targetCode) ? targetCode : leaveCode).ToUpperInvariant();
            if (check.Contains("CSL") || check.Contains("SICK"))
            {
                return wrktypId == 2 ? "OUT-SICK-CSL" : "INH-SICK-CSL";
            }

            return wrktypId == 2 ? "OUT-SICK-ANNUAL" : "INH-ANUL";
        }

        // =====================================================================
        // START WORKFLOW
        // Called when employee submits a leave application.
        // Looks up workflow config for the leave type, starts the WF engine
        // instance, creates HrLvWfInstance, and sends submission notification.
        // =====================================================================

        public async Task<HrLvWfInstance> StartLeaveWorkflowAsync(
            HrLvRegister leaveApp,
            int          submitterEmpSysId)
        {
            // 1 & 2. Fetch leave type and workflow config sequentially (DbContext is not thread-safe).
            var lvType = await _hrmContext.HrLvType
                .FirstOrDefaultAsync(t => t.LvTypId == leaveApp.LvTypId);
            var wfConfig = await _hrmContext.HrLvWfConfig
                .FirstOrDefaultAsync(c => c.LvTypId == leaveApp.LvTypId && c.IsEnabled == true);

            if (wfConfig == null || string.IsNullOrWhiteSpace(wfConfig.WfCode))
            {
                string targetWfCode = lvType?.LvTypCode ?? "LV_SICK";
                wfConfig = new HrLvWfConfig
                {
                    LvTypId        = leaveApp.LvTypId ?? 0,
                    WfCode         = targetWfCode,
                    IsEnabled      = true,
                    ApproverConfig = "{}",
                    PolicyRules    = "{}"
                };
            }

            // 3. Validate policy rules before starting.
            // Pass lvType so ValidateLeavePolicyAsync doesn't need to re-query it.
            await ValidateLeavePolicyAsync(leaveApp, wfConfig);

            // 4. Authenticate with WF Engine
            var token = await GetWfTokenAsync();

            // DbContext is not thread-safe; queries must be awaited sequentially
            var empGen = await _hrmContext.HrEmpGenInfo
                .FirstOrDefaultAsync(e => e.EmpSysId == leaveApp.EmpSysId);
            var jobAssign = await _hrmContext.HrEmpJobAssign
                .Where(j => j.EmpSysId == leaveApp.EmpSysId && j.WkStat == 1)
                .OrderByDescending(j => j.StDate)
                .FirstOrDefaultAsync();

            // Ensure the submitter has a WF Engine user account.
            var wfUserId = await GetOrCreateWfUserAsync(submitterEmpSysId, token);

            // 5. Build employee full name.
            string empFullName = "";
            if (empGen != null)
            {
                var nameParts = new[] { empGen.FirstName, empGen.MdlName, empGen.LastName }
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim());
                empFullName = string.Join(" ", nameParts);
                if (string.IsNullOrWhiteSpace(empFullName))
                    empFullName = !string.IsNullOrWhiteSpace(empGen.PstName) ? empGen.PstName.Trim() : (empGen.EmpId ?? "");
            }
            if (string.IsNullOrWhiteSpace(empFullName))
                empFullName = $"Employee #{leaveApp.EmpSysId}";

            // 6. Resolve department name (needs jobAssign result first).
            string deptName = "—";
            if (jobAssign != null && jobAssign.DeptId > 0)
            {
                var dept = await _hrmContext.OrgDepartment
                    .FirstOrDefaultAsync(d => d.DeptId == jobAssign.DeptId);
                if (dept != null) deptName = dept.NameEng;
            }

            int wrktypId = (empGen != null && (empGen.WrktypId == 2 || empGen.GrpId == 7)) ? 2 : (empGen?.WrktypId ?? 1);
            string workTypeName = wrktypId == 2 ? "Outsourced" : "In-House";

            var inputData = new Dictionary<string, string>
            {
                ["LvId"]        = leaveApp.LvId.ToString(),
                ["EmpSysId"]    = leaveApp.EmpSysId.ToString(),
                ["EmployeeName"]= empFullName,
                ["Department"]  = deptName,
                ["LeaveType"]   = lvType?.NameEng ?? "Unknown",
                ["LvTypCode"]   = lvType?.LvTypCode ?? "",
                ["LvTypId"]     = leaveApp.LvTypId.ToString(),
                ["WrktypId"]    = wrktypId.ToString(),
                ["WorkType"]    = workTypeName,
                ["StartDate"]   = leaveApp.StDate.ToString("dd MMM yyyy"),
                ["EndDate"]     = leaveApp.EndDate.ToString("dd MMM yyyy"),
                ["FromDate"]    = leaveApp.StDate.ToString("yyyy-MM-dd"),
                ["ToDate"]      = leaveApp.EndDate.ToString("yyyy-MM-dd"),
                ["Days"]        = leaveApp.Days.ToString(),
                ["Reason"]      = leaveApp.Note ?? string.Empty,
                ["LvYear"]      = leaveApp.LvYear.ToString(),
                ["AppBaseUrl"]  = _configuration["AppSettings:AppBaseUrl"] ?? _configuration["WorkflowEngine:CallbackBaseUrl"] ?? "http://localhost:55465"
            };

            // 7. Resolve the concrete Workflow Engine definition Code and call start endpoint.
            string resolvedWfCode = await ResolveWorkflowCodeAsync(wfConfig.WfCode, lvType?.LvTypCode, wrktypId);
            inputData["WorkflowCode"] = resolvedWfCode;

            var client   = GetWfClient(token);
            var startReq = new WfStartRequest
            {
                WorkflowCode = resolvedWfCode,
                InputData    = inputData,
                RequesterId  = wfUserId
            };

            var content  = new StringContent(
                JsonSerializer.Serialize(startReq),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{WfEngineBaseUrl}/api/workflowexecution/start", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(
                    $"Workflow Engine failed to start workflow '{resolvedWfCode}': {error}");
            }

            var json      = await response.Content.ReadAsStringAsync();
            var opts      = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var startResp = JsonSerializer.Deserialize<WfStartResponse>(json, opts);

            // 8. Create HrLvWfInstance record.
            var wfInstance = new HrLvWfInstance
            {
                LvId         = leaveApp.LvId,
                WfInstanceId = startResp.InstanceId,
                WfInstanceNo = startResp.InstanceNumber,
                WfStatus     = "Leave Initiated",
                CrtDateTime  = DateTime.Now
            };

            _hrmContext.HrLvWfInstance.Add(wfInstance);

            // 9. Update HrLvRegister workflow status.
            var lvRecord = await _hrmContext.HrLvRegister
                .FirstOrDefaultAsync(l => l.LvId == leaveApp.LvId);
            if (lvRecord != null)
            {
                lvRecord.WfStat = "Leave Initiated";
                _hrmContext.HrLvRegister.Update(lvRecord);
            }

            await _hrmContext.SaveChangesAsync();

            // 10. Send submission notifications (fire-and-forget — must not block submission).
            var submittedLvId = leaveApp.LvId;
            var submittedWfInstId = wfInstance.WfInstId;
            var submittedLvTypeName = lvType?.NameEng;
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_scopeFactory != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var scopedSvc = scope.ServiceProvider.GetRequiredService<LeaveWorkflowService>();
                        var scopedHrm = scope.ServiceProvider.GetRequiredService<qHRMasterContext>();
                        var scopedLv = await scopedHrm.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == submittedLvId);
                        var scopedInst = await scopedHrm.HrLvWfInstance.FirstOrDefaultAsync(i => i.WfInstId == submittedWfInstId);
                        if (scopedLv != null && scopedInst != null)
                        {
                            await scopedSvc.SendSubmissionNotificationsAsync(scopedLv, scopedInst, submittedLvTypeName);
                        }
                    }
                    else
                    {
                        await SendSubmissionNotificationsAsync(leaveApp, wfInstance, submittedLvTypeName);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] Submission notification: {ex.Message}");
                }
            });

            return wfInstance;
        }

        // =====================================================================
        // VALIDATE LEAVE POLICY
        // Validates business rules before the workflow can start.
        // =====================================================================

        public async Task ValidateLeavePolicyAsync(HrLvRegister leaveApp, HrLvWfConfig wfConfig)
        {
            var lvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == leaveApp.LvTypId);
            string code = lvType?.LvTypCode ?? "";
            string name = lvType?.NameEng ?? "";

            // Parse policy rules from config if available
            var rules = string.IsNullOrEmpty(wfConfig?.PolicyRules)
                ? new Dictionary<string, JsonElement>()
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    wfConfig.PolicyRules,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // 1. SICK LEAVE: Medical certificate attachment is mandatory
            if (code == "LV_SICK" || name.Contains("Sick", StringComparison.OrdinalIgnoreCase) ||
                (rules.TryGetValue("MedCertRequired", out var medCertEl) && medCertEl.GetBoolean()))
            {
                if (leaveApp.DocId == null || leaveApp.DocId == 0)
                {
                    throw new Exception(
                        "Medical certificate attachment is mandatory for Sick Leave. Please attach the certificate before submitting.");
                }
            }

            // 2. CASUAL LEAVE: Maximum 3 paid days per year
            if (code == "LV_CSL" || name.Contains("Casual", StringComparison.OrdinalIgnoreCase) ||
                rules.ContainsKey("AnnualEntitlement"))
            {
                int maxCasualDays = rules.TryGetValue("AnnualEntitlement", out var annualEl) ? annualEl.GetInt32() : 3;
                var usedDays = await _hrmContext.HrLvRegister
                    .Where(l => l.EmpSysId == leaveApp.EmpSysId
                             && l.LvTypId  == leaveApp.LvTypId
                             && l.LvYear   == (leaveApp.LvYear > 0 ? leaveApp.LvYear : DateTime.Now.Year)
                             && l.LvId     != leaveApp.LvId
                             && l.RecStat  != 0 && l.RecStat != 3)  // not cancelled or rejected
                    .SumAsync(l => (int?)l.Days) ?? 0;

                if (usedDays + leaveApp.Days > maxCasualDays)
                {
                    throw new Exception(
                        $"Casual Leave is restricted to a maximum of {maxCasualDays} paid days per year. " +
                        $"Already used/submitted: {usedDays} day(s). Requested: {leaveApp.Days} day(s). " +
                        $"Remaining: {Math.Max(0, maxCasualDays - usedDays)} day(s).");
                }
            }

            // 3. EMERGENCY LEAVE: Restricted to maximum 30 days while applying
            if (code == "LV_EMG" || name.Contains("Emergency", StringComparison.OrdinalIgnoreCase))
            {
                if (leaveApp.Days > 30)
                {
                    throw new Exception(
                        $"Emergency Leave is restricted to a maximum of 30 days per application. Requested: {leaveApp.Days} days.");
                }
            }

            // 4. HAJJ LEAVE: Restricted to maximum 14 days, once in lifetime
            if (code == "LV_HAJJ" || name.Contains("Hajj", StringComparison.OrdinalIgnoreCase) ||
                (rules.TryGetValue("OnceInService", out var onceEl) && onceEl.GetBoolean()))
            {
                if (leaveApp.Days > 14)
                {
                    throw new Exception(
                        $"Hajj Leave is restricted to a maximum of 14 days. Requested: {leaveApp.Days} days.");
                }

                var priorHajj = await _hrmContext.HrLvRegister
                    .FirstOrDefaultAsync(l => l.EmpSysId == leaveApp.EmpSysId
                                           && l.LvTypId  == leaveApp.LvTypId
                                           && l.LvId     != leaveApp.LvId
                                           && (l.RecStat == 2 || l.WfStat == "Approved"));
                if (priorHajj != null)
                {
                    throw new Exception(
                        "Hajj Leave can only be taken once during the employee's entire lifetime / employment period.");
                }
            }

            // 5. MATERNITY LEAVE: Restricted to female employees only
            if (code.Contains("MAT", StringComparison.OrdinalIgnoreCase) || name.Contains("Maternity", StringComparison.OrdinalIgnoreCase) ||
                rules.ContainsKey("GenderRequired"))
            {
                var emp = await _hrmContext.HrEmpGenInfo
                    .FirstOrDefaultAsync(e => e.EmpSysId == leaveApp.EmpSysId);

                if (emp == null)
                    throw new Exception("Employee record not found.");

                string gender = emp.Gender?.Trim() ?? "";
                if (!string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "Maternity Leave is restricted to female employees only.");
                }

                // Service length check if configured
                if (rules.TryGetValue("MinServiceYears", out var minSvcEl))
                {
                    int minYears = minSvcEl.GetInt32();
                    double yearsOfService = (DateTime.Today - emp.JoinedDate).TotalDays / 365.25;
                    if (yearsOfService < minYears)
                    {
                        throw new Exception(
                            $"Maternity leave requires a minimum of {minYears} year(s) of service. Current service: {yearsOfService:F1} year(s).");
                    }
                }
            }

            // 6. ANNUAL LEAVE: Restricted to apply only within available running balance
            if (code == "LV_ANL" || name.Contains("Annual", StringComparison.OrdinalIgnoreCase))
            {
                if (_balanceService != null && leaveApp.EmpSysId.HasValue && leaveApp.LvTypId.HasValue)
                {
                    // Use the leave start date (StDate) as the evaluation date so that
                    // available balance calculation matches the controller/save-time
                    // behavior which evaluates balance as of the leave start date.
                    DateTime evalAsOf = leaveApp.StDate != default ? leaveApp.StDate : DateTime.Today;
                    int yr = leaveApp.LvYear > 0 ? leaveApp.LvYear : evalAsOf.Year;
                    decimal avbl = await _balanceService.GetAvailableBalanceAsync(
                        leaveApp.EmpSysId.Value,
                        leaveApp.LvTypId.Value,
                        yr,
                        evalAsOf);

                    // If the balance service unexpectedly returns 0 (e.g. unavailable for the eval date),
                    // fall back to the cached CurrAvblDays saved on the leave record so previously saved
                    // applications can still be submitted while the root cause is investigated.
                    if (avbl == 0 && leaveApp.CurrAvblDays > 0)
                    {
                        avbl = leaveApp.CurrAvblDays;
                    }

                    if (leaveApp.Days > avbl)
                    {
                        // Include evaluation context in the exception to aid debugging
                        throw new Exception(
                            $"Requested Annual Leave ({leaveApp.Days} days) exceeds available leave balance ({avbl} days). " +
                            $"EvalAsOf={evalAsOf:yyyy-MM-dd}; EmpSysId={leaveApp.EmpSysId}; LvTypId={leaveApp.LvTypId}; LvYear={yr}; CurrAvblDays={leaveApp.CurrAvblDays}");
                    }
                }
            }

            // 7. OVERLAPPING LEAVE VALIDATION: Ensure no overlapping active leaves across any leave types
            var overlapping = await _hrmContext.HrLvRegister
                .Where(l => l.EmpSysId == leaveApp.EmpSysId
                         && l.LvId     != leaveApp.LvId
                         && l.RecStat  != 0 && l.RecStat != 3
                         && l.WfStat   != "Rejected" && l.WfStat != "Cancelled"
                         && l.StDate.Date <= leaveApp.EndDate.Date
                         && l.EndDate.Date >= leaveApp.StDate.Date)
                .FirstOrDefaultAsync();

            if (overlapping != null)
            {
                var existType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == overlapping.LvTypId);
                string existTypeName = existType?.NameEng ?? "leave";
                throw new Exception(
                    $"You already have a leave ({existTypeName}: {overlapping.StDate:dd/MM/yyyy} to {overlapping.EndDate:dd/MM/yyyy}) within the selected date range.");
            }
        }

        // =====================================================================
        // GET WORKFLOW INSTANCE STATUS
        // Fetches the current state of a workflow instance from the WF engine.
        // =====================================================================

        public async Task<WfInstanceDto> GetWorkflowInstanceAsync(Guid wfInstanceId)
        {
            try
            {
                var token  = await GetWfTokenAsync();
                var client = GetWfClient(token);

                var response = await client.GetAsync(
                    $"{WfEngineBaseUrl}/api/workflowexecution/instance/{wfInstanceId}");

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WfInstanceDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        public async Task<HrLvWfInstance> GetWorkflowInstanceByLvIdAsync(int lvId)
        {
            return await _hrmContext.HrLvWfInstance
                .OrderByDescending(x => x.WfInstId)
                .FirstOrDefaultAsync(x => x.LvId == lvId);
        }

        // =====================================================================
        // CALLBACK: UPDATE LEAVE STATUS FROM WORKFLOW ENGINE
        // Called by the workflow engine webhook or polled on page load.
        // Updates HrLvRegister.WfStat, HrLvWfInstance.WfStatus, and running balance.
        // =====================================================================

        public async Task ProcessWorkflowCallbackAsync(Guid wfInstanceId, string newStatus)
        {
            var wfInst = await _hrmContext.HrLvWfInstance
                .FirstOrDefaultAsync(i => i.WfInstanceId == wfInstanceId);

            if (wfInst == null) return;

            wfInst.WfStatus    = newStatus;
            wfInst.MdfDateTime = DateTime.Now;

            // Map WF Engine status to HR leave status
            string hrWfStat = newStatus switch
            {
                "Approved"  => "Approved",
                "Rejected"  => "Rejected",
                "Cancelled" => "Cancelled",
                "Returned"  => "PendingResubmission",
                _           => "InProgress"
            };

            var lvRecord = await _hrmContext.HrLvRegister
                .FirstOrDefaultAsync(l => l.LvId == wfInst.LvId);

            if (lvRecord != null)
            {
                lvRecord.WfStat = hrWfStat;

                // Final approval: update RecStat to approved (2) and deduct running leave balance
                if (newStatus == "Approved")
                {
                    lvRecord.RecStat = 2;
                    if (_balanceService != null && wfInst.LvId > 0)
                    {
                        await _balanceService.DeductLeaveBalanceAsync(wfInst.LvId);
                    }
                }
                // Rejection: update RecStat to rejected (3) and restore balance if it was previously deducted
                else if (newStatus == "Rejected")
                {
                    lvRecord.RecStat = 3;
                    if (_balanceService != null && wfInst.LvId > 0)
                    {
                        await _balanceService.RestoreLeaveBalanceAsync(wfInst.LvId, 0, "Leave Rejected");
                    }
                }
                // Cancellation: restore balance if it was previously deducted
                else if (newStatus == "Cancelled")
                {
                    lvRecord.RecStat = 0;
                    if (_balanceService != null && wfInst.LvId > 0)
                    {
                        await _balanceService.RestoreLeaveBalanceAsync(wfInst.LvId, 0, "Leave Cancelled");
                    }
                }
            }

            await _hrmContext.SaveChangesAsync();
        }

        // =====================================================================
        // GET LEAVE APPROVAL TIMELINE
        // Returns detailed timeline structure formatted for the Approval Timeline modal.
        // =====================================================================
        public async Task<LeaveApprovalTimelineViewModel> GetLeaveApprovalTimelineAsync(int lvId)
        {
            try
            {
                var lvRecord = await _hrmContext.HrLvRegister
                    .FirstOrDefaultAsync(l => l.LvId == lvId);
                if (lvRecord == null)
                    return new LeaveApprovalTimelineViewModel { Success = false, Message = "Leave application record not found." };

                var requester = await _hrmContext.HrEmpGenInfo
                    .FirstOrDefaultAsync(e => e.EmpSysId == lvRecord.EmpSysId);
                var requesterJob = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == lvRecord.EmpSysId && j.WkStat == 1)
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();

                var lvType = await _hrmContext.HrLvType
                    .FirstOrDefaultAsync(t => t.LvTypId == lvRecord.LvTypId);

                string requesterName = requester?.FullName ?? $"Employee #{lvRecord.EmpSysId}";
                string requesterEmpId = requester?.EmpId ?? lvRecord.EmpSysId.ToString();
                // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table
                string requesterEmail = requesterJob?.OfcEmail ?? "";

                var wfInst = await _hrmContext.HrLvWfInstance
                    .OrderByDescending(x => x.WfInstId)
                    .FirstOrDefaultAsync(x => x.LvId == lvId);

                // 1. Resolve attachment metadata from Note, HrLvRegDocs DB table, or wwwroot
                var parsedNote = ParseNoteAndAttachment(lvRecord.Note, "Leave request initiated");
                string submissionNote = parsedNote.cleanReason;
                bool hasAttachment = parsedNote.hasAttachment;
                string attachmentUrl = parsedNote.attachmentUrl;
                string attachmentFileName = parsedNote.attachmentFileName;
                string attachmentType = parsedNote.attachmentType;

                // Check HrLvRegDocs table if note didn't have explicit [MedCert:] tag
                if (!hasAttachment)
                {
                    try
                    {
                        var regDoc = await _hrmContext.HrLvRegDocs
                            .OrderByDescending(d => d.DocId)
                            .FirstOrDefaultAsync(d => d.LvId == lvId);
                        if (regDoc != null)
                        {
                            hasAttachment = true;
                            attachmentFileName = regDoc.DocName ?? $"medical_cert_{lvId}.pdf";
                            attachmentUrl = $"/Leave/HrLvRegister/GetDocument?id={regDoc.DocId}";
                            var ext = System.IO.Path.GetExtension(attachmentFileName).ToLowerInvariant();
                            attachmentType = (ext == ".pdf" || regDoc.DocMime?.Contains("pdf") == true) ? "pdf" :
                                             (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(ext) || regDoc.DocMime?.StartsWith("image") == true ? "image" : "document");
                        }
                    }
                    catch { }
                }

                // Check if DocId is marked or if Sick Leave has attachment on disk
                if (!hasAttachment && ((lvRecord.DocId != null && lvRecord.DocId > 0) || lvType?.LvTypCode == "LV_SICK" || lvType?.NameEng?.Contains("Sick", StringComparison.OrdinalIgnoreCase) == true))
                {
                    try
                    {
                        var medCertDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "attach", "medCert");
                        if (System.IO.Directory.Exists(medCertDir))
                        {
                            var files = System.IO.Directory.GetFiles(medCertDir);
                            if (files.Length > 0)
                            {
                                var matchedFile = files.OrderByDescending(System.IO.File.GetCreationTime).FirstOrDefault();
                                if (matchedFile != null)
                                {
                                    var fName = System.IO.Path.GetFileName(matchedFile);
                                    hasAttachment = true;
                                    attachmentFileName = fName;
                                    attachmentUrl = $"/attach/medCert/{fName}";
                                    var ext = System.IO.Path.GetExtension(fName).ToLowerInvariant();
                                    attachmentType = (ext == ".pdf") ? "pdf" : (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(ext) ? "image" : "document");
                                }
                            }
                        }
                    }
                    catch { }
                }

                // If not yet submitted to workflow engine, return Draft timeline
                if (wfInst == null)
                {
                    return new LeaveApprovalTimelineViewModel
                    {
                        Success = true,
                        InstanceNo = "Draft / Unsubmitted",
                        LeaveType = lvType?.NameEng ?? "Leave",
                        Period = $"{lvRecord.StDate:yyyy-MM-dd} to {lvRecord.EndDate:yyyy-MM-dd} ({lvRecord.Days} Days)",
                        TotalDuration = "—",
                        OverallStatus = "Draft",
                        StatusBadgeText = "Not Submitted to Workflow",
                        LastUpdatedDate = lvRecord.StDate.ToString("dd-MMM-yyyy"),
                        Reason = submissionNote,
                        SeatingLocation = string.IsNullOrWhiteSpace(requester?.SeatingLocation) ? "—" : requester.SeatingLocation.Trim(),
                        HasAttachment = hasAttachment,
                        AttachmentUrl = attachmentUrl,
                        AttachmentFileName = attachmentFileName,
                        AttachmentType = attachmentType,
                        Events = new List<LeaveTimelineEventViewModel>
                        {
                            new LeaveTimelineEventViewModel
                            {
                                Type = "submission",
                                Status = "Draft",
                                StatusText = "Leave application created. Ready to submit to approval workflow.",
                                ActionDate = lvRecord.StDate.ToString("dd-MMM-yyyy"),
                                EmployeeCode = requesterEmpId,
                                EmployeeName = requesterName,
                                Email = requesterEmail,
                                Comment = submissionNote,
                                ActedIn = null,
                                IsCompleted = false,
                                IsCurrent = true,
                                IsRejected = false
                            }
                        }
                    };
                }

                string repManagerName = null;
                var managerHierarchy = new List<string>();
                try
                {
                    int? currEmpSysId = lvRecord.EmpSysId;
                    var visitedEmps = new HashSet<int>();
                    while (currEmpSysId.HasValue && visitedEmps.Add(currEmpSysId.Value))
                    {
                        var job = await _hrmContext.HrEmpJobAssign
                            .Where(j => j.EmpSysId == currEmpSysId.Value && j.WkStat == 1)
                            .OrderByDescending(j => j.StDate)
                            .FirstOrDefaultAsync();

                        if (job == null || job.RptMngId == null || job.RptMngId <= 0)
                            break;

                        int mgrId = job.RptMngId.Value;
                        var mgr = await _hrmContext.HrEmpGenInfo
                            .FirstOrDefaultAsync(e => e.EmpSysId == mgrId);

                        if (mgr != null && !string.IsNullOrWhiteSpace(mgr.FullName))
                        {
                            managerHierarchy.Add(mgr.FullName.Trim());
                        }

                        currEmpSysId = mgrId;
                    }
                }
                catch { }

                if (managerHierarchy.Count > 0)
                {
                    repManagerName = managerHierarchy[0];
                }

                string submissionDateStr = wfInst.CrtDateTime.ToString("dd-MMM-yyyy hh:mm tt");

                List<LeaveTimelineEventViewModel> timelineEvents = null;

                // Fetch directly from WorkflowEngineDb for 100% precision
                try
                {
                    timelineEvents = await GetTimelineEventsFromDbAsync(wfInst.WfInstanceId, requesterName, repManagerName, managerHierarchy, submissionDateStr, submissionNote);
                }
                catch { }

                // Fallback to API if DB direct query was not available
                if (timelineEvents == null || timelineEvents.Count == 0)
                {
                    WfInstanceDto instanceDetails = null;
                    List<WfHistoryDto> history = null;
                    try
                    {
                        instanceDetails = await GetWorkflowInstanceAsync(wfInst.WfInstanceId);
                        history = await GetWorkflowHistoryAsync(wfInst.WfInstanceId);
                    }
                    catch { }

                    timelineEvents = new List<LeaveTimelineEventViewModel>();
                    int fallbackHierarchyIndex = 0;

                    var steps = instanceDetails?.Steps ?? new List<WfInstanceStepDto>();
                    if (steps.Count > 0)
                    {
                        for (int i = 0; i < steps.Count; i++)
                        {
                            var step = steps[i];
                            var stepApprovals = step.Approvals ?? new List<WfApprovalDto>();
                            var approval = stepApprovals.FirstOrDefault();
                            var histMatch = history?.FirstOrDefault(h => h.StepName == step.Name);

                            DateTime? stepActionDate = approval?.ActionDate ?? histMatch?.ActionDate ?? step.EndDate;
                            bool isStepApproved = step.Status == 2 || (approval != null && approval.Status == 2) ||
                                                  string.Equals(step.StatusText, "Approved", StringComparison.OrdinalIgnoreCase) ||
                                                  (approval != null && string.Equals(approval.StatusText, "Approved", StringComparison.OrdinalIgnoreCase));
                            bool isStepRejected = step.Status == 3 || (approval != null && approval.Status == 3) ||
                                                  string.Equals(step.StatusText, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                                                  (approval != null && string.Equals(approval.StatusText, "Rejected", StringComparison.OrdinalIgnoreCase));
                            bool isStepPending = !isStepApproved && !isStepRejected &&
                                                 (step.Status == 1 || (approval != null && approval.Status == 1) ||
                                                  string.Equals(step.StatusText, "Pending", StringComparison.OrdinalIgnoreCase) ||
                                                  string.Equals(step.StatusText, "InProgress", StringComparison.OrdinalIgnoreCase));
                            bool isStepWaiting = !isStepApproved && !isStepRejected && !isStepPending;

                            string appName = approval?.ApproverName ?? histMatch?.PerformedByName ?? "";
                            if (step.Name.ToLower().Contains("initiate"))
                            {
                                appName = requesterName;
                                isStepApproved = true;
                                isStepPending = false;
                                isStepWaiting = false;
                            }
                            else if (string.IsNullOrWhiteSpace(appName) || appName == "System")
                            {
                                if (step.Name.ToLower().Contains("manager") || step.Name.ToLower().Contains("head"))
                                {
                                    if (managerHierarchy != null && fallbackHierarchyIndex < managerHierarchy.Count)
                                    {
                                        appName = managerHierarchy[fallbackHierarchyIndex];
                                        fallbackHierarchyIndex++;
                                    }
                                    else if (managerHierarchy != null && managerHierarchy.Count > 0)
                                    {
                                        appName = managerHierarchy.Last();
                                    }
                                    else
                                    {
                                        appName = !string.IsNullOrEmpty(repManagerName) ? repManagerName : "Approver";
                                    }
                                }
                                else
                                {
                                    appName = "Approver";
                                }
                            }

                            if (!string.IsNullOrEmpty(appName) && managerHierarchy != null)
                            {
                                int matchIdx = managerHierarchy.FindIndex(m => string.Equals(m, appName, StringComparison.OrdinalIgnoreCase));
                                if (matchIdx >= 0)
                                {
                                    fallbackHierarchyIndex = Math.Max(fallbackHierarchyIndex, matchIdx + 1);
                                }
                            }

                            string roleSubtitle = step.Name;
                            string statusLabel = step.Name.ToLower().Contains("initiate") ? "Leave Initiated" : (isStepApproved ? "Approved" : (isStepRejected ? "Rejected" : (isStepPending ? "Pending" : "Waiting")));

                            timelineEvents.Add(new LeaveTimelineEventViewModel
                            {
                                Type = step.Name.ToLower().Contains("initiate") ? "submission" : "approval",
                                StepNumber = step.StepNumber,
                                StepName = step.Name,
                                RoleName = roleSubtitle,
                                Status = statusLabel,
                                StatusText = statusLabel,
                                ActionDate = stepActionDate.HasValue ? stepActionDate.Value.ToString("dd-MMM-yyyy hh:mm tt") : (isStepPending ? "Pending Approval" : "—"),
                                EmployeeName = appName,
                                Comment = approval?.Comment ?? histMatch?.Comment ?? "",
                                IsCompleted = isStepApproved,
                                IsCurrent = isStepPending,
                                IsRejected = isStepRejected,
                                IsWaiting = isStepWaiting
                            });
                        }

                        // Ensure Initiator node is present
                        if (!timelineEvents.Any(e => e.StepName.ToLower().Contains("initiate") || (e.RoleName != null && e.RoleName.ToLower().Contains("initiate"))))
                        {
                            timelineEvents.Insert(0, new LeaveTimelineEventViewModel
                            {
                                Type = "submission",
                                StepNumber = 0,
                                StepName = "Initiator",
                                RoleName = "Initiator",
                                Status = "Leave Initiated",
                                StatusText = "Leave Initiated",
                                ActionDate = submissionDateStr,
                                EmployeeName = !string.IsNullOrEmpty(requesterName) ? requesterName : "Leave Applicant",
                                Comment = !string.IsNullOrEmpty(submissionNote) ? submissionNote : "Leave request initiated",
                                IsCompleted = true,
                                IsCurrent = false,
                                IsRejected = false,
                                IsWaiting = false
                            });
                        }
                    }
                }

                // Compute summary overall status
                string overallStatus = "Leave Initiated";
                if (string.Equals(wfInst.WfStatus, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    (timelineEvents.Count > 0 && timelineEvents.All(e => e.IsCompleted)))
                {
                    overallStatus = "Approved";
                }
                else if (string.Equals(wfInst.WfStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                         timelineEvents.Any(e => e.IsRejected))
                {
                    overallStatus = "Rejected";
                }

                int totalLevels = timelineEvents.Count;
                int completedLevels = timelineEvents.Count(e => e.IsCompleted);
                int currentLevel = Math.Min(totalLevels, completedLevels + 1);

                string statusBadgeHtml = overallStatus switch
                {
                    "Approved" => $"Approved {completedLevels}/{totalLevels} Levels",
                    "Rejected" => "Rejected",
                    _ => "Leave Initiated"
                };

                return new LeaveApprovalTimelineViewModel
                {
                    Success = true,
                    ApplicantName = requesterName,
                    ApplicantCode = requesterEmpId,
                    ReferenceNo = $"Ref # {lvRecord.LvId}",
                    InstanceNo = wfInst.WfInstanceNo,
                    LeaveType = lvType?.NameEng ?? "Leave",
                    Period = $"{lvRecord.StDate:yyyy-MM-dd} to {lvRecord.EndDate:yyyy-MM-dd} ({lvRecord.Days} Days)",
                    TotalDuration = "—",
                    OverallStatus = overallStatus,
                    StatusBadgeText = statusBadgeHtml,
                    LastUpdatedDate = wfInst.MdfDateTime?.ToString("dd-MMM-yyyy hh:mm tt") ?? wfInst.CrtDateTime.ToString("dd-MMM-yyyy hh:mm tt"),
                    Reason = submissionNote,
                    SeatingLocation = string.IsNullOrWhiteSpace(requester?.SeatingLocation) ? "—" : requester.SeatingLocation.Trim(),
                    HasAttachment = hasAttachment,
                    AttachmentUrl = attachmentUrl,
                    AttachmentFileName = attachmentFileName,
                    AttachmentType = attachmentType,
                    Events = timelineEvents
                };
            }
            catch (Exception ex)
            {
                return new LeaveApprovalTimelineViewModel { Success = false, Message = "Error loading timeline: " + ex.Message };
            }
        }

        private string GetWorkflowEngineDbConnectionString()
        {
            var configured = _configuration.GetConnectionString("workflowconstring")
                          ?? _configuration.GetConnectionString("WorkflowEngineConnection")
                          ?? _configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            var constr = _configuration.GetConnectionString("qhrconstring");
            if (string.IsNullOrWhiteSpace(constr))
                return string.Empty;

            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(constr);
                builder.InitialCatalog = "WorkflowEngineDb";
                return builder.ConnectionString;
            }
            catch
            {
                return System.Text.RegularExpressions.Regex.Replace(
                    constr,
                    @"(database|initial catalog)\s*=\s*[^;]+",
                    "$1=WorkflowEngineDb",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        private async Task<List<LeaveTimelineEventViewModel>> GetTimelineEventsFromDbAsync(
            Guid wfInstanceId,
            string requesterName = null,
            string repManagerName = null,
            List<string> managerHierarchy = null,
            string submissionDateStr = null,
            string submissionNote = null)
        {
            var list = new List<LeaveTimelineEventViewModel>();

            string wfConStr = GetWorkflowEngineDbConnectionString();
            if (string.IsNullOrEmpty(wfConStr)) return list;

            using var conn = new Microsoft.Data.SqlClient.SqlConnection(wfConStr);
            await conn.OpenAsync();

            // Pre-load user hierarchy from WorkflowEngineDb for accurate ManagerHierarchy resolution
            var usersById = new Dictionary<Guid, (string FullName, Guid? ManagerId)>();
            var usersByName = new Dictionary<string, (Guid Id, Guid? ManagerId)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var uCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT Id, FullName, ManagerId FROM Users WHERE IsDeleted = 0", conn);
                using var uRdr = await uCmd.ExecuteReaderAsync();
                while (await uRdr.ReadAsync())
                {
                    var uid = uRdr.GetGuid(0);
                    var ufn = uRdr.IsDBNull(1) ? string.Empty : uRdr.GetString(1).Trim();
                    Guid? mId = uRdr.IsDBNull(2) ? null : (Guid?)uRdr.GetGuid(2);
                    usersById[uid] = (ufn, mId);
                    if (!string.IsNullOrEmpty(ufn) && !usersByName.ContainsKey(ufn))
                    {
                        usersByName[ufn] = (uid, mId);
                    }
                }
            }
            catch { }

            Guid? lastTargetUserId = null;
            string lastApproverName = requesterName;
            int hierarchyLevelIndex = 0;

            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                SELECT 
                    ws.Id AS WorkflowStepId,
                    ws.StepNumber,
                    ws.Sequence,
                    ws.Name AS ConfiguredStepName,
                    ws.ApprovalType,
                    ws.TargetUserId,
                    ws.TargetRoleId,
                    uTarget.FullName AS TargetUserName,
                    r.Name AS TargetRoleName,
                    s.Id AS InstanceStepId,
                    s.Status AS StepStatus,
                    s.Name AS InstanceStepName,
                    s.StartDate AS StepStartDate,
                    s.EndDate AS StepEndDate,
                    a.Status AS ApprovalStatus,
                    a.ApproverId,
                    uApprover.FullName AS ApproverName,
                    a.ActionDate,
                    a.Comment
                FROM WorkflowSteps ws
                INNER JOIN WorkflowInstances wi ON wi.Id = @InstId AND ws.WorkflowVersionId = wi.WorkflowVersionId AND ws.IsDeleted = 0
                LEFT JOIN WorkflowInstanceSteps s ON s.WorkflowInstanceId = wi.Id AND s.WorkflowStepId = ws.Id AND s.IsDeleted = 0
                LEFT JOIN WorkflowApprovals a ON a.WorkflowInstanceStepId = s.Id AND a.IsDeleted = 0
                LEFT JOIN Users uApprover ON a.ApproverId = uApprover.Id
                LEFT JOIN Users uTarget ON (TRY_CAST(ws.TargetUserId AS uniqueidentifier) = uTarget.Id)
                LEFT JOIN Roles r ON (TRY_CAST(ws.TargetRoleId AS uniqueidentifier) = r.Id OR ws.TargetRoleId = r.Code OR ws.TargetRoleId = r.Name)
                ORDER BY ws.Sequence, ws.StepNumber", conn);
            cmd.Parameters.AddWithValue("@InstId", wfInstanceId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int stepNum = reader["StepNumber"] != DBNull.Value ? Convert.ToInt32(reader["StepNumber"]) : 1;
                int? stepStat = reader["StepStatus"] != DBNull.Value ? (int?)Convert.ToInt32(reader["StepStatus"]) : null;
                int? appStat = reader["ApprovalStatus"] != DBNull.Value ? (int?)Convert.ToInt32(reader["ApprovalStatus"]) : null;
                string approver = reader["ApproverName"] != DBNull.Value ? reader["ApproverName"].ToString() : "";
                string approverIdStr = reader["ApproverId"] != DBNull.Value ? reader["ApproverId"].ToString() : "";
                string targetUserName = reader["TargetUserName"] != DBNull.Value ? reader["TargetUserName"].ToString() : "";
                string targetUserIdStr = reader["TargetUserId"] != DBNull.Value ? reader["TargetUserId"].ToString() : "";
                string stepName = reader["ConfiguredStepName"] != DBNull.Value ? reader["ConfiguredStepName"].ToString() : 
                                 (reader["InstanceStepName"] != DBNull.Value ? reader["InstanceStepName"].ToString() : $"Step {stepNum}");
                DateTime? actDate = reader["ActionDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ActionDate"]) : null;
                string comment = reader["Comment"] != DBNull.Value ? reader["Comment"].ToString() : "";
                int? appType = reader["ApprovalType"] != DBNull.Value ? (int?)Convert.ToInt32(reader["ApprovalType"]) : null;
                string targetRole = reader["TargetRoleName"] != DBNull.Value ? reader["TargetRoleName"].ToString() : "";

                // Resolve approver / employee name for this step
                string empName = approver;
                Guid? thisStepUserId = null;

                if (!string.IsNullOrWhiteSpace(empName))
                {
                    if (Guid.TryParse(approverIdStr, out var aId))
                    {
                        thisStepUserId = aId;
                    }
                    else if (usersByName.TryGetValue(empName, out var uMatch))
                    {
                        thisStepUserId = uMatch.Id;
                    }

                    if (appType == 8 || stepName.ToLower().Contains("reporting manager") || stepName.ToLower().Contains("bu head") || stepName.ToLower().Contains("manager hierarchy"))
                    {
                        hierarchyLevelIndex++;
                    }
                }
                else if (!string.IsNullOrEmpty(targetUserName))
                {
                    empName = targetUserName;
                    if (Guid.TryParse(targetUserIdStr, out var tId))
                    {
                        thisStepUserId = tId;
                    }
                    else if (usersByName.TryGetValue(targetUserName, out var uMatch))
                    {
                        thisStepUserId = uMatch.Id;
                    }

                    if (appType == 8 || stepName.ToLower().Contains("reporting manager") || stepName.ToLower().Contains("bu head") || stepName.ToLower().Contains("manager hierarchy"))
                    {
                        hierarchyLevelIndex++;
                    }
                }
                else if (appType == 8 || stepName.ToLower().Contains("reporting manager") || stepName.ToLower().Contains("bu head") || stepName.ToLower().Contains("business unit head") || stepName.ToLower().Contains("manager hierarchy") || stepName.ToLower().Contains("manager"))
                {
                    // Sequential management hierarchy resolved strictly from HR_EmpJobAssign:
                    // Level 0: Direct Reporting Manager of applicant (e.g. Salman Khan)
                    // Level 1: Reporting Manager of Level 0 (e.g. Hasan Sayed - BU Head)
                    // Level 2+: Higher levels in reporting hierarchy
                    if (managerHierarchy != null && hierarchyLevelIndex < managerHierarchy.Count)
                    {
                        empName = managerHierarchy[hierarchyLevelIndex];
                    }
                    else if (lastTargetUserId.HasValue && usersById.TryGetValue(lastTargetUserId.Value, out var prevUser) && prevUser.ManagerId.HasValue && usersById.TryGetValue(prevUser.ManagerId.Value, out var mgrUser) && !string.IsNullOrEmpty(mgrUser.FullName))
                    {
                        empName = mgrUser.FullName;
                    }
                    else if (!string.IsNullOrEmpty(lastApproverName) && usersByName.TryGetValue(lastApproverName, out var prevByName) && prevByName.ManagerId.HasValue && usersById.TryGetValue(prevByName.ManagerId.Value, out var mgrUser2) && !string.IsNullOrEmpty(mgrUser2.FullName))
                    {
                        empName = mgrUser2.FullName;
                    }
                    else if (managerHierarchy != null && managerHierarchy.Count > 0)
                    {
                        empName = managerHierarchy.Last();
                    }
                    else if (!string.IsNullOrEmpty(repManagerName))
                    {
                        empName = repManagerName;
                    }
                    else
                    {
                        empName = "Reporting Manager";
                    }

                    hierarchyLevelIndex++;

                    if (usersByName.TryGetValue(empName, out var hierMatch))
                    {
                        thisStepUserId = hierMatch.Id;
                    }
                }
                else if (appType == 9 || stepName.ToLower().Contains("department head"))
                {
                    empName = !string.IsNullOrEmpty(repManagerName) ? repManagerName : "Department Head";
                }
                else if (!string.IsNullOrEmpty(targetRole))
                {
                    empName = targetRole;
                }
                else
                {
                    empName = "Approver";
                }

                // If resolved approver matches someone in the manager hierarchy, advance pointer past that level
                if (!string.IsNullOrEmpty(empName) && managerHierarchy != null)
                {
                    int matchIdx = managerHierarchy.FindIndex(m => string.Equals(m, empName, StringComparison.OrdinalIgnoreCase));
                    if (matchIdx >= 0)
                    {
                        hierarchyLevelIndex = Math.Max(hierarchyLevelIndex, matchIdx + 1);
                    }
                }

                // Update tracking for sequential / chained hierarchy steps
                if (thisStepUserId.HasValue)
                {
                    lastTargetUserId = thisStepUserId;
                }
                if (!string.IsNullOrWhiteSpace(empName) && empName != "Approver")
                {
                    lastApproverName = empName;
                }

                // Determine Step Status
                bool isApproved = (stepStat == 2 || appStat == 2);
                bool isRejected = (stepStat == 3 || appStat == 3);
                bool isPending = !isApproved && !isRejected && (stepStat == 1 || appStat == 1);
                bool isWaiting = !isApproved && !isRejected && !isPending && (stepStat == null || stepStat == 0);

                string statusLabel = isApproved ? "Approved" : (isRejected ? "Rejected" : (isPending ? "Pending" : "Waiting"));

                // Role is retrieved directly from the configured Step Name in Workflow Engine
                string roleName = stepName;

                var existingStep = list.FirstOrDefault(e => e.StepNumber == stepNum);
                if (existingStep != null)
                {
                    if (isApproved || (!string.IsNullOrEmpty(comment) && !existingStep.IsCompleted))
                    {
                        existingStep.EmployeeName = empName;
                        existingStep.RoleName = roleName;
                        existingStep.Status = statusLabel;
                        existingStep.StatusText = statusLabel;
                        existingStep.ActionDate = actDate.HasValue ? actDate.Value.ToString("dd-MMM-yyyy hh:mm tt") : existingStep.ActionDate;
                        existingStep.Comment = comment;
                        existingStep.IsCompleted = isApproved;
                        existingStep.IsCurrent = isPending;
                        existingStep.IsRejected = isRejected;
                        existingStep.IsWaiting = isWaiting;
                    }
                }
                else
                {
                    list.Add(new LeaveTimelineEventViewModel
                    {
                        Type = "approval",
                        StepNumber = stepNum,
                        StepName = stepName,
                        RoleName = roleName,
                        Status = statusLabel,
                        StatusText = statusLabel,
                        ActionDate = actDate.HasValue ? actDate.Value.ToString("dd-MMM-yyyy hh:mm tt") : (isPending ? "Pending Approval" : "—"),
                        EmployeeName = empName,
                        Comment = comment,
                        IsCompleted = isApproved,
                        IsCurrent = isPending,
                        IsRejected = isRejected,
                        IsWaiting = isWaiting
                    });
                }
            }

            // Fallback query if no steps found in definition (e.g. legacy workflow instances)
            if (list.Count == 0)
            {
                using var cmdFallback = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT s.StepNumber, s.Name as StepName, s.Status as StepStatus,
                           a.Status as ApprovalStatus, u.FullName as ApproverName, a.ActionDate, a.Comment,
                           ws.ApprovalType, r.Name as TargetRoleName
                    FROM WorkflowInstanceSteps s
                    LEFT JOIN WorkflowApprovals a ON a.WorkflowInstanceStepId = s.Id
                    LEFT JOIN Users u ON a.ApproverId = u.Id
                    LEFT JOIN WorkflowSteps ws ON s.WorkflowStepId = ws.Id
                    LEFT JOIN Roles r ON ws.TargetRoleId = r.Id
                    WHERE s.WorkflowInstanceId = @InstId
                    ORDER BY s.StepNumber", conn);
                cmdFallback.Parameters.AddWithValue("@InstId", wfInstanceId);

                using var readerFallback = await cmdFallback.ExecuteReaderAsync();
                while (await readerFallback.ReadAsync())
                {
                    int stepNum = Convert.ToInt32(readerFallback["StepNumber"]);
                    int stepStat = readerFallback["StepStatus"] != DBNull.Value ? Convert.ToInt32(readerFallback["StepStatus"]) : 1;
                    int appStat = readerFallback["ApprovalStatus"] != DBNull.Value ? Convert.ToInt32(readerFallback["ApprovalStatus"]) : 1;
                    string approver = readerFallback["ApproverName"] != DBNull.Value ? readerFallback["ApproverName"].ToString() : "";
                    string stepName = readerFallback["StepName"] != DBNull.Value ? readerFallback["StepName"].ToString() : "";
                    DateTime? actDate = readerFallback["ActionDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(readerFallback["ActionDate"]) : null;
                    string comment = readerFallback["Comment"] != DBNull.Value ? readerFallback["Comment"].ToString() : "";

                    string empName = approver;
                    if (string.IsNullOrWhiteSpace(empName))
                    {
                        empName = !string.IsNullOrEmpty(repManagerName) ? repManagerName : "Reporting Manager";
                    }

                    bool isApproved = (stepStat == 2 || appStat == 2);
                    bool isRejected = (stepStat == 3 || appStat == 3);
                    bool isPending = !isApproved && !isRejected && (stepStat == 1 || appStat == 1);
                    bool isWaiting = !isApproved && !isRejected && !isPending;

                    string statusLabel = isApproved ? "Approved" : (isRejected ? "Rejected" : (isPending ? "Pending" : "Waiting"));

                    list.Add(new LeaveTimelineEventViewModel
                    {
                        Type = "approval",
                        StepNumber = stepNum,
                        StepName = stepName,
                        RoleName = stepName,
                        Status = statusLabel,
                        StatusText = statusLabel,
                        ActionDate = actDate.HasValue ? actDate.Value.ToString("dd-MMM-yyyy hh:mm tt") : (isPending ? "Pending Approval" : "—"),
                        EmployeeName = empName,
                        Comment = comment,
                        IsCompleted = isApproved,
                        IsCurrent = isPending,
                        IsRejected = isRejected,
                        IsWaiting = isWaiting
                    });
                }
            }

            // Ensure the Initiator step is always at Step 0
            bool hasInitiatorStep = list.Any(e => e.StepName.ToLower().Contains("initiate") || (e.RoleName != null && e.RoleName.ToLower().Contains("initiate")));
            if (hasInitiatorStep)
            {
                var initStep = list.First(e => e.StepName.ToLower().Contains("initiate") || (e.RoleName != null && e.RoleName.ToLower().Contains("initiate")));
                initStep.EmployeeName = !string.IsNullOrEmpty(requesterName) ? requesterName : "Leave Applicant";
                initStep.RoleName = "Initiator";
                initStep.StepName = "Initiator";
                initStep.Status = "Leave Initiated";
                initStep.StatusText = "Leave Initiated";
                initStep.IsCompleted = true;
                initStep.IsCurrent = false;
                initStep.IsWaiting = false;
                initStep.IsRejected = false;
                if (string.IsNullOrEmpty(initStep.ActionDate) || initStep.ActionDate == "—")
                {
                    initStep.ActionDate = submissionDateStr ?? DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
                }
            }
            else
            {
                var initiatorNode = new LeaveTimelineEventViewModel
                {
                    Type = "submission",
                    StepNumber = 0,
                    StepName = "Initiator",
                    RoleName = "Initiator",
                    Status = "Leave Initiated",
                    StatusText = "Leave Initiated",
                    ActionDate = submissionDateStr ?? DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"),
                    EmployeeName = !string.IsNullOrEmpty(requesterName) ? requesterName : "Leave Applicant",
                    Comment = !string.IsNullOrEmpty(submissionNote) ? submissionNote : "Leave request initiated",
                    IsCompleted = true,
                    IsCurrent = false,
                    IsRejected = false,
                    IsWaiting = false
                };
                list.Insert(0, initiatorNode);
            }

            return list;
        }

        private static string FormatDuration(TimeSpan span)
        {
            if (span.TotalDays >= 1)
            {
                int days = (int)span.TotalDays;
                int hours = span.Hours;
                return hours > 0 ? $"{days} Day{(days > 1 ? "s" : "")} {hours} Hr{(hours > 1 ? "s" : "")}" : $"{days} Day{(days > 1 ? "s" : "")}";
            }
            if (span.TotalHours >= 1)
            {
                int hours = (int)span.TotalHours;
                int mins = span.Minutes;
                return mins > 0 ? $"{hours} Hr{(hours > 1 ? "s" : "")} {mins} Min{(mins > 1 ? "s" : "")}" : $"{hours} Hr{(hours > 1 ? "s" : "")}";
            }
            int minutes = Math.Max(1, (int)span.TotalMinutes);
            return $"{minutes} Min{(minutes > 1 ? "s" : "")}";
        }

        // =====================================================================
        // RESOLVE APPROVER
        // Resolves dynamic approvers (Reporting Manager, BU Head) from HR data.
        // Called by the Workflow Engine via ApiLookup when those step types are used.
        // =====================================================================

        public async Task<LvApproverResolveResult> ResolveReportingManagerAsync(int empSysId)
        {
            var jobAssign = await _hrmContext.HrEmpJobAssign
                .Where(j => j.EmpSysId == empSysId)
                .OrderByDescending(j => j.StDate)
                .FirstOrDefaultAsync();

            if (jobAssign?.RptMngId == null)
                return new LvApproverResolveResult { Found = false };

            var manager = await _hrmContext.HrEmpGenInfo
                .FirstOrDefaultAsync(e => e.EmpSysId == jobAssign.RptMngId);

            if (manager == null)
                return new LvApproverResolveResult { Found = false };

            var mgJob = await _hrmContext.HrEmpJobAssign
                .Where(j => j.EmpSysId == manager.EmpSysId && j.WkStat == 1)
                .OrderByDescending(j => j.StDate)
                .FirstOrDefaultAsync();
            if (mgJob == null)
            {
                mgJob = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == manager.EmpSysId)
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();
            }

            // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table
            var mgEmail = mgJob?.OfcEmail ?? "";

            return new LvApproverResolveResult
            {
                Found     = true,
                EmpSysId  = manager.EmpSysId,
                FullName  = manager.FullName,
                Email     = mgEmail
            };
        }

        // =====================================================================
        // GET ACTIVE DELEGATION
        // Checks if an approver has an active delegation for today's date.
        // Returns the delegate employee if found.
        // =====================================================================

        public async Task<HrLvWfDelegation> GetActiveDelegationAsync(int approverEmpSysId, int? lvTypId = null)
        {
            var today = DateTime.Today;
            var query = _hrmContext.HrLvWfDelegation
                .Where(d => d.DlgtrEmpId == approverEmpSysId
                         && d.RecStat    == 1
                         && d.FromDate   <= today
                         && d.ToDate     >= today);

            if (lvTypId.HasValue)
                query = query.Where(d => d.LvTypId == null || d.LvTypId == lvTypId);

            return await query.FirstOrDefaultAsync();
        }

        // =====================================================================
        // NOTIFICATIONS
        // Dispatches email notifications (using templates & SMTP) and in-app notifications
        // =====================================================================

        public async Task NotifyActiveApproversAsync(
            HrLvRegister leaveApp,
            HrLvWfInstance wfInstance,
            string lvTypeName,
            string requesterName,
            string requesterEmpId)
        {
            if (_notificationService == null) return;

            string wfConStr = GetWorkflowEngineDbConnectionString();
            if (string.IsNullOrEmpty(wfConStr)) return;

            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(wfConStr);
                await conn.OpenAsync();

                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        s.StepNumber,
                        s.Name AS StepName,
                        a.Id AS ApprovalId,
                        a.ApproverId,
                        u.FullName AS ApproverName,
                        u.Email AS ApproverEmail,
                        u.Username AS ApproverUsername,
                        u.EmpSysId
                    FROM WorkflowInstanceSteps s
                    INNER JOIN WorkflowApprovals a ON a.WorkflowInstanceStepId = s.Id AND a.IsDeleted = 0
                    LEFT JOIN Users u ON a.ApproverId = u.Id
                    WHERE s.WorkflowInstanceId = @InstId AND s.Status = 1 AND a.Status = 1 AND s.IsDeleted = 0", conn);
                cmd.Parameters.AddWithValue("@InstId", wfInstance.WfInstanceId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string stepName = reader["StepName"]?.ToString() ?? "Approval Step";
                    Guid approverWfId = (Guid)reader["ApproverId"];
                    string approverName = reader["ApproverName"]?.ToString() ?? "Approver";
                    string username = reader["ApproverUsername"]?.ToString() ?? "";

                    // Resolve approver's EmpSysId
                    int approverEmpSysId = 0;
                    if (reader["EmpSysId"] != DBNull.Value && int.TryParse(reader["EmpSysId"].ToString(), out int uEmpSysId) && uEmpSysId > 0)
                    {
                        approverEmpSysId = uEmpSysId;
                    }

                    if (approverEmpSysId == 0)
                    {
                        var mapping = await _hrmContext.HrWfUserMapping
                            .FirstOrDefaultAsync(m => m.WfUserId == approverWfId);
                        if (mapping != null)
                        {
                            approverEmpSysId = mapping.EmpSysId;
                        }
                    }

                    if (approverEmpSysId == 0 && !string.IsNullOrEmpty(username))
                    {
                        var emp = await _hrmContext.HrEmpGenInfo
                            .FirstOrDefaultAsync(e => e.EmpId == username);
                        if (emp != null) approverEmpSysId = emp.EmpSysId;
                    }

                    if (approverEmpSysId == 0 && !string.IsNullOrEmpty(approverName))
                    {
                        var trimmed = approverName.Trim();
                        var emp = await _hrmContext.HrEmpGenInfo
                            .FirstOrDefaultAsync(e => (e.FirstName + " " + e.LastName).Trim() == trimmed || e.FullName == trimmed);
                        if (emp != null) approverEmpSysId = emp.EmpSysId;
                    }

                    // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table.
                    // Do NOT pick u.Email, PsnEmail, or any other email source.
                    string approverEmail = "";
                    if (approverEmpSysId > 0)
                    {
                        var job = await _hrmContext.HrEmpJobAssign
                            .Where(j => j.EmpSysId == approverEmpSysId && j.WkStat == 1 && !string.IsNullOrEmpty(j.OfcEmail))
                            .OrderByDescending(j => j.StDate)
                            .FirstOrDefaultAsync();
                        if (job == null)
                        {
                            job = await _hrmContext.HrEmpJobAssign
                                .Where(j => j.EmpSysId == approverEmpSysId && !string.IsNullOrEmpty(j.OfcEmail))
                                .OrderByDescending(j => j.StDate)
                                .FirstOrDefaultAsync();
                        }
                        approverEmail = job?.OfcEmail?.Trim() ?? "";
                    }

                    await _notificationService.SendLeaveApprovalRequiredNotificationAsync(
                        leaveApp.LvId,
                        wfInstance.WfInstId,
                        stepName,
                        approverEmpSysId,
                        approverName,
                        approverEmail,
                        leaveApp,
                        lvTypeName,
                        requesterName,
                        requesterEmpId,
                        sendEmail: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] NotifyActiveApproversAsync failed: {ex.Message}");
            }
        }

        private async Task SendSubmissionNotificationsAsync(
            HrLvRegister leaveApp,
            HrLvWfInstance wfInstance,
            string lvTypeName)
        {
            var emp = await _hrmContext.HrEmpGenInfo
                .FirstOrDefaultAsync(e => e.EmpSysId == leaveApp.EmpSysId);

            if (emp == null) return;

            // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table.
            var empJob = await _hrmContext.HrEmpJobAssign
                .Where(j => j.EmpSysId == emp.EmpSysId && j.WkStat == 1)
                .OrderByDescending(j => j.StDate)
                .FirstOrDefaultAsync();
            if (empJob == null)
            {
                empJob = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == emp.EmpSysId)
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();
            }
            string empEmail = empJob?.OfcEmail ?? "";

            string requesterName = emp.FullName ?? $"Employee #{emp.EmpSysId}";
            string requesterEmpId = emp.EmpId ?? emp.EmpSysId.ToString();

            if (_notificationService != null)
            {
                // 1. Employee confirmation email + in-app notification
                try
                {
                    await _notificationService.SendLeaveSubmissionConfirmationAsync(
                        leaveApp.LvId,
                        wfInstance.WfInstId,
                        emp.EmpSysId,
                        requesterName,
                        empEmail,
                        lvTypeName ?? "Leave",
                        leaveApp,
                        wfInstance.WfInstanceNo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] Employee submission notification: {ex.Message}");
                }

                // 2. Step 1 approver email notification with direct action link + in-app notification
                try
                {
                    await NotifyActiveApproversAsync(
                        leaveApp,
                        wfInstance,
                        lvTypeName ?? "Leave",
                        requesterName,
                        requesterEmpId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] Step 1 approver notification: {ex.Message}");
                }
            }
        }

        public async Task SendApprovalActionNotificationAsync(
            int    lvId,
            int    wfInstId,
            string eventType,       // Approved | Rejected | FinalApproved | Delegated
            string approverName,
            string employeeEmail,
            int    employeeEmpSysId,
            string employeeName,
            string lvTypeName,
            string actionComment,
            string nextApproverEmail = null,
            string nextApproverName  = null)
        {
            var leaveApp = await _hrmContext.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == lvId);

            if (employeeEmpSysId <= 0 && leaveApp?.EmpSysId.HasValue == true)
            {
                employeeEmpSysId = leaveApp.EmpSysId.Value;
            }

            // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table.
            if (employeeEmpSysId > 0)
            {
                var empJob = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == employeeEmpSysId && j.WkStat == 1 && !string.IsNullOrEmpty(j.OfcEmail))
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();
                if (empJob == null)
                {
                    empJob = await _hrmContext.HrEmpJobAssign
                        .Where(j => j.EmpSysId == employeeEmpSysId && !string.IsNullOrEmpty(j.OfcEmail))
                        .OrderByDescending(j => j.StDate)
                        .FirstOrDefaultAsync();
                }
                employeeEmail = empJob?.OfcEmail?.Trim() ?? "";
            }

            if (_notificationService != null)
            {
                await _notificationService.SendLeaveStatusUpdateNotificationAsync(
                    lvId,
                    wfInstId,
                    eventType,
                    approverName,
                    actionComment,
                    employeeEmpSysId,
                    employeeName,
                    employeeEmail,
                    lvTypeName,
                    leaveApp,
                    sendEmail: false);
            }
        }

        // =====================================================================
        // EMAIL TEMPLATES
        // =====================================================================

        private static string BuildEmployeeSubmissionEmail(
            string empName, string lvTypeName,
            HrLvRegister leaveApp, string instanceNo)
        {
            return $@"<html><body style='font-family:Nunito,Segoe UI,sans-serif;font-size:14px;color:#333;'>
<div style='max-width:600px;margin:0 auto;border:1px solid #e0e0e0;border-radius:8px;overflow:hidden;'>
  <div style='background:#990049;padding:24px;text-align:center;'>
    <h2 style='color:#fff;margin:0;'>Leave Application Submitted</h2>
  </div>
  <div style='padding:24px;'>
    <p>Dear <strong>{empName}</strong>,</p>
    <p>Your leave application has been submitted successfully and is now pending approval.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
      <tr style='background:#f9f0f4;'><td style='padding:8px 12px;font-weight:600;'>Reference</td><td style='padding:8px 12px;'>{instanceNo}</td></tr>
      <tr><td style='padding:8px 12px;font-weight:600;'>Leave Type</td><td style='padding:8px 12px;'>{lvTypeName}</td></tr>
      <tr style='background:#f9f0f4;'><td style='padding:8px 12px;font-weight:600;'>From Date</td><td style='padding:8px 12px;'>{leaveApp.StDate:dd MMM yyyy}</td></tr>
      <tr><td style='padding:8px 12px;font-weight:600;'>To Date</td><td style='padding:8px 12px;'>{leaveApp.EndDate:dd MMM yyyy}</td></tr>
      <tr style='background:#f9f0f4;'><td style='padding:8px 12px;font-weight:600;'>Days</td><td style='padding:8px 12px;'>{leaveApp.Days}</td></tr>
      <tr><td style='padding:8px 12px;font-weight:600;'>Status</td><td style='padding:8px 12px;'><span style='color:#990049;font-weight:700;'>Pending Approval</span></td></tr>
    </table>
    <p>You will receive further notifications as your application progresses through the approval workflow.</p>
    <hr style='border:none;border-top:1px solid #eee;margin:20px 0;'/>
    <p style='font-size:12px;color:#999;'>This is a system-generated email. Please do not reply.</p>
  </div>
</div></body></html>";
        }

        private static string BuildStatusUpdateEmail(
            string empName, string lvTypeName,
            string eventType, string approverName, string comment)
        {
            string statusColor = eventType is LvWfNotifyEvent.Rejected ? "#c0392b" : "#27ae60";
            string statusText  = eventType is LvWfNotifyEvent.FinalApproved
                ? "Fully Approved" : eventType;

            return $@"<html><body style='font-family:Nunito,Segoe UI,sans-serif;font-size:14px;color:#333;'>
<div style='max-width:600px;margin:0 auto;border:1px solid #e0e0e0;border-radius:8px;overflow:hidden;'>
  <div style='background:#990049;padding:24px;text-align:center;'>
    <h2 style='color:#fff;margin:0;'>Leave Application Update</h2>
  </div>
  <div style='padding:24px;'>
    <p>Dear <strong>{empName}</strong>,</p>
    <p>Your <strong>{lvTypeName}</strong> leave application status has been updated.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
      <tr style='background:#f9f0f4;'><td style='padding:8px 12px;font-weight:600;'>Action By</td><td style='padding:8px 12px;'>{approverName}</td></tr>
      <tr><td style='padding:8px 12px;font-weight:600;'>Status</td><td style='padding:8px 12px;'><span style='color:{statusColor};font-weight:700;'>{statusText}</span></td></tr>
      {(string.IsNullOrEmpty(comment) ? "" : $"<tr style='background:#f9f0f4;'><td style='padding:8px 12px;font-weight:600;'>Comments</td><td style='padding:8px 12px;'>{comment}</td></tr>")}
    </table>
    <hr style='border:none;border-top:1px solid #eee;margin:20px 0;'/>
    <p style='font-size:12px;color:#999;'>This is a system-generated email. Please do not reply.</p>
  </div>
</div></body></html>";
        }

        private static string BuildApproverNotificationEmail(
            string approverName, string empName, string lvTypeName, string priorComment)
        {
            return $@"<html><body style='font-family:Nunito,Segoe UI,sans-serif;font-size:14px;color:#333;'>
<div style='max-width:600px;margin:0 auto;border:1px solid #e0e0e0;border-radius:8px;overflow:hidden;'>
  <div style='background:#990049;padding:24px;text-align:center;'>
    <h2 style='color:#fff;margin:0;'>Leave Approval Required</h2>
  </div>
  <div style='padding:24px;'>
    <p>Dear <strong>{approverName}</strong>,</p>
    <p>A leave application from <strong>{empName}</strong> is waiting for your approval.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
      <tr style='background:#f9f0f4;'><td style='padding:8px 12px;font-weight:600;'>Employee</td><td style='padding:8px 12px;'>{empName}</td></tr>
      <tr><td style='padding:8px 12px;font-weight:600;'>Leave Type</td><td style='padding:8px 12px;'>{lvTypeName}</td></tr>
    </table>
    <p>Please log in to the HR system to review and take action on this leave application.</p>
    <p style='text-align:center;margin:24px 0;'>
      <a href='#' style='background:#990049;color:#fff;padding:12px 28px;text-decoration:none;border-radius:6px;font-weight:600;'>Review Leave Application</a>
    </p>
    <hr style='border:none;border-top:1px solid #eee;margin:20px 0;'/>
    <p style='font-size:12px;color:#999;'>This is a system-generated email. Please do not reply.</p>
  </div>
</div></body></html>";
        }

        // =====================================================================
        // GET PENDING APPROVALS FOR CURRENT USER
        // Fetches workflow approvals assigned to this employee from the WF Engine.
        // =====================================================================

        public async Task<List<object>> GetPendingApprovalsForUserAsync(int empSysId)
        {
            var token = await GetWfTokenAsync();

            // Get or create the WF user GUID for this employee
            Guid wfUserId;
            try
            {
                wfUserId = await GetOrCreateWfUserAsync(empSysId, token);
            }
            catch
            {
                var mapping = await _hrmContext.HrWfUserMapping
                    .FirstOrDefaultAsync(m => m.EmpSysId == empSysId);
                if (mapping == null) return new List<object>();
                wfUserId = mapping.WfUserId;
            }

            List<WfPendingItem> pendingItems = null;

            try
            {
                var client   = GetWfClient(token);
                var response = await client.GetAsync(
                    $"{WfEngineBaseUrl}/api/workflowexecution/pending?empSysId={empSysId}&userId={wfUserId}&_t={DateTime.UtcNow.Ticks}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    pendingItems = JsonSerializer.Deserialize<List<WfPendingItem>>(json, opts);
                }
            }
            catch { /* Fallback to direct DB query below */ }

            // If API didn't return items (e.g. 404 on pending endpoint or connection issue), fallback to direct DB query
            if (pendingItems == null || !pendingItems.Any())
            {
                string wfConStr = GetWorkflowEngineDbConnectionString();
                if (!string.IsNullOrEmpty(wfConStr))
                {
                    try
                    {
                        using var conn = new Microsoft.Data.SqlClient.SqlConnection(wfConStr);
                        await conn.OpenAsync();

                        using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                            SELECT 
                                a.Id AS ApprovalId,
                                s.WorkflowInstanceId AS InstanceId,
                                s.Name AS StepName,
                                'Pending' AS Status
                            FROM WorkflowApprovals a
                            INNER JOIN WorkflowInstanceSteps s ON a.WorkflowInstanceStepId = s.Id
                            LEFT JOIN Users u ON a.ApproverId = u.Id
                            WHERE (a.ApproverId = @WfUserId OR a.DelegatedFromUserId = @WfUserId OR u.EmpSysId = @EmpSysId)
                              AND a.Status = 1
                              AND a.IsDeleted = 0
                              AND s.Status = 1
                              AND s.IsDeleted = 0", conn);

                        cmd.Parameters.AddWithValue("@WfUserId", wfUserId);
                        cmd.Parameters.AddWithValue("@EmpSysId", empSysId);

                        using var reader = await cmd.ExecuteReaderAsync();
                        pendingItems = new List<WfPendingItem>();
                        while (await reader.ReadAsync())
                        {
                            pendingItems.Add(new WfPendingItem
                            {
                                ApprovalId = (Guid)reader["ApprovalId"],
                                InstanceId = (Guid)reader["InstanceId"],
                                StepName   = reader["StepName"]?.ToString() ?? "Approval Step",
                                Status     = "Pending"
                            });
                        }
                    }
                    catch { /* Continue with empty list if DB query also fails */ }
                }
            }

            pendingItems ??= new List<WfPendingItem>();

            var result = new List<object>();
            foreach (var item in pendingItems)
            {
                // Try to find the linked leave record
                var wfInst = await _hrmContext.HrLvWfInstance
                    .FirstOrDefaultAsync(i => i.WfInstanceId == item.InstanceId);

                if (wfInst == null) continue;

                var lvRec = await _hrmContext.HrLvRegister
                    .FirstOrDefaultAsync(l => l.LvId == wfInst.LvId);
                if (lvRec == null) continue;

                var emp = await _hrmContext.HrEmpGenInfo
                    .FirstOrDefaultAsync(e => e.EmpSysId == lvRec.EmpSysId);
                var lvType = await _hrmContext.HrLvType
                    .FirstOrDefaultAsync(t => t.LvTypId == lvRec.LvTypId);

                var job = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == lvRec.EmpSysId && j.WkStat == 1)
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();

                string deptName = "—";
                string desgName = "";
                if (job != null)
                {
                    if (job.DeptId > 0)
                    {
                        var dept = await _hrmContext.OrgDepartment.FirstOrDefaultAsync(d => d.DeptId == job.DeptId);
                        if (dept != null) deptName = dept.NameEng;
                    }
                    if (job.DesigId > 0)
                    {
                        var desg = await _hrmContext.OrgDesignation.FirstOrDefaultAsync(d => d.DesigId == job.DesigId);
                        if (desg != null) desgName = desg.NameEng;
                    }
                }

                string empFullName = "";
                if (emp != null)
                {
                    var nameParts = new[] { emp.FirstName, emp.MdlName, emp.LastName }
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p.Trim());
                    empFullName = string.Join(" ", nameParts);

                    if (string.IsNullOrWhiteSpace(empFullName))
                        empFullName = !string.IsNullOrWhiteSpace(emp.PstName) ? emp.PstName.Trim() : (emp.EmpId ?? "");
                }
                if (string.IsNullOrWhiteSpace(empFullName))
                {
                    empFullName = $"Employee #{lvRec.EmpSysId}";
                }

                var parsedLvNote = ParseNoteAndAttachment(lvRec.Note, "—");
                bool hasAttachment = parsedLvNote.hasAttachment;
                string attachmentUrl = parsedLvNote.attachmentUrl;
                string attachmentFileName = parsedLvNote.attachmentFileName;
                string attachmentType = parsedLvNote.attachmentType;

                if (!hasAttachment)
                {
                    try
                    {
                        var regDoc = await _hrmContext.HrLvRegDocs
                            .OrderByDescending(d => d.DocId)
                            .FirstOrDefaultAsync(d => d.LvId == wfInst.LvId);
                        if (regDoc != null)
                        {
                            hasAttachment = true;
                            attachmentFileName = regDoc.DocName ?? $"document_{wfInst.LvId}.pdf";
                            attachmentUrl = $"/Leave/HrLvRegister/GetDocument?id={regDoc.DocId}&lvId={wfInst.LvId}";
                            var ext = System.IO.Path.GetExtension(attachmentFileName).ToLowerInvariant();
                            attachmentType = (ext == ".pdf" || regDoc.DocMime?.Contains("pdf") == true) ? "pdf" :
                                             (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(ext) || regDoc.DocMime?.StartsWith("image") == true ? "image" : "document");
                        }
                    }
                    catch { }
                }

                result.Add(new
                {
                    LvId         = wfInst.LvId,
                    WfInstanceId = item.InstanceId,
                    ApprovalId   = item.ApprovalId,
                    InstanceNo   = wfInst.WfInstanceNo,
                    EmployeeName = empFullName,
                    EmpId        = emp?.EmpId ?? lvRec.EmpSysId.ToString(),
                    Department   = deptName,
                    Designation  = desgName,
                    LeaveType    = lvType?.NameEng ?? "—",
                    StDate       = lvRec.StDate,
                    EndDate      = lvRec.EndDate,
                    Days         = lvRec.Days,
                    Reason       = parsedLvNote.cleanReason,
                    SeatingLocation = string.IsNullOrWhiteSpace(emp?.SeatingLocation) ? "—" : emp.SeatingLocation.Trim(),
                    HasAttachment = hasAttachment,
                    AttachmentUrl = attachmentUrl,
                    AttachmentFileName = attachmentFileName,
                    AttachmentType = attachmentType,
                    WfStatus     = item.Status ?? wfInst.WfStatus,
                    WfStatusBadge = (item.Status ?? wfInst.WfStatus) switch
                    {
                        "Approved"  => "badge-success",
                        "Rejected"  => "badge-danger",
                        _           => "badge-info"
                    },
                    SubmittedOn  = wfInst.CrtDateTime
                });
            }

            return result;
        }

        // =====================================================================
        // GET LEAVE INCIDENT FOR REVIEW
        // Loads a single leave incident details, timeline, and current approver action context.
        // =====================================================================
        public async Task<LeaveIncidentReviewViewModel> GetLeaveIncidentForReviewAsync(int lvId, int approverEmpSysId)
        {
            var model = new LeaveIncidentReviewViewModel { LvId = lvId };

            var lvRec = await _hrmContext.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == lvId);
            if (lvRec == null)
            {
                model.Success = false;
                model.Message = $"Leave application #{lvId} was not found.";
                return model;
            }

            var parsedRecordNote = ParseNoteAndAttachment(lvRec.Note, "—");

            model.LvTypId = lvRec.LvTypId ?? 0;
            model.StDate = lvRec.StDate;
            model.EndDate = lvRec.EndDate;
            model.Days = lvRec.Days;
            model.Reason = parsedRecordNote.cleanReason;
            model.HasAttachment = parsedRecordNote.hasAttachment;
            model.AttachmentUrl = parsedRecordNote.attachmentUrl;
            model.AttachmentFileName = parsedRecordNote.attachmentFileName;
            model.AttachmentType = parsedRecordNote.attachmentType;
            model.EmpSysId = lvRec.EmpSysId ?? 0;

            if (!model.HasAttachment)
            {
                try
                {
                    var regDoc = await _hrmContext.HrLvRegDocs
                        .OrderByDescending(d => d.DocId)
                        .FirstOrDefaultAsync(d => d.LvId == lvId);
                    if (regDoc != null)
                    {
                        model.HasAttachment = true;
                        model.AttachmentFileName = regDoc.DocName ?? $"document_{lvId}.pdf";
                        model.AttachmentUrl = $"/Leave/HrLvRegister/GetDocument?id={regDoc.DocId}&lvId={lvId}";
                        var ext = System.IO.Path.GetExtension(model.AttachmentFileName).ToLowerInvariant();
                        model.AttachmentType = (ext == ".pdf" || regDoc.DocMime?.Contains("pdf") == true) ? "pdf" :
                                               (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(ext) || regDoc.DocMime?.StartsWith("image") == true ? "image" : "document");
                    }
                }
                catch { }
            }

            // Employee Info
            var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == lvRec.EmpSysId);
            if (emp != null)
            {
                var nameParts = new[] { emp.FirstName, emp.MdlName, emp.LastName }
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim());
                model.EmployeeName = string.Join(" ", nameParts);
                if (string.IsNullOrWhiteSpace(model.EmployeeName))
                    model.EmployeeName = !string.IsNullOrWhiteSpace(emp.PstName) ? emp.PstName.Trim() : (emp.EmpId ?? "");
                model.EmpId = emp.EmpId ?? lvRec.EmpSysId.ToString();
                model.SeatingLocation = string.IsNullOrWhiteSpace(emp.SeatingLocation) ? "—" : emp.SeatingLocation.Trim();
            }
            if (string.IsNullOrWhiteSpace(model.EmployeeName))
            {
                model.EmployeeName = $"Employee #{lvRec.EmpSysId}";
                model.EmpId = lvRec.EmpSysId.ToString();
            }

            var job = await _hrmContext.HrEmpJobAssign
                .Where(j => j.EmpSysId == lvRec.EmpSysId && j.WkStat == 1)
                .OrderByDescending(j => j.StDate)
                .FirstOrDefaultAsync();
            if (job != null)
            {
                if (job.DeptId > 0)
                {
                    var dept = await _hrmContext.OrgDepartment.FirstOrDefaultAsync(d => d.DeptId == job.DeptId);
                    if (dept != null) model.Department = dept.NameEng;
                }
                if (job.DesigId > 0)
                {
                    var desg = await _hrmContext.OrgDesignation.FirstOrDefaultAsync(d => d.DesigId == job.DesigId);
                    if (desg != null) model.Designation = desg.NameEng;
                }
            }
            if (string.IsNullOrWhiteSpace(model.Department)) model.Department = "—";
            if (string.IsNullOrWhiteSpace(model.Designation)) model.Designation = "—";

            var lvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == lvRec.LvTypId);
            model.LeaveType = lvType?.NameEng ?? "Leave";

            // Workflow Instance Info
            var wfInst = await _hrmContext.HrLvWfInstance.FirstOrDefaultAsync(i => i.LvId == lvId);
            if (wfInst != null)
            {
                model.WfInstanceId = wfInst.WfInstanceId;
                model.InstanceNo = wfInst.WfInstanceNo ?? "";
                model.WfStatus = wfInst.WfStatus ?? "Initiated";
                model.SubmittedOn = wfInst.CrtDateTime;
            }
            else
            {
                model.WfStatus = "Not Started";
            }

            model.WfStatusBadge = model.WfStatus switch
            {
                "Approved" or "FinalApproved" => "badge-success",
                "Rejected" => "badge-danger",
                "Running" or "Pending" => "badge-warning",
                _ => "badge-info"
            };

            // Load Timeline
            model.Timeline = await GetLeaveApprovalTimelineAsync(lvId);

            // Check if current user is the pending approver for this leave request
            if (approverEmpSysId > 0 && wfInst != null && wfInst.WfInstanceId != Guid.Empty)
            {
                try
                {
                    var pendingList = await GetPendingApprovalsForUserAsync(approverEmpSysId);
                    foreach (var pObj in pendingList)
                    {
                        var propLvId = pObj.GetType().GetProperty("LvId")?.GetValue(pObj);
                        if (propLvId is int pLvId && pLvId == lvId)
                        {
                            var propApprId = pObj.GetType().GetProperty("ApprovalId")?.GetValue(pObj);
                            if (propApprId is Guid aId)
                            {
                                model.CanApprove = true;
                                model.ApprovalId = aId;
                            }
                            break;
                        }
                    }
                }
                catch { }

                // Fallback: If not found via pending list (e.g. dynamic role/user), check direct DB query from WorkflowEngineDb
                if (!model.CanApprove)
                {
                    try
                    {
                        var wfConnStr = GetWorkflowEngineDbConnectionString();
                        if (!string.IsNullOrEmpty(wfConnStr))
                        {
                            var mapping = await _hrmContext.HrWfUserMapping.FirstOrDefaultAsync(m => m.EmpSysId == approverEmpSysId);
                            if (mapping != null)
                            {
                                using var conn = new Microsoft.Data.SqlClient.SqlConnection(wfConnStr);
                                await conn.OpenAsync();
                                using var cmd = conn.CreateCommand();
                                cmd.CommandText = @"
                                    SELECT TOP 1 a.Id AS ApprovalId, s.Name AS StepName, s.StepNumber
                                    FROM WorkflowApprovals a
                                    INNER JOIN WorkflowInstanceSteps s ON a.WorkflowInstanceStepId = s.Id
                                    WHERE s.WorkflowInstanceId = @instId 
                                      AND (a.ApproverId = @wfUserId OR a.DelegatedFromUserId = @wfUserId)
                                      AND a.Status = 1
                                      AND a.IsDeleted = 0";
                                cmd.Parameters.AddWithValue("@instId", wfInst.WfInstanceId);
                                cmd.Parameters.AddWithValue("@wfUserId", mapping.WfUserId);
                                using var r = await cmd.ExecuteReaderAsync();
                                if (await r.ReadAsync())
                                {
                                    model.CanApprove = true;
                                    model.ApprovalId = (Guid)r["ApprovalId"];
                                    model.CurrentStepName = r["StepName"]?.ToString() ?? "";
                                    model.CurrentStepNumber = r["StepNumber"] != DBNull.Value ? Convert.ToInt32(r["StepNumber"]) : 0;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            return model;
        }

        // =====================================================================
        // GET WORKFLOW HISTORY
        // Fetches the full approval history for a WF instance.
        // =====================================================================

        public async Task<List<WfHistoryDto>> GetWorkflowHistoryAsync(Guid wfInstanceId)
        {
            try
            {
                var token = await GetWfTokenAsync();
                var client   = GetWfClient(token);
                var response = await client.GetAsync(
                    $"{WfEngineBaseUrl}/api/workflowexecution/history/{wfInstanceId}");

                if (!response.IsSuccessStatusCode)
                    return new List<WfHistoryDto>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<WfHistoryDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<WfHistoryDto>();
            }
            catch
            {
                return new List<WfHistoryDto>();
            }
        }

        // =====================================================================
        // SYNC ACTIVE WORKFLOW STATUSES
        // Polls the Workflow Engine for any pending/in-progress instances to keep Leave Register up-to-date.
        // =====================================================================

        public async Task SyncActiveWorkflowStatusesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // If workflow engine was detected offline recently, skip immediately
                if (DateTime.UtcNow - _lastConnectionFailureTime < FailureCooldown)
                    return;

                var activeWfInstances = await _hrmContext.HrLvWfInstance
                    .Where(w => w.WfStatus == "InProgress" || w.WfStatus == "Submitted" || w.WfStatus == "Pending")
                    .ToListAsync(cancellationToken);

                if (!activeWfInstances.Any()) return;

                // Test / pre-fetch token once. If token fails or engine is down, abort immediately
                string token;
                try
                {
                    if (DateTime.UtcNow - _lastConnectionFailureTime < FailureCooldown)
                        return;

                    using var quickCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, quickCts.Token);
                    token = await GetWfTokenAsync(linkedCts.Token);
                }
                catch
                {
                    // Engine is unreachable or offline, exit fast without stalling
                    return;
                }

                if (string.IsNullOrEmpty(token)) return;

                // Fetch all active instance statuses from the WF Engine in parallel using the pre-fetched token
                var fetchTasks = activeWfInstances.Select(async inst =>
                {
                    try
                    {
                        var client = GetWfClient(token);
                        client.Timeout = TimeSpan.FromSeconds(3);
                        var response = await client.GetAsync(
                            $"{WfEngineBaseUrl}/api/workflowexecution/instance/{inst.WfInstanceId}", cancellationToken);
                        if (!response.IsSuccessStatusCode) return (inst, (WfInstanceDto)null);

                        var json = await response.Content.ReadAsStringAsync();
                        var wfDto = JsonSerializer.Deserialize<WfInstanceDto>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return (inst, wfDto);
                    }
                    catch
                    {
                        return (inst, (WfInstanceDto)null);
                    }
                });

                var fetched = await Task.WhenAll(fetchTasks);

                // Update database sequentially (DbContext is NOT thread-safe)
                foreach (var (inst, wfDto) in fetched)
                {
                    if (wfDto != null && !string.IsNullOrEmpty(wfDto.StatusText) && wfDto.StatusText != inst.WfStatus)
                    {
                        await ProcessWorkflowCallbackAsync(inst.WfInstanceId, wfDto.StatusText);
                    }
                }
            }
            catch { }
        }

        // =====================================================================
        // APPROVE STEP
        // Posts an approval decision to the Workflow Engine.
        // =====================================================================

        public async Task ApproveStepAsync(Guid approvalId, string comment)
        {
            var token  = await GetWfTokenAsync();
            var client = GetWfClient(token);
            var req    = new WfApproveRequest { ApprovalId = approvalId, Comment = comment };
            var content = new StringContent(
                JsonSerializer.Serialize(req),
                System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{WfEngineBaseUrl}/api/workflowexecution/approve", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Workflow Engine approve failed: {err}");
            }

            try
            {
                var respStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respStr);
                string newStatus = "InProgress";
                if (doc.RootElement.TryGetProperty("status", out var stProp) || doc.RootElement.TryGetProperty("Status", out stProp))
                {
                    newStatus = stProp.GetString() ?? "InProgress";
                }

                if (doc.RootElement.TryGetProperty("instanceId", out var instProp) || doc.RootElement.TryGetProperty("InstanceId", out instProp))
                {
                    if (Guid.TryParse(instProp.GetString(), out Guid wfInstId))
                    {
                        await ProcessWorkflowCallbackAsync(wfInstId, newStatus);

                        // Trigger notifications for the next approver or final completion
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (_scopeFactory != null)
                                {
                                    using var scope = _scopeFactory.CreateScope();
                                    var scopedHrm = scope.ServiceProvider.GetRequiredService<qHRMasterContext>();
                                    var scopedNotif = scope.ServiceProvider.GetService<WorkflowNotificationService>();
                                    var scopedSvc = scope.ServiceProvider.GetRequiredService<LeaveWorkflowService>();

                                    var inst = await scopedHrm.HrLvWfInstance
                                        .FirstOrDefaultAsync(i => i.WfInstanceId == wfInstId);
                                    if (inst != null)
                                    {
                                        var lv = await scopedHrm.HrLvRegister
                                            .FirstOrDefaultAsync(l => l.LvId == inst.LvId);
                                        if (lv != null)
                                        {
                                            var reqEmp = await scopedHrm.HrEmpGenInfo
                                                .FirstOrDefaultAsync(e => e.EmpSysId == lv.EmpSysId);
                                            var reqJob = await scopedHrm.HrEmpJobAssign
                                                .Where(j => j.EmpSysId == lv.EmpSysId && j.WkStat == 1 && !string.IsNullOrEmpty(j.OfcEmail))
                                                .OrderByDescending(j => j.StDate)
                                                .FirstOrDefaultAsync();
                                            if (reqJob == null)
                                            {
                                                reqJob = await scopedHrm.HrEmpJobAssign
                                                    .Where(j => j.EmpSysId == lv.EmpSysId && !string.IsNullOrEmpty(j.OfcEmail))
                                                    .OrderByDescending(j => j.StDate)
                                                    .FirstOrDefaultAsync();
                                            }
                                            var lvTyp = await scopedHrm.HrLvType
                                                .FirstOrDefaultAsync(t => t.LvTypId == lv.LvTypId);

                                            string reqEmail = reqJob?.OfcEmail?.Trim() ?? "";
                                            string reqName = reqEmp?.FullName ?? $"Employee #{lv.EmpSysId}";
                                            string reqIdStr = reqEmp?.EmpId ?? lv.EmpSysId.ToString();

                                            if (string.Equals(newStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (scopedNotif != null)
                                                {
                                                    // Final approval: push in-app notification to employee; final approval email is sent by WorkflowEngine
                                                    await scopedNotif.SendLeaveStatusUpdateNotificationAsync(
                                                        lv.LvId, inst.WfInstId, "FinalApproved", "Approver", comment,
                                                        lv.EmpSysId ?? 0, reqName, reqEmail, lvTyp?.NameEng ?? "Leave", lv, sendEmail: false);
                                                }
                                            }
                                            else
                                            {
                                                if (scopedNotif != null)
                                                {
                                                    // Intermediate approval: push in-app notification to employee (no intermediate email per requirement)
                                                    await scopedNotif.SendLeaveStatusUpdateNotificationAsync(
                                                        lv.LvId, inst.WfInstId, "Approved", "Approver", comment,
                                                        lv.EmpSysId ?? 0, reqName, reqEmail, lvTyp?.NameEng ?? "Leave", lv, sendEmail: false);

                                                    // Next approver in-app notification (approver email is queued by WorkflowEngine)
                                                    await scopedSvc.NotifyActiveApproversAsync(
                                                        lv, inst, lvTyp?.NameEng ?? "Leave", reqName, reqIdStr);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] Approve notification dispatch: {ex.Message}");
                            }
                        });
                    }
                }
            }
            catch { }
        }

        // =====================================================================
        // REJECT STEP
        // Posts a rejection decision to the Workflow Engine.
        // =====================================================================

        public async Task RejectStepAsync(Guid approvalId, string comment)
        {
            var token  = await GetWfTokenAsync();
            var client = GetWfClient(token);
            var req    = new WfRejectRequest { ApprovalId = approvalId, Comment = comment };
            var content = new StringContent(
                JsonSerializer.Serialize(req),
                System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{WfEngineBaseUrl}/api/workflowexecution/reject", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Workflow Engine reject failed: {err}");
            }

            try
            {
                var respStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respStr);
                string newStatus = "Rejected";
                if (doc.RootElement.TryGetProperty("status", out var stProp) || doc.RootElement.TryGetProperty("Status", out stProp))
                {
                    newStatus = stProp.GetString() ?? "Rejected";
                }

                if (doc.RootElement.TryGetProperty("instanceId", out var instProp) || doc.RootElement.TryGetProperty("InstanceId", out instProp))
                {
                    if (Guid.TryParse(instProp.GetString(), out Guid wfInstId))
                    {
                        await ProcessWorkflowCallbackAsync(wfInstId, newStatus);

                        // Notify employee of rejection
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (_scopeFactory != null)
                                {
                                    using var scope = _scopeFactory.CreateScope();
                                    var scopedHrm = scope.ServiceProvider.GetRequiredService<qHRMasterContext>();
                                    var scopedNotif = scope.ServiceProvider.GetService<WorkflowNotificationService>();

                                    var inst = await scopedHrm.HrLvWfInstance
                                        .FirstOrDefaultAsync(i => i.WfInstanceId == wfInstId);
                                    if (inst != null)
                                    {
                                        var lv = await scopedHrm.HrLvRegister
                                            .FirstOrDefaultAsync(l => l.LvId == inst.LvId);
                                        if (lv != null)
                                        {
                                            var reqEmp = await scopedHrm.HrEmpGenInfo
                                                .FirstOrDefaultAsync(e => e.EmpSysId == lv.EmpSysId);
                                            var reqJob = await scopedHrm.HrEmpJobAssign
                                                .Where(j => j.EmpSysId == lv.EmpSysId && j.WkStat == 1 && !string.IsNullOrEmpty(j.OfcEmail))
                                                .OrderByDescending(j => j.StDate)
                                                .FirstOrDefaultAsync();
                                            if (reqJob == null)
                                            {
                                                reqJob = await scopedHrm.HrEmpJobAssign
                                                    .Where(j => j.EmpSysId == lv.EmpSysId && !string.IsNullOrEmpty(j.OfcEmail))
                                                    .OrderByDescending(j => j.StDate)
                                                    .FirstOrDefaultAsync();
                                            }
                                            var lvTyp = await scopedHrm.HrLvType
                                                .FirstOrDefaultAsync(t => t.LvTypId == lv.LvTypId);

                                            string reqEmail = reqJob?.OfcEmail?.Trim() ?? "";
                                            string reqName = reqEmp?.FullName ?? $"Employee #{lv.EmpSysId}";

                                            if (scopedNotif != null)
                                            {
                                                // Rejection: push in-app notification to employee; rejection email is sent by WorkflowEngine
                                                await scopedNotif.SendLeaveStatusUpdateNotificationAsync(
                                                    lv.LvId, inst.WfInstId, "Rejected", "Approver", comment,
                                                    lv.EmpSysId ?? 0, reqName, reqEmail, lvTyp?.NameEng ?? "Leave", lv, sendEmail: false);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] Reject notification dispatch: {ex.Message}");
                            }
                        });
                    }
                }
            }
            catch { }
        }
    }
}

/// <summary>DTO for a single pending approval item returned by the Workflow Engine.</summary>
public class WfPendingItem
{
    public Guid   InstanceId  { get; set; }
    public Guid   ApprovalId  { get; set; }
    public string Status      { get; set; }
    public string StepName    { get; set; }
}

