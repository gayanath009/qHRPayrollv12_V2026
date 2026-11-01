using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Services;

namespace WorkflowEngine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient<ConditionEvaluator>();
        services.AddScoped<IWorkflowEngine, Services.WorkflowEngine>();
        return services;
    }
}
