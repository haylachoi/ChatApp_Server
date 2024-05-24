using ChatApp_Server.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ChatApp_Server.DTOs
{
    public class GroupInfoDto
    {
        [JsonProperty("id")]
        public int GroupId { get; set; }

        public string Name { get; set; } = null!;

        public string? Avatar { get; set; }
        public int GroupOwnerId { get; set; }
        public UserDto GroupOwner { get; set; } = null!;
    }
}
