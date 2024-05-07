using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Parameters;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Hubs;
using System.Security.Claims;

namespace ChatApp_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController(IGroupService groupService, IHubContext<ChatHub> hubContext) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromForm]GroupParam param)
        {
            var stringId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(stringId, out var userId))
            {
                return Unauthorized();
            }
            param.userIds = [userId];

            var result = await groupService.CreateGroupAsync(param);

            if (result.IsFailed)
            {
                // todo: 
                return BadRequest();
            }
           
            var room = await groupService.GetByIdAsync(result.Value.Id);
            if (room == null)
            {
                // todo: 
                return BadRequest();
            }

            await hubContext.Clients.User(stringId!).SendAsync("CreateGroup", room);

            return Ok();
        }
    }
}
