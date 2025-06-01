using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp_Server.Hubs
{
    [Authorize]
    public class UserHub(IUserService userService, IReactionService reactionService) : Hub
    {    
        public async Task<HubResponse> ChangeProfile(JsonPatchDocument<ProfileDto> patchDoc)
        {
            var userId = int.Parse(Context.UserIdentifier!);
            var result = await userService.ChangeProfileAsync(userId, patchDoc);
            if (result.IsFailed)
                return HubResponse.Fail(result.Errors);
            
            return HubResponse.Ok(result.Value);
        }
        public async Task<HubResponse> SearchUser(string searchTerm)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var users = await userService.SearchUser(searchTerm, userId);
            return HubResponse.Ok(users);
        }
        public async Task<HubResponse> SearchUserNotInRoom(int roomId, string searchTerm)
        {
            var users = await userService.SearchUserNotInRoom(roomId, searchTerm);
            return HubResponse.Ok(users);
        }

        public async Task<HubResponse> GetReactions()
        {
            var reactions = await reactionService.GetAllAsync();
            return HubResponse.Ok(reactions);
        }
        public HubResponse GetConnectionId()
        {
            return HubResponse.Ok(Context.ConnectionId);
        }
        //public async Task RequestFriendship(int receiverId)
        //{
        //    if (!int.TryParse(Context.UserIdentifier, out int userId))
        //    {
        //        await Clients.Caller.SendAsync("CreateGroup", HubResponse.Fail(new { error = "Mã định danh người dùng ko hợp lệ" }));
        //        return;
        //    }

        //    var result = await _friendshipService.InsertAsync(new FriendshipDto
        //    {
        //        SenderId = userId,
        //        ReceiverId = receiverId
        //    });

        //    if (result.IsFailed)
        //    {
        //        await Clients.Caller.SendAsync("RequestFriendship", HubResponse.Fail(new { error = result.Errors[0].Message }));
        //        return;
        //    }
        //    await Clients.Caller.SendAsync("RequestFriendship", HubResponse.Ok(new { id = result.Value }));
        //}

        //public async Task HandleFriendRequest(int id, bool isAccepted)
        //{
        //    var friendRequest = await _friendshipService.GetByIdAsync(id);
        //    if (friendRequest == null || friendRequest.ReceiverId.ToString() == Context.UserIdentifier)
        //    {
        //        await Clients.Caller.SendAsync("HandleFriendRequest", HubResponse.Fail(new { error = "Request không hợp lệ" }));
        //    }

        //    var result = isAccepted ? await _friendshipService.AcceptFriendRequest(id) : await _friendshipService.RefuseFriendRequest(id);
        //    if (result.IsFailed)
        //    {
        //        await Clients.Caller.SendAsync("HandleFriendRequest", HubResponse.Fail(new { error = result.Errors[0].Message }));
        //        return;
        //    }
        //    await Clients.Caller.SendAsync("HandleFriendRequest", HubResponse.Ok());
        //}

        //public async Task CancelFriendRequest(int id)
        //{
        //    var friendRequest = await _friendshipService.GetByIdAsync(id);
        //    if (friendRequest == null || friendRequest.SenderId.ToString() == Context.UserIdentifier)
        //    {
        //        await Clients.Caller.SendAsync("CancelFriendRequest", HubResponse.Fail(new { error = "Request không hợp lệ" }));
        //    }

        //    var result = await _friendshipService.CancelFriendRequest(id);
        //    if (result.IsFailed)
        //    {
        //        await Clients.Caller.SendAsync("CancelFriendRequest", HubResponse.Fail(new { error = result.Errors[0].Message }));
        //        return;
        //    }
        //    await Clients.Caller.SendAsync("CancelFriendRequest", HubResponse.Ok());
        //}       
    }
}
