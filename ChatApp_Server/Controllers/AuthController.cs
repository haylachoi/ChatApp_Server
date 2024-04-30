using ChatApp_Server.DTOs;
using ChatApp_Server.Parameters;
using ChatApp_Server.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;


namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService): ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterParameter param)
        {
            var result = await authService.Register(param.Email, param.Password);
            return result.ToActionResult();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginParameter param)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized("Tài khoản hoặc mật khẩu không chính xác");
            }

            var userResult = await authService.ValidateCredential(param.Email, param.Password);
            if (userResult.IsFailed)
            {
                return Unauthorized("Tài khoản hoặc mật khẩu không chính xác");
            }

            var jwtTokenResult = await authService.CreateToken(userResult.Value);
            

            return jwtTokenResult.ToActionResult();

        }
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var identifier = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(identifier, out var userId))
            {
                return Unauthorized();
            }
            var user = await authService.GetProfile(userId);
            return Ok(user);
        }

        //[HttpGet("test")]
        //public async Task<IActionResult> Test()
        //{
        //    var r =  await privateMessageService.UpdateEmotionMessage(1278, 5, 2);
        //    return Ok(r.Value);
        //}
    }
}
