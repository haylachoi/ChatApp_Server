using ChatApp_Server.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp_Server.DTOs
{
    public class PrivateRoomDto
    {
        public int? Id { get; set; }

        public int BiggerUserId { get; set; }

        public int SmallerUserId { get; set; }
        public  UserDto? BiggerUser { get; set; } 

        public  UserDto? SmallerUser { get; set; }

        public long? LastMessageId { get; set; }
       
        public long? FirstMessageId { get; set; }
        public long? FirstUnseenMessageId { get; set; }
        public long? LastUnseenMessageId { get; set; }
        public PrivateMessageDto? LastUnseenMessage { get; set; }
        public long UnseenMessageCount { get; set; }

        //public long? FirstUnseenBiggerUserMessageId { get; set; }

        //public long? FirstUnseenSmallerUserMessageId { get; set; }

        public UserDto? Friend { get; set; }

        [JsonProperty("chats")]
        public virtual IEnumerable<PrivateMessageDto>? PrivateMessages { get; set; }
        [JsonIgnore]
        public virtual IEnumerable<PrivateRoomInfoDto>? PrivateRoomInfos { get; set; }

    }
}
