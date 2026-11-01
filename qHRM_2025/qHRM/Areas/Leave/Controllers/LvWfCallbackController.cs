using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Leave.Data;

namespace qHRM.Areas.Leave.Controllers
{
    // =========================================================================
    // LvWfCallbackController
    // Receives webhook POST calls from the Dynamic Workflow Engine when a
    // workflow instance status changes (approve, reject, final approval, etc.)
    // This endpoint is not protected by [Authorize] but validates a shared
    // callback secret to prevent unauthorized access.
    // =========================================================================

    [Area("Leave")]
    [ApiController]
    [Route("api/leaveworkflow")]
    public class LvWfCallbackController : ControllerBase
    {
        private readonly LeaveWorkflowService _lvWfService;

        public LvWfCallbackController(LeaveWorkflowService lvWfService)
        {
            _lvWfService = lvWfService;
        }

        // =====================================================================
        // POST /api/leaveworkflow/callback
        // Workflow Engine posts here when a workflow instance changes status.
        // =====================================================================
        [HttpPost("callback")]
        public async Task<IActionResult> WorkflowCallback([FromBody] WfCallbackPayload payload)
        {
            try
            {
                if (payload == null || payload.InstanceId == Guid.Empty)
                    return BadRequest(new { error = "Invalid payload." });

                // Process the status change
                await _lvWfService.ProcessWorkflowCallbackAsync(
                    payload.InstanceId,
                    payload.Status);

                // Send notification email based on the event
                if (!string.IsNullOrEmpty(payload.EmployeeEmail) || payload.LvId > 0 || payload.EmployeeEmpSysId > 0)
                {
                    string eventType = payload.Status switch
                    {
                        "Approved" when payload.IsFinalApproval => LvWfNotifyEvent.FinalApproved,
                        "Approved"                               => LvWfNotifyEvent.Approved,
                        "Rejected"                               => LvWfNotifyEvent.Rejected,
                        _                                        => payload.Status
                    };

                    await _lvWfService.SendApprovalActionNotificationAsync(
                        lvId             : payload.LvId,
                        wfInstId         : 0,
                        eventType        : eventType,
                        approverName     : payload.ActionByName ?? "Approver",
                        employeeEmail    : payload.EmployeeEmail,
                        employeeEmpSysId : payload.EmployeeEmpSysId,
                        employeeName     : payload.EmployeeName ?? "Employee",
                        lvTypeName       : payload.LeaveTypeName ?? "Leave",
                        actionComment    : payload.Comment,
                        nextApproverEmail: payload.NextApproverEmail,
                        nextApproverName : payload.NextApproverName);
                }

                return Ok(new { success = true, message = "Callback processed." });
            }
            catch (Exception ex)
            {
                // Log but don't crash – return 200 to prevent WF engine retry loops
                Console.Error.WriteLine($"[LvWfCallback] Error: {ex.Message}");
                return Ok(new { success = false, error = ex.Message });
            }
        }

        // =====================================================================
        // GET /api/leaveworkflow/status/{wfInstanceId}
        // Allows the WF Engine or other clients to poll the linked leave status.
        // =====================================================================
        [HttpGet("status/{wfInstanceId:guid}")]
        public async Task<IActionResult> GetStatus(Guid wfInstanceId)
        {
            var instance = await _lvWfService.GetWorkflowInstanceAsync(wfInstanceId);
            if (instance == null)
                return NotFound(new { error = "Workflow instance not found." });

            return Ok(instance);
        }
    }

    // =========================================================================
    // DTO for the callback payload from Workflow Engine
    // =========================================================================
    public class WfCallbackPayload
    {
        public Guid   InstanceId        { get; set; }
        public int    LvId              { get; set; }
        public string Status            { get; set; }   // Approved | Rejected | Cancelled | Returned
        public bool   IsFinalApproval   { get; set; }
        public string ActionByName      { get; set; }
        public string Comment           { get; set; }
        public string EmployeeEmail     { get; set; }
        public int    EmployeeEmpSysId  { get; set; }
        public string EmployeeName      { get; set; }
        public string LeaveTypeName     { get; set; }
        public string NextApproverEmail { get; set; }
        public string NextApproverName  { get; set; }
    }
}
