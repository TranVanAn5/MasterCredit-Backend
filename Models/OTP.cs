using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class OTP
    {
        [Key]
        public int Id { get; set;}
        public string OTPCode { get; set;} = string.Empty;
        public string OTPType { get; set;} = string.Empty;
        public DateTime ExpiryTime { get; set;}
        public bool IsUsed { get; set;} = false;
        public int UserId { get; set;}
        [ForeignKey("UserId")]
        public User User{ get; set;} = null!;
    }
}