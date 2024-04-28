using System.ComponentModel.DataAnnotations;

namespace Online_API.Models;

public class UpdateModel
{
    [Required] public Guid UserId { get; set; }
    [Required] public int RankId { get; set; }
}