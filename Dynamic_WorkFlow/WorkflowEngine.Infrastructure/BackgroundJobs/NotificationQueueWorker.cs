using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.Infrastructure.BackgroundJobs;

public class NotificationQueueWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationQueueWorker> _logger;

    public NotificationQueueWorker(IServiceProvider serviceProvider, ILogger<NotificationQueueWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationQueueWorker background job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
                    var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    // 1. Process Email Queue
                    var pendingEmails = await context.EmailQueues
                        .Where(e => !e.IsSent && e.RetryCount < 3 && !e.IsDeleted)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    foreach (var email in pendingEmails)
                    {
                        try
                        {
                            await notifier.SendEmailAsync(email.RecipientEmail, email.Subject, email.Body);
                            email.IsSent = true;
                            email.SentDate = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            email.RetryCount++;
                            email.ErrorMessage = ex.Message;
                        }
                        context.EmailQueues.Update(email);
                    }

                    // 2. Process SMS Queue
                    var pendingSms = await context.SMSQueues
                        .Where(s => !s.IsSent && s.RetryCount < 3 && !s.IsDeleted)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    foreach (var sms in pendingSms)
                    {
                        try
                        {
                            await notifier.SendSMSAsync(sms.RecipientPhone, sms.Message);
                            sms.IsSent = true;
                            sms.SentDate = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            sms.RetryCount++;
                            sms.ErrorMessage = ex.Message;
                        }
                        context.SMSQueues.Update(sms);
                    }

                    // 3. Process Webhook Queue
                    var pendingWebhooks = await context.WebhookQueues
                        .Where(w => !w.IsSent && w.RetryCount < 3 && !w.IsDeleted)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    foreach (var wh in pendingWebhooks)
                    {
                        try
                        {
                            await notifier.TriggerWebhookAsync(wh.TargetUrl, wh.Payload);
                            wh.IsSent = true;
                            wh.SentDate = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            wh.RetryCount++;
                            wh.ErrorMessage = ex.Message;
                        }
                        context.WebhookQueues.Update(wh);
                    }

                    if (pendingEmails.Any() || pendingSms.Any() || pendingWebhooks.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the NotificationQueueWorker background job.");
            }

            // Check every 10 seconds
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("NotificationQueueWorker background job stopped.");
    }
}
