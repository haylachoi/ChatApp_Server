using ChatApp_Server.Criteria;
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
        Task<Result<RoomDto>> DeleteGroupAsync(MemberParam param);
        Task<Result<RoomMemberInfoDto>> AddMemberAsync(MemberParam param);
        Task<Result<RoomMemberInfoDto>> RemoveMemberAsync(int userId, MemberParam param);
        Task<Result<UserDto>> SetGroupOwnerAsync(int currentGroupOwnerId, MemberParam param);
        Task<Result<RoomMemberInfoDto>> LeaveGroupAsync(MemberParam param);
    }
    public class GroupService(       
        IUserRepository _userRepo,
        IRoomRepository _roomRepo, 
        IRoomMemberInfoRepository _memberRepo) : IGroupService
    {
        public async Task<Result<RoomMemberInfoDto>> AddMemberAsync(MemberParam param)
        => await ExceptionHandler.HandleLazy<RoomMemberInfoDto>(async () =>
            {
            var member = param.Adapt<RoomMemberInfo>();
            _memberRepo.Insert(member);
            await _memberRepo.SaveAsync();
            member = await _memberRepo.GetAsync(new MemberCriteria { Id = member.Id, IncludeUserInfo = true });
            return member.Adapt<RoomMemberInfoDto>();
        });

        public async Task<Result<RoomDto>> CreateGroupAsync(GroupParam param)
        => await ExceptionHandler.HandleLazy<RoomDto>(async () =>
            {
            var entity = param.Adapt<Room>();
            _roomRepo.Insert(entity);
            await _roomRepo.SaveAsync();
            return entity.Adapt<RoomDto>();
        });

        public async Task<Result<RoomDto>> DeleteGroupAsync(MemberParam param)
        => await ExceptionHandler.HandleLazy<RoomDto>(async () =>
        {
            var group = await _roomRepo.GetAsync(new RoomCriteria { Id = param.GroupId});

            if (group == null)
            {
                return Result.Fail("Group không tồn tại");
            }
            if (group.GroupInfo == null || group.GroupInfo.GroupOwnerId != param.UserId)
            {
                return Result.Fail("Bạn không có quyền xóa group");
            }
            _roomRepo.Delete(group);
            await _roomRepo.SaveAsync();
            return group.Adapt<RoomDto>();
        });

        public async Task<RoomDto> GetByIdAsync(int groupId)
        {
            var group = await _roomRepo.GetAsync(
                new RoomCriteria
                {
                    Id = groupId
                });

            return group.Adapt<RoomDto>();
        }

        public async Task<Result<RoomMemberInfoDto>> LeaveGroupAsync(MemberParam param)
        => await ExceptionHandler.HandleLazy<RoomMemberInfoDto>(async () =>
        {
            var member = await _memberRepo.GetAsync(new MemberCriteria { RoomId = param.GroupId, UserId = param.UserId });
            if (member == null)
            {
                return Result.Fail("Thành viên không ở trong nhóm hoặc đang làm chủ nhóm");
            }

            _memberRepo.Delete(member);
            await _memberRepo.SaveAsync();
            return member.Adapt<RoomMemberInfoDto>();
        });

        public async Task<Result<RoomMemberInfoDto>> RemoveMemberAsync(int userId, MemberParam param)
        => await ExceptionHandler.HandleLazy<RoomMemberInfoDto>(async () =>
        {
            var member = await _memberRepo.GetAsync(new MemberCriteria { UserId = param.UserId, RoomId = param.GroupId, OwnerId = userId, HasOwner = true });
            if (member == null)
            {
                return Result.Fail("Thành viên không có trong nhóm hoặc bạn không có quyền kích thành viên");
            }
            _memberRepo.Delete(member);
            await _memberRepo.SaveAsync();
            return member.Adapt<RoomMemberInfoDto>();             
        });
      

        public async Task<Result<UserDto>> SetGroupOwnerAsync(int currentGroupOwnerId, MemberParam param)
        => await ExceptionHandler.HandleLazy<UserDto>(async () =>
        {         
            if (currentGroupOwnerId == param.UserId)
            {
                return Result.Fail("Người dùng đã là chủ nhóm");
            }

            var group = await _roomRepo.GetAsync(new RoomCriteria { Id = param.GroupId, OwnerId = currentGroupOwnerId, MemberId = param.UserId });
            if (group == null)
            {
                return Result.Fail("Người dùng ko phải là thành viên của nhóm hoặc bạn không có quyền thay đổi chủ nhóm");
            }
            if (group.GroupInfo == null)
            {
                return Result.Fail("Có lỗi xảy ra");
            }

            group.GroupInfo.GroupOwnerId = param.UserId;
            group.GroupInfo.GroupOwner = null!;

            _roomRepo.Update(group);
            await _roomRepo.SaveAsync();

            var user = await _userRepo.GetByIdAsync(group.GroupInfo.GroupOwnerId);
            if (user == null) {
                return Result.Fail("Người dùng không tồn tại");
            }
            return user.Adapt<UserDto>();
        });
    }
}
