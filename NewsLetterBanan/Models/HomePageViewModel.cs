using NewsLetterBanan.Data;

namespace NewsLetterBanan.Models
{
    public class HomePageViewModel
    {
        public IEnumerable<Article> Latest { get; set; }
        public IEnumerable<Article> EditorsChoice { get; set; }
    }
}
