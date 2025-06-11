using ChatApp_Server.Hubs;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using System.Security.Claims;

namespace ChatApp_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController(IUserService userService, IImagesUploadService 
        imagesUploadService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var identifier = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(identifier, out var userId))
            {
                return Unauthorized();
            }
            var user = await userService.GetProfileAsync(userId);
            return Ok(user);
        }
        [HttpPost("change-avatar")]
        public async Task<IActionResult> ChangeAvatar([FromForm]IFormFile file)
        {
           
            var identifier = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(identifier, out var userId))
            {
                return Unauthorized();
            }
            var urlResult = await imagesUploadService.UploadFile(file.FileName, file);
            if (urlResult.IsFailed)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            var result = await userService.ChangeAvatarAsync(userId, urlResult.Value);
            if (result.IsFailed)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            // todo: notify avatar change
            return Ok(result.Value.Avatar);
        }
    }
}
