using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.DTOs
{
    public class ReactionDto
    {        
        public int Id { get; set; }

        public string? Name { get; set; }

    }
}
