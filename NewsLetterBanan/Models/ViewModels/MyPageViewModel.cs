using System.ComponentModel.DataAnnotations;
using NewsLetterBanan.Data;

namespace NewsLetterBanan.ViewModels
{
    public class MyPageViewModel
    {
        public string UserId { get; set; }
        public string? UserName { get; set; }
        public string FirstName { get; set; } = string.Empty; // Default empty
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [MaxLength(23)]
        public string City { get; set; } = string.Empty;

        [MaxLength(69)]
        public string Country { get; set; } = string.Empty;
        public List<Comment> UserComments { get; set; } = new();
        public List<Comment> UserCommentsLikes { get; set; } = new();
        public List<Subscription> Subscriptions { get; set; } = new();
        public List<Article> ExclusiveArticles { get; set; } = new();

        public List<Article> UserLikedArticles { get; set; } = new();

        public List<CommentReply> UserCommentReplies { get; set; } = new();
        public List<CommentReply> UserLikedCommentReplies { get; set; } = new();
    }

}