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
        Task<IEnumerable<RoomDto>> GetAllByUserAsync(int userId);
        Task<Result<RoomDto>> CreateRoomAsync(RoomParam param);
        Task<RoomDto?> GetOneAsync(int roomId);
        Task<RoomDto?> GetOneByRoomAsync(int roomId, int? userId);    
        Task<IEnumerable<RoomMemberInfoDto>> GetAllRoomOfMembersAsync(int roomId);
        Task<MessageDto> GetLastUnseenMessage(int roomId, int userId);
        Task<Result> UpdateCanDisplayRoom(int roomId, int userId, bool canDisplay);
        

    }
    public class RoomService: IRoomService
    {
        private readonly IRoomRepository roomRepository;
        private readonly IRoomMemberInfoRepository roomMemberInfoRepository;

        public RoomService(IRoomRepository roomRepository, IRoomMemberInfoRepository roomMemberInfoRepository) 
        {
            this.roomRepository = roomRepository;
            this.roomMemberInfoRepository = roomMemberInfoRepository;
        }

        public async Task<IEnumerable<RoomDto>> GetAllAsync()
        {        
         
            List<Expression<Func<Room, bool>>> filters = new List<Expression<Func<Room, bool>>>();
            Func<IQueryable<Room>, IIncludableQueryable<Room, object>>? includes = query => query
                .Include(r => r.GroupInfo).ThenInclude(gi => gi!.GroupOnwer)
                .Include(r => r.RoomMemberInfos).ThenInclude(rm => rm.LastUnseenMessage)
                .Include(r => r.RoomMemberInfos).ThenInclude(rm => rm.User);          
            
            var prs = await roomRepository.GetAllAsync(filters, includes: includes);
            var prDtos = prs.Adapt<IEnumerable<RoomDto>>().ToArray();
          
            return prDtos;
        }

        public async Task<MessageDto> GetLastUnseenMessage(int roomId, int userId)
        {
            var message = await roomRepository.GetLastUnseenMessage(roomId, userId);
            var dto = message.Adapt<MessageDto>();
            return dto;
        }

        public async Task<RoomDto?> GetOneByRoomAsync(int roomId, int? userId = null)
        {
            Func<IQueryable<Room>, IIncludableQueryable<Room, object>> includes = query => query           
                .Include(pm => pm.RoomMemberInfos);             
                
            var prs = await roomRepository.GetAllAsync([pr => pr.Id == roomId && pr.RoomMemberInfos.Any(info => info.UserId == userId)], includes: includes);
            var pr = prs.FirstOrDefault();
            var prDto = pr.Adapt<RoomDto>();          
                    
            return prDto;
        }

        public async Task<RoomDto?> GetOneAsync(int roomId)
        {
            var room = await roomRepository.GetOneWithInfoAsync(roomId);
            return room.Adapt<RoomDto>();
        }

        public async Task<Result<RoomDto>> CreateRoomAsync(RoomParam param)
        {
            return await ExceptionHandler.HandleLazy<RoomDto>(async () =>
            {
                var room = param.Adapt<Room>();
                roomRepository.Insert(room);
                await roomRepository.SaveAsync();
                var id = room.Id;
                room = await roomRepository.GetOneWithInfoAsync(id);
                return room.Adapt<RoomDto>();
            });
        }

        public async Task<IEnumerable<RoomDto>> GetAllByUserAsync(int userId)
        {
            var rooms = await roomRepository.GetAllAsync(
                [r => r.RoomMemberInfos.Any(info => info.UserId == userId)], 
                includes: query => query
                    .Include(r => r.GroupInfo).ThenInclude(gi => gi!.GroupOnwer)
                    .Include(r => r.RoomMemberInfos).ThenInclude(info => info.LastUnseenMessage)
                    .Include(r => r.RoomMemberInfos).ThenInclude(info => info.User)
            );       
            return rooms.Adapt<IEnumerable<RoomDto>>();
        }

        public async Task<Result> UpdateCanDisplayRoom(int roomId, int userId, bool canDisplay)
        {
            return await ExceptionHandler.HandleLazy(async () =>
            {
                roomMemberInfoRepository.Update(new RoomMemberInfo { UserId = userId, RoomId = roomId, CanDisplayRoom = canDisplay });
                await roomMemberInfoRepository.SaveAsync();
                return Result.Ok();
            });
        }

        public async Task<IEnumerable<RoomMemberInfoDto>> GetAllRoomOfMembersAsync(int userId)
        {
            var members = await roomMemberInfoRepository.GetAllAsync([rm => rm.UserId == userId]);
            return members.Adapt<IEnumerable<RoomMemberInfoDto>>();
        }
    }
}
