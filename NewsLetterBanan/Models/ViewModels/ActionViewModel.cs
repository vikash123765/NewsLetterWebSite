namespace NewsLetterBanan.Models.ViewModels
{
    public class ActionViewModel
    {
        public string SelectedAction { get; set; }
        public Dictionary<string, Dictionary<string, string>> ActionFields { get; set; }
    }
}
