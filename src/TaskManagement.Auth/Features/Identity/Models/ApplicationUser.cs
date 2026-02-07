using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Auth.Features.Identity.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [MaxLength(256)]
        public string? DisplayName { get; set; }
    }
}
