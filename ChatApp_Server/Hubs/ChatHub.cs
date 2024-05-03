using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
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
        private readonly IGroupService _groupService;
        private readonly IFriendshipService _friendshipService;
        private readonly IPrivateMessageService privateMessageService;
        private readonly IPrivateRoomService privateRoomService;
        private readonly IGroupMemberService _groupMemberService;
        private readonly static ConnectionMapping<string> _connections =
            new ConnectionMapping<string>();

        public ChatHub(
            IGroupService groupService,
            IFriendshipService friendshipService,
            IPrivateMessageService privateMessageService,
            IPrivateRoomService privateRoomService,
            IGroupMemberService groupMemberService)
        {
            _groupService = groupService;
            this._friendshipService = friendshipService;
            this.privateMessageService = privateMessageService;
            this.privateRoomService = privateRoomService;
            _groupMemberService = groupMemberService;
           
        }


        public async Task<HubResponse> SendPrivateMessage(int receiverId, string message)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }

            var room = await privateRoomService.GetOneByMemberAsync(userId, receiverId);
            if (room == null || room.Id == null)
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }
            var result = await privateMessageService.InsertAsync(new PrivateMessageDto
            {
                Content = message,
                IsImage = false,
                SenderId = userId,
                ReceiverId = receiverId,
                IsReaded = false,
                PrivateRoomId = room.Id ?? throw new ArgumentNullException(nameof(room.Id)),
                
            });
            if (result.IsFailed)
            {
                return HubResponse.Fail(new { error = result.Errors[0].Message });
            }
            if (!result.Value.Id.HasValue)
            {
                return HubResponse.Fail("Ko lấy dc Id");
            }
            var pm = await privateMessageService.GetByIdAsync(result.Value.Id.Value);

            await Clients.Caller.SendAsync("ReceivePrivateMessage", pm);
            await Clients.User(receiverId.ToString()).SendAsync("ReceivePrivateMessage", pm);
            return HubResponse.Ok();
        }
        public async Task<HubResponse> UpdateReactionMessage(long messageId, int? reactionId)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }
            var result = await privateMessageService.UpdateReactionMessage(messageId, userId, reactionId);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            var pm = result.Value;

            await Clients.Caller.SendAsync("UpdateReactionMessage", pm);
            await Clients.User(pm.SenderId.ToString()).SendAsync("UpdateReactionMessage", pm);
            return HubResponse.Ok(result.Value);
        }
        public async Task<HubResponse> UpdateSeenMessage(long messageId)
        {
            // todo: check user permision
            //
            var result = await privateMessageService.UpdateIsSeen(messageId);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors[0].Message);
            }
            var message = result.Value;
            var senderId = message.SenderId;
            var pr = await privateRoomService.GetOneByRoomAsync(message.PrivateRoomId, message.ReceiverId);
            //if (pr != null && pr.LastUnseenMessageId.HasValue)
            //{
            //    pr.LastUnseenMessage = await privateMessageService.GetByIdAsync(pr.LastUnseenMessageId.Value);
            //}
           
            await Clients.User(senderId.ToString()).SendAsync("UpdateSeenMessage", message);
            await Clients.Caller.SendAsync("UpdateSeenMessage", message, pr);
            return HubResponse.Ok();
        }



        public async Task SendGroupMessage(string groupId, string message)
        {
            await Clients.Group(groupId).SendAsync("GroupMessage", message);
        }
       
        public async Task CreateGroup(string groupName)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                await Clients.Caller.SendAsync("CreateGroup", HubResponse.Fail(new { error = "Mã định danh người dùng ko hợp lệ" }));
                return;
            }
            var result = await _groupService.InsertAsync(
                new GroupDto
                {
                    Name = groupName,
                    GroupOwnerId = userId
                }
                );

            if (result.IsSuccess)
            {
                await Clients.Caller.SendAsync("CreateGroup", HubResponse.Ok(new { id = result.Value }));
                return;
            }

            await Clients.Caller.SendAsync("CreateGroup", HubResponse.Fail(new { error = result.Errors[0].Message }));

        }

        public async Task AddToGroup(AddMemberToGroupParameter param)
        {

            var result = await _groupMemberService.InsertAsync(param.Adapt<GroupMemberDto>());
            if (result.IsSuccess)
            {
                await AddConnectionToGroup(result.Value.ToString(), Context.UserIdentifier!);
                await Clients.Caller.SendAsync("AddToGroup", HubResponse.Ok(new { id = result.Value }));
            }
            else
            {
                await Clients.Caller.SendAsync("AddToGroup", new
                {
                    isSuccess = false,
                    error = result.Errors[0].Message
                }); ;
            }

        }
        public async Task LeaveGroup(string groupName)
        {

            if (int.TryParse(groupName, out var groupId))
            {
                await Clients.Caller.SendAsync("LeaveGroup", HubResponse.Fail(new { error = "Tên group không hợp lệ" }));
            }

            int userId = 0;
            if (Context.UserIdentifier == null || int.TryParse(Context.UserIdentifier, out userId))
            {
                await Clients.Caller.SendAsync("LeaveGroup", HubResponse.Fail(new { error = "User chưa xác thực" }));
            }
            var result = await _groupMemberService.DeleteAsync(groupId, userId);
            if (result.IsSuccess)
            {
                await RemoveConnectionFromGroup(groupName, userId.ToString());
                await Clients.Caller.SendAsync("LeaveGroup", HubResponse.Ok(new { id = result.Value }));
            }
            else
            {
                await Clients.Caller.SendAsync("LeaveGroup", HubResponse.Fail(new { error = result.Errors[0].Message }));
            }
        }

        public async Task RequestFriendship(int receiverId)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                await Clients.Caller.SendAsync("CreateGroup", HubResponse.Fail(new { error = "Mã định danh người dùng ko hợp lệ" }));
                return;
            }

            var result = await _friendshipService.InsertAsync(new FriendshipDto
            {
                SenderId = userId,
                ReceiverId = receiverId
            });

            if (result.IsFailed)
            {
                await Clients.Caller.SendAsync("RequestFriendship", HubResponse.Fail(new { error = result.Errors[0].Message }));
                return;
            }
            await Clients.Caller.SendAsync("RequestFriendship", HubResponse.Ok(new { id = result.Value }));
        }

        public async Task HandleFriendRequest(int id, bool isAccepted)
        {
            var friendRequest = await _friendshipService.GetByIdAsync(id);
            if (friendRequest == null || friendRequest.ReceiverId.ToString() == Context.UserIdentifier)
            {
                await Clients.Caller.SendAsync("HandleFriendRequest", HubResponse.Fail(new { error = "Request không hợp lệ"}));
            }

            var result = isAccepted ? await _friendshipService.AcceptFriendRequest(id) : await _friendshipService.RefuseFriendRequest(id);
            if (result.IsFailed)
            {
                await Clients.Caller.SendAsync("HandleFriendRequest", HubResponse.Fail(new { error = result.Errors[0].Message }));
                return;
            }
            await Clients.Caller.SendAsync("HandleFriendRequest", HubResponse.Ok());
        }

        public async Task CancelFriendRequest(int id)
        {
            var friendRequest = await _friendshipService.GetByIdAsync(id);
            if (friendRequest == null || friendRequest.SenderId.ToString() == Context.UserIdentifier)
            {
                await Clients.Caller.SendAsync("CancelFriendRequest", HubResponse.Fail(new { error = "Request không hợp lệ" }));
            }

            var result = await _friendshipService.CancelFriendRequest(id);
            if (result.IsFailed)
            {
                await Clients.Caller.SendAsync("CancelFriendRequest", HubResponse.Fail(new { error = result.Errors[0].Message }));
                return;
            }
            await Clients.Caller.SendAsync("CancelFriendRequest", HubResponse.Ok());
        }
        public override async Task OnConnectedAsync()
        {
            if (Context.UserIdentifier == null)
            {
                return;
            }
            _connections.Add(Context.UserIdentifier, Context.ConnectionId);

            if (!int.TryParse(Context.UserIdentifier, out var id))
            {
                return;
            }
            var groupMembers = await _groupMemberService.GetAllAsync(new GroupMemberParameter { MemberId = id });
            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var gm in groupMembers)
            {
                addToGroupTaskList.Add(Groups.AddToGroupAsync(Context.ConnectionId, gm.Groupid.ToString()));
            }
            await Task.WhenAll(addToGroupTaskList);
            //await Clients.Caller.SendAsync("OnConnected", $"{Context.UserIdentifier} has joined");
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


        private async Task AddConnectionToGroup(string groupname, string userId)
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
        private async Task RemoveConnectionFromGroup(string groupname, string userId)
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