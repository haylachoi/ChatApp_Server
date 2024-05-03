using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;

namespace ChatApp_Server.Services
{
    public interface IPrivateRoomInfoService: IBaseService<IPrivateRoomInfoRepository, PrivateRoomInfo, PrivateRoomInfoParameter, int, PrivateRoomInfoDto>
    {
        Task<Result<PrivateRoomInfoDto>> UpdateCanRoomDisplay(int roomId, int userId, bool canDisplay);
    }
    public class PrivateRoomInfoService : BaseService<IPrivateRoomInfoRepository, PrivateRoomInfo, PrivateRoomInfoParameter, int, PrivateRoomInfoDto>, IPrivateRoomInfoService
    {
        public PrivateRoomInfoService(IPrivateRoomInfoRepository repo) : base(repo)
        {
        }

        public override Task<IEnumerable<PrivateRoomInfoDto>> GetAllAsync(PrivateRoomInfoParameter parameter)
        {
            throw new NotImplementedException();
        }
        public async Task<Result<PrivateRoomInfoDto>> UpdateCanRoomDisplay(int roomId, int userId, bool canDisplay)
        {
           return await ExceptionHandler.HandleLazy(async () =>
            {
                var roomInfo = await _repo.GetOne([prinfo => prinfo.PrivateRoomId == roomId && prinfo.UserId == userId]);
                if (roomInfo == null)
                {
                    return Result.Fail("Không tồn tại");
                }
             
                await _repo.SaveAsync();
                return Result.Ok(roomInfo.Adapt<PrivateRoomInfoDto>());
            });
            
        }
    }
}
