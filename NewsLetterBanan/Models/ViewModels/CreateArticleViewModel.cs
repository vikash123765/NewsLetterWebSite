using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Models.ViewModels
{
    public class CreateArticleViewModel
    {
        [Required]
        public string Headline { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public string ContentSummary { get; set; }

        [Required]
        public DateTime DateStamp { get; set; }

        [Required]
        [Url] // Ensures valid URL format
        public string SourceURL { get; set; }

        [Required]
        public bool IsArchived { get; set; }

        [Required]
        public bool IsApproved { get; set; }

        [Required]
        public bool CommentsOnOff { get; set; }

        [Required]
        public bool IsEditorsChoice { get; set; }

        [Required]
        public string TagNames { get; set; }

        [Required]
        public string TagDescriptions { get; set; }

        [Required]
        public string CategoryNames { get; set; }

        [Required]
        public string CategoryDescriptions { get; set; }

        [Required]
        public string Titles { get; set; }

        [Required]
        public string ImgDescriptions { get; set; }

        [Required]
        [Url] // Ensures valid URL format
        public string ImgSourceURLs { get; set; }

        [Required]
        public string TakenBys { get; set; }

        [Required]
        public bool Exclusive { get; set; }

        [Required]
        public string Licenses { get; set; }


    }
}




