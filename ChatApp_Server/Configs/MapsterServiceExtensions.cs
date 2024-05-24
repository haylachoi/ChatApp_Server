using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
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
                 .Map(dest => dest.IsGroup, src => true)
                 .Map(dest => dest.GroupInfo, src  => new GroupInfo { Avatar = src.Avatar, GroupOwnerId = src.GroupOwnerId, Name = src.Name })
                 .Map(dest => dest.RoomMemberInfos, src => src.userIds == null ? new List<RoomMemberInfo>() :src.userIds.Select(id => new RoomMemberInfo { UserId = id, CanDisplayRoom = true}));

            TypeAdapterConfig<MemberParam, RoomMemberInfo>
                .NewConfig()
                .Map(dest => dest.CanDisplayRoom, src => true)
                .Map(dest => dest.RoomId, src => src.GroupId);

            TypeAdapterConfig<RoomParam, Room>
                 .NewConfig()
                 .Map(
                    dest => dest.RoomMemberInfos, 
                    src => new []
                    {
                        new RoomMemberInfo { UserId = src.SenderId, CanDisplayRoom = true},
                        new RoomMemberInfo { UserId = src.ReceiverId, CanDisplayRoom = true}
                    });
            TypeAdapterConfig<UserParam, User>
                .NewConfig()
                .Map(dest => dest.Salt, src => MapContext.Current!.Parameters["salt"])
                .Map(dest => dest.Password, src => src.Password.ToSHA512Hash(MapContext.Current!.Parameters["salt"] as string));



            //
           

        }
    }
}
