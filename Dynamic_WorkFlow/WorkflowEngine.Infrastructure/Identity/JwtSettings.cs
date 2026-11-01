namespace WorkflowEngine.Infrastructure.Identity;

public class JwtSettings
{
    public string Secret { get; set; } = "SuperSecretKeyForWorkflowEngineSecurityToken123!";
    public int ExpiryMinutes { get; set; } = 120;
    public int RefreshTokenExpiryDays { get; set; } = 7;
    public string Issuer { get; set; } = "WorkflowEngineAPI";
    public string Audience { get; set; } = "WorkflowEngineUI";
}
