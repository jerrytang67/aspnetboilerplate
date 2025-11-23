using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Abp.Logging;

namespace Abp.Net.Mail {
    /// <summary>
    /// This class is an implementation of <see cref="IEmailSender"/> as similar to null pattern.
    /// It does not send emails but logs them.
    /// </summary>
    public class NullEmailSender : EmailSenderBase {
        private readonly ILogger<NullEmailSender> _logger;

        /// <summary>
        /// Creates a new <see cref="NullEmailSender"/> object.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public NullEmailSender(IEmailSenderConfiguration configuration, ILogger<NullEmailSender> logger)
            : base(configuration) {
            _logger = logger;
        }

        protected override Task SendEmailAsync(MailMessage mail) {
            _logger.LogWarning("USING NullEmailSender!");
            _logger.LogDebug("SendEmailAsync:");
            LogEmail(mail);
            return Task.FromResult(0);
        }

        protected override void SendEmail(MailMessage mail) {
            _logger.LogWarning("USING NullEmailSender!");
            _logger.LogDebug("SendEmail:");
            LogEmail(mail);
        }

        private void LogEmail(MailMessage mail) {
            _logger.LogDebug(mail.To.ToString());
            _logger.LogDebug(mail.CC.ToString());
            _logger.LogDebug(mail.Subject);
            _logger.LogDebug(mail.Body);
        }
    }
}