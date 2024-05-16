using System.Linq.Expressions;
using Mapster;
using FluentResults;
using ChatApp_Server.Repositories;
using ChatApp_Server.Models;
using ChatApp_Server.Helper;
using ChatApp_Server.DTOs;
using ChatApp_Server.Params;
using Microsoft.AspNetCore.JsonPatch;

namespace ChatApp_Server.Services
{
    public interface IUserService
    {
        Task<Result<ProfileDto>> CreateUser(UserParam param);
        Task<Result<UserDto>> ChangeAvatarAsync(int userId, string imgUrl);
        Task<UserDto?> GetByEmailAsync(string email);
        Task<UserDto?> GetByIdAsync(int userId);
        Task<ProfileDto?> GetProfileAsync(int userId);
        Task<Result<ProfileDto>> ChangeProfileAsync(int userId, JsonPatchDocument<ProfileDto> patchDoc);
        Task<Result> UpdateConnectionStatus(int id, bool isOnline);
        Task<IEnumerable<UserDto>> SearchUserNotInRoom(int roomId, string searchTerm);
        Task<IEnumerable<UserDto>> SearchUser(string searchTerm, int userId);
    }
    public class UserService(IUserRepository _repo) : IUserService
    {


        //public override async Task<IEnumerable<UserDto>> GetAllAsync(UserParameter parameter)
        //{
        //    var name = parameter.SearchTerm;
        //    var ignoreList = parameter.IgnoreList;
        //    var filters = new List<Expression<Func<User, bool>>>();

        //    if (name != null && name != string.Empty)
        //    {
        //        filters.Add(u => u.Email.Contains(name) ||u.Fullname.Contains(name));
        //    }
        //    if (ignoreList != null && ignoreList.Count() > 0)
        //    {
        //        filters.Add(u => !ignoreList.Contains(u.Id));
        //    }
        //    var users = await _repo.GetAllAsync(filters); 
        //    return users.Adapt<IEnumerable<UserDto>>(); 
        //}

        public async Task<Result<UserDto>> InsertAsync(UserParam param)
        {
            try
            {
                var salt = HashedPassword.GenerateRandomKey();
                var user = param.Adapt<User>();
                if (user == null)
                {
                    return Result.Fail("Ko thể tạo user");
                }
                user.Salt = salt;
                user.Password = user.Password!.ToSHA512Hash(salt);
                _repo.Insert(user);
                await _repo.SaveAsync();
                return Result.Ok(user.Adapt<UserDto>());
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            Expression<Func<User, bool>> predicate = u => u.Email == email;
            var users = await _repo.GetOne([predicate]);
            return users.Adapt<UserDto>();
        }

        public async Task<Result> UpdateConnectionStatus(int id, bool isOnline)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null)
            {
                return Result.Fail("User không tồ tại");
            }
            user.IsOnline = isOnline;
            try
            {
                await _repo.SaveAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

        public async Task<IEnumerable<UserDto>> SearchUserNotInRoom(int roomId, string searchTerm)
        {
            var users = await _repo.GetAllAsync([u => u.Fullname.Contains(searchTerm), u => !u.RoomMemberInfos.Any(info => info.RoomId == roomId)]);

            return users.Adapt<IEnumerable<UserDto>>();
        }

        public async Task<IEnumerable<UserDto>> SearchUser(string searchTerm, int userId)
        {
            var users = await _repo.SearchUser(searchTerm, userId);
            return users.Adapt<IEnumerable<UserDto>>();
        }

        public async Task<UserDto?> GetByIdAsync(int userId)
        {
            var user = await _repo.GetByIdAsync(userId);
            return user.Adapt<UserDto>();
        }

        public async Task<Result<UserDto>> ChangeAvatarAsync(int userId, string imgUrl)
         => await ExceptionHandler.HandleLazy<UserDto>(async () =>
            {
                var user = await _repo.GetByIdAsync(userId);
                if (user == null)
                {
                    return Result.Fail("User Không tồn tại");
                }
                user.Avatar = imgUrl;
                await _repo.SaveAsync();
                return user.Adapt<UserDto>();
            });

        public async Task<ProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _repo.GetByIdAsync(userId);
            return user.Adapt<ProfileDto>();
        }

        public async Task<Result<ProfileDto>> CreateUser(UserParam param)
        {
            return await ExceptionHandler.HandleLazy<ProfileDto>(async () =>
            {
                var salt = HashedPassword.GenerateRandomKey();
                var user = param.BuildAdapter().AddParameters("salt", salt).AdaptToType<User>();
                _repo.Insert(user);
                await _repo.SaveAsync();
                return user.Adapt<ProfileDto>();
            });
        }

        public async Task<Result<ProfileDto>> ChangeProfileAsync(int userId, JsonPatchDocument<ProfileDto> patchDoc)
        => await ExceptionHandler.HandleLazy<ProfileDto>(async () =>
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
            {
                return Result.Fail("User không tồn tại");
            }
            var userPatchDoc = patchDoc.Adapt<JsonPatchDocument<User>>();
            userPatchDoc.ApplyTo(user);
            await _repo.SaveAsync();
            return user.Adapt<ProfileDto>();
        });
    }
}

