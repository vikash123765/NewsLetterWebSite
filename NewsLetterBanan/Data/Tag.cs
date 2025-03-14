using NewsLetterBanan.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Tag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public  int Id { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    public required string TagName { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    public  string TagDescription { get; set; }

    // Many-to-many relationship with Article
    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
}
