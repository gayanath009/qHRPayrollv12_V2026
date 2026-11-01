using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Interfaces;

namespace WorkflowEngine.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("[NOTIFICATION - EMAIL] Cannot send email: recipient address is empty.");
            return;
        }

        try
        {
            // Dynamically load SMTP configuration strictly from qHRMaster database (SYS_Config)
            var qhrConnStr = _configuration.GetConnectionString("qHRconString");
            if (string.IsNullOrEmpty(qhrConnStr))
            {
                _logger.LogError("[NOTIFICATION - EMAIL] Connection string 'qHRconString' is not configured in appsettings.json. Cannot load SMTP settings.");
                return;
            }

            string? server = null;
            int port = 25;
            string? fromEmail = null;
            string? serviceAcct = null;
            string? servicePwd = null;

            using (var conn = new SqlConnection(qhrConnStr))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TOP 1 Server, Port, From_Email, Service_Acct, Service_AcctPwd FROM SYS_Config";
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    server = r["Server"]?.ToString()?.Trim();
                    var portStr = r["Port"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int parsedPort))
                    {
                        port = parsedPort;
                    }
                    fromEmail = r["From_Email"]?.ToString()?.Trim();
                    serviceAcct = r["Service_Acct"]?.ToString()?.Trim();
                    servicePwd = r["Service_AcctPwd"]?.ToString()?.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(server))
            {
                _logger.LogError("[NOTIFICATION - EMAIL] SMTP Server is not configured in SYS_Config table of qHRM database.");
                return;
            }

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = serviceAcct;
            }

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("[NOTIFICATION - EMAIL] From_Email is not configured in SYS_Config table of qHRM database.");
                return;
            }

            string decryptedPwd = (!string.IsNullOrEmpty(serviceAcct) && !string.IsNullOrEmpty(servicePwd))
                ? WorkflowEngine.Infrastructure.Common.CryptoSecurityHelper.Decrypt(servicePwd)
                : string.Empty;

            bool isExchangePort25OnWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) &&
                                             port == 25 &&
                                             !string.IsNullOrEmpty(serviceAcct);

            if (isExchangePort25OnWindows)
            {
                try
                {
                    int psRes = await Task.Run(() => SendViaWindowsPowerShell(server, port, serviceAcct, decryptedPwd, fromEmail, to, subject, body));
                    if (psRes == 1)
                    {
                        _logger.LogInformation("[NOTIFICATION - EMAIL SENT VIA POWERSHELL] To: {To} | Subject: {Subject} | Server: {Server}:{Port}", to, subject, server, port);
                        return;
                    }
                }
                catch (Exception psEx)
                {
                    _logger.LogWarning(psEx, "[NOTIFICATION - EMAIL POWERSHELL DISPATCH FAILED, trying SmtpClient]");
                }
            }

            using var client = new SmtpClient(server, port)
            {
                EnableSsl = (port == 587 || port == 465),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 15000
            };

            if (!string.IsNullOrEmpty(serviceAcct) && !string.IsNullOrEmpty(decryptedPwd))
            {
                client.Credentials = new NetworkCredential(serviceAcct, decryptedPwd);
            }
            else
            {
                client.UseDefaultCredentials = true;
            }

            using var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, "QCS Workflow Engine"),
                Subject = subject,
                Body = body.Contains("<html", StringComparison.OrdinalIgnoreCase) || body.Contains("<div", StringComparison.OrdinalIgnoreCase) || body.Contains("<p>", StringComparison.OrdinalIgnoreCase)
                    ? body 
                    : $"<div style=\"font-family: Arial, sans-serif; font-size: 14px; color: #333; line-height: 1.6;\">{body.Replace("\n", "<br/>")}</div>",
                IsBodyHtml = true
            };

            mail.To.Add(to);

            try
            {
                await client.SendMailAsync(mail);
                _logger.LogInformation("[NOTIFICATION - EMAIL SENT] To: {To} | Subject: {Subject} | Server: {Server}:{Port}", to, subject, server, port);
            }
            catch (Exception ex)
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    _logger.LogWarning("[NOTIFICATION - EMAIL] SmtpClient failed ({Error}), attempting Windows PowerShell SSPI fallback...", ex.Message);
                    int psRes = await Task.Run(() => SendViaWindowsPowerShell(server, port, serviceAcct, decryptedPwd, fromEmail, to, subject, body));
                    if (psRes == 1)
                    {
                        _logger.LogInformation("[NOTIFICATION - EMAIL SENT VIA POWERSHELL FALLBACK] To: {To} | Subject: {Subject}", to, subject);
                        return;
                    }
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION - EMAIL FAILED] To: {To} | Subject: {Subject} | Error: {Error}", to, subject, ex.Message);
            throw;
        }
    }

    private static int SendViaWindowsPowerShell(
        string server,
        int port,
        string? user,
        string? pwd,
        string fromEmail,
        string toEmail,
        string subject,
        string body)
    {
        try
        {
            var payload = new
            {
                Server = server,
                Port = port,
                User = user,
                Password = pwd,
                From = fromEmail,
                To = toEmail,
                Subject = subject,
                Body = body
            };
            string json = System.Text.Json.JsonSerializer.Serialize(payload);
            string b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

            string script = $@"
$raw = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{b64}'))
$data = ConvertFrom-Json $raw

$client = New-Object System.Net.Mail.SmtpClient($data.Server, [int]$data.Port)
if (![string]::IsNullOrEmpty($data.User) -and ![string]::IsNullOrEmpty($data.Password)) {{
    $client.Credentials = New-Object System.Net.NetworkCredential($data.User, $data.Password)
    $client.UseDefaultCredentials = $false
}} else {{
    $client.UseDefaultCredentials = $true
}}
$client.DeliveryMethod = [System.Net.Mail.SmtpDeliveryMethod]::Network
if ([int]$data.Port -eq 587 -or [int]$data.Port -eq 465) {{
    $client.EnableSsl = $true
}}

$mail = New-Object System.Net.Mail.MailMessage
$mail.From = New-Object System.Net.Mail.MailAddress($data.From)
$mail.To.Add($data.To)
$mail.Subject = $data.Subject
$mail.Body = $data.Body
$mail.IsBodyHtml = $true

$client.Send($mail)
$mail.Dispose()
$client.Dispose()
Write-Output 'DISPATCH_OK'
";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command -",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return 0;

            proc.StandardInput.WriteLine(script);
            proc.StandardInput.Close();

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(30000);

            return output.Contains("DISPATCH_OK") ? 1 : 0;
        }
        catch
        {
            return 0;
        }
    }

    public Task SendSMSAsync(string to, string message)
    {
        Console.WriteLine($"[NOTIFICATION - SMS] To: {to} | Message: {message}");
        return Task.CompletedTask;
    }

    public Task TriggerWebhookAsync(string url, string payload)
    {
        Console.WriteLine($"[NOTIFICATION - WEBHOOK] POST to: {url}");
        return Task.CompletedTask;
    }
}

