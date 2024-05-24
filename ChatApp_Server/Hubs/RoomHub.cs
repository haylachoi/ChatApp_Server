using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Params;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Hubs;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ChatApp_Server.Hubs
{
    [Authorize]
    public class RoomHub(
           IRoomService _roomService,
            IGroupService _groupService,
            IMessageService _messageService,
            IHubContext<ClientHub> _clientContext,
            ConnectionMapping<string> _connections
    ) : Hub
    {    
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

            await _connections.AddConnectionToGroup(room.Id.ToString(), userId.ToString(), _clientContext);
            await _connections.AddConnectionToGroup(room.Id.ToString(), friendId.ToString(), _clientContext);
            _ = _clientContext.Clients.Groups(room.Id.ToString()).SendAsync("JoinRoom", room);

            return HubResponse.Ok();
        }
        public async Task<HubResponse> LeaveGroup(int groupId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var result = await _groupService.LeaveGroupAsync(new MemberParam { GroupId = groupId, UserId = userId });
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }

            _ = _clientContext.Clients.User(userId.ToString()).SendAsync("LeftRoom", groupId);
            _ = _clientContext.Clients.GroupExcept(groupId.ToString(), userId.ToString()).SendAsync("RemoveGroupMember", result.Value.RoomId, result.Value.UserId);

            return HubResponse.Ok();
        }
        public async Task<HubResponse> DeleteGroup(int groupId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var result = await _groupService.DeleteGroupAsync(new MemberParam
            {
                GroupId = groupId,
                UserId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            _ = _clientContext.Clients.Group(groupId.ToString()).SendAsync("DeleteGroup", result.Value.Id);

            return HubResponse.Ok();
        }
        public async Task<HubResponse> AddGroupMember(int groupId, int userId)
        {
            var result = await _groupService.AddMemberAsync(new MemberParam
            {
                GroupId = groupId,
                UserId = userId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            _ = _clientContext.Clients.Group(groupId.ToString()).SendAsync("AddGroupMember", result.Value);

            await _connections.AddConnectionToGroup(groupId.ToString(), userId.ToString(), _clientContext);

            var group = await _roomService.GetWithMembersAsync(groupId);
            if (group != null)
            {
                _ = _clientContext.Clients.User(userId.ToString()).SendAsync("JoinRoom", group);
            }

            return HubResponse.Ok();
        }
        public async Task<HubResponse> RemoveGroupMember(int groupId, int removeUserId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var result = await _groupService.RemoveMemberAsync(userId, new MemberParam
            {
                GroupId = groupId,
                UserId = removeUserId
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }

            await _connections.RemoveConnectionFromGroup(groupId.ToString(), removeUserId.ToString(), _clientContext);

            _ = _clientContext.Clients.User(removeUserId.ToString()).SendAsync("LeftRoom", groupId);
            _ = _clientContext.Clients.GroupExcept(groupId.ToString(), removeUserId.ToString()).SendAsync("RemoveGroupMember", result.Value.RoomId, result.Value.UserId);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> SetGroupOwner(MemberParam param)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            if (userId == param.UserId)
            {
                return HubResponse.Fail("Người dùng đã là chủ nhóm");
            }

            var result = await _groupService.SetGroupOwnerAsync(userId, param);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }

            _ = _clientContext.Clients.Group(param.GroupId.ToString()).SendAsync("ChangeGroupOwner", param.GroupId ,result.Value);
            return HubResponse.Ok();
        }

        public async Task<HubResponse> GetRooms()
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var rooms = await _roomService.GetAllAsync(userId);

            return HubResponse.Ok(rooms);
        }

        public async Task<HubResponse> UpdateCanRoomDisplay(int roomId, bool canDisplay)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var result = await _roomService.UpdateCanDisplayRoomAsync(roomId, userId, canDisplay);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            return HubResponse.Ok();
        }
        public async Task<HubResponse> GetSomeMessages(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Bạn không ở trong nhóm này");
            }

            var pms = await _messageService.GetSomeMessagesAsync(roomId, userId);

            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetFirstMessage(int roomId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Bạn không ở trong phòng này");
            }

            var firstMessage = await _messageService.GetFirstMessageAsync(roomId);
            if (firstMessage == null)
            {
                return HubResponse.Fail("Chưa có tin nhắn");
            }

            return HubResponse.Ok(firstMessage);
        }

        public async Task<HubResponse> GetPreviousMessages(int roomId, long messageId, int numberMessages)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Bạn không ở trong phòng này");
            }

            var pms = await _messageService.GetPreviousMessagesAsync(room.Id, messageId, numberMessages);
            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetNextMessages(int roomId, long messageId, int numberMessages)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var room = await _roomService.GetAsync(roomId, userId);
            if (room == null)
            {
                return HubResponse.Fail("Bạn không ở trong phòng này");

            }
            var pms = await _messageService.GetNextMessagesAsync(room.Id, messageId, numberMessages);

            return HubResponse.Ok(pms);
        }

        public async Task<HubResponse> UpdateFirstUnseenMessage(long messageId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var rmResult = await _roomService.UpdateFirstUnseenMessageAsync(messageId, userId);
            if (rmResult.IsFailed)
            {
                return HubResponse.Fail(rmResult.Errors);
            }

            var roomMember = await _roomService.GetRoomMember(new MemberParam { GroupId = rmResult.Value.RoomId , UserId = userId});
            await _clientContext.Clients.Group(roomMember.RoomId.ToString()).SendAsync("UpdateFirstUnseenMessage", roomMember);

            return HubResponse.Ok();
        }
    }
}
