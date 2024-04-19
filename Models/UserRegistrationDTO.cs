using System.ComponentModel.DataAnnotations;
public class UserRegistrationDto
{
    [Required] [MinLength(1)] public string Email { get; set; } = string.Empty;
    [Required] [MinLength(1)] public string Username { get; set; } = string.Empty;
    [Required] [MinLength(1)] public string Password { get; set; } = string.Empty;
}