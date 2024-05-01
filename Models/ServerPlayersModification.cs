using System.ComponentModel.DataAnnotations;

public class ServerPlayersModification
{
    [Required] public string ServerIp { get; set; }
    [Required] public string UserId { get; set; }
}