using System.ComponentModel.DataAnnotations;

public class Role
{
    public int Id { get; set; }
    
    [Required] [MinLength(1)] public string Name { get; set; } = string.Empty;
}