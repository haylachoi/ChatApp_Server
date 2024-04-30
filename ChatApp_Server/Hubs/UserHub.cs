using ChatApp_Server.Helper;
using ChatApp_Server.Parameters;
using ChatApp_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ChatApp_Server.Hubs
{
    [Authorize]
    public class UserHub: Hub
    {
        private readonly IUserService userService;
        private readonly IEmotionService emotionService;

        public UserHub(IUserService userService, IEmotionService emotionService)
        {
            this.userService = userService;
            this.emotionService = emotionService;
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

        public async Task<HubResponse> GetEmotions()
        {
            var emotions = await emotionService.GetAllAsync(new EmotionParameter());
            return HubResponse.Ok(emotions);
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
