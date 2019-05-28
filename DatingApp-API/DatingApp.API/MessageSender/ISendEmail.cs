using DatingApp.API.Models;
using SendGrid;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DatingApp.API.MessageSender
{
    public interface ISendEmail
    {
        //Task<Response> SendGridEmail();
        Task SendEmailConfirmation(User user, string subject, string message);
        Task sendEmailRecoverPassword(User user, string subject, string message);
    }
}
