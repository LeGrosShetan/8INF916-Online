using System.ComponentModel.DataAnnotations;

public class GrantAchievementDTO
{
    [Required] public int AchievementId { get; set; }
    
    [Required] public Guid UserId { get; set; }
}