using Microsoft.AspNetCore.Http.HttpResults;

namespace ChatApp_Server.Helper
{
    public class HubResponse
    {
        public bool IsSuccess { get; set; }
        public object? Data { get; set; }

        public object? Error { get; set; }

        public static HubResponse Ok()
        {
            return new HubResponse
            {
                IsSuccess = true,            
            };
        }
        public static HubResponse Ok(object  data)
        {
            return new HubResponse { 
                IsSuccess = true,
                Data = data 
            };
        }
        public static HubResponse Fail(object error)
        {
            return new HubResponse
            {
                IsSuccess = false,
                Error = error
            };
        }
    }
}
