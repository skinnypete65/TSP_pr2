using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;


namespace SystemResourceMonitor
{
    public class AlertService
    {
        private readonly SmtpSettings _smtpSettings;

        public AlertService(IConfiguration configuration)
        {
            SmtpSettings? temp = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
            if (temp == null)
            {
                throw new Exception("[ERROR]  configuration.GetSection(\"SmtpSettings\").Get<SmtpSettings>() is null");
            } else
            {
                _smtpSettings = temp;
            }  
        }

        public void SendEmail(string subject, string body)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpSettings.From),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false
                    };
                    mailMessage.To.Add(_smtpSettings.To);

                    smtpClient.Send(mailMessage);
                }
                Console.WriteLine($"[ALERT] Email отправлен.  {subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Не удалось отправить email.  {ex.Message}");
            }
        }
    }

    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }

}
