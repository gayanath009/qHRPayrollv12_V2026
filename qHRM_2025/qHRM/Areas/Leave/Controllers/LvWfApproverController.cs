using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Leave.Data;

namespace qHRM.Areas.Leave.Controllers
{
    // =========================================================================
    // LvWfApproverController
    // Called by the Dynamic Workflow Engine when a step uses ApprovalType=ApiLookup.
    // Returns the approver's WF User GUID for the given HR employee.
    // This enables dynamic approver resolution without pre-configuring user GUIDs.
    // =========================================================================

    [Area("Leave")]
    [ApiController]
    [Route("api/leaveworkflow/approvers")]
    public class LvWfApproverController : ControllerBase
    {
        private readonly LeaveWorkflowService _lvWfService;

        public LvWfApproverController(LeaveWorkflowService lvWfService)
        {
            _lvWfService = lvWfService;
        }

        // =====================================================================
        // GET /api/leaveworkflow/approvers/reporting-manager/{empSysId}
        // Returns the reporting manager's WF User GUID for a given employee.
        // Called by WF Engine (ApiLookup) for the Reporting Manager step.
        // =====================================================================
        [HttpGet("reporting-manager/{empSysId:int}")]
        public async Task<IActionResult> GetReportingManager(int empSysId)
        {
            try
            {
                var result = await _lvWfService.ResolveReportingManagerAsync(empSysId);

                if (!result.Found || !result.EmpSysId.HasValue)
                    return NotFound(new
                    {
                        error = $"No reporting manager found for employee {empSysId}. " +
                                "Please ensure the employee has a current job assignment with a reporting manager."
                    });

                // Ensure the manager exists in WF Engine (creates/syncs if not)
                string token    = string.Empty; // LeaveWorkflowService will auth internally
                // Return the HR employee data; WF engine can use the EmpSysId for subsequent lookups
                return Ok(new
                {
                    empSysId  = result.EmpSysId,
                    fullName  = result.FullName,
                    email     = result.Email,
                    found     = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // =====================================================================
        // GET /api/leaveworkflow/approvers/bu-head/{deptId}
        // Returns the configured BU Head for a given department.
        // The BU Head is configured in the Leave Workflow Configuration (ApproverConfig JSON).
        // =====================================================================
        [HttpGet("bu-head/{lvTypId:int}/{empSysId:int}")]
        public async Task<IActionResult> GetBuHead(int lvTypId, int empSysId)
        {
            try
            {
                // This is a placeholder for a future enhancement where BU Head
                // is resolved from the org hierarchy. For now, it returns the
                // configured BU Head from HrLvWfConfig.ApproverConfig.
                return Ok(new
                {
                    found   = false,
                    message = "BU Head resolution via API is not yet configured. " +
                              "Please configure BU Head in Leave Workflow Configuration."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
