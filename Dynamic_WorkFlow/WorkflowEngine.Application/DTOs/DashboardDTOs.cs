using System;
using System.Collections.Generic;

namespace WorkflowEngine.Application.DTOs;

public record DashboardMetricsDto(
    int TotalRequests,
    int PendingRequests,
    int ApprovedRequests,
    int RejectedRequests,
    double AverageApprovalTimeHours,
    int SlaViolationsCount,
    List<MonthlyStatDto> MonthlyStats,
    List<WorkflowStatDto> WorkflowStats,
    List<DepartmentStatDto> DepartmentStats,
    List<TopDelayUserDto> TopDelayUsers
);

public record MonthlyStatDto(string Month, int Total, int Approved, int Rejected);

public record WorkflowStatDto(string WorkflowName, int Count, double AvgTimeHours);

public record DepartmentStatDto(string DepartmentName, int Count);

public record TopDelayUserDto(string UserName, string DepartmentName, int DelayCount, double AverageDelayHours);

public record PendingTaskDto(
    Guid ApprovalId,
    Guid InstanceId,
    string InstanceNumber,
    string WorkflowName,
    string CurrentStepName,
    string RequesterName,
    DateTime RequestDate,
    DateTime DueDate
);
