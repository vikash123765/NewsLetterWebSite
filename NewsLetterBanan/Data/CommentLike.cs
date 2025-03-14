using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Data
{

    public class CommentLike
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        
        [Required]
        public int Id { get; set; } // Primary Key
        public string UserId { get; set; } // Foreign Key to User
        public int CommentId { get; set; } // Foreign Key to Comment
        public bool Liked { get; set; } = true;
        // Navigation properties
        public virtual User User { get; set; }
        public virtual Comment Comment { get; set; }
    }


}
