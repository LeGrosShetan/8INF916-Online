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
    
    /**
     * <summary>Try to save a RedisGameServer to redis' database</summary>
     * <param name="server">The server we want to save</param>
     * <exception cref="BadRequestResult">Thrown if at least one of DTO's fields is null, or if an error prevented Redis to save</exception>
     * <exception cref="UnauthorizedResult">Thrown if JWT's Token has empty or null Role field, or if it do not have DGS role</exception>
     * <returns>Status code 200 - OK containing saved server</returns>
     */
    [HttpPost("save")]
    [Authorize]
    public async Task<IActionResult> SaveGameServer([FromBody] RedisGameServer server)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(jwtUserRoleId) ||
            !"Dedicated Game Server".Equals(_context.Roles.Find(Int32.Parse(jwtUserRoleId))?.Name))
        {
            return Unauthorized("User does not have DGS permissions");
        }
        
        bool result = await _gameServerService.SaveGameServerAsync(server.Ip, server);
        if (result)
            return Ok(new {Server = server});
        else
            return BadRequest("An error occured while saving the game server");
    }

    /**
     * <summary>Tries to retrieve a server from redis' database using it's IP</summary>
     * <param name="serverIp">The Ip of the server we want info on</param>
     * <exception cref="NotFoundResult">Thrown if server is not stored on redis</exception>
     * <returns>Status code 200 - OK containing server info</returns>
     */
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
    
    /**
     * <summary>Retrieves all the servers saved on redis</summary>
     * <returns>Status code 200 - OK containing a list of stored servers</returns>
     */
    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllGameServers()
    {
        var servers = await _gameServerService.GetAllGameServersAsync();
        return Ok(servers);
    }

    /**
     * <summary>Tries to matchmake an authenticated user using his JWT. Matchmaking will try and find a sever whose playerRank mean is close to Auth user's rank</summary>
     * <exception cref="UnauthorizedResult">Thrown if JWT is invalid</exception>
     * <exception cref="NotFoundResult">Thrown if JWT's UserId is not stored in database, or if no server is up in redis' database</exception>
     * <exception cref="BadRequestResult">Thrown if a DGS initiated the matchmaking</exception>
     * <returns>Status code 200 - OK containing server info of either the best server based on connected player's rank mean or an empty server</returns>
     */
    [HttpGet("matchmake")]
    [Authorize]
    public async Task<IActionResult> MatchmakeAuthUser()
    {
        var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(jwtUserId))
        {
            return Unauthorized("JWT token is invalid");
        }

        var user = _context.Users.SingleOrDefault(u => u.Id.ToString().Equals(jwtUserId));
        if (user == null)
        {
            return NotFound("User not found");
        }

        if ("Dedicated Game Server".Equals(_context.Roles.SingleOrDefault(r => r.Id == user.RoleId)?.Name))
        {
            return BadRequest("A server cant be in queue for matchmaking !");
        }
        
        var server = await _gameServerService.MatchmakeForUser(user);
        if (server == null)
        {
            return NotFound("Servers are unavailable");
        }
        return Ok(server);
    }
}