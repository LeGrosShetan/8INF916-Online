using System.ComponentModel.DataAnnotations;

public class User
{
    public Guid Id { get; set; }
    [Required] [MinLength(1)] public string Email { get; set; } = string.Empty;
    [Required] [MinLength(1)] public string Username { get; set; } = string.Empty;
    [Required] [MinLength(1)] public string Password { get; set; } = string.Empty;
    [Required] [MinLength(1)] public string Salt { get; set; } = string.Empty;
    public int RoleId { get; set; }
}