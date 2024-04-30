using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;
using System.Linq.Expressions;

namespace ChatApp_Server.Services
{
    public interface IGroupMemberService : IBaseService<IGroupMemberRepository, GroupMember, GroupMemberParameter, int, GroupMemberDto>
    {
        Task<Result<int>> DeleteAsync(int groupId, int memberId);
    }
    public class GroupMemberService : BaseService<IGroupMemberRepository, GroupMember, GroupMemberParameter, int, GroupMemberDto>, IGroupMemberService
    {
        public GroupMemberService(IGroupMemberRepository repo) : base(repo)
        {
        }

        public async Task<Result<int>> DeleteAsync(int groupId, int memberId)
        {
            try
            {
                var groupMember = await _repo.GetGroupMemberByGroupIdAndMemberIdAsync(groupId, memberId);
                if (groupMember == null)
                {
                    return Result.Fail("Người dùng không ở trong group này");
                }
                _repo.Delete(groupMember.Id);
                await _repo.SaveAsync();
                return Result.Ok(groupMember.Id);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

        public override async Task<IEnumerable<GroupMemberDto>> GetAllAsync(GroupMemberParameter parameter)
        {
            List<Expression<Func<GroupMember, bool>>> filters = new List<Expression<Func<GroupMember, bool>>>();

            var memberId = parameter.MemberId;
            var groupId = parameter.GroupId;

            if (memberId.HasValue)
            {
                filters.Add(g => g.MemberId == memberId.Value);
            }
            if (groupId.HasValue)
            {
                filters.Add(g => g.GroupId == groupId);
            }


            var groupMember = await _repo.GetAllAsync(filters);
            return groupMember.Adapt<IEnumerable<GroupMemberDto>>();
        }
    }
}
