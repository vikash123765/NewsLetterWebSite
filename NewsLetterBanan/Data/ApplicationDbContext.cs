using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Models.API;

namespace NewsLetterBanan.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<CommentLike> CommentLikes { get; set; }
        public virtual DbSet<CommentReply> CommentReplies { get; set; }
        public virtual DbSet<CommentReplyLike> CommentReplyLikes { get; set; }
        public virtual DbSet<ArticleLike> ArticleLikes { get; set; }
        public virtual DbSet<Image> Images { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<SubscriptionType> SubscriptionTypes { get; set; }
        public virtual DbSet<WeatherForecast> WeatherForecasts { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Inbox> Inboxes { get; set; }
        public DbSet<Sent> SentMessages { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed SubscriptionType table with predefined values
            modelBuilder.Entity<SubscriptionType>().HasData(
                new SubscriptionType { Id = 1, TypeName = "Local", Description = "Local news and updates", Price = 9.99 },
                new SubscriptionType { Id = 2, TypeName = "Sweden", Description = "Sweden-specific news and updates", Price = 14.99 },
                new SubscriptionType { Id = 3, TypeName = "World", Description = "Global news and updates", Price = 19.99 },
                new SubscriptionType { Id = 4, TypeName = "Weather", Description = "Weather forecasts and updates", Price = 4.99 },
                new SubscriptionType { Id = 5, TypeName = "Economy", Description = "Economic news and analysis", Price = 12.99 },
                new SubscriptionType { Id = 6, TypeName = "Sport", Description = "Sport news and events", Price = 7.99 },
                new SubscriptionType { Id = 7, TypeName = "Technology", Description = "Latest tech news and innovations", Price = 9.99 }


            );

            // One-to-Many: User → Subscriptions
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-One: Subscription → SubscriptionType
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.SubscriptionType)
                .WithMany(st => st.Subscriptions)
                .HasForeignKey(s => s.SubscriptionTypeId)
             .OnDelete(DeleteBehavior.Cascade);

            // Many-to-Many: Articles ↔ Tags
            modelBuilder.Entity<Article>()
                .HasMany(a => a.Tags)
                .WithMany(t => t.Articles)
                .UsingEntity<Dictionary<string, object>>(
                    "ArticleTag",
                    at => at.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                    at => at.HasOne<Article>().WithMany().HasForeignKey("ArticleId"));

            // Many-to-Many: Articles ↔ Categories
            modelBuilder.Entity<Article>()
                .HasMany(a => a.Categories)
                .WithMany(c => c.Articles)
                .UsingEntity<Dictionary<string, object>>(
                    "ArticleCategory",
                    ac => ac.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                    ac => ac.HasOne<Article>().WithMany().HasForeignKey("ArticleId"));

            // One-to-Many: User → ArticleLikes
            modelBuilder.Entity<ArticleLike>()
                .HasOne(al => al.User)
                .WithMany(u => u.ArticleLikes)
                .HasForeignKey(al => al.UserId)
             .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict



            // One-to-Many: Article → ArticleLikes
            modelBuilder.Entity<ArticleLike>()
                .HasOne(al => al.Article)
                .WithMany(a => a.ArticleLikes)
                .HasForeignKey(al => al.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);


            // One-to-Many: User → Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Article → Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Article)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → CommentLikes
            modelBuilder.Entity<CommentLike>()
                .HasOne(cl => cl.User)
                .WithMany(u => u.CommentLikes)
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Comment → CommentLikes
            modelBuilder.Entity<CommentLike>()
                .HasOne(cl => cl.Comment)
                .WithMany(c => c.CommentLikes)
                .HasForeignKey(cl => cl.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → CommentReplies
            modelBuilder.Entity<CommentReply>()
                .HasOne(cr => cr.User)
                .WithMany(u => u.CommentReplies)
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Comment → CommentReplies
            modelBuilder.Entity<CommentReply>()
                .HasOne(cr => cr.Comment)
                .WithMany(c => c.Replies)
                .HasForeignKey(cr => cr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → CommentReplyLikes
            modelBuilder.Entity<CommentReplyLike>()
                .HasOne(crl => crl.User)
                .WithMany(u => u.CommentReplyLikes)
                .HasForeignKey(crl => crl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: CommentReply → CommentReplyLikes
            modelBuilder.Entity<CommentReplyLike>()
                .HasOne(crl => crl.CommentReply)
                .WithMany(cr => cr.CommentReplyLikes)
                .HasForeignKey(crl => crl.CommentReplyId)
                .OnDelete(DeleteBehavior.Cascade);



            // ✅ Define Message Relationships with explicit FK properties
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ Define Inbox Relationships
            modelBuilder.Entity<Inbox>()
                .HasOne(i => i.Message)
                .WithMany()
                .HasForeignKey(i => i.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inbox>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Define Sent Messages Relationships
            modelBuilder.Entity<Sent>()
                .HasOne(s => s.Message)
                .WithMany()
                .HasForeignKey(s => s.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Sent>()
                .HasOne(s => s.Sender)
                .WithMany()
                .HasForeignKey(s => s.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure WeatherForecast has no primary key
            modelBuilder.Entity<WeatherForecast>().HasNoKey();
        }
    }
}
