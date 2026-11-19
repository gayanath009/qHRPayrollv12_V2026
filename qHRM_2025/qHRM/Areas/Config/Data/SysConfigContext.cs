using Microsoft.EntityFrameworkCore;
using qHRM.Models;
using qHRM.Areas.Email.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace qHRM.Area.Config
{
    public class SysConfigContext 
    {
        
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;        
        #endregion

        #region ==============Constructors==============
        public SysConfigContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
        }


        public IEnumerable<SysConfig> GetSysCongif()
        {
            try
            {
                var x = _hrmContext.SysConfig;
                return x;

            }
            catch (Exception ex) { throw ex; }
        }

        public int Update(SysConfig sysCfg)
        {
            try
            {
                var varChanges = _hrmContext.SysConfig.Attach(sysCfg);
                varChanges.State = EntityState.Modified;
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        #endregion

        /// <summary>
        /// Creates a configured SmtpClient from the given SysConfig and decrypted password.
        /// </summary>
        public SmtpClient CreateSmtpClient(SysConfig sysConfig, string decryptedPwd)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            int port = sysConfig.Port > 0 ? sysConfig.Port : 587;
            var client = new SmtpClient(sysConfig.Server, port)
            {
                Timeout = 30000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrEmpty(sysConfig.ServiceAcct) && !string.IsNullOrEmpty(decryptedPwd))
            {
                client.Credentials = new NetworkCredential(sysConfig.ServiceAcct, decryptedPwd);
                client.UseDefaultCredentials = false;
            }
            else
            {
                client.UseDefaultCredentials = true;
            }

            if (port == 587 || port == 465)
            {
                client.EnableSsl = true;
            }

            return client;
        }

        /// <summary>
        /// Resolves the sender email address and decrypted SMTP password from SysConfig.
        /// </summary>
        public (SysConfig Config, string FromEmail, string DecryptedPwd) GetResolvedEmailConfig(string overrideFromEmail = null)
        {
            SysConfig cfg = GetSysCongif().FirstOrDefault();
            if (cfg == null) return (null, null, null);

            string resolvedFrom = string.IsNullOrEmpty(overrideFromEmail) ? cfg.FromEmail : overrideFromEmail;
            if (string.IsNullOrEmpty(resolvedFrom) && !string.IsNullOrEmpty(cfg.ServiceAcct))
            {
                resolvedFrom = cfg.ServiceAcct;
            }

            string decryptedPwd = (!string.IsNullOrEmpty(cfg.ServiceAcct) && !string.IsNullOrEmpty(cfg.ServiceAcctPwd))
                ? qHRM.SharedSysFuncs.CryptoSecurityHelper.Decrypt(cfg.ServiceAcctPwd)
                : null;

            return (cfg, resolvedFrom?.Trim(), decryptedPwd);
        }

        /// <summary>
        /// Sends an email using an existing SmtpClient session for high-throughput batch dispatch.
        /// Returns 1 on success, 0 on failure (rethrows SmtpException/IOException if caller wants to reconnect).
        /// </summary>
        public async Task<int> SendWithClientAsync(SmtpClient client, SysConfig cfg, string fromEmail, string decryptedPwd, string toEmail, string subject, string message, string filepath, bool allowFallback = true)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || cfg == null) return 0;
            string cleanTo = toEmail.Trim();

            try
            {
                MailAddress fromAddr = new MailAddress(fromEmail, "qHR System");
                MailAddress toAddr = new MailAddress(cleanTo);
                using (MailMessage msg = new MailMessage(fromAddr, toAddr))
                {
                    msg.Body = message;
                    msg.Subject = subject;
                    msg.IsBodyHtml = true;

                    if (!string.IsNullOrEmpty(filepath) && System.IO.File.Exists(filepath))
                    {
                        msg.Attachments.Add(new Attachment(filepath));
                    }

                    await client.SendMailAsync(msg);
                    Console.WriteLine($"[EMAIL SUCCESS] Email successfully sent to {cleanTo}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SmtpClient failed for {cleanTo}: {ex.Message}");
                if (allowFallback && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        int psRes = await Task.Run(() => SendViaWindowsPowerShell(
                            cfg.Server,
                            cfg.Port > 0 ? cfg.Port : 587,
                            cfg.ServiceAcct,
                            decryptedPwd,
                            fromEmail,
                            cleanTo,
                            subject,
                            message,
                            filepath));
                        if (psRes == 1) return 1;
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[EMAIL FALLBACK ERROR] {ex2.Message}");
                    }
                }
                return 0;
            }
        }

        public async Task<int> SendEmailNotificationAsync(string toEmail, string fromEmail, string subject, string message, string filepath)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return 0;

            try
            {
                var (cfg, resolvedFrom, decryptedPwd) = GetResolvedEmailConfig(fromEmail);
                if (cfg == null) return 0;

                using (SmtpClient client = CreateSmtpClient(cfg, decryptedPwd))
                {
                    Console.WriteLine($"[EMAIL ATTEMPT] Sending email to {toEmail} from {resolvedFrom} via {cfg.Server}:{cfg.Port} (SSL={client.EnableSsl}, Auth={!string.IsNullOrEmpty(cfg.ServiceAcct)})");
                    return await SendWithClientAsync(client, cfg, resolvedFrom, decryptedPwd, toEmail, subject, message, filepath, allowFallback: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL CRITICAL ERROR]: {ex.Message}");
                return 0;
            }
        }

        public int SendEmailNotification(string toEmail, string fromEmail, string subject, string message, string filepath)
        {
            return SendEmailNotificationAsync(toEmail, fromEmail, subject, message, filepath).GetAwaiter().GetResult();
        }

        public static int SendViaWindowsPowerShell(string server, int port, string user, string pwd, string fromEmail, string toEmail, string subject, string body, string filepath)
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
                    Body = body,
                    Attachment = filepath
                };
                string json = JsonSerializer.Serialize(payload);
                string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

                string script = $@"
$raw = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{b64}'))
$data = ConvertFrom-Json $raw

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {{ $true }}
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
$mail.From = New-Object System.Net.Mail.MailAddress($data.From, 'qHR System')
$mail.To.Add($data.To)
$mail.Subject = $data.Subject
$mail.Body = $data.Body
$mail.IsBodyHtml = $true
if (![string]::IsNullOrEmpty($data.Attachment) -and (Test-Path $data.Attachment)) {{
    $mail.Attachments.Add((New-Object System.Net.Mail.Attachment($data.Attachment)))
}}

$client.Send($mail)
$mail.Dispose()
$client.Dispose()
Write-Output 'DISPATCH_OK'
";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command -",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return 0;

                proc.StandardInput.WriteLine(script);
                proc.StandardInput.Close();

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit(30000);

                if (output.Contains("DISPATCH_OK"))
                {
                    return 1;
                }
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"[POWERSHELL DISPATCH ERROR]: {error.Trim()}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[POWERSHELL DISPATCH EXCEPTION]: {ex.Message}");
                return 0;
            }
        }
    }

}
