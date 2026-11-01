using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using qHRM.Area.Config;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.Notify.Models;
using qHRM.Models;

namespace qHRM.Areas.Notify.Data
{
    public class NotificationModelDto
    {
        public int MsgId { get; set; }
        public int? ReqId { get; set; }
        public int? EmpSysId { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool PushReadStat { get; set; }
        public DateTime? CrtDateTime { get; set; }
        public string ActionUrl { get; set; }
        public string TimeAgo { get; set; }
    }

    public class WorkflowNotificationService
    {
        private readonly NotificationContext _notifyContext;
        private readonly qHRMasterContext _hrmContext;
        private readonly SysConfigContext _sysConfigContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkflowNotificationService> _logger;

        public WorkflowNotificationService(
            NotificationContext notifyContext,
            qHRMasterContext hrmContext,
            SysConfigContext sysConfigContext,
            IConfiguration configuration,
            ILogger<WorkflowNotificationService> logger)
        {
            _notifyContext = notifyContext;
            _hrmContext = hrmContext;
            _sysConfigContext = sysConfigContext;
            _configuration = configuration;
            _logger = logger;
        }

        // =====================================================================
        // APPLICATION BASE URL CONFIGURATION
        // =====================================================================
        public string GetAppBaseUrl()
        {
            // 1. Check AppSettings:AppBaseUrl
            string baseUrl = _configuration["AppSettings:AppBaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl)) return baseUrl.TrimEnd('/');

            // 2. Check WorkflowEngine:CallbackBaseUrl
            baseUrl = _configuration["WorkflowEngine:CallbackBaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl)) return baseUrl.TrimEnd('/');

            // 3. Fallback default
            return "http://localhost:55465";
        }

        // =====================================================================
        // TEMPLATE RETRIEVAL & PLACEHOLDER REPLACEMENT
        // =====================================================================
        public async Task<(string Subject, string Body)> GetTemplateAsync(string templateCode, string defaultSubject, string defaultBody)
        {
            try
            {
                if (_notifyContext != null && _notifyContext.NtfMsgTmpl != null)
                {
                    var tmpl = await _notifyContext.NtfMsgTmpl
                        .FirstOrDefaultAsync(t => t.TmplCode == templateCode);

                    if (tmpl != null)
                    {
                        return (tmpl.MessageSubject ?? defaultSubject, tmpl.MessageBody ?? defaultBody);
                    }

                    // Auto-seed default template into DB if it doesn't exist
                    try
                    {
                        var newTmpl = new NtfMsgTmpl
                        {
                            TmplCode = templateCode,
                            TmplDesc = $"Template for {templateCode}",
                            MessageSubject = defaultSubject,
                            MessageBody = defaultBody,
                            CrtDateTime = DateTime.Now
                        };
                        _notifyContext.NtfMsgTmpl.Add(newTmpl);
                        await _notifyContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Could not auto-seed template {Code} into NTF_Msg_Tmpl.", templateCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to query NTF_Msg_Tmpl for {Code}, using default template.", templateCode);
            }

            return (defaultSubject, defaultBody);
        }

        public string RenderText(string templateText, Dictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(templateText)) return string.Empty;
            string result = templateText;
            foreach (var kvp in placeholders)
            {
                result = result.Replace("{" + kvp.Key + "}", kvp.Value ?? "");
            }
            return result;
        }

        // =====================================================================
        // OFFICIAL COMPANY EMAIL RESOLUTION
        // Strictly retrieves the official company email from HR_EmpJobAssign.Ofc_Email.
        // Personal emails (Psn_Email) or user profile emails are never used.
        // =====================================================================
        public async Task<string> GetOfficialEmailAsync(int empSysId, string fallbackName = null)
        {
            try
            {
                if (empSysId <= 0 && !string.IsNullOrWhiteSpace(fallbackName))
                {
                    var trimmed = fallbackName.Trim();
                    var emp = await _hrmContext.HrEmpGenInfo
                        .FirstOrDefaultAsync(e => (e.FirstName + " " + e.LastName).Trim() == trimmed || e.FullName == trimmed || e.EmpId == trimmed);
                    if (emp != null)
                    {
                        empSysId = emp.EmpSysId;
                    }
                }

                if (empSysId <= 0) return string.Empty;

                // 1. Active job assignment first (WkStat == 1) with non-empty OfcEmail
                var job = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == empSysId && j.WkStat == 1 && !string.IsNullOrEmpty(j.OfcEmail))
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();

                if (job != null && !string.IsNullOrWhiteSpace(job.OfcEmail))
                {
                    return job.OfcEmail.Trim();
                }

                // 2. Any job assignment record with non-empty OfcEmail
                job = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == empSysId && !string.IsNullOrEmpty(j.OfcEmail))
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();

                if (job != null && !string.IsNullOrWhiteSpace(job.OfcEmail))
                {
                    return job.OfcEmail.Trim();
                }

                _logger?.LogWarning("[WorkflowNotificationService] No Ofc_Email found in HR_EmpJobAssign for EmpSysId: {EmpSysId}.", empSysId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to retrieve Ofc_Email from HR_EmpJobAssign for EmpSysId: {EmpSysId}", empSysId);
            }

            return string.Empty;
        }

        // =====================================================================
        // 1. APPROVER NOTIFICATION (Step Reached)
        // Sends email to approver with direct action link & creates in-app notification
        // =====================================================================
        public async Task SendLeaveApprovalRequiredNotificationAsync(
            int lvId,
            int wfInstId,
            string stepName,
            int approverEmpSysId,
            string approverName,
            string approverEmail,
            HrLvRegister leaveApp,
            string lvTypeName,
            string requesterName,
            string requesterEmpId,
            bool sendEmail = false)
        {
            try
            {
                // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table.
                string officialEmail = await GetOfficialEmailAsync(approverEmpSysId, approverName);
                approverEmail = officialEmail;

                // Use AppSettings:AppBaseUrl explicitly for email action links so
                // links always point to the HR application and not the workflow engine.
                string appBaseUrl = _configuration["AppSettings:AppBaseUrl"]?.TrimEnd('/') ?? "http://localhost:55465";
                string actionUrl = $"{appBaseUrl}/Leave/HrLvRegister/Review?openLvId={lvId}&action=approve";

                string deptName = "—";
                try
                {
                    var job = await _hrmContext.HrEmpJobAssign
                        .Where(j => j.EmpSysId == leaveApp.EmpSysId && j.WkStat == 1)
                        .OrderByDescending(j => j.StDate)
                        .FirstOrDefaultAsync();
                    if (job != null && job.DeptId > 0)
                    {
                        var dept = await _hrmContext.OrgDepartment.FirstOrDefaultAsync(d => d.DeptId == job.DeptId);
                        if (dept != null) deptName = dept.NameEng;
                    }
                }
                catch { }

                var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "EmployeeName", requesterName },
                    { "Department", deptName },
                    { "EmpId", requesterEmpId },
                    { "LeaveType", lvTypeName },
                    { "StartDate", leaveApp.StDate.ToString("dd MMM yyyy") },
                    { "EndDate", leaveApp.EndDate.ToString("dd MMM yyyy") },
                    { "Days", leaveApp.Days.ToString("0.#") },
                    { "Reason", string.IsNullOrEmpty(leaveApp.Note) ? "—" : leaveApp.Note },
                    { "ApproverName", approverName },
                    { "StepName", stepName },
                    { "ActionUrl", actionUrl },
                    { "AppUrl", appBaseUrl }
                };

                // --- A. In-App Notification Center ---
                string inAppSubject = $"Leave Approval Required: {requesterName} - {lvTypeName}";
                string inAppMessage = $"{requesterName} requested {leaveApp.Days:0.#} day(s) of {lvTypeName} ({leaveApp.StDate:dd MMM} to {leaveApp.EndDate:dd MMM yyyy}). Action required: {stepName}.";
                await PushInAppNotificationAsync(approverEmpSysId, inAppSubject, inAppMessage, lvId, actionUrl);

                // --- B. Email Notification with Direct Action Link ---
                // NOTE: Approver emails are dispatched directly by the WorkflowEngine (QueueApprovalNotificationAsync)
                // to prevent duplicate approver notifications. Only dispatch SMTP email here if explicitly requested.
                if (sendEmail && !string.IsNullOrEmpty(approverEmail))
                {
                    var (tmplSubj, tmplBody) = await GetTemplateAsync(
                        "LEAVE_APPROVAL_REQ",
                        "Leave Approval Required: {LeaveType} – {EmployeeName} ({StepName})",
                        BuildDefaultApproverEmailTemplate());

                    string emailSubject = RenderText(tmplSubj, placeholders);

                    // Modify template body: hide direct link line by removing the hardcoded direct-link paragraph
                    string emailBody = RenderText(tmplBody, placeholders);
                    // Remove the 'Alternatively, you can copy and paste this direct link' paragraph if present
                    // This is a simple textual removal; templates from DB may vary so keep it defensive.
                    emailBody = emailBody.Replace("<p style='margin:0;font-size:13px;line-height:1.5;color:#64748b;text-align:center;'>\n              Alternatively, you can copy and paste this direct link into your browser:<br/>\n              <a href='{ActionUrl}' style='color:#540033;word-break:break-all;font-size:12px;'>{ActionUrl}</a>\n            </p>", "");

                    await SendAndLogEmailAsync(
                        lvId, wfInstId, approverEmpSysId, approverEmail,
                        "ApprovalRequired", emailSubject, emailBody);
                }
                else if (sendEmail)
                {
                    _logger?.LogWarning("[WorkflowNotificationService] Skipping approver email notification for {ApproverName} (EmpSysId: {EmpSysId}): No official Ofc_Email in HR_EmpJobAssign.", approverName, approverEmpSysId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send LeaveApprovalRequired notification for LvId: {LvId}", lvId);
            }
        }

        // =====================================================================
        // 2. EMPLOYEE STATUS NOTIFICATIONS (Submitted / Approved / Rejected)
        // =====================================================================
        public async Task SendLeaveSubmissionConfirmationAsync(
            int lvId,
            int wfInstId,
            int employeeEmpSysId,
            string employeeName,
            string employeeEmail,
            string lvTypeName,
            HrLvRegister leaveApp,
            string instanceNo)
        {
            try
            {
                // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table.
                string officialEmail = await GetOfficialEmailAsync(employeeEmpSysId, employeeName);
                employeeEmail = officialEmail;

                string appBaseUrl = GetAppBaseUrl();
                string viewUrl = $"{appBaseUrl}/Leave/HrLvRegister/Index?openLvId={lvId}";

                var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "EmployeeName", employeeName },
                    { "LeaveType", lvTypeName },
                    { "StartDate", leaveApp.StDate.ToString("dd MMM yyyy") },
                    { "EndDate", leaveApp.EndDate.ToString("dd MMM yyyy") },
                    { "Days", leaveApp.Days.ToString("0.#") },
                    { "ReferenceNo", instanceNo },
                    { "ActionUrl", viewUrl },
                    { "AppUrl", appBaseUrl }
                };

                // In-App Notification
                await PushInAppNotificationAsync(
                    employeeEmpSysId,
                    $"Leave Application Submitted: {lvTypeName}",
                    $"Your leave application for {leaveApp.Days:0.#} day(s) of {lvTypeName} from {leaveApp.StDate:dd MMM yyyy} to {leaveApp.EndDate:dd MMM yyyy} has been submitted (Ref: {instanceNo}).",
                    lvId, viewUrl);

                // Email Notification
                if (!string.IsNullOrEmpty(employeeEmail))
                {
                    var (tmplSubj, tmplBody) = await GetTemplateAsync(
                        "LEAVE_SUBMITTED",
                        "Leave Application Submitted – {LeaveType}",
                        BuildDefaultSubmissionEmailTemplate());

                    string emailSubject = RenderText(tmplSubj, placeholders);
                    string emailBody = RenderText(tmplBody, placeholders);

                    await SendAndLogEmailAsync(
                        lvId, wfInstId, employeeEmpSysId, employeeEmail,
                        "Submitted", emailSubject, emailBody);
                }
                else
                {
                    _logger?.LogWarning("[WorkflowNotificationService] Skipping employee submission confirmation email for {EmployeeName} (EmpSysId: {EmpSysId}): No official Ofc_Email in HR_EmpJobAssign.", employeeName, employeeEmpSysId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send LeaveSubmissionConfirmation for LvId: {LvId}", lvId);
            }
        }

        public async Task SendLeaveStatusUpdateNotificationAsync(
            int lvId,
            int wfInstId,
            string eventType, // Approved | Rejected | FinalApproved
            string approverName,
            string actionComment,
            int employeeEmpSysId,
            string employeeName,
            string employeeEmail,
            string lvTypeName,
            HrLvRegister leaveApp,
            bool sendEmail = false)
        {
            try
            {
                // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table.
                string officialEmail = await GetOfficialEmailAsync(employeeEmpSysId, employeeName);
                employeeEmail = officialEmail;

                string appBaseUrl = GetAppBaseUrl();
                string viewUrl = $"{appBaseUrl}/Leave/HrLvRegister/Index?openLvId={lvId}";

                string statusText = eventType switch
                {
                    "FinalApproved" => "Fully Approved",
                    "Approved" => $"Approved by {approverName}",
                    "Rejected" => $"Rejected by {approverName}",
                    _ => eventType
                };

                var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "EmployeeName", employeeName },
                    { "LeaveType", lvTypeName },
                    { "Status", statusText },
                    { "ActionBy", approverName },
                    { "Comment", string.IsNullOrEmpty(actionComment) ? "—" : actionComment },
                    { "StartDate", leaveApp?.StDate.ToString("dd MMM yyyy") ?? "" },
                    { "EndDate", leaveApp?.EndDate.ToString("dd MMM yyyy") ?? "" },
                    { "Days", leaveApp?.Days.ToString("0.#") ?? "" },
                    { "ActionUrl", viewUrl },
                    { "AppUrl", appBaseUrl }
                };

                // In-App Notification (Always created so user sees status change in web portal bell notification)
                await PushInAppNotificationAsync(
                    employeeEmpSysId,
                    $"Leave Application {statusText}: {lvTypeName}",
                    $"Your leave application for {lvTypeName} was {statusText}. Comments: {actionComment ?? "None"}",
                    lvId, viewUrl);

                // Email Notification
                // NOTE: Final decision emails (Approved/Rejected) are sent directly by WorkflowEngine
                // (QueueNotificationsForStatusChangeAsync) and intermediate approvals do not send emails to employees.
                // Only dispatch SMTP email here if explicitly requested.
                if (sendEmail && !string.IsNullOrEmpty(employeeEmail))
                {
                    var (tmplSubj, tmplBody) = await GetTemplateAsync(
                        "LEAVE_STATUS_UPDATE",
                        "Leave Application Update – {LeaveType} – {Status}",
                        BuildDefaultStatusUpdateEmailTemplate());

                    string emailSubject = RenderText(tmplSubj, placeholders);
                    string emailBody = RenderText(tmplBody, placeholders);

                    await SendAndLogEmailAsync(
                        lvId, wfInstId, employeeEmpSysId, employeeEmail,
                        eventType, emailSubject, emailBody);
                }
                else if (sendEmail)
                {
                    _logger?.LogWarning("[WorkflowNotificationService] Skipping employee status update email for {EmployeeName} (EmpSysId: {EmpSysId}): No official Ofc_Email in HR_EmpJobAssign.", employeeName, employeeEmpSysId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send LeaveStatusUpdate notification for LvId: {LvId}", lvId);
            }
        }

        // =====================================================================
        // 3. IN-APP NOTIFICATION CENTER INTEGRATION
        // =====================================================================
        public async Task PushInAppNotificationAsync(int empSysId, string subject, string message, int? reqId, string actionUrl = null)
        {
            if (empSysId <= 0) return;

            try
            {
                if (_notifyContext != null && _notifyContext.NtfNotification != null)
                {
                    var ntf = new NtfNotification
                    {
                        EmpSysId = empSysId,
                        Subject = subject,
                        Message = message,
                        ReqId = reqId,
                        NtfTypRefId = 1, // Leave
                        PushReadStat = false,
                        IsDelivered = 1,
                        CrtDateTime = DateTime.Now
                    };

                    _notifyContext.NtfNotification.Add(ntf);
                    await _notifyContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to save In-App notification for EmpSysId: {EmpSysId}", empSysId);
            }
        }

        public async Task<List<NotificationModelDto>> GetInAppNotificationsForUserAsync(int empSysId, int maxCount = 50)
        {
            if (empSysId <= 0 || _notifyContext == null || _notifyContext.NtfNotification == null)
                return new List<NotificationModelDto>();

            try
            {
                string appBaseUrl = GetAppBaseUrl();

                var query = await _notifyContext.NtfNotification
                    .Where(x => x.EmpSysId == empSysId)
                    .OrderByDescending(x => x.CrtDateTime)
                    .Take(maxCount)
                    .ToListAsync();

                return query.Select(x => new NotificationModelDto
                {
                    MsgId = x.MsgId,
                    ReqId = x.ReqId,
                    EmpSysId = x.EmpSysId,
                    Subject = x.Subject,
                    Message = x.Message,
                    PushReadStat = x.PushReadStat ?? false,
                    CrtDateTime = x.CrtDateTime,
                    ActionUrl = x.ReqId.HasValue && x.ReqId.Value > 0 
                        ? $"{appBaseUrl}/Leave/HrLvRegister/Index?openLvId={x.ReqId.Value}&action=approve" 
                        : "#",
                    TimeAgo = FormatTimeAgo(x.CrtDateTime)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to query notifications for EmpSysId: {EmpSysId}", empSysId);
                return new List<NotificationModelDto>();
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(int empSysId)
        {
            if (empSysId <= 0 || _notifyContext == null || _notifyContext.NtfNotification == null)
                return 0;

            try
            {
                return await _notifyContext.NtfNotification
                    .CountAsync(x => x.EmpSysId == empSysId && (x.PushReadStat == null || x.PushReadStat == false));
            }
            catch
            {
                return 0;
            }
        }

        public async Task MarkNotificationsAsReadAsync(int empSysId, int? msgId = null)
        {
            if (empSysId <= 0 || _notifyContext == null || _notifyContext.NtfNotification == null)
                return;

            try
            {
                var query = _notifyContext.NtfNotification.Where(x => x.EmpSysId == empSysId);
                if (msgId.HasValue && msgId.Value > 0)
                {
                    query = query.Where(x => x.MsgId == msgId.Value);
                }
                else
                {
                    query = query.Where(x => x.PushReadStat == null || x.PushReadStat == false);
                }

                var list = await query.ToListAsync();
                foreach (var item in list)
                {
                    item.PushReadStat = true;
                }

                await _notifyContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to mark notifications as read for EmpSysId: {EmpSysId}", empSysId);
            }
        }

        // =====================================================================
        // 4. EMAIL DISPATCH & AUDIT LOGGING
        // =====================================================================
        private async Task SendAndLogEmailAsync(
            int lvId,
            int wfInstId,
            int toEmpSysId,
            string toEmail,
            string eventType,
            string subject,
            string body)
        {
            // STRICT REQUIREMENT: Only pick Ofc_Email from HR_EmpJobAssign table.
            // As the central email gatekeeper, resolve official company email if toEmpSysId is known.
            if (toEmpSysId > 0)
            {
                string officialEmail = await GetOfficialEmailAsync(toEmpSysId);
                toEmail = officialEmail;
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger?.LogWarning("[WorkflowNotificationService] Aborting email dispatch: EmpSysId {ToEmpSysId} has no official company email (Ofc_Email) in HR_EmpJobAssign.", toEmpSysId);
                return;
            }

            var logEntry = new HrLvWfNotifyLog
            {
                LvId = lvId,
                WfInstId = wfInstId,
                EventType = eventType,
                ToEmpSysId = toEmpSysId,
                ToEmail = toEmail,
                Subject = subject,
                Body = body,
                IsSent = false,
                RetryCount = 0,
                CrtDateTime = DateTime.Now
            };

            try
            {
                _hrmContext.HrLvWfNotifyLog.Add(logEntry);
                await _hrmContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not insert initial HrLvWfNotifyLog record.");
            }

            try
            {
                int result = await _sysConfigContext.SendEmailNotificationAsync(
                    toEmail, string.Empty, subject, body, string.Empty);

                if (result == 1)
                {
                    logEntry.IsSent = true;
                    logEntry.SentDateTime = DateTime.Now;
                }
                else
                {
                    logEntry.ErrorMsg = "SendEmailNotificationAsync returned 0 (Failed).";
                    logEntry.RetryCount++;
                }
            }
            catch (Exception ex)
            {
                logEntry.ErrorMsg = ex.Message.Length > 490 ? ex.Message[..490] : ex.Message;
                logEntry.RetryCount++;
            }

            try
            {
                await _hrmContext.SaveChangesAsync();
            }
            catch { }
        }

        private static string FormatTimeAgo(DateTime? dt)
        {
            if (!dt.HasValue) return "Recently";
            var span = DateTime.Now - dt.Value;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return dt.Value.ToString("dd MMM yyyy");
        }

        // =====================================================================
        // 5. DEFAULT RESPONSIVE HTML EMAIL TEMPLATES
        // =====================================================================
        private static string BuildDefaultApproverEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<meta name='viewport' content='width=device-width, initial-scale=1.0'/>
<title>Leave Approval Required</title>
</head>
<body style='margin:0;padding:0;background-color:#f8fafc;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;color:#1e293b;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f8fafc;padding:30px 15px;'>
  <tr>
    <td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:12px;box-shadow:0 4px 20px rgba(0,0,0,0.06);border:1px solid #e2e8f0;overflow:hidden;max-width:600px;width:100%;'>
        
        <!-- Header -->
        <tr>
          <td style='background:linear-gradient(135deg, #540033 0%, #8a0c56 100%);padding:30px 35px;text-align:left;'>
            <h1 style='margin:0;font-size:22px;color:#ffffff;font-weight:700;letter-spacing:-0.5px;'>Leave Approval Required</h1>
            <p style='margin:6px 0 0 0;color:rgba(255,255,255,0.85);font-size:14px;'>Workflow Step: <strong>{StepName}</strong></p>
          </td>
        </tr>

        <!-- Content -->
        <tr>
          <td style='padding:32px 35px;'>
            <p style='margin:0 0 16px 0;font-size:15px;line-height:1.6;color:#334155;'>
              Dear <strong>{ApproverName}</strong>,
            </p>
            <p style='margin:0 0 24px 0;font-size:15px;line-height:1.6;color:#334155;'>
              A leave request has reached your approval queue in the workflow engine and requires your action.
            </p>

            <!-- Details Card -->
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f8fafc;border-radius:8px;border:1px solid #e2e8f0;margin-bottom:28px;'>
              <tr>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#64748b;font-weight:600;width:35%;'>Employee Name</td>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#0f172a;font-weight:700;'>{EmployeeName} <span style='font-weight:normal;color:#64748b;'>(#{EmpId})</span></td>
              </tr>
              <tr>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#64748b;font-weight:600;'>Department</td>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#0f172a;'>{Department}</td>
              </tr>
              <tr>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#64748b;font-weight:600;'>Leave Type</td>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#0f172a;font-weight:700;'>{LeaveType}</td>
              </tr>
              <tr>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#64748b;font-weight:600;'>Duration</td>
                <td style='padding:14px 18px;border-bottom:1px solid #e2e8f0;font-size:14px;color:#0f172a;'><strong>{StartDate}</strong> to <strong>{EndDate}</strong> ({Days} day(s))</td>
              </tr>
              <tr>
                <td style='padding:14px 18px;font-size:14px;color:#64748b;font-weight:600;'>Reason / Comments</td>
                <td style='padding:14px 18px;font-size:14px;color:#334155;'>{Reason}</td>
              </tr>
            </table>

            <!-- Direct Action Button -->
            <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:28px;'>
              <tr>
                <td align='center'>
                  <a href='{ActionUrl}' target='_blank' style='display:inline-block;background:#540033;color:#ffffff;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:700;font-size:15px;box-shadow:0 4px 12px rgba(84,0,51,0.25);'>
                    Open &amp; Review Leave Request
                  </a>
                </td>
              </tr>
            </table>

            <p style='margin:0;font-size:13px;line-height:1.5;color:#64748b;text-align:center;'>
              Alternatively, you can copy and paste this direct link into your browser:<br/>
              <a href='{ActionUrl}' style='color:#540033;word-break:break-all;font-size:12px;'>{ActionUrl}</a>
            </p>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style='background-color:#f1f5f9;padding:20px 35px;text-align:center;border-top:1px solid #e2e8f0;font-size:12px;color:#94a3b8;'>
            This is an automated workflow notification from <strong>QCS Workflow Engine</strong>.<br/>
            Please do not reply directly to this email.
          </td>
        </tr>

      </table>
    </td>
  </tr>
</table>
</body>
</html>";
        }

        private static string BuildDefaultSubmissionEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/><title>Leave Application Submitted</title></head>
<body style='margin:0;padding:0;background-color:#f8fafc;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif;color:#1e293b;'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 15px;'>
  <tr>
    <td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#fff;border-radius:12px;border:1px solid #e2e8f0;overflow:hidden;'>
        <tr>
          <td style='background:#540033;padding:25px 30px;color:#fff;'>
            <h2 style='margin:0;font-size:20px;'>Leave Application Submitted</h2>
          </td>
        </tr>
        <tr>
          <td style='padding:25px 30px;'>
            <p>Dear <strong>{EmployeeName}</strong>,</p>
            <p>Your <strong>{LeaveType}</strong> request has been successfully submitted and forwarded to the approval workflow.</p>
            <table width='100%' style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;padding:12px;margin:15px 0;'>
              <tr><td style='padding:6px;'><strong>Reference No:</strong></td><td>{ReferenceNo}</td></tr>
              <tr><td style='padding:6px;'><strong>Period:</strong></td><td>{StartDate} – {EndDate} ({Days} day(s))</td></tr>
              <tr><td style='padding:6px;'><strong>Status:</strong></td><td><span style='color:#2563eb;font-weight:bold;'>Leave Initiated</span></td></tr>
            </table>
            <p style='text-align:center;margin-top:20px;'>
              <a href='{ActionUrl}' style='background:#540033;color:#fff;padding:10px 24px;text-decoration:none;border-radius:6px;font-weight:600;'>View Application Status</a>
            </p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
</body>
</html>";
        }

        private static string BuildDefaultStatusUpdateEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/><title>Leave Status Update</title></head>
<body style='margin:0;padding:0;background-color:#f8fafc;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif;color:#1e293b;'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 15px;'>
  <tr>
    <td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#fff;border-radius:12px;border:1px solid #e2e8f0;overflow:hidden;'>
        <tr>
          <td style='background:#540033;padding:25px 30px;color:#fff;'>
            <h2 style='margin:0;font-size:20px;'>Leave Application Status Update</h2>
          </td>
        </tr>
        <tr>
          <td style='padding:25px 30px;'>
            <p>Dear <strong>{EmployeeName}</strong>,</p>
            <p>Your <strong>{LeaveType}</strong> application status has been updated:</p>
            <table width='100%' style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;padding:12px;margin:15px 0;'>
              <tr><td style='padding:6px;'><strong>Status:</strong></td><td><strong>{Status}</strong></td></tr>
              <tr><td style='padding:6px;'><strong>Action By:</strong></td><td>{ActionBy}</td></tr>
              <tr><td style='padding:6px;'><strong>Period:</strong></td><td>{StartDate} – {EndDate} ({Days} day(s))</td></tr>
              <tr><td style='padding:6px;'><strong>Comments:</strong></td><td>{Comment}</td></tr>
            </table>
            <p style='text-align:center;margin-top:20px;'>
              <a href='{ActionUrl}' style='background:#540033;color:#fff;padding:10px 24px;text-decoration:none;border-radius:6px;font-weight:600;'>View Leave Details</a>
            </p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
</body>
</html>";
        }
    }
}
