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
         IMessageService messageService,
            IUserService userService,
            IRoomService roomService,
            IHubContext<ClientHub> clientContext            
        ) : Hub
    {
        //readonly IMessageService messageService;
        //private readonly IUserService userService;
        //readonly IRoomService roomService;

        //readonly ConnectionMapping<string> _connections;

        //public ChatHub(
       
        //    IMessageService messageService,
        //    IUserService userService,
        //    IRoomService RoomService,
        //    ConnectionMapping<string> connections
        //)
        //{
        //    this.messageService = messageService;
        //    this.userService = userService;
        //    this.roomService = RoomService;

        //    _connections = connections;
        //}


        public async Task<HubResponse> SendMessage(int roomId, string message)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }
            var result = await messageService.CreateMessageAsync(new MessageParam
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

            await clientContext.Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", result.Value);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> UpdateReactionMessage(long messageId, int? reactionId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var messageResult = await messageService.GetOneIncludeRoomInfoAsync(messageId, userId);
            if (messageResult.IsFailed)
            {
                return HubResponse.Fail(messageResult.Errors);
            }
            var result = await messageService.AddOrUpdateReaction(messageId, userId, reactionId);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            var md = result.Value;
            var roomId = messageResult.Value.RoomId;
            await clientContext.Clients.Group(roomId.ToString()).SendAsync("UpdateReactionMessage", roomId, md);

            return HubResponse.Ok(result.Value);
        }
        public async Task<HubResponse> UpdateIsReaded(long messageId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var messageResult = await messageService.GetOneIncludeRoomInfoAsync(messageId, userId);
            if (messageResult.IsFailed)
            {
                return HubResponse.Fail(messageResult.Errors);
            }
            var result = await messageService.AddOrUpdateIsReaded(messageId, userId);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            var md = result.Value;
            var room = await roomService.GetOneAsync(messageResult.Value.RoomId);
            await clientContext.Clients.Group(messageResult.Value.RoomId.ToString()).SendAsync("UpdateIsReaded", md, room);

            return HubResponse.Ok(result.Value);
        }
        public async Task<HubResponse> CallVideo(int roomId, int receiverId, string peerId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var caller = await userService.GetByIdAsync(userId);

            await clientContext.Clients.User(receiverId.ToString()).SendAsync("CallVideo", roomId, peerId, caller);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> AcceptVideoCall(int callerId, string peerId)
        {          
            await clientContext.Clients.User(callerId.ToString()).SendAsync("AcceptVideoCall", peerId);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> RejectVideoCall(int callerId, string peerId)
        {
            await clientContext.Clients.User(callerId.ToString()).SendAsync("RejectVideoCall",  peerId);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> CancelVideoCall(int receiverId, string peerId) 
        {
            await clientContext.Clients.User(receiverId.ToString()).SendAsync("CancelVideoCall", peerId);
            return HubResponse.Ok();
        }
        //public override async Task OnConnectedAsync()
        //{
        //    if (Context.UserIdentifier == null)
        //    {
        //        return;
        //    }
           
        //    // save connectionid to user identity;;
        //    _connections.Add(Context.UserIdentifier, Context.ConnectionId);

        //    if (!int.TryParse(Context.UserIdentifier, out var userId))
        //    {
        //        return;
        //    }
        //    var groupMembers = await roomService.GetAllRoomOfMembersAsync(userId);
        //    List<Task> addToGroupTaskList = new List<Task>();
        //    foreach (var gm in groupMembers)
        //    {
        //        addToGroupTaskList.Add(Groups.AddToGroupAsync(Context.ConnectionId, gm.RoomId.ToString()));
        //    }
        //    await Task.WhenAll(addToGroupTaskList);

        //}
        //public override Task OnDisconnectedAsync(Exception? exception)
        //{
        //    if (Context.UserIdentifier != null)
        //    {
        //        _connections.Remove(Context.UserIdentifier, Context.ConnectionId);
        //    }
        //    base.OnDisconnectedAsync(exception);
        //    return Task.CompletedTask;
        //}

       

        
    }

}