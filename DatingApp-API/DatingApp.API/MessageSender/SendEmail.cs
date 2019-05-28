using Microsoft.Extensions.Configuration;
using DatingApp.API.Models;
using MailKit.Net.Smtp;
using MimeKit;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace DatingApp.API.MessageSender
{
    public class SendEmail : ISendEmail
    {
        private readonly IHostingEnvironment _env;
        private readonly IConfiguration _configuration;

        public SendEmail(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public async Task SendEmailConfirmation(User user, string subject, string message)
        {
            var webRoot = _env.WebRootPath;

            var pathToFile = $"{_env.WebRootPath}{Path.DirectorySeparatorChar.ToString()}Template{Path.DirectorySeparatorChar.ToString()}email_template.html";

            var builder = new BodyBuilder();

            using (StreamReader SourceReader = File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }

            string messageBody = string.Format(builder.HtmlBody,
                        DateTime.Now.ToShortDateString(),
                        user.UserName,
                        message);

            string FromAdressTitle = _configuration["EmailSettings:Username"];;
            string FromAddress = _configuration["EmailSettings:Email"];

            string ToAdressTitle = user.UserName;
            string ToAddress = user.Email;

            string Subject = subject;
            string BodyContent = messageBody;

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(FromAdressTitle, FromAddress));
            mimeMessage.To.Add(new MailboxAddress(ToAdressTitle, ToAddress));
            mimeMessage.Subject = Subject;
            mimeMessage.Body = new TextPart("html") { Text = BodyContent };

            using (var client = new SmtpClient())
            {
                client.Connect(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:PortNumber"]), false);
                client.Authenticate(FromAddress, _configuration["EmailSettings:Password"]);

                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
        }

        public Task sendEmailRecoverPassword(User user, string subject, string message)
        {
            throw new NotImplementedException();
        }
    }
}