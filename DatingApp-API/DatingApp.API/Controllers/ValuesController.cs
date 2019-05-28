using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.MessageSender;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ISendEmail _sendEmail;
        private readonly UserManager<User> _userManager;
        public ValuesController(DataContext context, ISendEmail sendEmail, UserManager<User> userManager)
        {
            _context = context;
            _sendEmail = sendEmail;
            _userManager = userManager;
        }

        // GET api/values
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetValues()
        {
            var values = await _context.Values.ToListAsync();

            return Ok(values);
        }

        // GET api/values/5
        [Authorize(Roles = "Member")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetValue(int id)
        {
            var value = await _context.Values.FirstOrDefaultAsync(x => x.Id == id);

            return Ok(value);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [HttpGet]
        [Route("SendEmailConfirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> SendEmailConfirmation()
        {
            User user = await _userManager.FindByNameAsync("Duke");
            var cToken = _userManager.GenerateEmailConfirmationTokenAsync(user).Result;
            var urlToken = Url.Action("confirmEmail", "Values", new
            {
                userId = user.Id,
                token = cToken
            },
            protocol: HttpContext.Request.Scheme);
            await _sendEmail.SendEmailConfirmation(user, "Confirmação de Email", urlToken);
            return Ok();
        }

        [HttpGet]
        [Route("confirmEmail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest();
            }
            User user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest();
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            return Ok(result.Succeeded ? "Confirmado" : "Erro");
        }

        [HttpGet]
        [Route("sendEmailRecoverPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> sendEmailRecoverPassword()
        {
            User user = await _userManager.FindByNameAsync("Duke");
            var rToken = _userManager.GeneratePasswordResetTokenAsync(user).Result;
            var urlToken = Url.Action("recoverPassword", "Values", new
            {
                userId = user.Id,
                token = rToken
            },
            protocol: HttpContext.Request.Scheme);


            await _sendEmail.sendEmailRecoverPassword(user, "Recuperação de Senha", urlToken);

            return Ok();
        }

        [HttpGet]
        [Route("recoverPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> recoverPassword(string userId, string password, string confirmationPassword,string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest();
            }
            User user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest();
            }

            if (!password.Equals(confirmationPassword))
            {
                return BadRequest("Senhas não coincidem");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, "password");
            return Ok(result.Succeeded ? "Confirmado" : "Erro");
        }
    }
}
