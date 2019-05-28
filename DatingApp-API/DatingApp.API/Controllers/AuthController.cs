using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.MessageSender;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        //private readonly LockoutOptions _lockoutOptions;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ISendEmail _sendEmail;

        public AuthController(IConfiguration config,
            IMapper mapper,
            //LockoutOptions lockoutOptions,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ISendEmail sendEmail)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            //_lockoutOptions = lockoutOptions;
            _mapper = mapper;
            _config = config;
            _sendEmail = sendEmail;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var result = await _userManager.CreateAsync(userToCreate, userForRegisterDto.Password);
            _userManager.AddToRoleAsync(userToCreate, "Member").Wait();

            var userToReturn = _mapper.Map<UserForDetailedDto>(userToCreate);

            if (result.Succeeded)
            {
                return CreatedAtRoute("GetUser", 
                    new { controller = "Users", id = userToCreate.Id }, userToReturn);
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var user = await _userManager.FindByNameAsync(userForLoginDto.Username);

            if (user != null)
            {
                //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                //var callbackUrl = Url.Page(
                //    "/Account/ConfirmEmail",
                //    pageHandler: null,
                //    values: new { userId = user.Id, code = code },
                //    protocol: Request.Scheme);

                //await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                //    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                ////await _signInManager.SignInAsync(user, isPersistent: false);
                //return LocalRedirect(returnUrl);
                
                var result = await _signInManager
                .CheckPasswordSignInAsync(user, userForLoginDto.Password, true);

                var UserLockoutDate = await _userManager.GetLockoutEndDateAsync(user);
                if (UserLockoutDate == null || UserLockoutDate < DateTime.Now)
                    user.PenaltEnable = false;

                if (result.Succeeded)
                {
                    user.NumberOfLockouts = 0;
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.UpdateAsync(user);
                    var appUser = await _userManager.Users.Include(p => p.Photos)
                        .FirstOrDefaultAsync(u => u.NormalizedUserName == userForLoginDto.Username.ToUpper());

                    var userToReturn = _mapper.Map<UserForListDto>(appUser);

                    return Ok(new
                    {
                        token = GenerateJwtToken(appUser).Result,
                        user = userToReturn
                    });
                }
                else if (result.IsLockedOut)
                {
                    if (!user.PenaltEnable)
                    {
                        user.PenaltEnable = true;
                        DateTimeOffset Penalt = DateTimeOffset.Now.AddMinutes(5 * user.NumberOfLockouts);
                        await _userManager.SetLockoutEndDateAsync(user, Penalt);
                    }
                    
                    var LockoutTime = await _userManager.GetLockoutEndDateAsync(user);
                    var RemainingTime = LockoutTime.Value.Subtract(DateTime.Now).Minutes;

                    return new CustomUnauthorizedResult(string.Format(Mensagens.LoginBloqueado,
                        user.NumberOfLockouts, 
                        RemainingTime));
                }
            }
            
            user.NumberOfLockouts++;
            await _userManager.UpdateAsync(user);
            return Unauthorized();
        }

        public class CustomUnauthorizedResult : JsonResult
        {
            public CustomUnauthorizedResult(string message)
                : base(new CustomError(message))
            {
                StatusCode = StatusCodes.Status401Unauthorized;
            }
        }

        public class CustomError
        {
            public string Error { get; }

            public CustomError(string message)
            {
                Error = message;
            }
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}