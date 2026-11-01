using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Application.Services;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ConditionEvaluator _conditionEvaluator;
    private readonly IConfiguration? _configuration;

    public WorkflowEngine(IUnitOfWork unitOfWork, ConditionEvaluator conditionEvaluator, IConfiguration? configuration = null)
    {
        _unitOfWork = unitOfWork;
        _conditionEvaluator = conditionEvaluator;
        _configuration = configuration;
    }

    public async Task<WorkflowInstance> StartWorkflowAsync(string workflowCode, Guid requesterId, Dictionary<string, string> inputData)
    {
        // Begin Transaction
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1. Find the active workflow definition and version
            string targetCode = workflowCode?.Trim() ?? string.Empty;
            string lvCode = inputData != null && inputData.TryGetValue("LvTypCode", out var lvCodeVal) ? lvCodeVal : targetCode;
            string lvName = inputData != null && inputData.TryGetValue("LeaveType", out var lvNameVal) ? lvNameVal : string.Empty;

            // Determine requester employee work type (1 = IN_HOUSE, 2 = OUTSOURCED)
            int wrktypId = 1;
            if (inputData != null && inputData.TryGetValue("WrktypId", out var wrkVal) && int.TryParse(wrkVal, out var parsedWrk))
            {
                wrktypId = parsedWrk;
            }
            string reqWorkType = wrktypId == 2 ? "OUTSOURCED" : "IN_HOUSE";

            var allDefs = (await _unitOfWork.Repository<WorkflowDefinition>().FindAsync(d => d.IsActive && !d.IsDeleted)).ToList();
            
            bool MatchesWorkType(WorkflowDefinition d, bool exactOnly)
            {
                string defWorkType = (d.TargetWorkType ?? "ALL").Trim().ToUpperInvariant();
                if (exactOnly)
                {
                    return defWorkType == reqWorkType || (reqWorkType == "OUTSOURCED" && defWorkType == "OUTSRC");
                }
                return defWorkType == "ALL" || string.IsNullOrEmpty(defWorkType) || defWorkType == reqWorkType || (reqWorkType == "OUTSOURCED" && defWorkType == "OUTSRC");
            }

            bool MatchesLeaveCode(WorkflowDefinition d)
            {
                if (string.Equals(d.Code, targetCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Name, targetCode, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!string.IsNullOrWhiteSpace(d.AssociatedLeaveTypes))
                {
                    var tokens = d.AssociatedLeaveTypes.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return tokens.Any(code => string.Equals(code.Trim(), targetCode, StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(code.Trim(), lvCode, StringComparison.OrdinalIgnoreCase));
                }
                return false;
            }

            // Priority 1: Specific work-type match (e.g. IN_HOUSE only or OUTSOURCED only) + matching leave type/code
            var definition = allDefs.FirstOrDefault(d => MatchesLeaveCode(d) && MatchesWorkType(d, exactOnly: true));

            // Priority 2: Generic work-type match (ALL) + matching leave type/code
            if (definition == null)
            {
                definition = allDefs.FirstOrDefault(d => MatchesLeaveCode(d) && MatchesWorkType(d, exactOnly: false));
            }

            // Priority 3: Fallback general leave workflow matching work type
            if (definition == null)
            {
                definition = allDefs.FirstOrDefault(d => (d.Code.Contains("leave", StringComparison.OrdinalIgnoreCase) ||
                    (d.Category != null && d.Category.Contains("leave", StringComparison.OrdinalIgnoreCase)) ||
                    d.Name.Contains("leave", StringComparison.OrdinalIgnoreCase)) && MatchesWorkType(d, exactOnly: true));
            }

            if (definition == null)
            {
                definition = allDefs.FirstOrDefault(d => (d.Code.Contains("leave", StringComparison.OrdinalIgnoreCase) ||
                    (d.Category != null && d.Category.Contains("leave", StringComparison.OrdinalIgnoreCase)) ||
                    d.Name.Contains("leave", StringComparison.OrdinalIgnoreCase)) && MatchesWorkType(d, exactOnly: false));
            }

            if (definition == null)
                throw new Exception($"Workflow with code '{workflowCode}' not found or inactive for work type '{reqWorkType}'. Available workflows: please check Workflow Definitions in the Workflow Engine.");

            var versions = await _unitOfWork.Repository<WorkflowVersion>().FindAsync(
                v => v.WorkflowDefinitionId == definition.Id && v.Status == WorkflowStatus.Published && v.IsActive && !v.IsDeleted);
            var version = versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

            if (version == null)
            {
                // If no published version exists, check for the latest active draft version
                var draftVersions = await _unitOfWork.Repository<WorkflowVersion>().FindAsync(
                    v => v.WorkflowDefinitionId == definition.Id && v.IsActive && !v.IsDeleted);
                version = draftVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            }

            if (version == null)
                throw new Exception($"No active version found for workflow '{definition.Name}' ({definition.Code}).");

            // 2. Generate instance number
            var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var rand = new Random().Next(1000, 9999);
            var instanceNumber = $"{workflowCode}-{todayStr}-{rand}";

            // 3. Create Workflow Instance
            var instance = new WorkflowInstance
            {
                WorkflowVersionId = version.Id,
                InstanceNumber = instanceNumber,
                Status = WorkflowInstanceStatus.InProgress,
                RequesterId = requesterId,
                StartDate = DateTime.UtcNow
            };
            await _unitOfWork.Repository<WorkflowInstance>().AddAsync(instance);

            // 4. Save form data
            var instanceDataRepo = _unitOfWork.Repository<WorkflowInstanceData>();
            foreach (var kvp in inputData)
            {
                await instanceDataRepo.AddAsync(new WorkflowInstanceData
                {
                    WorkflowInstanceId = instance.Id,
                    FieldName = kvp.Key,
                    FieldValue = kvp.Value
                });
            }

            // 5. Add to history
            await _unitOfWork.Repository<WorkflowHistory>().AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = "Start",
                Action = "Started",
                PerformedById = requesterId,
                Comment = "Workflow instance initiated."
            });

            // 6. Move to first step
            await MoveToNextStepAsync(instance, null, inputData);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return instance;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> ApproveStepAsync(Guid approvalId, Guid userId, string? comment, string? signature)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var approvalRepo = _unitOfWork.Repository<WorkflowApproval>();
            var approval = await approvalRepo.GetByIdAsync(approvalId);
            if (approval == null || approval.Status != StepStatus.Pending)
                return false;

            var caller = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            bool isPrivileged = caller != null && (caller.Username == "admin" || caller.Username == "system");
            if (!isPrivileged && approval.ApproverId != userId && approval.DelegatedFromUserId != userId)
                return false;

            // 1. Update approval status
            approval.Status = StepStatus.Approved;
            approval.ActionDate = DateTime.UtcNow;
            approval.Comment = comment;
            approval.DigitalSignature = signature;
            approvalRepo.Update(approval);

            var stepInstanceRepo = _unitOfWork.Repository<WorkflowInstanceStep>();
            var stepInstance = await stepInstanceRepo.GetByIdAsync(approval.WorkflowInstanceStepId);
            if (stepInstance == null) return false;

            // 2. Load other approvals at this step
            var approvals = await approvalRepo.FindAsync(a => a.WorkflowInstanceStepId == stepInstance.Id && !a.IsDeleted);
            var stepRepo = _unitOfWork.Repository<WorkflowStep>();
            var step = await stepRepo.GetByIdAsync(stepInstance.WorkflowStepId);
            if (step == null) return false;

            // 3. Determine if step is completed based on ApprovalType
            bool stepCompleted = false;
            switch (step.ApprovalType)
            {
                case ApprovalType.SpecificUser:
                case ApprovalType.DepartmentHead:
                case ApprovalType.ManagerHierarchy:
                case ApprovalType.DynamicUser:
                case ApprovalType.AnyOne:
                case ApprovalType.RoleBased:
                    stepCompleted = true; // Single approver completed
                    break;
                case ApprovalType.AllMustApprove:
                case ApprovalType.Sequential:
                    stepCompleted = approvals.All(a => a.Status == StepStatus.Approved);
                    break;
                case ApprovalType.Majority:
                    int majorityCount = approvals.Count(a => a.Status == StepStatus.Approved);
                    stepCompleted = majorityCount > (approvals.Count() / 2.0);
                    break;
                default:
                    stepCompleted = true;
                    break;
            }

            // 4. Log history
            var instanceRepo = _unitOfWork.Repository<WorkflowInstance>();
            var instance = await instanceRepo.GetByIdAsync(stepInstance.WorkflowInstanceId);
            if (instance == null) return false;

            await _unitOfWork.Repository<WorkflowHistory>().AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = stepInstance.Name,
                Action = "Approved",
                PerformedById = userId,
                Comment = comment ?? "Step approved."
            });

            if (stepCompleted)
            {
                stepInstance.Status = StepStatus.Approved;
                stepInstance.EndDate = DateTime.UtcNow;
                stepInstanceRepo.Update(stepInstance);

                // Fetch current inputs
                var dataEntities = await _unitOfWork.Repository<WorkflowInstanceData>().FindAsync(d => d.WorkflowInstanceId == instance.Id);
                var inputData = dataEntities.ToDictionary(d => d.FieldName, d => d.FieldValue ?? string.Empty);

                // Flush changes to DB so subsequent steps and queries see current step as Approved
                await _unitOfWork.SaveChangesAsync();

                // Move to next step
                await MoveToNextStepAsync(instance, step, inputData);
                instanceRepo.Update(instance);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> RejectStepAsync(Guid approvalId, Guid userId, string? comment)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var approvalRepo = _unitOfWork.Repository<WorkflowApproval>();
            var approval = await approvalRepo.GetByIdAsync(approvalId);
            if (approval == null || approval.Status != StepStatus.Pending)
                return false;

            var caller = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            bool isPrivileged = caller != null && (caller.Username == "admin" || caller.Username == "system");
            if (!isPrivileged && approval.ApproverId != userId && approval.DelegatedFromUserId != userId)
                return false;

            approval.Status = StepStatus.Rejected;
            approval.ActionDate = DateTime.UtcNow;
            approval.Comment = comment;
            approvalRepo.Update(approval);

            var stepInstanceRepo = _unitOfWork.Repository<WorkflowInstanceStep>();
            var stepInstance = await stepInstanceRepo.GetByIdAsync(approval.WorkflowInstanceStepId);
            if (stepInstance == null) return false;

            stepInstance.Status = StepStatus.Rejected;
            stepInstance.EndDate = DateTime.UtcNow;
            stepInstanceRepo.Update(stepInstance);

            var instanceRepo = _unitOfWork.Repository<WorkflowInstance>();
            var instance = await instanceRepo.GetByIdAsync(stepInstance.WorkflowInstanceId);
            if (instance == null) return false;

            instance.Status = WorkflowInstanceStatus.Rejected;
            instance.EndDate = DateTime.UtcNow;
            instanceRepo.Update(instance);

            // Log history
            await _unitOfWork.Repository<WorkflowHistory>().AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = stepInstance.Name,
                Action = "Rejected",
                PerformedById = userId,
                Comment = comment ?? "Step rejected."
            });

            // Set other pending approvals to Rejected/Cancelled
            var approvals = await approvalRepo.FindAsync(a => a.WorkflowInstanceStepId == stepInstance.Id && a.Status == StepStatus.Pending && !a.IsDeleted);
            foreach (var pendingApp in approvals)
            {
                pendingApp.Status = StepStatus.Rejected;
                approvalRepo.Update(pendingApp);
            }

            // Queue notifications
            await QueueNotificationsForStatusChangeAsync(instance);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> ReturnStepAsync(Guid approvalId, Guid userId, string? comment)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var approvalRepo = _unitOfWork.Repository<WorkflowApproval>();
            var approval = await approvalRepo.GetByIdAsync(approvalId);
            if (approval == null || approval.ApproverId != userId || approval.Status != StepStatus.Pending)
                return false;

            approval.Status = StepStatus.Returned;
            approval.ActionDate = DateTime.UtcNow;
            approval.Comment = comment;
            approvalRepo.Update(approval);

            var stepInstanceRepo = _unitOfWork.Repository<WorkflowInstanceStep>();
            var stepInstance = await stepInstanceRepo.GetByIdAsync(approval.WorkflowInstanceStepId);
            if (stepInstance == null) return false;

            stepInstance.Status = StepStatus.Returned;
            stepInstance.EndDate = DateTime.UtcNow;
            stepInstanceRepo.Update(stepInstance);

            var instanceRepo = _unitOfWork.Repository<WorkflowInstance>();
            var instance = await instanceRepo.GetByIdAsync(stepInstance.WorkflowInstanceId);
            if (instance == null) return false;

            instance.Status = WorkflowInstanceStatus.Returned;
            instanceRepo.Update(instance);

            // Log history
            await _unitOfWork.Repository<WorkflowHistory>().AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = stepInstance.Name,
                Action = "Returned",
                PerformedById = userId,
                Comment = comment ?? "Returned for clarification."
            });

            // Queue notifications
            await QueueNotificationsForStatusChangeAsync(instance);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> DelegateStepAsync(Guid approvalId, Guid userId, Guid delegateUserId, string? comment)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var approvalRepo = _unitOfWork.Repository<WorkflowApproval>();
            var approval = await approvalRepo.GetByIdAsync(approvalId);
            if (approval == null || approval.ApproverId != userId || approval.Status != StepStatus.Pending)
                return false;

            var userRepo = _unitOfWork.Repository<User>();
            var delegateUser = await userRepo.GetByIdAsync(delegateUserId);
            if (delegateUser == null) return false;

            // Log current approval state
            approval.Status = StepStatus.Delegated;
            approval.ActionDate = DateTime.UtcNow;
            approval.Comment = $"Delegated to {delegateUser.FullName}. Comment: {comment}";
            approvalRepo.Update(approval);

            // Create new approval for delegated user
            var newApproval = new WorkflowApproval
            {
                WorkflowInstanceStepId = approval.WorkflowInstanceStepId,
                ApproverId = delegateUserId,
                Status = StepStatus.Pending,
                DelegatedFromUserId = userId
            };
            await approvalRepo.AddAsync(newApproval);

            var stepInstanceRepo = _unitOfWork.Repository<WorkflowInstanceStep>();
            var stepInstance = await stepInstanceRepo.GetByIdAsync(approval.WorkflowInstanceStepId);
            if (stepInstance == null) return false;

            var instanceRepo = _unitOfWork.Repository<WorkflowInstance>();
            var instance = await instanceRepo.GetByIdAsync(stepInstance.WorkflowInstanceId);
            if (instance == null) return false;

            // Log History
            await _unitOfWork.Repository<WorkflowHistory>().AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = stepInstance.Name,
                Action = "Delegated",
                PerformedById = userId,
                Comment = $"Delegated task to {delegateUser.FullName}. Reason: {comment}"
            });

            // Queue notification for delegate
            var stepRepo = _unitOfWork.Repository<WorkflowStep>();
            var step = await stepRepo.GetByIdAsync(stepInstance.WorkflowStepId);
            if (step != null)
            {
                await QueueApprovalNotificationAsync(instance, step, delegateUser);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> EscalateStepAsync(Guid stepInstanceId)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var stepInstanceRepo = _unitOfWork.Repository<WorkflowInstanceStep>();
            var stepInstance = await stepInstanceRepo.GetByIdAsync(stepInstanceId);
            if (stepInstance == null || stepInstance.Status != StepStatus.Pending)
                return false;

            var stepRepo = _unitOfWork.Repository<WorkflowStep>();
            var step = await stepRepo.GetByIdAsync(stepInstance.WorkflowStepId);
            if (step == null) return false;

            var escalations = await _unitOfWork.Repository<WorkflowEscalation>().FindAsync(e => e.WorkflowStepId == step.Id && !e.IsDeleted && !e.IsProcessed);
            var escalation = escalations.FirstOrDefault();
            if (escalation == null) return false;

            var approvalRepo = _unitOfWork.Repository<WorkflowApproval>();
            var pendingApprovals = await approvalRepo.FindAsync(a => a.WorkflowInstanceStepId == stepInstance.Id && a.Status == StepStatus.Pending && !a.IsDeleted);

            var instanceRepo = _unitOfWork.Repository<WorkflowInstance>();
            var instance = await instanceRepo.GetByIdAsync(stepInstance.WorkflowInstanceId);
            if (instance == null) return false;

            escalation.IsProcessed = true;
            _unitOfWork.Repository<WorkflowEscalation>().Update(escalation);

            var historyRepo = _unitOfWork.Repository<WorkflowHistory>();

            if (escalation.Action == EscalationAction.AutoApprove)
            {
                // Auto-approve approvals
                foreach (var app in pendingApprovals)
                {
                    app.Status = StepStatus.Approved;
                    app.ActionDate = DateTime.UtcNow;
                    app.Comment = "Auto-approved by Escalation Engine.";
                    approvalRepo.Update(app);
                }

                stepInstance.Status = StepStatus.Approved;
                stepInstance.EndDate = DateTime.UtcNow;
                stepInstanceRepo.Update(stepInstance);

                await historyRepo.AddAsync(new WorkflowHistory
                {
                    WorkflowInstanceId = instance.Id,
                    StepName = stepInstance.Name,
                    Action = "AutoApproved",
                    PerformedById = Guid.Empty, // System
                    Comment = "Step auto-approved due to SLA violation."
                });

                var dataEntities = await _unitOfWork.Repository<WorkflowInstanceData>().FindAsync(d => d.WorkflowInstanceId == instance.Id);
                var inputData = dataEntities.ToDictionary(d => d.FieldName, d => d.FieldValue ?? string.Empty);
                await MoveToNextStepAsync(instance, step, inputData);
                instanceRepo.Update(instance);
            }
            else if (escalation.Action == EscalationAction.AutoReject)
            {
                foreach (var app in pendingApprovals)
                {
                    app.Status = StepStatus.Rejected;
                    app.ActionDate = DateTime.UtcNow;
                    app.Comment = "Auto-rejected by Escalation Engine.";
                    approvalRepo.Update(app);
                }

                stepInstance.Status = StepStatus.Rejected;
                stepInstance.EndDate = DateTime.UtcNow;
                stepInstanceRepo.Update(stepInstance);

                instance.Status = WorkflowInstanceStatus.Rejected;
                instance.EndDate = DateTime.UtcNow;
                instanceRepo.Update(instance);

                await historyRepo.AddAsync(new WorkflowHistory
                {
                    WorkflowInstanceId = instance.Id,
                    StepName = stepInstance.Name,
                    Action = "AutoRejected",
                    PerformedById = Guid.Empty, // System
                    Comment = "Step auto-rejected due to SLA violation."
                });

                await QueueNotificationsForStatusChangeAsync(instance);
            }
            else if (escalation.Action == EscalationAction.EscalateToManager || escalation.Action == EscalationAction.EscalateToNextLevel)
            {
                // Add escalated user as an approver
                Guid? targetUserEscalateId = escalation.EscalateToUserId;
                if (!targetUserEscalateId.HasValue && pendingApprovals.Any())
                {
                    // Fallback to manager of first pending approver
                    var firstApproverId = pendingApprovals.First().ApproverId;
                    var appUser = await _unitOfWork.Repository<User>().GetByIdAsync(firstApproverId);
                    targetUserEscalateId = appUser?.ManagerId;
                }

                if (targetUserEscalateId.HasValue)
                {
                    var userRepo = _unitOfWork.Repository<User>();
                    var managerUser = await userRepo.GetByIdAsync(targetUserEscalateId.Value);
                    if (managerUser != null)
                    {
                        // Add manager approval record
                        await approvalRepo.AddAsync(new WorkflowApproval
                        {
                            WorkflowInstanceStepId = stepInstance.Id,
                            ApproverId = managerUser.Id,
                            Status = StepStatus.Pending
                        });

                        await historyRepo.AddAsync(new WorkflowHistory
                        {
                            WorkflowInstanceId = instance.Id,
                            StepName = stepInstance.Name,
                            Action = "Escalated",
                            PerformedById = Guid.Empty, // System
                            Comment = $"Escalated to Manager: {managerUser.FullName}"
                        });

                        await QueueApprovalNotificationAsync(instance, step, managerUser);
                    }
                }
            }
            else if (escalation.Action == EscalationAction.Reminder)
            {
                // Send reminder notification to pending approvers
                foreach (var app in pendingApprovals)
                {
                    var appUser = await _unitOfWork.Repository<User>().GetByIdAsync(app.ApproverId);
                    if (appUser != null)
                    {
                        string appUserEmail = await GetOfficialEmailFromJobAssignAsync(appUser.EmpSysId);
                        if (!string.IsNullOrWhiteSpace(appUserEmail))
                        {
                            await QueueNotificationAsync(appUserEmail, "SLA Reminder: Approval Pending", 
                                $"Reminder: The workflow task '{stepInstance.Name}' under instance '{instance.InstanceNumber}' is pending your approval.");
                        }
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task MoveToNextStepAsync(WorkflowInstance instance, WorkflowStep? currentStep, Dictionary<string, string> inputData)
    {
        var stepRepo = _unitOfWork.Repository<WorkflowStep>();
        var transitionRepo = _unitOfWork.Repository<WorkflowTransition>();
        var versionRepo = _unitOfWork.Repository<WorkflowVersion>();
        var instanceStepRepo = _unitOfWork.Repository<WorkflowInstanceStep>();
        var approvalRepo = _unitOfWork.Repository<WorkflowApproval>();
        var historyRepo = _unitOfWork.Repository<WorkflowHistory>();

        WorkflowStep? nextStep = null;

        if (currentStep == null)
        {
            var steps = await stepRepo.FindAsync(s => s.WorkflowVersionId == instance.WorkflowVersionId && !s.IsDeleted && s.IsActive);
            nextStep = steps.OrderBy(s => s.Sequence).FirstOrDefault();
        }
        else
        {
            var transitions = await transitionRepo.FindAsync(t => t.FromStepId == currentStep.Id && !t.IsDeleted && t.IsActive);
            foreach (var trans in transitions.OrderBy(t => t.CreatedDate))
            {
                var condGroups = await _unitOfWork.Repository<WorkflowConditionGroup>().FindAsync(cg => cg.WorkflowTransitionId == trans.Id && !cg.IsDeleted);
                bool meetsCondition = true;
                foreach (var cg in condGroups)
                {
                    var conds = await _unitOfWork.Repository<WorkflowCondition>().FindAsync(c => c.ConditionGroupId == cg.Id && !c.IsDeleted);
                    cg.Conditions = conds.ToList();
                    if (!_conditionEvaluator.Evaluate(cg, inputData))
                    {
                        meetsCondition = false;
                        break;
                    }
                }

                if (meetsCondition)
                {
                    nextStep = await stepRepo.GetByIdAsync(trans.ToStepId);
                    break;
                }
            }

            if (nextStep == null)
            {
                var steps = await stepRepo.FindAsync(s => s.WorkflowVersionId == instance.WorkflowVersionId && !s.IsDeleted && s.IsActive);
                nextStep = steps.Where(s => s.Sequence > currentStep.Sequence).OrderBy(s => s.Sequence).FirstOrDefault();
            }
        }

        if (nextStep == null)
        {
            instance.Status = WorkflowInstanceStatus.Approved;
            instance.EndDate = DateTime.UtcNow;

            var systemUser = await _unitOfWork.Repository<User>().FindAsync(u => u.Username == "system" || u.Username == "admin");
            var systemUserId = systemUser.FirstOrDefault()?.Id ?? instance.RequesterId;

            await historyRepo.AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = "Final",
                Action = "Approved",
                PerformedById = systemUserId,
                Comment = "Workflow completed successfully."
            });

            await QueueNotificationsForStatusChangeAsync(instance, inputData);
            return;
        }

        // Work Type evaluation: Check if this step is restricted to In-House or Outsourced staff
        bool workTypeSkip = false;
        string? stepWorkType = nextStep.TargetWorkType?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(stepWorkType) && stepWorkType != "ALL")
        {
            string empWrktypId = inputData != null && inputData.TryGetValue("WrktypId", out var wtId) ? wtId.Trim() : "1";
            string empWorkType = inputData != null && inputData.TryGetValue("WorkType", out var wtName) ? wtName.Trim().ToUpperInvariant() : "IN-HOUSE";

            bool isEmpInHouse = empWrktypId == "1" || empWorkType.Contains("IN-HOUSE") || empWorkType.Contains("INHOUSE");
            bool isEmpOutsourced = empWrktypId == "2" || empWorkType.Contains("OUTSOURCE");

            if ((stepWorkType == "IN_HOUSE" || stepWorkType == "INHOUSE" || stepWorkType == "1") && !isEmpInHouse)
            {
                workTypeSkip = true;
            }
            else if ((stepWorkType == "OUTSOURCED" || stepWorkType == "OUTSOURCE" || stepWorkType == "2") && !isEmpOutsourced)
            {
                workTypeSkip = true;
            }
        }

        var skipGroups = await _unitOfWork.Repository<WorkflowConditionGroup>().FindAsync(cg => cg.WorkflowStepId == nextStep.Id && !cg.IsDeleted);
        bool shouldSkip = workTypeSkip;
        if (!shouldSkip && skipGroups.Any())
        {
            foreach (var sg in skipGroups)
            {
                var conds = await _unitOfWork.Repository<WorkflowCondition>().FindAsync(c => c.ConditionGroupId == sg.Id && !c.IsDeleted);
                sg.Conditions = conds.ToList();
                if (_conditionEvaluator.Evaluate(sg, inputData))
                {
                    shouldSkip = true;
                    break;
                }
            }
        }

        if (shouldSkip)
        {
            var systemUser = await _unitOfWork.Repository<User>().FindAsync(u => u.Username == "system" || u.Username == "admin");
            var systemUserId = systemUser.FirstOrDefault()?.Id ?? instance.RequesterId;
            string skipReason = workTypeSkip
                ? $"Step skipped: Configured for {stepWorkType} employees only."
                : "Step skipped due to condition match.";

            await historyRepo.AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = nextStep.Name,
                Action = "Skipped",
                PerformedById = systemUserId,
                Comment = skipReason
            });

            await MoveToNextStepAsync(instance, nextStep, inputData);
            return;
        }

        // If the first step is configured as an Initiation / Submission step, auto-complete it for the requester and move to the first approval step
        bool isInitiationStep = currentStep == null && (
            nextStep.Name.ToLower().Contains("initiate") ||
            nextStep.Name.ToLower().Contains("submit") ||
            nextStep.Name.ToLower().Contains("creation") ||
            nextStep.Name.ToLower().Contains("draft") ||
            nextStep.Name.ToLower().Contains("apply")
        );

        if (isInitiationStep)
        {
            var initStep = new WorkflowInstanceStep
            {
                WorkflowInstanceId = instance.Id,
                WorkflowStepId = nextStep.Id,
                StepNumber = nextStep.StepNumber,
                Name = nextStep.Name,
                Status = StepStatus.Approved,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow
            };
            await instanceStepRepo.AddAsync(initStep);

            await approvalRepo.AddAsync(new WorkflowApproval
            {
                WorkflowInstanceStepId = initStep.Id,
                ApproverId = instance.RequesterId,
                Status = StepStatus.Approved,
                ActionDate = DateTime.UtcNow,
                Comment = "Workflow initiated by requester."
            });

            await historyRepo.AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = nextStep.Name,
                Action = "Initiated",
                PerformedById = instance.RequesterId,
                Comment = "Step completed: Workflow initiated by requester."
            });

            await MoveToNextStepAsync(instance, nextStep, inputData);
            return;
        }

        var instanceStep = new WorkflowInstanceStep
        {
            WorkflowInstanceId = instance.Id,
            WorkflowStepId = nextStep.Id,
            StepNumber = nextStep.StepNumber,
            Name = nextStep.Name,
            Status = StepStatus.Pending,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddHours(nextStep.SLAHours > 0 ? nextStep.SLAHours : 24)
        };
        await instanceStepRepo.AddAsync(instanceStep);

        Guid? previousApproverId = null;
        if (currentStep != null)
        {
            var approvedList = await approvalRepo.FindAsync(a => a.WorkflowInstanceStep != null && a.WorkflowInstanceStep.WorkflowInstanceId == instance.Id && a.Status == StepStatus.Approved && !a.IsDeleted);
            var lastApproved = approvedList.OrderByDescending(a => a.ActionDate).FirstOrDefault();
            if (lastApproved != null)
            {
                previousApproverId = lastApproved.ApproverId;
            }
            else if (!string.IsNullOrEmpty(currentStep.TargetUserId) && Guid.TryParse(currentStep.TargetUserId, out var parsedCurrentTargetId))
            {
                previousApproverId = parsedCurrentTargetId;
            }
        }

        var approvers = await ResolveApproversAsync(nextStep, instance.RequesterId, inputData, previousApproverId);
        if (!approvers.Any())
        {
            instanceStep.Status = StepStatus.Approved;
            instanceStep.EndDate = DateTime.UtcNow;

            var systemUser = await _unitOfWork.Repository<User>().FindAsync(u => u.Username == "system" || u.Username == "admin");
            var systemUserId = systemUser.FirstOrDefault()?.Id ?? instance.RequesterId;

            await historyRepo.AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = nextStep.Name,
                Action = "AutoApproved",
                PerformedById = systemUserId,
                Comment = "Step auto-approved: No approvers could be resolved."
            });

            await _unitOfWork.SaveChangesAsync();
            await MoveToNextStepAsync(instance, nextStep, inputData);
            return;
        }

        // Duplicate / Redundant Approver Auto-Approval:
        // If the resolved approver has already approved an earlier stage in this workflow instance
        // (e.g. BU Head also acting as CEO, or same executive approving multiple stages),
        // auto-approve this step so the user does not have to approve twice.
        var previousApprovals = await approvalRepo.FindAsync(a =>
            a.WorkflowInstanceStep != null &&
            a.WorkflowInstanceStep.WorkflowInstanceId == instance.Id &&
            a.WorkflowInstanceStep.StepNumber > 0 &&
            a.ApproverId != instance.RequesterId &&
            a.Status == StepStatus.Approved &&
            !a.IsDeleted);

        var matchedPriorApproval = previousApprovals
            .OrderByDescending(a => a.ActionDate)
            .FirstOrDefault(a => approvers.Any(appr => appr.Id == a.ApproverId));

        if (matchedPriorApproval != null)
        {
            var priorStep = await instanceStepRepo.GetByIdAsync(matchedPriorApproval.WorkflowInstanceStepId);
            string priorStepName = priorStep?.Name ?? $"Step {matchedPriorApproval.WorkflowInstanceStep?.StepNumber}";
            var priorApprover = approvers.First(appr => appr.Id == matchedPriorApproval.ApproverId);

            instanceStep.Status = StepStatus.Approved;
            instanceStep.EndDate = DateTime.UtcNow;

            await approvalRepo.AddAsync(new WorkflowApproval
            {
                WorkflowInstanceStepId = instanceStep.Id,
                ApproverId = priorApprover.Id,
                Status = StepStatus.Approved,
                ActionDate = DateTime.UtcNow,
                Comment = $"Auto-approved: Approver ({priorApprover.FullName}) already approved at earlier stage ({priorStepName})."
            });

            await historyRepo.AddAsync(new WorkflowHistory
            {
                WorkflowInstanceId = instance.Id,
                StepName = nextStep.Name,
                Action = "AutoApproved",
                PerformedById = priorApprover.Id,
                Comment = $"Step auto-approved: Approver ({priorApprover.FullName}) already approved at earlier stage ({priorStepName})."
            });

            await _unitOfWork.SaveChangesAsync();
            await MoveToNextStepAsync(instance, nextStep, inputData);
            return;
        }

        foreach (var approver in approvers)
        {
            await approvalRepo.AddAsync(new WorkflowApproval
            {
                WorkflowInstanceStepId = instanceStep.Id,
                ApproverId = approver.Id,
                Status = StepStatus.Pending
            });

            await QueueApprovalNotificationAsync(instance, nextStep, approver, inputData);
        }
    }

    private async Task<List<User>> ResolveApproversAsync(WorkflowStep step, Guid requesterId, Dictionary<string, string> inputData, Guid? previousApproverId = null)
    {
        var approvers = new List<User>();
        var userRepo = _unitOfWork.Repository<User>();

        switch (step.ApprovalType)
        {
            case ApprovalType.SpecificUser:
                if (Guid.TryParse(step.TargetUserId, out Guid targetUserId))
                {
                    var user = await userRepo.GetByIdAsync(targetUserId);
                    if (user != null) approvers.Add(user);
                }
                else if (int.TryParse(step.TargetUserId, out int targetEmpSysId))
                {
                    var matchedUsers = await userRepo.FindAsync(u => u.EmpSysId == targetEmpSysId && !u.IsDeleted && u.IsActive);
                    var user = matchedUsers.FirstOrDefault();
                    if (user != null) approvers.Add(user);
                }
                break;

            case ApprovalType.RoleBased:
                if (Guid.TryParse(step.TargetRoleId, out Guid targetRoleId))
                {
                    var usersInRole = await userRepo.FindAsync(u => u.RoleId == targetRoleId && !u.IsDeleted && u.IsActive);
                    approvers.AddRange(usersInRole);
                }
                else if (!string.IsNullOrEmpty(step.TargetRoleId))
                {
                    var roles = await _unitOfWork.Repository<Role>().FindAsync(r => r.Code == step.TargetRoleId || r.Name == step.TargetRoleId);
                    var role = roles.FirstOrDefault();
                    if (role != null)
                    {
                        var usersInRole = await userRepo.FindAsync(u => u.RoleId == role.Id && !u.IsDeleted && u.IsActive);
                        approvers.AddRange(usersInRole);
                    }
                }
                if (approvers.Count > 1)
                {
                    approvers.RemoveAll(a => a.Id == requesterId);
                }
                break;

            case ApprovalType.DepartmentHead:
                var requester = await userRepo.GetByIdAsync(requesterId);
                if (requester != null && requester.DepartmentId.HasValue)
                {
                    var deptHeads = await userRepo.FindAsync(u => u.DepartmentId == requester.DepartmentId &&
                        u.DesignationId.HasValue && !u.IsDeleted && u.IsActive);
                    
                    // Match genuine department heads (HOD, Head of Department, Director)
                    foreach (var dh in deptHeads)
                    {
                        var designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(dh.DesignationId!.Value);
                        if (designation != null && (designation.Code == "HOD" || designation.Code == "DEPT_HEAD" || designation.Name.Contains("Head of Department") || designation.Name.StartsWith("Head", StringComparison.OrdinalIgnoreCase)))
                        {
                            approvers.Add(dh);
                        }
                    }

                    if (!approvers.Any() && requester.ManagerId.HasValue)
                    {
                        var manager = await userRepo.GetByIdAsync(requester.ManagerId.Value);
                        if (manager != null) approvers.Add(manager);
                    }
                }
                break;

            case ApprovalType.ManagerHierarchy:
                // 1. Resolve requester EmpSysId from inputData or requester entity
                int requesterEmpSysId = 0;
                if (inputData != null && inputData.TryGetValue("EmpSysId", out var empSysIdStr) && int.TryParse(empSysIdStr, out var parsedEmpSysId))
                {
                    requesterEmpSysId = parsedEmpSysId;
                }

                var requesterUser = await userRepo.GetByIdAsync(requesterId);
                if (requesterEmpSysId <= 0 && requesterUser?.EmpSysId.HasValue == true)
                {
                    requesterEmpSysId = requesterUser.EmpSysId.Value;
                }

                // 2. Query HR_EmpJobAssign table strictly using Rpt_Mng_Id to construct full management chain
                var managerChain = await GetManagerHierarchyChainFromJobAssignAsync(requesterEmpSysId);

                // 3. Determine the hierarchy level index for this step within the workflow
                var allVersionSteps = await _unitOfWork.Repository<WorkflowStep>().FindAsync(
                    s => s.WorkflowVersionId == step.WorkflowVersionId && !s.IsDeleted && s.IsActive);

                var hierarchySteps = allVersionSteps
                    .Where(s => s.ApprovalType == ApprovalType.ManagerHierarchy)
                    .OrderBy(s => s.Sequence)
                    .ThenBy(s => s.StepNumber)
                    .ToList();

                int hierarchyIndex = hierarchySteps.FindIndex(s => s.Id == step.Id);
                if (hierarchyIndex < 0)
                {
                    if (step.Name.Contains("Reporting Manager", StringComparison.OrdinalIgnoreCase))
                        hierarchyIndex = 0;
                    else if (step.Name.Contains("BU Head", StringComparison.OrdinalIgnoreCase) || step.Name.Contains("Business Unit", StringComparison.OrdinalIgnoreCase))
                        hierarchyIndex = 1;
                    else
                        hierarchyIndex = 0;
                }

                // 4. Resolve approver from managerChain based on Rpt_Mng_Id
                User? resolvedManager = null;
                if (managerChain.Count > 0)
                {
                    int targetMgrEmpSysId = hierarchyIndex < managerChain.Count 
                        ? managerChain[hierarchyIndex] 
                        : managerChain.Last();

                    var matchedUsers = await userRepo.FindAsync(u => u.EmpSysId == targetMgrEmpSysId && !u.IsDeleted && u.IsActive);
                    resolvedManager = matchedUsers.FirstOrDefault();
                }

                // 5. Fallback: check if step TargetUserId was configured directly
                if (resolvedManager == null)
                {
                    if (!string.IsNullOrEmpty(step.TargetUserId) && Guid.TryParse(step.TargetUserId, out var configuredTargetId))
                    {
                        resolvedManager = await userRepo.GetByIdAsync(configuredTargetId);
                    }
                    else if (!string.IsNullOrEmpty(step.TargetUserId) && int.TryParse(step.TargetUserId, out var configuredEmpSysId))
                    {
                        var matchedUsers = await userRepo.FindAsync(u => u.EmpSysId == configuredEmpSysId && !u.IsDeleted && u.IsActive);
                        resolvedManager = matchedUsers.FirstOrDefault();
                    }
                }

                // 6. Secondary fallback: Walk ManagerId in Users table
                if (resolvedManager == null && requesterUser != null)
                {
                    var currentUserInChain = requesterUser;
                    for (int i = 0; i <= hierarchyIndex; i++)
                    {
                        if (currentUserInChain?.ManagerId.HasValue == true)
                        {
                            var nextMgr = await userRepo.GetByIdAsync(currentUserInChain.ManagerId.Value);
                            if (i == hierarchyIndex && nextMgr != null)
                            {
                                resolvedManager = nextMgr;
                                break;
                            }
                            currentUserInChain = nextMgr;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (resolvedManager != null)
                {
                    approvers.Add(resolvedManager);
                }
                break;

            case ApprovalType.DynamicUser:
                if (inputData.TryGetValue("ApproverId", out var dynamicUserIdStr) && Guid.TryParse(dynamicUserIdStr, out Guid dynamicUserId))
                {
                    var user = await userRepo.GetByIdAsync(dynamicUserId);
                    if (user != null) approvers.Add(user);
                }
                break;

            default:
                var fallbackReq = await userRepo.GetByIdAsync(requesterId);
                if (fallbackReq != null && fallbackReq.ManagerId.HasValue)
                {
                    var manager = await userRepo.GetByIdAsync(fallbackReq.ManagerId.Value);
                    if (manager != null) approvers.Add(manager);
                }
                break;
        }

        // Final fallback: admin user if empty
        if (!approvers.Any())
        {
            var adminUsers = await userRepo.FindAsync(u => u.Username == "admin");
            var admin = adminUsers.FirstOrDefault();
            if (admin != null) approvers.Add(admin);
        }

        return approvers;
    }

    private async Task<List<int>> GetManagerHierarchyChainFromJobAssignAsync(int empSysId)
    {
        var chain = new List<int>();
        if (empSysId <= 0) return chain;

        var qhrConnStr = _configuration?.GetConnectionString("qHRconString");
        if (string.IsNullOrEmpty(qhrConnStr)) return chain;

        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(qhrConnStr);
            await conn.OpenAsync();

            int currentEmpSysId = empSysId;
            var visited = new HashSet<int> { empSysId };

            while (true)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT TOP 1 Rpt_Mng_Id 
                    FROM HR_EmpJobAssign 
                    WHERE Emp_Sys_Id = @EmpSysId AND Wk_Stat = 1 AND Rpt_Mng_Id IS NOT NULL AND Rpt_Mng_Id > 0
                    ORDER BY St_Date DESC";
                cmd.Parameters.AddWithValue("@EmpSysId", currentEmpSysId);
                var res = await cmd.ExecuteScalarAsync();

                if (res == null || res == DBNull.Value)
                {
                    cmd.CommandText = @"
                        SELECT TOP 1 Rpt_Mng_Id 
                        FROM HR_EmpJobAssign 
                        WHERE Emp_Sys_Id = @EmpSysId AND Rpt_Mng_Id IS NOT NULL AND Rpt_Mng_Id > 0
                        ORDER BY St_Date DESC";
                    res = await cmd.ExecuteScalarAsync();
                }

                if (res == null || res == DBNull.Value || !int.TryParse(res.ToString(), out int rptMngId) || rptMngId <= 0)
                {
                    break;
                }

                if (!visited.Add(rptMngId))
                {
                    break; // Prevent cyclical loops
                }

                chain.Add(rptMngId);
                currentEmpSysId = rptMngId;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ManagerHierarchy Error] EmpSysId {empSysId}: {ex.Message}");
        }

        return chain;
    }

    private async Task<string> GetOfficialEmailFromJobAssignAsync(int? empSysId)
    {
        if (!empSysId.HasValue || empSysId.Value <= 0) return string.Empty;
        var qhrConnStr = _configuration?.GetConnectionString("qHRconString");
        if (string.IsNullOrEmpty(qhrConnStr)) return string.Empty;

        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(qhrConnStr);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TOP 1 Ofc_Email 
                FROM HR_EmpJobAssign 
                WHERE Emp_Sys_Id = @EmpSysId AND Wk_Stat = 1 AND Ofc_Email IS NOT NULL AND LTRIM(RTRIM(Ofc_Email)) <> ''
                ORDER BY St_Date DESC";
            cmd.Parameters.AddWithValue("@EmpSysId", empSysId.Value);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
            {
                return result.ToString()!.Trim();
            }

            cmd.CommandText = @"
                SELECT TOP 1 Ofc_Email 
                FROM HR_EmpJobAssign 
                WHERE Emp_Sys_Id = @EmpSysId AND Ofc_Email IS NOT NULL AND LTRIM(RTRIM(Ofc_Email)) <> ''
                ORDER BY St_Date DESC";
            result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
            {
                return result.ToString()!.Trim();
            }
        }
        catch { }

        return string.Empty;
    }

    private async Task QueueApprovalNotificationAsync(WorkflowInstance instance, WorkflowStep step, User approver, Dictionary<string, string>? inputData = null)
    {
        // STRICT REQUIREMENT: Only Ofc_Email from HR_EmpJobAssign may be used for workflow notifications.
        string approverEmail = await GetOfficialEmailFromJobAssignAsync(approver.EmpSysId);
        if (string.IsNullOrWhiteSpace(approverEmail)) return;

        var mergedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (inputData != null)
        {
            foreach (var kvp in inputData)
            {
                mergedData[kvp.Key] = kvp.Value;
            }
        }

        // If mergedData is empty or missing key properties, also try reading from database
        if (mergedData.Count == 0 || !mergedData.ContainsKey("EmployeeName") || !mergedData.ContainsKey("LeaveType"))
        {
            var dataEntities = await _unitOfWork.Repository<WorkflowInstanceData>().FindAsync(d => d.WorkflowInstanceId == instance.Id);
            foreach (var d in dataEntities)
            {
                if (!string.IsNullOrWhiteSpace(d.FieldName) && !mergedData.ContainsKey(d.FieldName))
                {
                    mergedData[d.FieldName] = d.FieldValue ?? string.Empty;
                }
            }
        }

        // Requester details lookup for robust fallback
        var requester = instance.Requester;
        if (requester == null && instance.RequesterId != Guid.Empty)
        {
            requester = await _unitOfWork.Repository<User>().GetByIdAsync(instance.RequesterId);
        }

        string lvIdStr = mergedData.GetValueOrDefault("LvId", "");
        
        string employeeName = mergedData.GetValueOrDefault("EmployeeName", "");
        if (string.IsNullOrWhiteSpace(employeeName) || employeeName.Equals("Employee", StringComparison.OrdinalIgnoreCase))
        {
            employeeName = requester?.FullName ?? "Employee";
        }

        string department = mergedData.GetValueOrDefault("Department", mergedData.GetValueOrDefault("DeptName", ""));
        if (string.IsNullOrWhiteSpace(department) || department == "—")
        {
            if (requester?.DepartmentId.HasValue == true)
            {
                var deptObj = await _unitOfWork.Repository<Department>().GetByIdAsync(requester.DepartmentId.Value);
                if (deptObj != null && !string.IsNullOrWhiteSpace(deptObj.Name))
                {
                    department = deptObj.Name;
                }
            }
        }
        if (string.IsNullOrWhiteSpace(department)) department = "—";

        string leaveType = mergedData.GetValueOrDefault("LeaveType", mergedData.GetValueOrDefault("LvTypCode", ""));
        if (string.IsNullOrWhiteSpace(leaveType) || leaveType.Equals("Leave", StringComparison.OrdinalIgnoreCase))
        {
            if (instance.WorkflowVersion?.WorkflowDefinition != null)
            {
                leaveType = instance.WorkflowVersion.WorkflowDefinition.Name;
            }
            else if (instance.WorkflowVersionId != Guid.Empty)
            {
                var wv = await _unitOfWork.Repository<WorkflowVersion>().GetByIdAsync(instance.WorkflowVersionId);
                if (wv != null && wv.WorkflowDefinitionId != Guid.Empty)
                {
                    var def = await _unitOfWork.Repository<WorkflowDefinition>().GetByIdAsync(wv.WorkflowDefinitionId);
                    if (def != null && !string.IsNullOrWhiteSpace(def.Name))
                    {
                        leaveType = def.Name;
                    }
                }
            }
        }
        if (string.IsNullOrWhiteSpace(leaveType)) leaveType = "Leave";

        string startDate = mergedData.GetValueOrDefault("StartDate", mergedData.GetValueOrDefault("FromDate", ""));
        string endDate = mergedData.GetValueOrDefault("EndDate", mergedData.GetValueOrDefault("ToDate", ""));
        string days = mergedData.GetValueOrDefault("Days", mergedData.GetValueOrDefault("LeaveDays", mergedData.GetValueOrDefault("NoOfDays", "")));
        string reason = mergedData.GetValueOrDefault("Reason", mergedData.GetValueOrDefault("Note", mergedData.GetValueOrDefault("Remarks", "—")));
        if (string.IsNullOrWhiteSpace(reason)) reason = "—";

        // Format duration display gracefully
        string durationText;
        if (!string.IsNullOrWhiteSpace(startDate) && !string.IsNullOrWhiteSpace(endDate))
        {
            string daysSuffix = !string.IsNullOrWhiteSpace(days) ? $" ({days} {(days == "1" ? "Day" : "Days")})" : "";
            durationText = startDate == endDate 
                ? $"{startDate}{daysSuffix}" 
                : $"{startDate} to {endDate}{daysSuffix}";
        }
        else if (!string.IsNullOrWhiteSpace(startDate))
        {
            string daysSuffix = !string.IsNullOrWhiteSpace(days) ? $" ({days} {(days == "1" ? "Day" : "Days")})" : "";
            durationText = $"{startDate}{daysSuffix}";
        }
        else if (!string.IsNullOrWhiteSpace(days))
        {
            durationText = $"{days} {(days == "1" ? "Day" : "Days")}";
        }
        else
        {
            durationText = "—";
        }

        string appUrl = mergedData.GetValueOrDefault("AppBaseUrl", mergedData.GetValueOrDefault("AppUrl", ""));
        if (string.IsNullOrWhiteSpace(appUrl))
        {
            appUrl = _configuration?["AppSettings:AppBaseUrl"] ?? _configuration?["ApplicationSettings:qHRMBaseUrl"] ?? "http://localhost:55465";
        }
        appUrl = appUrl.TrimEnd('/');
        if (appUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            appUrl = appUrl.Substring(0, appUrl.Length - 4).TrimEnd('/');
        }
        string actionUrl = !string.IsNullOrEmpty(lvIdStr)
            ? $"{appUrl}/Leave/HrLvRegister/Review?openLvId={lvIdStr}&instanceId={instance.Id}&action=approve"
            : $"{appUrl}/Leave/HrLvRegister/Review?instanceId={instance.Id}&action=approve";

        var subject = $"Leave Approval Required: {leaveType} – {employeeName} ({step.Name})";
        var body = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px;'>
  <table width='100%' border='0' cellspacing='0' cellpadding='0'>
    <tr>
      <td align='center'>
        <table width='600' border='0' cellspacing='0' cellpadding='0' style='background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;'>
          <tr>
            <td style='background: #540033; padding: 25px; text-align: center; color: #ffffff;'>
              <h2 style='margin: 0; font-size: 22px; font-weight: 600;'>Leave Approval Required</h2>
              <p style='margin: 5px 0 0; opacity: 0.9; font-size: 14px;'>Step: {step.Name} | {instance.InstanceNumber}</p>
            </td>
          </tr>
          <tr>
            <td style='padding: 30px 25px; color: #333333;'>
              <p style='font-size: 15px; margin-top: 0;'>Dear <strong>{approver.FullName}</strong>,</p>
              <p style='font-size: 14px; line-height: 1.6;'>A leave application has been submitted and is currently pending your approval at <strong>{step.Name}</strong>.</p>
              
              <table width='100%' style='background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 20px 0; border-collapse: collapse;'>
                <tr><td style='padding: 10px 15px; font-weight: bold; width: 35%; border-bottom: 1px solid #edf2f7;'>Employee:</td><td style='padding: 10px 15px; border-bottom: 1px solid #edf2f7;'>{employeeName}</td></tr>
                <tr><td style='padding: 10px 15px; font-weight: bold; border-bottom: 1px solid #edf2f7;'>Department:</td><td style='padding: 10px 15px; border-bottom: 1px solid #edf2f7;'>{department}</td></tr>
                <tr><td style='padding: 10px 15px; font-weight: bold; border-bottom: 1px solid #edf2f7;'>Leave Type:</td><td style='padding: 10px 15px; border-bottom: 1px solid #edf2f7;'>{leaveType}</td></tr>
                <tr><td style='padding: 10px 15px; font-weight: bold; border-bottom: 1px solid #edf2f7;'>Duration:</td><td style='padding: 10px 15px; border-bottom: 1px solid #edf2f7;'>{durationText}</td></tr>
                <tr><td style='padding: 10px 15px; font-weight: bold;'>Reason:</td><td style='padding: 10px 15px;'>{reason}</td></tr>
              </table>

              <div style='text-align: center; margin: 30px 0;'>
                <a href='{actionUrl}' style='background: #540033; color: #ffffff; padding: 13px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px; display: inline-block;'>Review &amp; Take Action</a>
              </div>
              <p style='font-size: 12px; color: #718096; text-align: center;'>Direct link: <a href='{actionUrl}' style='color: #540033;'>{actionUrl}</a></p>
            </td>
          </tr>
          <tr>
            <td style='background: #f7fafc; padding: 15px; text-align: center; font-size: 12px; color: #a0aec0; border-top: 1px solid #edf2f7;'>
              This is an automated notification from the QCS Workflow Engine.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        await QueueNotificationAsync(approverEmail, subject, body);
    }

    private async Task QueueNotificationsForStatusChangeAsync(WorkflowInstance instance, Dictionary<string, string>? inputData = null)
    {
        var requester = await _unitOfWork.Repository<User>().GetByIdAsync(instance.RequesterId);
        if (requester == null) return;

        // STRICT REQUIREMENT: Only Ofc_Email from HR_EmpJobAssign may be used for workflow notifications.
        string requesterEmail = await GetOfficialEmailFromJobAssignAsync(requester.EmpSysId);
        if (string.IsNullOrWhiteSpace(requesterEmail)) return;

        var mergedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (inputData != null)
        {
            foreach (var kvp in inputData) mergedData[kvp.Key] = kvp.Value;
        }
        if (mergedData.Count == 0 || !mergedData.ContainsKey("LeaveType"))
        {
            var dataEntities = await _unitOfWork.Repository<WorkflowInstanceData>().FindAsync(d => d.WorkflowInstanceId == instance.Id);
            foreach (var d in dataEntities)
            {
                if (!string.IsNullOrWhiteSpace(d.FieldName) && !mergedData.ContainsKey(d.FieldName))
                    mergedData[d.FieldName] = d.FieldValue ?? string.Empty;
            }
        }

        string leaveType = mergedData.GetValueOrDefault("LeaveType", mergedData.GetValueOrDefault("LvTypCode", ""));
        if (string.IsNullOrWhiteSpace(leaveType) || leaveType.Equals("Leave", StringComparison.OrdinalIgnoreCase))
        {
            if (instance.WorkflowVersionId != Guid.Empty)
            {
                var wv = await _unitOfWork.Repository<WorkflowVersion>().GetByIdAsync(instance.WorkflowVersionId);
                if (wv != null && wv.WorkflowDefinitionId != Guid.Empty)
                {
                    var def = await _unitOfWork.Repository<WorkflowDefinition>().GetByIdAsync(wv.WorkflowDefinitionId);
                    if (def != null && !string.IsNullOrWhiteSpace(def.Name))
                    {
                        leaveType = def.Name;
                    }
                }
            }
        }
        if (string.IsNullOrWhiteSpace(leaveType)) leaveType = "Leave";

        string startDate = mergedData.GetValueOrDefault("StartDate", mergedData.GetValueOrDefault("FromDate", ""));
        string endDate = mergedData.GetValueOrDefault("EndDate", mergedData.GetValueOrDefault("ToDate", ""));
        string days = mergedData.GetValueOrDefault("Days", mergedData.GetValueOrDefault("LeaveDays", mergedData.GetValueOrDefault("NoOfDays", "")));

        string durationText = "";
        if (!string.IsNullOrWhiteSpace(startDate) && !string.IsNullOrWhiteSpace(endDate))
        {
            string daysSuffix = !string.IsNullOrWhiteSpace(days) ? $" ({days} {(days == "1" ? "Day" : "Days")})" : "";
            durationText = startDate == endDate ? $"{startDate}{daysSuffix}" : $"{startDate} to {endDate}{daysSuffix}";
        }
        else if (!string.IsNullOrWhiteSpace(startDate))
        {
            string daysSuffix = !string.IsNullOrWhiteSpace(days) ? $" ({days} {(days == "1" ? "Day" : "Days")})" : "";
            durationText = $"{startDate}{daysSuffix}";
        }
        else if (!string.IsNullOrWhiteSpace(days))
        {
            durationText = $"{days} {(days == "1" ? "Day" : "Days")}";
        }

        string appUrl = mergedData.GetValueOrDefault("AppBaseUrl", mergedData.GetValueOrDefault("AppUrl", ""));
        if (string.IsNullOrWhiteSpace(appUrl))
        {
            appUrl = _configuration?["AppSettings:AppBaseUrl"] ?? _configuration?["ApplicationSettings:qHRMBaseUrl"] ?? "http://localhost:55465";
        }
        appUrl = appUrl.TrimEnd('/');
        if (appUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            appUrl = appUrl.Substring(0, appUrl.Length - 4).TrimEnd('/');
        }

        bool isApproved = instance.Status == WorkflowInstanceStatus.Approved;
        string statusText = isApproved ? "Approved" : (instance.Status == WorkflowInstanceStatus.Rejected ? "Rejected" : instance.Status.ToString());
        string statusBadgeColor = isApproved ? "#16a34a" : "#dc2626";

        var subject = $"Leave Application Update: {leaveType} – {statusText}";
        var body = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px;'>
  <table width='100%' border='0' cellspacing='0' cellpadding='0'>
    <tr>
      <td align='center'>
        <table width='600' border='0' cellspacing='0' cellpadding='0' style='background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;'>
          <tr>
            <td style='background: #540033; padding: 25px; text-align: center; color: #ffffff;'>
              <h2 style='margin: 0; font-size: 22px; font-weight: 600;'>Leave Application Status Update</h2>
              <p style='margin: 5px 0 0; opacity: 0.9; font-size: 14px;'>{instance.InstanceNumber}</p>
            </td>
          </tr>
          <tr>
            <td style='padding: 30px 25px; color: #333333;'>
              <p style='font-size: 15px; margin-top: 0;'>Dear <strong>{requester.FullName}</strong>,</p>
              <p style='font-size: 14px; line-height: 1.6;'>Your leave request for <strong>{leaveType}</strong> is now: <span style='font-weight: bold; color: {statusBadgeColor};'>{statusText}</span>.</p>

              {(string.IsNullOrWhiteSpace(durationText) ? "" : $@"
              <table width='100%' style='background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 20px 0; border-collapse: collapse;'>
                <tr><td style='padding: 10px 15px; font-weight: bold; width: 35%; border-bottom: 1px solid #edf2f7;'>Leave Type:</td><td style='padding: 10px 15px; border-bottom: 1px solid #edf2f7;'>{leaveType}</td></tr>
                <tr><td style='padding: 10px 15px; font-weight: bold; border-bottom: 1px solid #edf2f7;'>Duration:</td><td style='padding: 10px 15px; border-bottom: 1px solid #edf2f7;'>{durationText}</td></tr>
                <tr><td style='padding: 10px 15px; font-weight: bold;'>Status:</td><td style='padding: 10px 15px; font-weight: bold; color: {statusBadgeColor};'>{statusText}</td></tr>
              </table>")}

              <div style='text-align: center; margin: 30px 0;'>
                <a href='{appUrl}/Leave/HrLvRegister/Index' style='background: #540033; color: #ffffff; padding: 13px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px; display: inline-block;'>View Leave Register</a>
              </div>
            </td>
          </tr>
          <tr>
            <td style='background: #f7fafc; padding: 15px; text-align: center; font-size: 12px; color: #a0aec0; border-top: 1px solid #edf2f7;'>
              This is an automated notification from the QCS Workflow Engine.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        await QueueNotificationAsync(requesterEmail, subject, body);
    }

    private async Task QueueNotificationAsync(string email, string subject, string body)
    {
        var emailQueueRepo = _unitOfWork.Repository<EmailQueue>();
        await emailQueueRepo.AddAsync(new EmailQueue
        {
            RecipientEmail = email,
            Subject = subject,
            Body = body,
            IsSent = false
        });
    }
}
