using ChatApp_Server.DTOs;
using ChatApp_Server.Parameters;
using ChatApp_Server.Services;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Hubs;
using System.Net;
using System.Security.Claims;

namespace ChatApp_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(IFireBaseCloudService fireBaseCloudService, 
        IRoomService roomService,
        IMessageService messageService, 
        IHubContext<ChatHub> hubContext) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateImageMessage(ImageMessageParameter param) 
        {
            var stringId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(stringId, out var userId)) {
                return Unauthorized();
            }
            var files = param.Files;
            var roomId = param.RoomId;
            var room = await roomService.GetOneAsync(roomId);
            if (room == null)
            {
                return BadRequest();
            }
            var uploadQueue = new Queue<Task<Result<string>>>();
            foreach (var file in files)
            {          
                uploadQueue.Enqueue(fireBaseCloudService.UploadFile(file.Name, file));
            }
                     

            while (uploadQueue.Count > 0)
            {
                var urlResult = await uploadQueue.Dequeue();
                if (urlResult.IsFailed)
                {
                    continue;
                }
                await messageService.InsertAsync(new MessageDto
                {
                    SenderId = userId,
                    RoomId = roomId,
                    Content = urlResult.Value,               
                    IsImage = true,
                }).ContinueWith(pm =>
                {
                    if (pm.Result != null)
                    {
                        hubContext.Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", pm.Result.Value);
                    }
                });
            }
            
            

            return Ok();
        }
    }
}
