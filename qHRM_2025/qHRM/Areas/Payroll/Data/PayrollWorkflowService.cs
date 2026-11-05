using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using qHRM.Area.Config;
using qHRM.Areas.Notify.Models;
using qHRM.Areas.Payroll.Models;
using qHRM.Models;

namespace qHRM.Areas.Payroll.Data
{
    public class PayrollWorkflowService
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly PayrollContext _payrollContext;
        private readonly SysConfigContext _sysConfigContext;
        private readonly NotificationContext _notifyContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayrollWorkflowService> _logger;

        public PayrollWorkflowService(
            qHRMasterContext hrmContext,
            PayrollContext payrollContext,
            SysConfigContext sysConfigContext,
            NotificationContext notifyContext,
            IConfiguration configuration,
            ILogger<PayrollWorkflowService> logger)
        {
            _hrmContext = hrmContext;
            _payrollContext = payrollContext;
            _sysConfigContext = sysConfigContext;
            _notifyContext = notifyContext;
            _configuration = configuration;
            _logger = logger;
        }

        public string GetAppBaseUrl()
        {
            string baseUrl = _configuration["AppSettings:AppBaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl)) return baseUrl.TrimEnd('/');

            baseUrl = _configuration["WorkflowEngine:CallbackBaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl)) return baseUrl.TrimEnd('/');

            return "http://localhost:55465";
        }

        public async Task<(string Name, string Email)> GetEmployeeDetailsAsync(int empSysId)
        {
            string name = $"Employee #{empSysId}";
            string email = string.Empty;

            try
            {
                var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == empSysId);
                if (emp != null)
                {
                    name = $"{emp.FirstName?.Trim()} {emp.LastName?.Trim()}".Trim();
                }

                // Official email from active job assignment first
                var job = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == empSysId && j.WkStat == 1 && !string.IsNullOrEmpty(j.OfcEmail))
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();

                if (job != null && !string.IsNullOrWhiteSpace(job.OfcEmail))
                {
                    email = job.OfcEmail.Trim();
                }
                else
                {
                    job = await _hrmContext.HrEmpJobAssign
                        .Where(j => j.EmpSysId == empSysId && !string.IsNullOrEmpty(j.OfcEmail))
                        .OrderByDescending(j => j.StDate)
                        .FirstOrDefaultAsync();
                    if (job != null && !string.IsNullOrWhiteSpace(job.OfcEmail))
                    {
                        email = job.OfcEmail.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to resolve details for EmpSysId: {EmpSysId}", empSysId);
            }

            return (name, email);
        }

        public async Task<(int InitiatorId, int Level1Id, int FinalId)> GetWorkflowApproversAsync(string wfType)
        {
            // Default configured roles
            int initiatorId = 6;  // Kaushalya Fonseka
            int level1Id = 452;   // Ashhar Zaeem
            int finalId = 11;     // Hasan Sayed

            try
            {
                var cfg = await _hrmContext.HrLvWfConfig
                    .FirstOrDefaultAsync(c => c.WfType == wfType && c.RecStat == 1 && c.IsEnabled);

                if (cfg != null && !string.IsNullOrWhiteSpace(cfg.ApproverConfig))
                {
                    using var doc = JsonDocument.Parse(cfg.ApproverConfig);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("InitiatorEmpId", out var initElem) && initElem.TryGetInt32(out int iVal) && iVal > 0)
                        initiatorId = iVal;

                    if (root.TryGetProperty("Level1ApproverEmpId", out var l1Elem) && l1Elem.TryGetInt32(out int l1Val) && l1Val > 0)
                        level1Id = l1Val;

                    if (root.TryGetProperty("FinalApproverEmpId", out var fnElem) && fnElem.TryGetInt32(out int fnVal) && fnVal > 0)
                        finalId = fnVal;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not load approvers for workflow type: {WfType}", wfType);
            }

            return (initiatorId, level1Id, finalId);
        }

        // =====================================================================
        // SALARY PROCESS WORKFLOW
        // =====================================================================

        public async Task<(bool Success, string Message)> SubmitSalaryProcessAsync(int prcId, int actorEmpSysId)
        {
            try
            {
                var hdr = await _hrmContext.HrPrSlryPrcHdr.FirstOrDefaultAsync(h => h.PrcId == prcId);
                if (hdr == null)
                    return (false, "Salary process record not found.");

                if (hdr.PrcStat != 2)
                    return (false, "Salary Process must be verified before submitting for approval.");

                var approvers = await GetWorkflowApproversAsync("Salary Process");

                // Validate initiator
                if (actorEmpSysId != approvers.InitiatorId && actorEmpSysId != 1)
                {
                    return (false, "Only the designated initiator can submit the Salary Process for approval.");
                }

                var (initiatorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);
                var (l1Name, l1Email) = await GetEmployeeDetailsAsync(approvers.Level1Id);

                hdr.WfStat = "PendingLevel1";
                hdr.WfSubmittedBy = actorEmpSysId;
                hdr.WfSubmittedDateTime = DateTime.Now;
                hdr.WfComments = null;

                // Log Workflow History
                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Salary Process",
                    RefId = prcId,
                    PrcYear = hdr.EftYear,
                    PrcMonth = hdr.EftMonth,
                    StepNumber = 1,
                    StepName = "Salary Process Submission",
                    Action = "Submitted",
                    ActorEmpSysId = actorEmpSysId,
                    ActorName = initiatorName,
                    Comments = "Verified and submitted for Level 1 Approval.",
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                // Send email & in-app notification to Level 1 Approver (Ashhar Zaeem)
                string monthName = new DateTime(hdr.EftYear, hdr.EftMonth, 1).ToString("MMMM");
                string periodText = $"{monthName} {hdr.EftYear}";
                string appBaseUrl = GetAppBaseUrl();
                string approvalUrl = $"{appBaseUrl}/Payroll/HrPrSlryPrcApproval?prcId={prcId}";

                string subject = $"Salary Process Approval Required: {periodText} (Level 1 Review)";
                string body = BuildWorkflowEmailHtml(
                    title: "Salary Process – Level 1 Approval Required",
                    heading: $"Action Required: Salary Process for {periodText}",
                    periodText: periodText,
                    statusText: "Pending Level 1 Approval",
                    actorText: $"Submitted by {initiatorName}",
                    comments: "The monthly salary process has been completed and verified. Please review the details, staff salary report, and summary sheet, and provide your approval.",
                    actionButtonText: "Open Salary Process for Approval",
                    actionUrl: approvalUrl);

                await SendEmailSafelyAsync(l1Email, subject, body);
                await PushInAppNotificationSafelyAsync(approvers.Level1Id, subject, $"Salary process for {periodText} is pending your Level 1 approval.", prcId, approvalUrl);

                return (true, "Salary process submitted successfully for Level 1 approval.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error submitting salary process for PrcId: {PrcId}", prcId);
                return (false, "Error submitting salary process: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> ApproveSalaryProcessAsync(int prcId, int actorEmpSysId, string comments)
        {
            try
            {
                var hdr = await _hrmContext.HrPrSlryPrcHdr.FirstOrDefaultAsync(h => h.PrcId == prcId);
                if (hdr == null)
                    return (false, "Salary process record not found.");

                var approvers = await GetWorkflowApproversAsync("Salary Process");
                var (actorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);
                string monthName = new DateTime(hdr.EftYear, hdr.EftMonth, 1).ToString("MMMM");
                string periodText = $"{monthName} {hdr.EftYear}";
                string appBaseUrl = GetAppBaseUrl();
                string approvalUrl = $"{appBaseUrl}/Payroll/HrPrSlryPrcApproval?prcId={prcId}";

                if (hdr.WfStat == "PendingLevel1")
                {
                    // Level 1 Approval by Ashhar Zaeem
                    if (actorEmpSysId != approvers.Level1Id && actorEmpSysId != 1)
                    {
                        return (false, $"Only the Level 1 Approver (EmpSysId: {approvers.Level1Id}) can perform Level 1 approval.");
                    }

                    hdr.WfStat = "PendingFinal";
                    hdr.WfComments = comments;

                    _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                    {
                        WfType = "Salary Process",
                        RefId = prcId,
                        PrcYear = hdr.EftYear,
                        PrcMonth = hdr.EftMonth,
                        StepNumber = 2,
                        StepName = "Level 1 Approval",
                        Action = "Approved",
                        ActorEmpSysId = actorEmpSysId,
                        ActorName = actorName,
                        Comments = string.IsNullOrWhiteSpace(comments) ? "Approved at Level 1." : comments,
                        ActionDateTime = DateTime.Now
                    });

                    await _hrmContext.SaveChangesAsync();

                    // Send email & in-app notification to Final Approver (Hasan Sayed)
                    var (finalName, finalEmail) = await GetEmployeeDetailsAsync(approvers.FinalId);
                    string subject = $"Salary Process Final Approval Required: {periodText}";
                    string body = BuildWorkflowEmailHtml(
                        title: "Salary Process – Final Approval Required",
                        heading: $"Action Required: Final Approval for {periodText}",
                        periodText: periodText,
                        statusText: "Pending Final Approval",
                        actorText: $"Approved at Level 1 by {actorName}",
                        comments: string.IsNullOrWhiteSpace(comments) ? "Level 1 review completed. Awaiting your final sign-off." : $"Level 1 Comments: {comments}",
                        actionButtonText: "Open Salary Process for Final Approval",
                        actionUrl: approvalUrl);

                    await SendEmailSafelyAsync(finalEmail, subject, body);
                    await PushInAppNotificationSafelyAsync(approvers.FinalId, subject, $"Salary process for {periodText} was approved at Level 1 by {actorName} and requires your final approval.", prcId, approvalUrl);

                    return (true, $"Level 1 approval completed. Forwarded to {finalName} for final approval.");
                }
                else if (hdr.WfStat == "PendingFinal")
                {
                    // Final Approval by Hasan Sayed
                    if (actorEmpSysId != approvers.FinalId && actorEmpSysId != 1)
                    {
                        return (false, $"Only the Final Approver (EmpSysId: {approvers.FinalId}) can perform Final approval.");
                    }

                    hdr.WfStat = "Approved";
                    hdr.PrcStat = 3; // Completed!
                    hdr.CmpdBy = actorEmpSysId;
                    hdr.CmpdDateTime = DateTime.Now;
                    hdr.WfApprovedBy = actorEmpSysId;
                    hdr.WfApprovedDateTime = DateTime.Now;
                    hdr.WfComments = comments;

                    _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                    {
                        WfType = "Salary Process",
                        RefId = prcId,
                        PrcYear = hdr.EftYear,
                        PrcMonth = hdr.EftMonth,
                        StepNumber = 3,
                        StepName = "Final Approval",
                        Action = "Approved",
                        ActorEmpSysId = actorEmpSysId,
                        ActorName = actorName,
                        Comments = string.IsNullOrWhiteSpace(comments) ? "Final approval granted." : comments,
                        ActionDateTime = DateTime.Now
                    });

                    await _hrmContext.SaveChangesAsync();

                    // Auto-generate payslips for the approved payroll
                    try
                    {
                        var lstSalDtl = _payrollContext.GetSalaryDtl(hdr.EntId, "_paysheet", 0, hdr.EftMonth, hdr.EftYear);
                        foreach (var itm in lstSalDtl)
                        {
                            _payrollContext.GetPaySlip(itm.Emp_Id, hdr.EftYear, hdr.EftMonth, hdr.EntId);
                        }
                    }
                    catch (Exception genEx)
                    {
                        _logger?.LogWarning(genEx, "Payslip generation warning for PrcId {PrcId}", prcId);
                    }

                    // Notify Initiator (Kaushalya) and Level 1 (Ashhar)
                    var (_, initEmail) = await GetEmployeeDetailsAsync(approvers.InitiatorId);
                    var (_, l1Email) = await GetEmployeeDetailsAsync(approvers.Level1Id);

                    string subject = $"Salary Process Approved: {periodText}";
                    string body = BuildWorkflowEmailHtml(
                        title: "Salary Process Approved",
                        heading: $"Salary Process for {periodText} is Approved",
                        periodText: periodText,
                        statusText: "Approved (Completed)",
                        actorText: $"Final Approval granted by {actorName}",
                        comments: string.IsNullOrWhiteSpace(comments) ? "The salary process has received final approval. Payslip printing and emailing are now enabled." : $"Final Approver Comments: {comments}<br/>Payslip printing and emailing are now enabled.",
                        actionButtonText: "View Salary Process",
                        actionUrl: approvalUrl);

                    await SendEmailSafelyAsync(initEmail, subject, body);
                    await SendEmailSafelyAsync(l1Email, subject, body);
                    await PushInAppNotificationSafelyAsync(approvers.InitiatorId, subject, $"Salary process for {periodText} received final approval from {actorName}. Payslips are now active.", prcId, approvalUrl);
                    await PushInAppNotificationSafelyAsync(approvers.Level1Id, subject, $"Salary process for {periodText} received final approval from {actorName}.", prcId, approvalUrl);

                    return (true, $"Final approval completed successfully. Salary process is now marked Completed and payslip actions are unlocked.");
                }
                else
                {
                    return (false, $"Cannot approve salary process in state '{hdr.WfStat}'.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error approving salary process for PrcId: {PrcId}", prcId);
                return (false, "Error approving salary process: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> RejectSalaryProcessAsync(int prcId, int actorEmpSysId, string comments)
        {
            try
            {
                var hdr = await _hrmContext.HrPrSlryPrcHdr.FirstOrDefaultAsync(h => h.PrcId == prcId);
                if (hdr == null)
                    return (false, "Salary process record not found.");

                if (string.IsNullOrWhiteSpace(comments))
                    return (false, "Please provide rejection comments explaining why the process is rejected.");

                var approvers = await GetWorkflowApproversAsync("Salary Process");

                // Can only be rejected by L1 approver or Final approver or Admin
                if (actorEmpSysId != approvers.Level1Id && actorEmpSysId != approvers.FinalId && actorEmpSysId != 1)
                {
                    return (false, "You are not authorized to reject this Salary Process.");
                }

                var (actorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);
                string previousState = hdr.WfStat;

                hdr.WfStat = "Rejected";
                hdr.PrcStat = 1; // Revert back to Process so changes/re-calculations can be made
                hdr.WfComments = comments;

                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Salary Process",
                    RefId = prcId,
                    PrcYear = hdr.EftYear,
                    PrcMonth = hdr.EftMonth,
                    StepNumber = previousState == "PendingFinal" ? 3 : 2,
                    StepName = previousState == "PendingFinal" ? "Final Approval" : "Level 1 Approval",
                    Action = "Rejected",
                    ActorEmpSysId = actorEmpSysId,
                    ActorName = actorName,
                    Comments = comments,
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                // Notify Initiator (Kaushalya)
                var (initName, initEmail) = await GetEmployeeDetailsAsync(approvers.InitiatorId);
                string monthName = new DateTime(hdr.EftYear, hdr.EftMonth, 1).ToString("MMMM");
                string periodText = $"{monthName} {hdr.EftYear}";
                string appBaseUrl = GetAppBaseUrl();
                string reprocessUrl = $"{appBaseUrl}/Payroll/HrPrSlryPrcDtl";

                string subject = $"Salary Process Rejected: {periodText}";
                string body = BuildWorkflowEmailHtml(
                    title: "Salary Process Rejected",
                    heading: $"Salary Process for {periodText} was Rejected",
                    periodText: periodText,
                    statusText: "Rejected",
                    actorText: $"Rejected by {actorName}",
                    comments: $"<strong>Reason for Rejection:</strong><br/>{comments}<br/><br/>The salary process has been returned to processing status. Please make any required corrections, re-process, verify, and re-submit for approval.",
                    actionButtonText: "Open Salary Processing Screen",
                    actionUrl: reprocessUrl);

                await SendEmailSafelyAsync(initEmail, subject, body);
                await PushInAppNotificationSafelyAsync(approvers.InitiatorId, subject, $"Salary process for {periodText} was rejected by {actorName}: {comments}", prcId, reprocessUrl);

                return (true, $"Salary process has been rejected and returned to {initName} for review.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rejecting salary process for PrcId: {PrcId}", prcId);
                return (false, "Error rejecting salary process: " + ex.Message);
            }
        }

        // =====================================================================
        // STAFF LEAVE SALARY & AIR TICKET ENCASHMENT WORKFLOW
        // =====================================================================

        public async Task<HrPrStaffLvTktEncashHdr> GetOrCreateEncashmentHdrAsync(int year, int month)
        {
            var hdr = await _hrmContext.HrPrStaffLvTktEncashHdr
                .FirstOrDefaultAsync(h => h.PrcYear == year && h.PrcMonth == month);

            if (hdr == null)
            {
                hdr = new HrPrStaffLvTktEncashHdr
                {
                    PrcYear = year,
                    PrcMonth = month,
                    StaffCategory = "ALL",
                    PrcStat = 1,
                    WfStat = "Draft",
                    InitBy = 6,
                    InitDateTime = DateTime.Now
                };
                _hrmContext.HrPrStaffLvTktEncashHdr.Add(hdr);
                await _hrmContext.SaveChangesAsync();
            }

            return hdr;
        }

        public async Task<(bool Success, string Message)> SubmitEncashmentAsync(
            int year, int month, int actorEmpSysId,
            decimal inLv, decimal inTkt, decimal outLv, decimal outTkt)
        {
            try
            {
                var hdr = await GetOrCreateEncashmentHdrAsync(year, month);
                var approvers = await GetWorkflowApproversAsync("Leave Salary & Air Ticket Encashment");

                if (actorEmpSysId != approvers.InitiatorId && actorEmpSysId != 1)
                {
                    return (false, "Only the designated initiator can submit Encashments for approval.");
                }

                var (initiatorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);
                var (l1Name, l1Email) = await GetEmployeeDetailsAsync(approvers.Level1Id);

                hdr.TotalInternalLeave = inLv;
                hdr.TotalInternalTicket = inTkt;
                hdr.TotalInternalSettlement = inLv + inTkt;
                hdr.TotalOutsourceLeave = outLv;
                hdr.TotalOutsourceTicket = outTkt;
                hdr.TotalOutsourceSettlement = outLv + outTkt;
                hdr.GrandTotalSettlement = hdr.TotalInternalSettlement + hdr.TotalOutsourceSettlement;
                hdr.PrcStat = 2; // Verified
                hdr.WfStat = "PendingLevel1";
                hdr.WfSubmittedBy = actorEmpSysId;
                hdr.WfSubmittedDateTime = DateTime.Now;

                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Leave Salary & Air Ticket Encashment",
                    RefId = hdr.EncashHdrId,
                    PrcYear = year,
                    PrcMonth = month,
                    StepNumber = 1,
                    StepName = "Encashment Submission",
                    Action = "Submitted",
                    ActorEmpSysId = actorEmpSysId,
                    ActorName = initiatorName,
                    Comments = "Encashment batch submitted for Level 1 Approval.",
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                string monthName = new DateTime(year, month, 1).ToString("MMMM");
                string periodText = $"{monthName} {year}";
                string appBaseUrl = GetAppBaseUrl();
                string approvalUrl = $"{appBaseUrl}/Payroll/HrPrStaffLvTktEncashApproval?year={year}&month={month}";

                string subject = $"Staff Leave & Ticket Encashment Approval Required: {periodText} (Level 1 Review)";
                string body = BuildWorkflowEmailHtml(
                    title: "Leave Salary & Air Ticket Encashment – Level 1 Approval Required",
                    heading: $"Action Required: Encashment Approval for {periodText}",
                    periodText: periodText,
                    statusText: "Pending Level 1 Approval",
                    actorText: $"Submitted by {initiatorName}",
                    comments: $"Total Settlement: QAR {hdr.GrandTotalSettlement:N2}<br/>Internal: QAR {hdr.TotalInternalSettlement:N2} | Outsource: QAR {hdr.TotalOutsourceSettlement:N2}<br/>Please review and approve.",
                    actionButtonText: "Open Encashment Batch for Approval",
                    actionUrl: approvalUrl);

                await SendEmailSafelyAsync(l1Email, subject, body);
                await PushInAppNotificationSafelyAsync(approvers.Level1Id, subject, $"Leave Salary & Air Ticket Encashment for {periodText} requires your Level 1 approval.", hdr.EncashHdrId, approvalUrl);

                return (true, "Encashment batch submitted successfully for Level 1 approval.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error submitting encashment for Year: {Year}, Month: {Month}", year, month);
                return (false, "Error submitting encashment: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> ApproveEncashmentAsync(int year, int month, int actorEmpSysId, string comments)
        {
            try
            {
                var hdr = await _hrmContext.HrPrStaffLvTktEncashHdr
                    .FirstOrDefaultAsync(h => h.PrcYear == year && h.PrcMonth == month);

                if (hdr == null)
                    return (false, "Encashment record not found.");

                var approvers = await GetWorkflowApproversAsync("Leave Salary & Air Ticket Encashment");
                var (actorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);
                string monthName = new DateTime(year, month, 1).ToString("MMMM");
                string periodText = $"{monthName} {year}";
                string appBaseUrl = GetAppBaseUrl();
                string approvalUrl = $"{appBaseUrl}/Payroll/HrPrStaffLvTktEncashApproval?year={year}&month={month}";

                if (hdr.WfStat == "PendingLevel1")
                {
                    if (actorEmpSysId != approvers.Level1Id && actorEmpSysId != 1)
                        return (false, $"Only the Level 1 Approver (EmpSysId: {approvers.Level1Id}) can perform Level 1 approval.");

                    hdr.WfStat = "PendingFinal";

                    _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                    {
                        WfType = "Leave Salary & Air Ticket Encashment",
                        RefId = hdr.EncashHdrId,
                        PrcYear = year,
                        PrcMonth = month,
                        StepNumber = 2,
                        StepName = "Level 1 Approval",
                        Action = "Approved",
                        ActorEmpSysId = actorEmpSysId,
                        ActorName = actorName,
                        Comments = string.IsNullOrWhiteSpace(comments) ? "Approved at Level 1." : comments,
                        ActionDateTime = DateTime.Now
                    });

                    await _hrmContext.SaveChangesAsync();

                    var (finalName, finalEmail) = await GetEmployeeDetailsAsync(approvers.FinalId);
                    string subject = $"Staff Leave & Ticket Encashment Final Approval Required: {periodText}";
                    string body = BuildWorkflowEmailHtml(
                        title: "Encashment – Final Approval Required",
                        heading: $"Action Required: Final Approval for {periodText}",
                        periodText: periodText,
                        statusText: "Pending Final Approval",
                        actorText: $"Approved at Level 1 by {actorName}",
                        comments: string.IsNullOrWhiteSpace(comments) ? "Level 1 review completed. Awaiting your final sign-off." : $"Level 1 Comments: {comments}",
                        actionButtonText: "Open Encashment for Final Approval",
                        actionUrl: approvalUrl);

                    await SendEmailSafelyAsync(finalEmail, subject, body);
                    await PushInAppNotificationSafelyAsync(approvers.FinalId, subject, $"Leave Salary & Air Ticket Encashment for {periodText} requires your final approval.", hdr.EncashHdrId, approvalUrl);

                    return (true, $"Level 1 approval completed. Forwarded to {finalName} for final approval.");
                }
                else if (hdr.WfStat == "PendingFinal")
                {
                    if (actorEmpSysId != approvers.FinalId && actorEmpSysId != 1)
                        return (false, $"Only the Final Approver (EmpSysId: {approvers.FinalId}) can perform Final approval.");

                    hdr.WfStat = "Approved";
                    hdr.PrcStat = 3; // Approved / Completed
                    hdr.WfApprovedBy = actorEmpSysId;
                    hdr.WfApprovedDateTime = DateTime.Now;

                    _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                    {
                        WfType = "Leave Salary & Air Ticket Encashment",
                        RefId = hdr.EncashHdrId,
                        PrcYear = year,
                        PrcMonth = month,
                        StepNumber = 3,
                        StepName = "Final Approval",
                        Action = "Approved",
                        ActorEmpSysId = actorEmpSysId,
                        ActorName = actorName,
                        Comments = string.IsNullOrWhiteSpace(comments) ? "Final approval granted." : comments,
                        ActionDateTime = DateTime.Now
                    });

                    await _hrmContext.SaveChangesAsync();

                    var (_, initEmail) = await GetEmployeeDetailsAsync(approvers.InitiatorId);
                    var (_, l1Email) = await GetEmployeeDetailsAsync(approvers.Level1Id);

                    string subject = $"Staff Leave & Ticket Encashment Approved: {periodText}";
                    string body = BuildWorkflowEmailHtml(
                        title: "Encashment Approved",
                        heading: $"Encashment Batch for {periodText} is Approved",
                        periodText: periodText,
                        statusText: "Approved (Completed)",
                        actorText: $"Final Approval granted by {actorName}",
                        comments: string.IsNullOrWhiteSpace(comments) ? "The encashment batch has received final approval." : $"Final Approver Comments: {comments}",
                        actionButtonText: "View Encashment Details",
                        actionUrl: approvalUrl);

                    await SendEmailSafelyAsync(initEmail, subject, body);
                    await SendEmailSafelyAsync(l1Email, subject, body);
                    await PushInAppNotificationSafelyAsync(approvers.InitiatorId, subject, $"Leave Salary & Air Ticket Encashment for {periodText} was approved by {actorName}.", hdr.EncashHdrId, approvalUrl);
                    await PushInAppNotificationSafelyAsync(approvers.Level1Id, subject, $"Leave Salary & Air Ticket Encashment for {periodText} was approved by {actorName}.", hdr.EncashHdrId, approvalUrl);

                    return (true, "Final approval completed successfully.");
                }
                else
                {
                    return (false, $"Cannot approve encashment in state '{hdr.WfStat}'.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error approving encashment for Year: {Year}, Month: {Month}", year, month);
                return (false, "Error approving encashment: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> RejectEncashmentAsync(int year, int month, int actorEmpSysId, string comments)
        {
            try
            {
                var hdr = await _hrmContext.HrPrStaffLvTktEncashHdr
                    .FirstOrDefaultAsync(h => h.PrcYear == year && h.PrcMonth == month);

                if (hdr == null)
                    return (false, "Encashment record not found.");

                if (string.IsNullOrWhiteSpace(comments))
                    return (false, "Please provide rejection comments.");

                var approvers = await GetWorkflowApproversAsync("Leave Salary & Air Ticket Encashment");
                if (actorEmpSysId != approvers.Level1Id && actorEmpSysId != approvers.FinalId && actorEmpSysId != 1)
                    return (false, "You are not authorized to reject this Encashment batch.");

                var (actorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);
                string previousState = hdr.WfStat;

                hdr.WfStat = "Rejected";
                hdr.PrcStat = 1; // Reverted

                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Leave Salary & Air Ticket Encashment",
                    RefId = hdr.EncashHdrId,
                    PrcYear = year,
                    PrcMonth = month,
                    StepNumber = previousState == "PendingFinal" ? 3 : 2,
                    StepName = previousState == "PendingFinal" ? "Final Approval" : "Level 1 Approval",
                    Action = "Rejected",
                    ActorEmpSysId = actorEmpSysId,
                    ActorName = actorName,
                    Comments = comments,
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                var (initName, initEmail) = await GetEmployeeDetailsAsync(approvers.InitiatorId);
                string monthName = new DateTime(year, month, 1).ToString("MMMM");
                string periodText = $"{monthName} {year}";
                string appBaseUrl = GetAppBaseUrl();
                string encashUrl = $"{appBaseUrl}/Payroll/HrPrStaffLvTktEncash";

                string subject = $"Staff Leave & Ticket Encashment Rejected: {periodText}";
                string body = BuildWorkflowEmailHtml(
                    title: "Encashment Rejected",
                    heading: $"Encashment Batch for {periodText} was Rejected",
                    periodText: periodText,
                    statusText: "Rejected",
                    actorText: $"Rejected by {actorName}",
                    comments: $"<strong>Reason for Rejection:</strong><br/>{comments}<br/><br/>The encashment batch has been returned to draft status for adjustments.",
                    actionButtonText: "Open Encashment Screen",
                    actionUrl: encashUrl);

                await SendEmailSafelyAsync(initEmail, subject, body);
                await PushInAppNotificationSafelyAsync(approvers.InitiatorId, subject, $"Encashment for {periodText} was rejected by {actorName}: {comments}", hdr.EncashHdrId, encashUrl);

                return (true, $"Encashment batch has been rejected and returned to {initName}.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rejecting encashment for Year: {Year}, Month: {Month}", year, month);
                return (false, "Error rejecting encashment: " + ex.Message);
            }
        }

        // =====================================================================
        // NOTIFICATION HELPERS
        // =====================================================================

        private async Task SendEmailSafelyAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger?.LogWarning("[PayrollWorkflowService] Skipping email: recipient has no valid email address.");
                return;
            }

            try
            {
                await _sysConfigContext.SendEmailNotificationAsync(toEmail, "", subject, htmlBody, null);
                _logger?.LogInformation("[PayrollWorkflowService] Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PayrollWorkflowService] Failed to send email to {ToEmail}", toEmail);
            }
        }

        private async Task PushInAppNotificationSafelyAsync(int empSysId, string subject, string message, int? reqId, string actionUrl)
        {
            if (empSysId <= 0 || _notifyContext == null || _notifyContext.NtfNotification == null)
                return;

            try
            {
                var ntf = new NtfNotification
                {
                    EmpSysId = empSysId,
                    Subject = subject,
                    Message = message,
                    ReqId = reqId,
                    NtfTypRefId = 2, // Payroll
                    PushReadStat = false,
                    IsDelivered = 1,
                    CrtDateTime = DateTime.Now
                };
                _notifyContext.NtfNotification.Add(ntf);
                await _notifyContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to push in-app notification for EmpSysId: {EmpSysId}", empSysId);
            }
        }

        private static string BuildWorkflowEmailHtml(
            string title,
            string heading,
            string periodText,
            string statusText,
            string actorText,
            string comments,
            string actionButtonText,
            string actionUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f6f8fb; margin: 0; padding: 20px; color: #333; }}
        .email-container {{ max-width: 620px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); border-top: 5px solid #540033; }}
        .header {{ background: #540033; color: #ffffff; padding: 24px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 20px; font-weight: 600; }}
        .header p {{ margin: 6px 0 0; font-size: 13px; opacity: 0.9; }}
        .body-content {{ padding: 28px 24px; }}
        .status-badge {{ display: inline-block; padding: 6px 14px; font-size: 13px; font-weight: 600; border-radius: 20px; background: rgba(84, 0, 51, 0.1); color: #540033; margin-bottom: 16px; }}
        .info-card {{ background: #faf8fa; border-left: 4px solid #540033; padding: 14px 16px; border-radius: 4px; margin: 18px 0; }}
        .btn-action {{ display: inline-block; background-color: #540033; color: #ffffff !important; padding: 12px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; font-size: 14px; }}
        .footer {{ background: #f2f2f2; color: #777; padding: 16px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>Qatar Computer Services WLL</h1>
            <p>HR & Payroll Management System</p>
        </div>
        <div class=""body-content"">
            <span class=""status-badge"">{statusText}</span>
            <h2 style=""color: #222; font-size: 18px; margin: 0 0 10px;"">{heading}</h2>
            <p style=""color: #666; font-size: 13px; margin: 0 0 16px;"">{actorText}</p>

            <div class=""info-card"">
                <p style=""margin: 0 0 8px;""><strong>Payroll Period:</strong> {periodText}</p>
                <div style=""margin: 0; font-size: 13px; line-height: 1.5;"">{comments}</div>
            </div>

            <div style=""text-align: center;"">
                <a href=""{actionUrl}"" class=""btn-action"" target=""_blank"">{actionButtonText}</a>
            </div>

            <p style=""color: #888; font-size: 12px; margin-top: 24px;"">
                Direct Link: <a href=""{actionUrl}"" style=""color: #540033; word-break: break-all;"">{actionUrl}</a>
            </p>
        </div>
        <div class=""footer"">
            This is an automated notification from QCS HR & Payroll System.<br/>
            Please do not reply directly to this email.
        </div>
    </div>
</body>
</html>";
        }
    }
}

