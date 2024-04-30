using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.DTOs
{
    public class GroupMemberDto: IBaseDto<int?>
    {
        public int Groupid { get; set; }
        public int Memberid { get; set; }      
        public DateTime? Createdat { get; set; }   
        public int? Id { get; set; }
    }
}
