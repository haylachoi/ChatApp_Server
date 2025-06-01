using System.Linq.Expressions;
using Mapster;
using FluentResults;
using ChatApp_Server.Repositories;
using ChatApp_Server.Models;
using ChatApp_Server.Helper;
using ChatApp_Server.DTOs;
using ChatApp_Server.Params;
using Microsoft.AspNetCore.JsonPatch;
using ChatApp_Server.Criteria;

namespace ChatApp_Server.Services
{
    public interface IUserService
    {
        Task<Result<ProfileDto>> CreateUser(UserParam param);
        Task<Result<UserDto>> ChangeAvatarAsync(int userId, string imgUrl);      
        Task<UserDto?> GetByIdAsync(int userId);
        Task<ProfileDto?> GetProfileAsync(int userId);
        Task<Result<ProfileDto>> ChangeProfileAsync(int userId, JsonPatchDocument<ProfileDto> patchDoc);
        Task<IEnumerable<UserDto>> SearchUserNotInRoom(int roomId, string searchTerm);
        Task<IEnumerable<UserDto>> SearchUser(string searchTerm, int userId);
        Task<IEnumerable<RoomMemberInfoDto>> GetAllRoomMembers(int userId);
    }
    public class UserService(IUserRepository _userRepo, IRoomMemberInfoRepository _memberRepo) : IUserService
    {     
        public async Task<Result<UserDto>> InsertAsync(UserParam param)
        => await ExceptionHandler.HandleLazy<UserDto>(async () =>
        {
            var salt = HashedPassword.GenerateRandomKey();
            var user = param.Adapt<User>();
            if (user == null)
            {
                return Result.Fail("Ko thể tạo user");
            }
            user.Salt = salt;
            user.Password = user.Password!.ToSHA512Hash(salt);
            _userRepo.Insert(user);
            await _userRepo.SaveAsync();
            return Result.Ok(user.Adapt<UserDto>());
        });

        public async Task<IEnumerable<UserDto>> SearchUserNotInRoom(int roomId, string searchTerm)
        {
            var users = await _userRepo.SearchUserNotInRoom(roomId, searchTerm);

            return users.Adapt<IEnumerable<UserDto>>();
        }

        public async Task<IEnumerable<UserDto>> SearchUser(string searchTerm, int userId)
        {
            var users = await _userRepo.SearchUsersNotInAllPrivateRooms(searchTerm, userId);
            return users.Adapt<IEnumerable<UserDto>>();
        }

        public async Task<UserDto?> GetByIdAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            return user.Adapt<UserDto>();
        }

        public async Task<Result<UserDto>> ChangeAvatarAsync(int userId, string imgUrl)
         => await ExceptionHandler.HandleLazy<UserDto>(async () =>
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    return Result.Fail("User Không tồn tại");
                }
                user.Avatar = imgUrl;
                await _userRepo.SaveAsync();
                return user.Adapt<UserDto>();
            });

        public async Task<ProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            return user.Adapt<ProfileDto>();
        }

        public async Task<Result<ProfileDto>> CreateUser(UserParam param)
        => await ExceptionHandler.HandleLazy<ProfileDto>(async () =>
        {
            var salt = HashedPassword.GenerateRandomKey();
            var user = param.BuildAdapter().AddParameters("salt", salt).AdaptToType<User>();
            _userRepo.Insert(user);
            await _userRepo.SaveAsync();
            return user.Adapt<ProfileDto>();
        });

        public async Task<Result<ProfileDto>> ChangeProfileAsync(int userId, JsonPatchDocument<ProfileDto> patchDoc)
        => await ExceptionHandler.HandleLazy<ProfileDto>(async () =>
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return Result.Fail("User không tồn tại");
            }
            var userPatchDoc = patchDoc.Adapt<JsonPatchDocument<User>>();
            userPatchDoc.ApplyTo(user);
            await _userRepo.SaveAsync();
            return user.Adapt<ProfileDto>();
        });

        public async Task<IEnumerable<RoomMemberInfoDto>> GetAllRoomMembers(int userId)
        {
            var members = await _memberRepo.GetAllAsync(new MembersCriteria { UserId = userId });
            return members.Adapt<IEnumerable<RoomMemberInfoDto>>();
        }
    }
}

