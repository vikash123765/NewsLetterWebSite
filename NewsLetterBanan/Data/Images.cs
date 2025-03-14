using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Data
{
    public class Image
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public  int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string ImgDescription { get; set; } = string.Empty;
        [Required]
        public string ImgSourceURL { get; set; } = string.Empty;
        [Required]
        public string TakenBy { get; set; } =string.Empty;
        [Required]
        public string License { get; set; } = string.Empty;
        [Required]

        // Foreign key relationship
        public int ArticleId
        {
            get; set;
        }
        public virtual Article Article { get; set; }
    }
}
