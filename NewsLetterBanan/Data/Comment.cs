
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Data
{

    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public  int Id { get; set; }

        public int ArticleId { get; set; }



        [Required]
        public required string UserId { get; set; } // UserId string(Guid) standard

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime DateStamp { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(300)]
        public string Content { get; set; }



        public bool IsArchived { get; set; } = false;

        public virtual User User { get; set; }
        public virtual Article Article { get; set; }

        // New: Collection of replies for this comment
        public virtual ICollection<CommentReply> Replies { get; set; } = new HashSet<CommentReply>();

        public virtual ICollection<CommentLike> CommentLikes { get; set; } = new HashSet<CommentLike>();
    }
}
