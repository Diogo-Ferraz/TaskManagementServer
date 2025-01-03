using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Auth.Presentation.Models.Authorize
{
    public class AuthorizeViewModel
    {
        [Display(Name = "Application")]
        public string ApplicationName { get; set; } = string.Empty;

        [Display(Name = "Scope")]
        public string Scope { get; set; } = string.Empty;
    }
}
