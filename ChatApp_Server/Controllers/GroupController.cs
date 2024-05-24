using ChatApp_Server.Helper;
using ChatApp_Server.Hubs;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Hubs;
using System.Security.Claims;

namespace ChatApp_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController(IGroupService groupService, IHubContext<ClientHub> hubContext, ConnectionMapping<string> connection) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromForm]GroupParam param)
        {
            var stringId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (stringId == null || !int.TryParse(stringId, out var userId))
            {
                return Unauthorized();
            }
            param.userIds = [userId];
            param.GroupOwnerId = userId;

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

            await connection.AddConnectionToGroup(room.Id.ToString(), stringId, hubContext);
            await hubContext.Clients.Group(room.Id.ToString()).SendAsync("JoinRoom", room);

            return Ok();
        }
    }
}
