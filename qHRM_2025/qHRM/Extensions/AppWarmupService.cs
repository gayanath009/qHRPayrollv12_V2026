using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using qHRM.Areas.Identity.Data;
using qHRM.Data;
using qHRM.Models;

namespace qHRM.Extensions
{
    /// <summary>
    /// Background service that proactively warms up EF Core models, SQL connection pools,
    /// and Microsoft Entra ID OpenID Connect metadata immediately after application startup.
    /// This eliminates the initial "cold start" delay on the first login attempt.
    /// </summary>
    public class AppWarmupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppWarmupService> _logger;

        public AppWarmupService(IServiceProvider serviceProvider, ILogger<AppWarmupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Run in a background thread so web host startup finishes immediately
            _ = Task.Run(async () =>
            {
                try
                {
                    // Brief delay to allow web server to bind ports and finish pipeline setup
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                    using var scope = _serviceProvider.CreateScope();

                    // 1. Warm up qHRMasterContext: pre-compile large EF Core metadata model and warm connection pool
                    try
                    {
                        var masterContext = scope.ServiceProvider.GetService<qHRMasterContext>();
                        if (masterContext != null)
                        {
                            _logger.LogInformation("[Warmup] Pre-compiling EF Core model for qHRMasterContext...");
                            _ = masterContext.Model; // Forces in-memory generation of the relational metadata model
                            await masterContext.Database.ExecuteSqlRawAsync("SELECT 1;").ConfigureAwait(false);
                            _logger.LogInformation("[Warmup] qHRMasterContext model compiled and DB connection warmed up.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Warmup] Failed to warm up qHRMasterContext: {Message}", ex.Message);
                    }

                    // 2. Warm up qHRMContext (Identity): pre-compile Identity EF Core model
                    try
                    {
                        var identityContext = scope.ServiceProvider.GetService<qHRMContext>();
                        if (identityContext != null)
                        {
                            _logger.LogInformation("[Warmup] Pre-compiling EF Core model for qHRMContext (Identity)...");
                            _ = identityContext.Model;
                            await identityContext.Database.ExecuteSqlRawAsync("SELECT 1;").ConfigureAwait(false);
                            _logger.LogInformation("[Warmup] qHRMContext model compiled and DB connection warmed up.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Warmup] Failed to warm up qHRMContext: {Message}", ex.Message);
                    }

                    // 3. Warm up Microsoft Entra ID OpenID Connect discovery metadata and JWKS signing keys
                    try
                    {
                        var oidcOptionsMonitor = scope.ServiceProvider.GetService<IOptionsMonitor<OpenIdConnectOptions>>();
                        var oidcOptions = oidcOptionsMonitor?.Get(OpenIdConnectDefaults.AuthenticationScheme);
                        if (oidcOptions?.ConfigurationManager != null)
                        {
                            _logger.LogInformation("[Warmup] Pre-fetching Microsoft Entra ID OpenID Connect metadata and JWKS keys...");
                            await oidcOptions.ConfigurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
                            _logger.LogInformation("[Warmup] Entra ID discovery metadata pre-cached successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Warmup] Failed to pre-fetch Entra ID metadata: {Message}", ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Warmup] Warmup service encountered an unexpected error: {Message}", ex.Message);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
