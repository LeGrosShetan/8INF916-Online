
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Online_API.Models;

[Route("api/[controller]")]
[ApiController]
public class AchievementController : ControllerBase
{
    private ApplicationDbContext _context;
    private IConfiguration _config;

    public AchievementController(ApplicationDbContext context,
        IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    /**
     * <summary>Gets achievements for a user using it's Id</summary>
     * <returns>Status code 200 - OK containing user's Id as well as a list of his achievements</returns>
     */
    [HttpGet("userIdAchievements")]
    [Authorize]
    public IActionResult GetUserIdAchievements([FromBody] Guid UserId)
    {
        List<AchievementsUsers> filteredUserAchievements = _context.AchievementsUsers.Where(au => au.UserId.Equals(UserId)).ToList();
        if (filteredUserAchievements.Count == 0)
        {
            return Ok(new { UserId = UserId, Achievements = Enumerable.Empty<Achievement>() });
        }
        
        List<Achievement> achievementsRes = new List<Achievement>();
        
        foreach (AchievementsUsers SingularAchievement in filteredUserAchievements)
        {
            var achievement = _context.Achievements.Find(SingularAchievement.AchievementId);
            if (achievement != null)
            {
                achievementsRes.Add(achievement);
            }
        }

        return Ok(new { UserId = UserId, Achievements = achievementsRes});
    }

    /**
     * <summary>Gets achievements for an authenticated user using it's JWT token</summary>
     * <exception cref="UnauthorizedResult">Thrown if JWT's UserId is empty or null</exception>
     * <returns>Status code 200 - OK containing user's Id as well as a list of his achievements</returns>
     */
    [HttpGet("authAchievements")]
    [Authorize]
    public IActionResult GetUserAchievements()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("JWT's UserId is empty or null");
        }
        
        List<AchievementsUsers> filteredUserAchievements = _context.AchievementsUsers.Where(au => au.UserId.ToString().Equals(userId)).ToList();
        if (filteredUserAchievements.Count == 0)
        {
            return Ok(new { UserId = userId, Achievements = Enumerable.Empty<Achievement>() });
        }
        
        List<Achievement> achievementsRes = new List<Achievement>();
        
        foreach (AchievementsUsers SingularAchievement in filteredUserAchievements)
        {
            var achievement = _context.Achievements.Find(SingularAchievement.AchievementId);
            if (achievement != null)
            {
                achievementsRes.Add(achievement);
            }
        }

        return Ok(new { UserId = userId, Achievements = achievementsRes});
    }

    /**
     * <summary>Grant an Achievement to a User, if JWT's token has DGS permission level</summary>
     * <param name="grantAchievementDto">DTO containing a player's Id as well as the of the achievement we want to grant</param>
     * <exception cref="BadRequestResult">Thrown if at least one of DTO's fields is null </exception>
     * <exception cref="UnauthorizedResult">Thrown if JWT is invalid, if DTO's objects do not exist or if JWT do not holds necessary permissions</exception>
     * <exception cref="ConflictResult">Thrown if DTO's User already has this achievement</exception>
     * <returns>Status code 200 - OK containing DTO's userId and the achievement he just received</returns>
     */
    [HttpPost("grant")]
    [Authorize]
    public IActionResult GrantAchievementToUser([FromBody] GrantAchievementDTO grantAchievementDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!VerifyGrantParams(grantAchievementDto))
        {
            return Unauthorized("Invalid JWT token : verify it's structure and/or your user's permissions");
        }

        if (_context.AchievementsUsers.Find(grantAchievementDto.UserId, grantAchievementDto.AchievementId) != null)
        {
            return Conflict("User already has this achievement !");
        }

        AchievementsUsers newAchievementsUsers = new AchievementsUsers();
        newAchievementsUsers.AchievementId = grantAchievementDto.AchievementId;
        newAchievementsUsers.UserId = grantAchievementDto.UserId;

        _context.AchievementsUsers.Add(newAchievementsUsers);
        _context.SaveChanges();
        
        return Ok(new { UserId = grantAchievementDto.UserId, Achievement = _context.Achievements.Find(newAchievementsUsers.AchievementId)});
    }

    /**
     * <summary>Verifies JWT Token's integrity, DTO's objects existence and checks if JWT holds DGS permissions</summary>
     * <param name="grantAchievementDto">The DTO we want to verify</param>
     * <returns>Returns <c>false</c> if JWT is incomplete, if DTO's objects do not exist or if JWT's role is not DGS. Returns <c>true</c> otherwise</returns>
     */
    private bool VerifyGrantParams(GrantAchievementDTO grantAchievementDto)
    {
        // Verify JWT's integrity
        var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(jwtUserRoleId) || string.IsNullOrEmpty(jwtUserId))
        {
            return false;
        }

        // Ensuring both DTO's User and Achievement exists
        var grantedUser = _context.Users.Find(grantAchievementDto.UserId);
        var grantedAchievement = _context.Achievements.Find(grantAchievementDto.AchievementId);
        if (grantedUser == null || grantedAchievement == null)
        {
            return false;
        }
        
        // Check if JWT's token is emitted from someone with DGS permission level
        if(!"Dedicated Game Server".Equals(_context.Roles.Find(Int32.Parse(jwtUserRoleId))?.Name))
        {
            return false;
        }
        
        return true;
    }
}