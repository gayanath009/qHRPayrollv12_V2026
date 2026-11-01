using System.Threading.Tasks;

namespace WorkflowEngine.Application.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendSMSAsync(string to, string message);
    Task TriggerWebhookAsync(string url, string payload);
}
