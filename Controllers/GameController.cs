using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Online_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GameController : ControllerBase
{
    private ApplicationDbContext _context;
    private readonly GameServerService _gameServerService;

    public GameController(ApplicationDbContext context,
        GameServerService gameServerService)
    {
        _context = context;
        _gameServerService = gameServerService;
    }
    
    [HttpPost("save")]
    [Authorize]
    public async Task<IActionResult> SaveGameServer([FromBody] RedisGameServer server)
    {
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (jwtUserRoleId.IsNullOrEmpty() ||
            !"Dedicated Game Server".Equals(_context.Roles.Find(Int32.Parse(jwtUserRoleId))?.Name))
        {
            return Unauthorized("Vous ne disposez pas des permissions nécessaires");
        }
        
        bool result = await _gameServerService.SaveGameServerAsync(server.Ip, server);
        if (result)
            return Ok();
        else
            return BadRequest("An error occured while saving the game server");
    }

    [HttpGet("get")]
    [Authorize]
    public async Task<IActionResult> GetGameServer([FromBody] string serverIp)
    {
        RedisGameServer server = await _gameServerService.GetGameServerAsync(serverIp);
        if (server != null)
            return Ok(server);
        else
            return NotFound("Server does not exist");
    }
    
    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllGameServers()
    {
        var servers = await _gameServerService.GetAllGameServersAsync();
        return Ok(servers);
    }
}