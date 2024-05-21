namespace ChatApp_Server.Params
{
    public class GroupParam
    {
        public string Name { get; set; } = null!;
        public string? Avatar { get; set; }
        public int GroupOwnerId { get; set; }
        public IEnumerable<int>? userIds { get; set; } = Enumerable.Empty<int>();
    }
}
