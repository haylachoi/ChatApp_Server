using ChatApp_Server.Helper;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ChatApp_Server.Hubs
{
    [Authorize]
    public class ClientHub(IUserService _userService, ConnectionMapping<string> _connections) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            if (Context.UserIdentifier == null)
            {
                return;
            }

            // save connectionid to user identity;;
            _connections.Add(Context.UserIdentifier, Context.ConnectionId);

            if (!int.TryParse(Context.UserIdentifier, out var userId))
            {
                return;
            }
            var groupMembers = await _userService.GetAllRoomMembers(userId);
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