using ChatApp_Server.Criteria;
using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Params;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace ChatApp_Server.Services
{
    public interface IRoomService
    {
        Task<RoomDto?> GetAsync(int roomId, int? userId = null);
        Task<RoomDto?> GetIncludeMemberInfoAsync(int roomId, int? userId = null);
        Task<IEnumerable<RoomDto>> GetAllAsync(int userId);
        Task<Result<RoomDto>> CreateRoomAsync(RoomParam param);
        Task<RoomMemberInfoDto> GetRoomMember(MemberParam param);     
        Task<Result<RoomMemberInfoDto>> UpdateFirstUnseenMessageAsync(long messageId, int userId);
        Task<Result> UpdateCanDisplayRoomAsync(int roomId, int userId, bool canDisplay);
    }
    public class RoomService(IRoomRepository _roomRepo, IRoomMemberInfoRepository _memberRepo) : IRoomService
    {
        public async Task<RoomDto?> GetAsync(int roomId, int? userId = null)
        {
            var criteria = new RoomCriteria { Id = roomId };
            if (userId != null)
                criteria.MemberId = userId;

            var room = await _roomRepo.GetAsync(criteria);
            return room.Adapt<RoomDto>();
        }
        public async Task<RoomDto?> GetIncludeMemberInfoAsync(int roomId, int? userId = null)
        {
            var criteria = RoomCriteria.CreateWithAllInclude(roomId);
            if (userId != null)
                criteria.MemberId = userId;

            var room = await _roomRepo.GetAsync(criteria);
            return room.Adapt<RoomDto>();
        }

        public async Task<Result<RoomDto>> CreateRoomAsync(RoomParam param)
        => await ExceptionHandler.HandleLazy<RoomDto>(async () =>
        {
            var room = param.Adapt<Room>();
            _roomRepo.Insert(room);
            await _roomRepo.SaveAsync();
              
            room = await _roomRepo.GetAsync(RoomCriteria.CreateWithAllInclude(room.Id));
            return room.Adapt<RoomDto>();
        });       

        public async Task<Result> UpdateCanDisplayRoomAsync(int roomId, int userId, bool canDisplay)
        => await ExceptionHandler.HandleLazy(async () =>
        {
            _memberRepo.Update(new RoomMemberInfo { UserId = userId, RoomId = roomId, CanDisplayRoom = canDisplay });
            await _memberRepo.SaveAsync();
            return Result.Ok();
        });

        public async Task<Result<RoomMemberInfoDto>> UpdateFirstUnseenMessageAsync(long messageId, int userId)
         => await ExceptionHandler.HandleLazy<RoomMemberInfoDto>(async () =>
         {
             var rm = await _memberRepo.UpdateFirstUnseenMessage(messageId, userId);
             return rm.Adapt<RoomMemberInfoDto>();
         });

        public async Task<RoomMemberInfoDto> GetRoomMember(MemberParam param)
        {
            var member = await _memberRepo.GetAsync(new MemberCriteria { RoomId = param.GroupId, UserId = param.UserId, IncludeUserInfo = true });
            return member.Adapt<RoomMemberInfoDto>();
        }

        public async Task<IEnumerable<RoomDto>> GetAllAsync(int userId)
        {
            var rooms = await _roomRepo.GetAllAsync(new RoomsCriteria { MemberId =  userId, IncludeMemberInfo = true });
            return rooms.Adapt<IEnumerable<RoomDto>>();
        }
    }
}