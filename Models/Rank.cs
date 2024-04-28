using System.ComponentModel.DataAnnotations;

public class Rank
{
    public int Id { get; set; }
    
    [Required] [MinLength(1)] public string Title { get; set; } = string.Empty;
}