using System.ComponentModel.DataAnnotations;

public class Achievement
{ 
    public int Id { get; set; }
    
    [Required] [MinLength(1)] public string Name { get; set; } = string.Empty;
    
    [Required] [MinLength(1)] public string Description { get; set; } = string.Empty;
    
    [Required] [MinLength(1)] public string Image { get; set; } = string.Empty;
}