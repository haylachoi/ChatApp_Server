using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;

namespace ChatApp_Server.Services
{
    public interface IFriendshipService: IBaseService<IFriendshipRepository, Friendship, FriendshipParameter, int,FriendshipDto>
    {
        Task<Result> AcceptFriendRequest(int id);
        Task<Result> RefuseFriendRequest(int id);
        Task<Result> CancelFriendRequest(int id);
    }
    public class FriendshipService : BaseService<IFriendshipRepository, Friendship, FriendshipParameter, int, FriendshipDto>, IFriendshipService
    {
        public FriendshipService(IFriendshipRepository repo) : base(repo)
        {
        }

        public async Task<Result> AcceptFriendRequest(int id)
        {
            try
            {
                var friendRequest = await _repo.GetByIdAsync(id);
                if (friendRequest == null)
                {
                    return Result.Fail("Yêu cầu kết bạn không tồn tại");
                }
                friendRequest.IsAccepted = true;
                _repo.Update(friendRequest);
                await _repo.SaveAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

        public async Task<Result> CancelFriendRequest(int id)
        {
            try
            {
                var friendRequest = await _repo.GetByIdAsync(id);
                if (friendRequest == null)
                {
                    return Result.Fail("Yêu cầu kết bạn không tồn tại");
                }
                _repo.Delete(id);
                await _repo.SaveAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

        public override async Task<IEnumerable<FriendshipDto>> GetAllAsync(FriendshipParameter parameter)
        {
           
            var friends = await _repo.GetAllAsync([fs => fs.ReceiverId == parameter.ReceiverId]);
            return friends.Adapt<IEnumerable<FriendshipDto>>();
        }

        public async Task<Result> RefuseFriendRequest(int id)
        {
            try
            {
                var friendRequest = await _repo.GetByIdAsync(id);
                if (friendRequest == null)
                {
                    return Result.Fail("Yêu cầu kết bạn không tồn tại");
                }
                _repo.Delete(id);
                await _repo.SaveAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
    }
}
