using ChatApp_Server.DTOs;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Mvc;


namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService, IFireBaseCloudService fireBaseCloudService): ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserParam param)
        {
            string? avatarUrl = null;
            if (param.File != null)
            {
                var urlResult = await fireBaseCloudService.UploadFile(param.File.FileName, param.File);
                if (urlResult != null)
                {
                    avatarUrl = urlResult.Value;
                }
            }
            param.Avatar = avatarUrl;
            var result = await authService.Register(param);
            return result.ToActionResult();       
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(CredentialParam param)
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
       

        [HttpPost("refresh")]
        public async Task<IActionResult> RenewToken(AuthToken jwtToken)
        {
            var tokenResult = await authService.RenewToken(jwtToken);
            if (tokenResult.IsFailed)
            {
                return BadRequest(tokenResult.Errors);
            }
            return Ok(tokenResult.Value);
           
        }
        //[HttpGet("test")]
        //public async Task<IActionResult> Test()
        //{
        //    var r =  await privateMessageService.UpdateReactionMessage(1278, 5, 2);
        //    return Ok(r.Value);
        //}
    }
}
