namespace ChatApp_Server.Parameters
{
    public class UserParameter
    {
        public string? SearchTerm { get; set ; }

        public IEnumerable<int>? IgnoreList { get; set; }
    }
}
