using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Pr4MVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(120)]
        public string? FullName { get; set; }
    }
}
