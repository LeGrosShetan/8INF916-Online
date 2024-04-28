using Microsoft.AspNetCore.Mvc;

namespace Online_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GameController : ControllerBase
{
    private readonly GameServerService _gameServerService;

    public GameController(GameServerService gameServerService)
    {
        _gameServerService = gameServerService;
    }
    
    [HttpPost("save")]
    public async Task<IActionResult> SaveGameServer([FromBody] RedisGameServer server)
    {
        bool result = await _gameServerService.SaveGameServerAsync(server.Ip, server);
        if (result)
            return Ok();
        else
            return BadRequest();
    }

    [HttpGet("get")]
    public async Task<IActionResult> GetGameServer([FromBody] string serverIp)
    {
        RedisGameServer server = await _gameServerService.GetGameServerAsync(serverIp);
        if (server != null)
            return Ok(server);
        else
            return NotFound();
    }
    
    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllGameServers()
    {
        var servers = await _gameServerService.GetAllGameServersAsync();
        return Ok(servers);
    }
}