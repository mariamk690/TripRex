using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CoreTripRex.Models
{
    public class UserSecurityQuestions
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string AnswerHash { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public AppUser? User { get; set; }
    }
}
