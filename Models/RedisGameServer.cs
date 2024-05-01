using System.ComponentModel.DataAnnotations;
public class RedisGameServer
{
    [Required] public string Ip { get; set; }
    [Required] public List<Guid> PlayerUuids { get; set; }
    [Required] public string MapName { get; set; }
}