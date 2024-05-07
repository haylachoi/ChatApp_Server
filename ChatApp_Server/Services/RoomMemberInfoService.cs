using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;

namespace ChatApp_Server.Services
{
    public interface IRoomMemberInfoService: IBaseService<IRoomMemberInfoRepository, RoomMemberInfo, RoomInfoParameter, int, RoomMemberInfoDto>
    {
        Task<Result<RoomMemberInfoDto>> UpdateCanRoomDisplay(int roomId, int userId, bool canDisplay);
    }
    public class RoomMemberInfoService : BaseService<IRoomMemberInfoRepository, RoomMemberInfo, RoomInfoParameter, int, RoomMemberInfoDto>, IRoomMemberInfoService
    {
        public RoomMemberInfoService(IRoomMemberInfoRepository repo) : base(repo)
        {
        }

        public override async Task<IEnumerable<RoomMemberInfoDto>> GetAllAsync(RoomInfoParameter parameter)
        {
            var roomMemberInfos = await _repo.GetAllAsync([info => info.UserId ==  parameter.UserId]);
            return roomMemberInfos.Adapt<IEnumerable<RoomMemberInfoDto>>();
        }
        public async Task<Result<RoomMemberInfoDto>> UpdateCanRoomDisplay(int roomId, int userId, bool canDisplay)
        {
           return await ExceptionHandler.HandleLazy(async () =>
            {
                var roomInfo = await _repo.GetOne([prinfo => prinfo.RoomId == roomId && prinfo.UserId == userId]);
                if (roomInfo == null)
                {
                    return Result.Fail("Không tồn tại");
                }
             
                await _repo.SaveAsync();
                return Result.Ok(roomInfo.Adapt<RoomMemberInfoDto>());
            });
            
        }
    }
}
