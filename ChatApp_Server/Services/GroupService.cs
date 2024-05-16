using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Params;
using ChatApp_Server.Repositories;

using FluentResults;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ChatApp_Server.Services
{
    public interface IGroupService
    {
        Task<RoomDto> GetByIdAsync(int groupId);
        Task<Result<RoomDto>> CreateGroupAsync(GroupParam param);
        Task<Result<RoomMemberInfoDto>> AddMemberAsync(GroupMemberParam param);
        Task<Result<RoomMemberInfoDto>> RemoveMemberAsync(GroupMemberParam param);
    }
    public class GroupService(       
        IRoomRepository roomRepository, 
        IRoomMemberInfoRepository roomMemberInfoRepository) : IGroupService
    {
        public async Task<Result<RoomMemberInfoDto>> AddMemberAsync(GroupMemberParam param)
        {
            return await ExceptionHandler.HandleLazy<RoomMemberInfoDto>(async () =>
            {
                var entity = param.Adapt<RoomMemberInfo>();
                roomMemberInfoRepository.Insert(entity);
                await roomMemberInfoRepository.SaveAsync();
                var member = await roomMemberInfoRepository.GetOne([rm => rm.Id == entity.Id], query => query.Include(rm => rm.User));
                return member.Adapt<RoomMemberInfoDto>();
            });
        }

        public async Task<Result<RoomDto>> CreateGroupAsync(GroupParam param)
        {
            return await ExceptionHandler.HandleLazy<RoomDto>(async () =>
            {
                var entity = param.Adapt<Room>();
                roomRepository.Insert(entity);
                await roomRepository.SaveAsync();
                return entity.Adapt<RoomDto>();
            });
        }

        public async Task<RoomDto> GetByIdAsync(int groupId)
        {
            var group = await roomRepository.GetOne([r => r.Id == groupId], query => query.Include(r => r.RoomMemberInfos).ThenInclude(info => info.LastUnseenMessage));
            return group.Adapt<RoomDto>();
        }

        public async Task<Result<RoomMemberInfoDto>> RemoveMemberAsync(GroupMemberParam param)
        {
            // todo: check permission
            var memberInfo = await roomMemberInfoRepository.GetOne([r => r.UserId == param.UserId && r.RoomId == param.GroupId]);
            if (memberInfo == null)
            {
                return Result.Fail("Thành viên không có trong nhóm");
            }
            return await ExceptionHandler.HandleLazy<RoomMemberInfoDto>(async () =>
            {
                roomMemberInfoRepository.Delete(memberInfo);
                await roomMemberInfoRepository.SaveAsync();
                return memberInfo.Adapt<RoomMemberInfoDto>();             
            });
        }
    }
}
