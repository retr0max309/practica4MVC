using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Pr4MVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = "";
        [StringLength(30)]
        public string? DisplayRole { get; set; }
    }
}
