using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.DTOs
{
    public class GroupDto: IBaseDto<int?>
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public int GroupOwnerId { get; set; } 
        public DateTime? Createdat { get; set; }
    }
}
