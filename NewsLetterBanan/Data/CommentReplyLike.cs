using System.ComponentModel.DataAnnotations;

namespace NewsLetterBanan.Data
{
    public class CommentReplyLike
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        public int CommentReplyId { get; set; }

        public bool Liked { get; set; } = true;

        public virtual User User { get; set; }

        public virtual CommentReply CommentReply { get; set; }
    }
}
