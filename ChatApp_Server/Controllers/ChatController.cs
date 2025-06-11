
using ChatApp_Server.Hubs;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using FluentResults;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using System.Security.Claims;

namespace ChatApp_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(IImagesUploadService imagesUploadService, 
        IRoomService roomService,
        IMessageService messageService, 
        IHubContext<ClientHub> hubContext) : ControllerBase
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
            var room = await roomService.GetAsync(roomId);
            if (room == null)
            {
                return BadRequest();
            }
            var uploadQueue = new Queue<Task<Result<string>>>();
            foreach (var file in files)
            {          
                uploadQueue.Enqueue(imagesUploadService.UploadFile(file.Name, file));
            }
                     

            while (uploadQueue.Count > 0)
            {
                var urlResult = await uploadQueue.Dequeue();
                if (urlResult.IsFailed)
                {
                    continue;
                }
                await messageService.CreateMessageAsync(new MessageParam
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
