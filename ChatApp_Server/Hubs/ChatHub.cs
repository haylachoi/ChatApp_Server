using ChatApp_Server.Helper;
using ChatApp_Server.Hubs;
using ChatApp_Server.Models;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs
{
    [Authorize]
    public class ChatHub(
         IMessageService _messageService,
            IUserService _userService,
            IRoomService _roomService,
            IHubContext<ClientHub> _clientContext            
        ) : Hub
    {
     
        public async Task<HubResponse> SendMessage(int roomId, string message)
        {
            var userId = int.Parse(Context.UserIdentifier!);
       
            var result = await _messageService.CreateMessageAsync(new MessageParam
            {
                Content = message,
                IsImage = false,
                SenderId = userId,
                RoomId = roomId,

            });
            if (result.IsFailed)
            {
                return HubResponse.Fail(new { error = result.Errors[0].Message });
            }          

            await _clientContext.Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", result.Value);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> UpdateReactionMessage(long messageId, int? reactionId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
        
            var result = await _messageService.AddOrUpdateReaction(messageId, userId, reactionId);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            var md = result.Value;
            var roomId = md.RoomId;
            await _clientContext.Clients.Group(roomId.ToString()).SendAsync("UpdateReactionMessage", roomId, md);

            return HubResponse.Ok(md);
        }
  
        public async Task<HubResponse> CallVideo(int roomId, int receiverId, string peerId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var caller = await _userService.GetByIdAsync(userId);

            await _clientContext.Clients.User(receiverId.ToString()).SendAsync("CallVideo", roomId, peerId, caller);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> AcceptVideoCall(int callerId, string peerId)
        {          
            await _clientContext.Clients.User(callerId.ToString()).SendAsync("AcceptVideoCall", peerId);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> RejectVideoCall(int callerId, string peerId)
        {
            await _clientContext.Clients.User(callerId.ToString()).SendAsync("RejectVideoCall",  peerId);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> CancelVideoCall(int receiverId, string peerId) 
        {
            await _clientContext.Clients.User(receiverId.ToString()).SendAsync("CancelVideoCall", peerId);
            return HubResponse.Ok();
        }   
    }

}