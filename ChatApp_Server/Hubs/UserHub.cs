﻿using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Parameters;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp_Server.Hubs
{
    [Authorize]
    public class UserHub: Hub
    {
        private readonly IUserService userService;
        private readonly IReactionService reactionService;
        private readonly IFriendshipService _friendshipService;

        public UserHub(IUserService userService, IReactionService reactionService, IFriendshipService friendshipService)
        {
            this.userService = userService;
            this.reactionService = reactionService;
            this._friendshipService = friendshipService;
        }

        public async Task<HubResponse> SearchUser(string searchTerm)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail(new { error = "Mã định danh người dùng ko hợp lệ" });
            }
            var users = await userService.GetAllAsync(new UserParameter
            {
                SearchTerm = searchTerm,
                IgnoreList = [userId]
            });
            return HubResponse.Ok(users);
        }

        public async Task<HubResponse> GetReactions()
        {
            var reactions = await reactionService.GetAllAsync(new ReactionParameter());
            return HubResponse.Ok(reactions);
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
                await Clients.Caller.SendAsync("HandleFriendRequest", HubResponse.Fail(new { error = "Request không hợp lệ" }));
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
            var user = await userService.GetByIdAsync(int.Parse(Context.UserIdentifier!));
            await Clients.All.SendAsync("OnConnected", user);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("OnDisconnected", $"{Context.UserIdentifier} has disconnected");
            await base.OnDisconnectedAsync(exception);          
        }
    }
}
