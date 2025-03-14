using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Data
{
    public class Article
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        [Required]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime DateStamp { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(60)]
        public string Headline { get; set; } = string.Empty;

        [Required]
        [MaxLength(15000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        [MaxLength(350)]
        public string ContentSummary { get; set; } = string.Empty;

        [Required]
        public bool Exclusive { get; set; } = false;

        [Required]
        [Column(TypeName = "nvarchar(450)")]
        public string UserId { get; set; } = string.Empty; // The ID of the User who authored this article

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string SourceURL { get; set; } = string.Empty;

        [Required]
        public bool IsArchived { get; set; } = false;

        [Required]
        public bool CommentsOnOff { get; set; } = false;
  
        public int Views { get; set; } = 0;

        [Required]
        public bool IsApproved { get; set; } = false;


        [Required]
        public bool IsEditorsChoice { get; set; }

        [Required]
        public virtual ICollection<Image> Images { get; set; }= new HashSet<Image>();

        [Required]
        // Many-to-many relationship with Tag
        public virtual ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
        [Required]
        // Many-to-many relationship with Category
        public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();
        public virtual ICollection<ArticleLike> ArticleLikes { get; set; } = new HashSet<ArticleLike>();
        [Required]
        public virtual User User { get; set; } // The author of the article
        [Required]
        public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
     
    }

}