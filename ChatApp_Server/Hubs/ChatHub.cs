using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Services;
using FluentResults;
using Google.Apis.Upload;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SignalRChat.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        readonly IMessageService messageService;
        private readonly IMessageDetailService messageDetailService;
        readonly IRoomService roomService;
        readonly IRoomMemberInfoService roomMemberInfoService;
        readonly ConnectionMapping<string> _connections;

        public ChatHub(
            IFriendshipService friendshipService,
            IMessageService MessageService,
            IMessageDetailService messageDetailService,
            IRoomService RoomService,
            ConnectionMapping<string> connections,
            IRoomMemberInfoService roomMemberInfoService)
        {         
            this.messageService = MessageService;
            this.messageDetailService = messageDetailService;
            this.roomService = RoomService;
            this.roomMemberInfoService = roomMemberInfoService;
            _connections = connections;
        }


        public async Task<HubResponse> SendMessage(int roomId, string message)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }
            var result = await messageService.InsertAsync(new MessageDto
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
                 

            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", result.Value);           
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
            var result = await messageDetailService.AddOrUpdateReaction(messageId, userId, reactionId);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            var md = result.Value;
            var roomId = messageResult.Value.RoomId;
            await Clients.Group(roomId.ToString()).SendAsync("UpdateReactionMessage", roomId ,md);

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
            var result = await messageDetailService.AddOrUpdateIsReaded(messageId, userId);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            var md = result.Value;
            var room = await roomService.GetOneAsync(messageResult.Value.RoomId);
            await Clients.Group(messageResult.Value.RoomId.ToString()).SendAsync("UpdateIsReaded", md, room);

            return HubResponse.Ok(result.Value);
        }
       
        
        public override async Task OnConnectedAsync()
        {
            if (Context.UserIdentifier == null)
            {
                return;
            }
            // save connectionid to user identity;;
            _connections.Add(Context.UserIdentifier, Context.ConnectionId);

            if (!int.TryParse(Context.UserIdentifier, out var id))
            {
                return;
            }
            //var groupMembers = await _groupMemberService.GetAllAsync(new GroupMemberParameter { UserId = id });
            var groupMembers = await roomMemberInfoService.GetAllAsync(new RoomInfoParameter
            {
                UserId = id,
            });
            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var gm in groupMembers)
            {
                addToGroupTaskList.Add(Groups.AddToGroupAsync(Context.ConnectionId, gm.RoomId.ToString()));
            }
            await Task.WhenAll(addToGroupTaskList);
            
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.UserIdentifier != null)
            {
                _connections.Remove(Context.UserIdentifier, Context.ConnectionId);
            }
            base.OnDisconnectedAsync(exception);
            return Task.CompletedTask;
        }


         async Task AddConnectionToGroup(string groupname, string userId)
        {
            var connections = _connections.GetConnections(userId);
            if (connections == null)
            {
                return;
            }

            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var connection in connections)
            {
                addToGroupTaskList.Add(Groups.AddToGroupAsync(connection, groupname));
            }
            await Task.WhenAll(addToGroupTaskList);
        }
         async Task RemoveConnectionFromGroup(string groupname, string userId)
        {
            var connections = _connections.GetConnections(userId);
            if (connections == null)
            {
                return;
            }

            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var connection in connections)
            {
                addToGroupTaskList.Remove(Groups.AddToGroupAsync(connection, groupname));
            }
            await Task.WhenAll(addToGroupTaskList);
        }
    }
}