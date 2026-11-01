using System;
using WorkflowEngine.Domain.Common;

namespace WorkflowEngine.Domain.Entities;

public class WorkflowAuditLog : BaseEntity
{
    public string TableName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty; // Insert, Update, Delete
    public string? ColumnName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string PerformedBy { get; set; } = "System";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class WorkflowEvent : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public string EventName { get; set; } = string.Empty; // e.g. InstanceCreated, StepApproved, Escalated
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; } = DateTime.UtcNow;
}

public class NotificationTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g. "ANNUAL_LEAVE_APPROVAL_REQ"
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty; // Markdown or HTML with template placeholders
    public string Type { get; set; } = "Email"; // Email, SMS, Slack, Webhook, Teams
}

public class EmailQueue : BaseEntity
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime? SentDate { get; set; }
}

public class SMSQueue : BaseEntity
{
    public string RecipientPhone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime? SentDate { get; set; }
}

public class WebhookQueue : BaseEntity
{
    public string TargetUrl { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty; // JSON payload
    public bool IsSent { get; set; } = false;
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime? SentDate { get; set; }
}

public class ActivityLog : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Browser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SystemLog : BaseEntity
{
    public string LogLevel { get; set; } = "Error"; // Info, Warning, Error, Fatal
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Source { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class APIClient : BaseEntity
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecretHash { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? AllowedScopes { get; set; } // Comma-separated scopes
}

public class APIAccessToken : BaseEntity
{
    public Guid ApiClientId { get; set; }
    public APIClient ApiClient { get; set; } = null!;
    
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;
}
