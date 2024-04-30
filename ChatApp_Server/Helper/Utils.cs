namespace ChatApp_Server.Helper
{
    public static class Utils
    {
        public static DateTime ConvertUnixTimeToDateTime(long utcExpireDate)
        {
            var dateTimeInterval = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeInterval.AddSeconds(utcExpireDate).ToUniversalTime();

            return dateTimeInterval;
        }

        public static string CreateGroupRoomName(int id) => "Group_" + id.ToString();
    }
}
