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
        IPrivateRoomService privateRoomService,
        IPrivateMessageService privateMessageService, 
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
            var room = await privateRoomService.GetByIdAsync(roomId);
            if (room == null)
            {
                return BadRequest();
            }
            var uploadQueue = new Queue<Task<Result<string>>>();
            foreach (var file in files)
            {          
                uploadQueue.Enqueue(fireBaseCloudService.UploadFile(file.Name, file));
            }
            
            var receiverId = room.PrivateRoomInfos?.FirstOrDefault(info => info.UserId != userId)?.UserId;
            if (receiverId == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing the request."
                };
                return new ObjectResult(problemDetails)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }


            //var receiverId = userId == room.BiggerUserId ? room.SmallerUserId : room.BiggerUserId;

            while (uploadQueue.Count > 0)
            {
                var urlResult = await uploadQueue.Dequeue();
                if (urlResult.IsFailed)
                {
                    continue;
                }
                await privateMessageService.InsertAsync(new PrivateMessageDto
                {
                    SenderId = userId,
                    PrivateRoomId = roomId,
                    Content = urlResult.Value,
                    ReceiverId = receiverId.Value,
                    IsImage = true,
                }).ContinueWith(pm =>
                {
                    if (pm.Result != null)
                    {
                        hubContext.Clients.Users([userId.ToString(), receiverId.Value.ToString()]).SendAsync("ReceivePrivateMessage", pm.Result.Value);
                    }
                });
            }
            
            

            return Ok();
        }
    }
}
