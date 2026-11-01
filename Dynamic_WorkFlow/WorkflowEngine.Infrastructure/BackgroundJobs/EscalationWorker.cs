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
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.Infrastructure.BackgroundJobs;

public class EscalationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EscalationWorker> _logger;

    public EscalationWorker(IServiceProvider serviceProvider, ILogger<EscalationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EscalationWorker background job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
                    var workflowEngine = scope.ServiceProvider.GetRequiredService<IWorkflowEngine>();

                    // Find all pending steps where DueDate is in the past
                    var pendingSteps = await context.WorkflowInstanceSteps
                        .Where(s => s.Status == StepStatus.Pending && s.DueDate.HasValue && s.DueDate.Value < DateTime.UtcNow && !s.IsDeleted)
                        .ToListAsync(stoppingToken);

                    if (pendingSteps.Any())
                    {
                        _logger.LogInformation($"Found {pendingSteps.Count} steps violating SLA. Processing escalations...");
                        foreach (var step in pendingSteps)
                        {
                            _logger.LogInformation($"Escalating step '{step.Name}' (ID: {step.Id}) under Instance ID: {step.WorkflowInstanceId}");
                            await workflowEngine.EscalateStepAsync(step.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the EscalationWorker background job.");
            }

            // Check every 60 seconds
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }

        _logger.LogInformation("EscalationWorker background job stopped.");
    }
}
