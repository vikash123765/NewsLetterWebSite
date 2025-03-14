using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NewsLetterBanan.Data
{
    public class CommentReply
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [Required]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // User who replied

        [Required]
        public int CommentId { get; set; } // The comment being replied to

        [Required]
        [MaxLength(300)]
        public string Content { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime DateStamp { get; set; } = DateTime.Now;
        public virtual ICollection<CommentReplyLike> CommentReplyLikes { get; set; } = new HashSet<CommentReplyLike>();
        public virtual User User { get; set; }
        public virtual Comment Comment { get; set; }
    }
}
