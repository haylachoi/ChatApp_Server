using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.AspNetCore.JsonPatch;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using FluentResults;
using ChatApp_Server.Services;
using ChatApp_Server.Repositories;
using ChatApp_Server.Models;
using ChatApp_Server.Helper;
using ChatApp_Server.DTOs;
using ChatApp_Server.Parameters;

namespace ChatApp_Server.Services
{
    public interface IUserService : IBaseService<IUserRepository, User, UserParameter, int, UserDto>
    {      
        Task<User?> GetByEmailAsync(string email);
        Task<Result> UpdateConnectionStatus(int id, bool isOnline);
    }
    public class UserService : BaseService<IUserRepository, User, UserParameter, int, UserDto>, IUserService
    {
        public UserService(IUserRepository repo) : base(repo)
        {
        }

        public override async Task<IEnumerable<UserDto>> GetAllAsync(UserParameter parameter)
        {
            var name = parameter.SearchTerm;
            var ignoreList = parameter.IgnoreList;
            var filters = new List<Expression<Func<User, bool>>>();

            if (name != null && name != string.Empty)
            {
                filters.Add(u => u.Email.Contains(name) || (u.Fullname != null && u.Fullname.Contains(name)));
            }
            if (ignoreList != null && ignoreList.Count() > 0)
            {
                filters.Add(u => !ignoreList.Contains(u.Id));
            }
            var users = await _repo.GetAllAsync(filters); 
            return users.Adapt<IEnumerable<UserDto>>(); 
        }

        public override async Task<Result<UserDto>> InsertAsync(UserDto dto)
        {
            try
            {
                var salt = HashedPassword.GenerateRandomKey();
                var user = dto.Adapt<User>();
                if (user == null)
                {
                    return Result.Fail("Ko thể tạo user");
                }
                user.Salt = salt;
                user.Password = dto.Password!.ToSHA512Hash(salt);
                _repo.Insert(user);
                await _repo.SaveAsync();
                return Result.Ok(user.Adapt<UserDto>());
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }        
        }
       
        public async Task<User?> GetByEmailAsync(string email)
        {
            Expression<Func<User, bool>> predicate = u => u.Email == email;
            return await _repo.GetOne([predicate]);
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
    }
}

