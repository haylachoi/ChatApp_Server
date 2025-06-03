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
            IHubContext<ClientHub> _clientContext            
        ) : Hub
    {
        public async Task<HubResponse> SendMessage(int roomId, string content, long? quoteId)
        {
            var userId = int.Parse(Context.UserIdentifier!);     
            var result = await _messageService.CreateMessageAsync(new MessageParam
            {
                Content = content,
                IsImage = false,
                SenderId = userId,
                RoomId = roomId,
                QuoteId = quoteId

            });

            if (result.IsFailed)
                return HubResponse.Fail(new { error = result.Errors[0].Message });
            var message = await _messageService.GetAsync(result.Value.Id);
            if (message == null)
            {
                return HubResponse.Fail("Có lỗi xảy ra");
            }
            await _clientContext.Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> UpdateReactionMessage(long messageId, int? reactionId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
        
            if (reactionId.HasValue)
            {
                var result = await _messageService.AddOrUpdateReactionMessage(messageId, userId, reactionId.Value);
                if (result.IsFailed)
                    return HubResponse.Fail(result.Errors);

                var md = result.Value;
                var roomId = md.RoomId;
                _ = _clientContext.Clients.Group(roomId.ToString()).SendAsync("UpdateReactionMessage", roomId, md);
            } else
            {
                var result = await _messageService.DeleteMessageDetail(messageId, userId);
                if (result.IsFailed)
                    return HubResponse.Fail(result.Errors);

                var md = result.Value;
                var roomId = md.RoomId;
                _ = _clientContext.Clients.Group(roomId.ToString()).SendAsync("DeleteMessageDetail", roomId, md);
            }

            return HubResponse.Ok();
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
        public async Task<HubResponse> FinishVideoCall(int receiverId, string peerId)
        {
            await _clientContext.Clients.User(receiverId.ToString()).SendAsync("FinishVideoCall", peerId);
            return HubResponse.Ok();
        }
    }

}