using System.ComponentModel.DataAnnotations;

namespace Pr4MVC.ViewModels
{
    public class CreateUserVM
    {
        [Required, StringLength(120)]
        public string FullName { get; set; } = "";
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";
        [Required]
        public string Role { get; set; } = "cliente";
        public List<string> Roles { get; set; } = new() { "admin", "empleado", "cliente" };
    }

    public class EditUserVM
    {
        [Required]
        public string Id { get; set; } = "";
        [Required, StringLength(120)]
        public string FullName { get; set; } = "";
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        public string Role { get; set; } = "cliente";
        public List<string> Roles { get; set; } = new() { "admin", "empleado", "cliente" };
    }
}
