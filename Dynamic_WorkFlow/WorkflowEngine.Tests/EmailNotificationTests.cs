using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Infrastructure.Notifications;
using Xunit;

namespace WorkflowEngine.Tests;

public class EmailNotificationTests
{
    [Fact]
    public async Task SendTestEmail_UsingWorkflowNotificationService_qHRMasterV27()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:qHRconString", "server=SOFT-NB-095;database=qHRMasterV27;uid=sa;password=sa#12345678;multipleactiveresultsets=true;trustservercertificate=true;" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger<NotificationService>.Instance;
        var service = new NotificationService(config, logger);

        // Diagnostic test with port 587 / SSL
        var qhrConnStr = config.GetConnectionString("qHRconString");
        string? server = null;
        int port = 25;
        string? fromEmail = null;
        string? serviceAcct = null;
        string? servicePwd = null;

        using (var conn = new Microsoft.Data.SqlClient.SqlConnection(qhrConnStr))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 Server, Port, From_Email, Service_Acct, Service_AcctPwd FROM SYS_Config";
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                server = r["Server"]?.ToString()?.Trim();
                fromEmail = r["From_Email"]?.ToString()?.Trim();
                serviceAcct = r["Service_Acct"]?.ToString()?.Trim();
                servicePwd = r["Service_AcctPwd"]?.ToString()?.Trim();
            }
        }

        await service.SendEmailAsync(
            "gayanath@qcs-qatar.com",
            "Test Email from QCS Workflow Notification Service",
            "Hello Gayanath,<br/><br/>This is an automated test email sent through the WorkflowEngine Notification Service to verify that email notifications and the encrypted SMTP database credentials in SYS_Config are functioning properly.<br/><br/>Regards,<br/>QCS Workflow Engine"
        );
    }

    [Fact]
    public async Task SendTestEmail_UsingWorkflowNotificationService_qHRMasterLive()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:qHRconString", "server=DB03\\SQL01;database=qHRMasterLive;uid=sa;password=Db@DB03SQL01;multipleactiveresultsets=true;trustservercertificate=true;" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger<NotificationService>.Instance;
        var service = new NotificationService(config, logger);

        await service.SendEmailAsync(
            "gayanath@qcs-qatar.com",
            "Test Email from QCS Workflow Notification Service (Live DB Settings)",
            "Hello Gayanath,<br/><br/>This is an automated test email sent through the WorkflowEngine Notification Service using SMTP settings configured in <b>qHRMasterLive (DB03\\SQL01)</b> with decrypted Service_AcctPwd.<br/><br/>Regards,<br/>QCS Workflow Engine"
        );
    }
}
