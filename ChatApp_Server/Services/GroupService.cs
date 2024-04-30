using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;

namespace ChatApp_Server.Services
{
    public interface IGroupService: IBaseService<IGroupRepository, Group, GroupParameter, int, GroupDto>
    {
    }
    public class GroupService : BaseService<IGroupRepository, Group, GroupParameter, int, GroupDto>, IGroupService
    {
        public GroupService(IGroupRepository repo) : base(repo)
        {
        }

        public override async Task<IEnumerable<GroupDto>> GetAllAsync(GroupParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
