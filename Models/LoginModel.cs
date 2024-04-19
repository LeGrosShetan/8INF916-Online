using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

public class LoginModel
{
    [Required] [MinLength(1)] public string Username { get; set; } = string.Empty;
    [Required] [MinLength(1)] public string Password { get; set; } = string.Empty;
}