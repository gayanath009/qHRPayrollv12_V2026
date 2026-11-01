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
        public async Task<int> SendEmailNotificationAsync(string toEmail, string fromEmail, string subject, string message, string filepath)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return 0;

            try
            {
                SysConfig _sysConfig = GetSysCongif().FirstOrDefault();
                if (_sysConfig == null) return 0;

                string resolvedFromEmail = string.IsNullOrEmpty(fromEmail) ? _sysConfig.FromEmail : fromEmail;
                if (string.IsNullOrEmpty(resolvedFromEmail) && !string.IsNullOrEmpty(_sysConfig.ServiceAcct))
                {
                    resolvedFromEmail = _sysConfig.ServiceAcct;
                }

                string decryptedPwd = (!string.IsNullOrEmpty(_sysConfig.ServiceAcct) && !string.IsNullOrEmpty(_sysConfig.ServiceAcctPwd))
                    ? qHRM.SharedSysFuncs.CryptoSecurityHelper.Decrypt(_sysConfig.ServiceAcctPwd)
                    : null;

                // Ensure internal Exchange / self-signed TLS certificates pass validation
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                // Standard SmtpClient dispatch (Supports Port 587/465 TLS and Port 25)
                try
                {
                    MailAddress _from = new MailAddress(resolvedFromEmail, "qHR System");
                    MailAddress _to = new MailAddress(toEmail);
                    using (MailMessage msg = new MailMessage(_from, _to))
                    {
                        msg.Body = message;
                        msg.Subject = subject;
                        msg.IsBodyHtml = true;

                        if (!string.IsNullOrEmpty(filepath) && System.IO.File.Exists(filepath))
                        {
                            msg.Attachments.Add(new Attachment(filepath));
                        }

                        using (SmtpClient _client = new SmtpClient(_sysConfig.Server, _sysConfig.Port > 0 ? _sysConfig.Port : 587))
                        {
                            _client.Timeout = 15000;
                            if (!string.IsNullOrEmpty(_sysConfig.ServiceAcct) && !string.IsNullOrEmpty(decryptedPwd))
                            {
                                _client.Credentials = new NetworkCredential(_sysConfig.ServiceAcct, decryptedPwd);
                                _client.UseDefaultCredentials = false;
                            }
                            else
                            {
                                _client.UseDefaultCredentials = true;
                            }

                            _client.DeliveryMethod = SmtpDeliveryMethod.Network;
                            if (_sysConfig.Port == 587 || _sysConfig.Port == 465)
                            {
                                _client.EnableSsl = true;
                            }

                            Console.WriteLine($"[EMAIL ATTEMPT] Sending email to {toEmail} from {_from.Address} via {_sysConfig.Server}:{_sysConfig.Port} (SSL={_client.EnableSsl}, Auth={!string.IsNullOrEmpty(_sysConfig.ServiceAcct)})");
                            await Task.Run(() => _client.Send(msg));
                            Console.WriteLine($"[EMAIL SUCCESS] Email successfully sent to {toEmail}");
                            return 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] Standard SmtpClient failed to send email to {toEmail}: {ex.Message}");
                    // Fallback to Windows PowerShell dispatcher if on Windows
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        try
                        {
                            Console.WriteLine($"[EMAIL FALLBACK] Attempting Windows PowerShell dispatcher to {toEmail}...");
                            int psRes = await Task.Run(() => SendViaWindowsPowerShell(
                                _sysConfig.Server,
                                _sysConfig.Port > 0 ? _sysConfig.Port : 587,
                                _sysConfig.ServiceAcct,
                                decryptedPwd,
                                resolvedFromEmail,
                                toEmail,
                                subject,
                                message,
                                filepath));
                            if (psRes == 1)
                            {
                                Console.WriteLine($"[EMAIL SUCCESS] Windows PowerShell fallback successfully delivered email to {toEmail}");
                                return 1;
                            }
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"[EMAIL FALLBACK ERROR] {ex2.Message}");
                        }
                    }
                    return 0; // Failed
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
