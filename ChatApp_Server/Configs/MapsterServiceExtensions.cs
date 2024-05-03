using ChatApp_Server.DTOs;
using Mapster;

namespace ChatApp_Server.Configs
{
    public static class MapsterServiceExtensions
    {
        public static void RegisterMapsterConfiguration(this IServiceCollection services)
        {
            TypeAdapterConfig<PrivateRoomDto, RoomDto>
              .NewConfig()
              .Map(dest=> dest.RoomMemberInfos, src=> src.PrivateRoomInfos)
              .Map(dest => dest.IsGroup, src => false);
        }
    }
}
