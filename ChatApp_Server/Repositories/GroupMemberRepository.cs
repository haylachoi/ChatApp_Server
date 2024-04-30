using ChatApp_Server.Models;
using ChatApp_Server.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IGroupMemberRepository : IBaseRepository<GroupMember, int>
    {
        Task<GroupMember?> GetGroupMemberByGroupIdAndMemberIdAsync(int groupId, int memberId);
    }
    public class GroupMemberRepository : BaseRepository<GroupMember, int>, IGroupMemberRepository
    {
        public GroupMemberRepository(ChatAppContext context) : base(context)
        {
        }

        public override void Delete(int id)
        {
           _context.Remove(new  GroupMember { Id = id });  
        }

        public async Task<GroupMember?> GetGroupMemberByGroupIdAndMemberIdAsync(int groupId, int memberId)
        {
            return await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.MemberId == memberId);
        }
    }
}
