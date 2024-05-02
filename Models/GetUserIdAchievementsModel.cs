using System.ComponentModel.DataAnnotations;

namespace Online_API.Models;

public class GetUserIdAchievementsModel
{
    [Required] [MinLength(1)] public string UserId { get; set; } = string.Empty;
}