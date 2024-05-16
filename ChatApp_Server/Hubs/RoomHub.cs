using ChatApp_Server.Helper;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Hubs;

namespace ChatApp_Server.Hubs
{
    [Authorize]
    public class RoomHub: Hub
    {
        private readonly IRoomService roomService;
        private readonly IGroupService groupService;
        private readonly IMessageService messageService;
        private readonly IHubContext<ChatHub> chatHubContext;
        private readonly ConnectionMapping<string> connections;

        public RoomHub(IRoomService roomService, 
            IGroupService groupService, 
            IMessageService messageService, 
            IHubContext<ChatHub> chatHubContext,
            ConnectionMapping<string> connections
        )
        {
            this.roomService = roomService;
            this.groupService = groupService;
            this.messageService = messageService;
            this.chatHubContext = chatHubContext;
            this.connections = connections;
        }
        public async Task<HubResponse> GetLastMessageUnseen(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var message = await roomService.GetLastUnseenMessage(roomId, userId);
            return HubResponse.Ok(message);
        }
        public async Task<HubResponse> CreateRoom(int friendId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var result = await roomService.CreateRoomAsync(new RoomParam
            {
                ReceiverId = friendId,
                SenderId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }

            var room = result.Value;
           
            _ = Clients.Caller.SendAsync("CreateRoom", room);
            _ = Clients.User(friendId.ToString()).SendAsync("CreateRoom", room);
            return HubResponse.Ok();
        }
      
        public async Task<HubResponse> AddGroupMember(int groupId,int userId)
        {
            var result = await groupService.AddMemberAsync(new GroupMemberParam
            {
                GroupId = groupId,
                UserId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            _ = chatHubContext.Clients.Group(groupId.ToString()).SendAsync("AddGroupMember", result.Value);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> RemoveGroupMember(int groupId, int userId)
        {
            var result = await groupService.RemoveMemberAsync(new GroupMemberParam
            {
                GroupId = groupId,
                UserId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            _ = chatHubContext.Clients.Group(groupId.ToString()).SendAsync("RemoveGroupMember", result.Value);
            return HubResponse.Ok();
        }
       
        public async Task<HubResponse> GetRooms()
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var rooms = await roomService.GetAllByUserAsync(userId);

            return HubResponse.Ok(rooms);
        }
        public async Task<HubResponse> UpdateCanRoomDisplay(int roomId, bool canDisplay)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var result = await roomService.UpdateCanDisplayRoom(roomId, userId, canDisplay);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            return HubResponse.Ok();
        }
        public async Task<HubResponse> GetSomeMessages(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null || room.RoomMemberInfos == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
         
            var roomInfoOfUser = room.RoomMemberInfos.FirstOrDefault(info => info.UserId == userId);
            if (roomInfoOfUser == null)
            {
                return HubResponse.Fail("User Ko có trong room này");
            }
            var pms = await messageService.GetSeenAndUnseenAsync(roomId, roomInfoOfUser?.FirstUnseenMessageId);
            
            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetFirstMessage(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
            var fMessageResult = await messageService.GetFirstMessageAsync(roomId);
            if (fMessageResult.IsFailed)
            {
                return HubResponse.Fail(fMessageResult.Errors[0].Message);
            }
            return HubResponse.Ok(fMessageResult.Value);
        }

        public async Task<HubResponse> GetPreviousMessages(int roomId, long messageId, int numberMessages)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await roomService.GetOneAsync(roomId);
            if (room == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
           
            var pms = await messageService.GetPreviousMessageAsync(room.Id, messageId, numberMessages);
            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetNextMessages(int roomId, long messageId, int? numberMessages)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await roomService.GetOneByRoomAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
            var pms = await messageService.GetNextMessageAsync(room.Id, messageId, numberMessages);
            return HubResponse.Ok(pms);
        }
    }
}
