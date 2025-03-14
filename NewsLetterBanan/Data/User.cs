using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Data
{
    public class User : IdentityUser
    {

        [MaxLength(50)] // string default is nvarchar(max)
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
        public bool Newsletter { get; set; } = false;

        [MaxLength(23)]
        public string City { get; set; } = string.Empty;

        [MaxLength(69)]
        public string Country { get; set; } = string.Empty;

        [MaxLength(40)]
        public string StripeKey { get; set; } = string.Empty;
        public bool AllowComment { get; set; } = true;

        public virtual ICollection<Article> Articles { get; set; } = new HashSet<Article>();
        public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new HashSet<Subscription>();
        public virtual ICollection<ArticleLike> ArticleLikes { get; set; } = new HashSet<ArticleLike>();
        public virtual ICollection<CommentReply> CommentReplies { get; set; } = new HashSet<CommentReply>();
        public virtual ICollection<Message> Messages { get; set; } = new HashSet<Message>();
        // ✅ Navigation property for messages sent by the user
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

        // ✅ Navigation property for messages received by the user
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<CommentReplyLike> CommentReplyLikes { get; set; } = new HashSet<CommentReplyLike>();
        public virtual ICollection<CommentLike> CommentLikes { get; set; } = new HashSet<CommentLike>();

        public static implicit operator User(string v)
        {
            throw new NotImplementedException();
        }
    }
}
