using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Hubs;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ChatApp_Server.Hubs
{
    [Authorize]
    public class RoomHub(
           IRoomService _roomService,
            IGroupService _groupService,
            IMessageService _messageService,
            IHubContext<ClientHub> _hubContext,
            ConnectionMapping<string> _connections
    ) : Hub
    {
        //private readonly IRoomService roomService;
        //private readonly IGroupService groupService;
        //private readonly IMessageService messageService;
        //private readonly IHubContext<ChatHub> chatHubContext;
        //private readonly ConnectionMapping<string> connections;

        //public RoomHub(
        //    IRoomService roomService, 
        //    IGroupService groupService, 
        //    IMessageService messageService, 
        //    IHubContext<ChatHub> chatHubContext,
        //    ConnectionMapping<string> connections
        //)
        //{
        //    this.roomService = roomService;
        //    this.groupService = groupService;
        //    this.messageService = messageService;
        //    this.chatHubContext = chatHubContext;
        //    this.connections = connections;
        //}
        public async Task<HubResponse> GetLastMessageUnseen(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var message = await _roomService.GetLastUnseenMessage(roomId, userId);
            return HubResponse.Ok(message);
        }
        public async Task<HubResponse> CreateRoom(int friendId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var result = await _roomService.CreateRoomAsync(new RoomParam
            {
                ReceiverId = friendId,
                SenderId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }

            var room = result.Value;
            await AddConnectionToGroup(room.Id.ToString(), userId.ToString(), _hubContext);
            await AddConnectionToGroup(room.Id.ToString(), friendId.ToString(), _hubContext);         
            _ = _hubContext.Clients.Groups(room.Id.ToString()).SendAsync("JoinRoom", room);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> DeleteGroup(int groupId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var result = await _groupService.DeleteGroupAsync(new GroupMemberParam
            {
                GroupId = groupId,
                UserId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }          
            _ = _hubContext.Clients.Group(groupId.ToString()).SendAsync("DeleteGroup", result.Value.Id);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> AddGroupMember(int groupId,int userId)
        {
            var result = await _groupService.AddMemberAsync(new GroupMemberParam
            {
                GroupId = groupId,
                UserId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            _ = _hubContext.Clients.Group(groupId.ToString()).SendAsync("AddGroupMember", result.Value);
            await AddConnectionToGroup(groupId.ToString(), userId.ToString(), _hubContext);

            var group = await _roomService.GetOneAsync(groupId);
            if (group != null)
            {
                _ = _hubContext.Clients.User(userId.ToString()).SendAsync("JoinRoom", group);
            }
            return HubResponse.Ok();
        }
        public async Task<HubResponse> RemoveGroupMember(int groupId, int removeUserId)
        {
            var userId = int.Parse(Context.UserIdentifier!);      
            var result = await _groupService.RemoveMemberAsync(userId,new GroupMemberParam
            {
                GroupId = groupId,
                UserId = removeUserId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }

            await RemoveConnectionFromGroup(groupId.ToString(), removeUserId.ToString(), _hubContext);
            _ = _hubContext.Clients.User(removeUserId.ToString()).SendAsync("LeftRoom", groupId);
            _ = _hubContext.Clients.GroupExcept(groupId.ToString(), removeUserId.ToString()).SendAsync("RemoveGroupMember", result.Value);
            return HubResponse.Ok();
        }
       
        public async Task<HubResponse> GetRooms()
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var rooms = await _roomService.GetAllByUserAsync(userId);

            return HubResponse.Ok(rooms);
        }
        public async Task<HubResponse> UpdateCanRoomDisplay(int roomId, bool canDisplay)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var result = await _roomService.UpdateCanDisplayRoom(roomId, userId, canDisplay);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            return HubResponse.Ok();
        }
        public async Task<HubResponse> GetSomeMessages(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null || room.RoomMemberInfos == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
         
            var roomInfoOfUser = room.RoomMemberInfos.FirstOrDefault(info => info.UserId == userId);
            if (roomInfoOfUser == null)
            {
                return HubResponse.Fail("User Ko có trong room này");
            }
            var pms = await _messageService.GetSeenAndUnseenAsync(roomId, roomInfoOfUser?.FirstUnseenMessageId);
            
            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetFirstMessage(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
            var fMessageResult = await _messageService.GetFirstMessageAsync(roomId);
            if (fMessageResult.IsFailed)
            {
                return HubResponse.Fail(fMessageResult.Errors[0].Message);
            }
            return HubResponse.Ok(fMessageResult.Value);
        }

        public async Task<HubResponse> GetPreviousMessages(int roomId, long messageId, int numberMessages)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetOneAsync(roomId);
            if (room == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
           
            var pms = await _messageService.GetPreviousMessageAsync(room.Id, messageId, numberMessages);
            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetNextMessages(int roomId, long messageId, int? numberMessages)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
            var pms = await _messageService.GetNextMessageAsync(room.Id, messageId, numberMessages);
            return HubResponse.Ok(pms);
        }

        public async Task AddConnectionToGroup(string groupname, string userId, IHubContext<ClientHub> hubContext)
        {
            var connections = _connections.GetConnections(userId);
            if (connections == null)
            {
                return;
            }

            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var connection in connections)
            {
                addToGroupTaskList.Add(hubContext.Groups.AddToGroupAsync(connection, groupname));
            }
            await Task.WhenAll(addToGroupTaskList);
        }
        public async Task RemoveConnectionFromGroup(string groupname, string userId, IHubContext<ClientHub> hubContext)
        {
            var connections = _connections.GetConnections(userId);
            if (connections == null)
            {
                return;
            }

            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var connection in connections)
            {
                addToGroupTaskList.Remove(hubContext.Groups.AddToGroupAsync(connection, groupname));
            }
            await Task.WhenAll(addToGroupTaskList);
        }
    }
}
