using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Params;
using Mapster;

namespace ChatApp_Server.Configs
{
    public static class MapsterServiceExtensions
    {
        public static void RegisterMapsterConfiguration(this IServiceCollection services)
        {
            TypeAdapterConfig<GroupParam, Room>
                 .NewConfig()
                 .Map(dest => dest.RoomMemberInfos, src => src.userIds == null ? new List<RoomMemberInfo>() :src.userIds.Select(id => new RoomMemberInfo { UserId = id}));

            TypeAdapterConfig<RoomParam, Room>
                 .NewConfig()
                 .Map(
                    dest => dest.RoomMemberInfos, 
                    src => new []
                    {
                        new RoomMemberInfo { UserId = src.SenderId, CanDisplayRoom = true},
                        new RoomMemberInfo { UserId = src.ReceiverId, CanDisplayRoom = false}
                    });

        }
    }
}
