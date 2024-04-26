
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
    
    [HttpGet("authAchievements")]
    [Authorize]
    public IActionResult GetUserAchievements()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId.IsNullOrEmpty())
        {
            return Unauthorized();
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

    [HttpPost("grant")]
    [Authorize]
    public IActionResult GrantAchievementToUser([FromBody] GrantAchievementDTO grantAchievementDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!VerifyGrantParams(grantAchievementDto))
        {
            return Unauthorized();
        }

        AchievementsUsers newAchievementsUsers = new AchievementsUsers();
        newAchievementsUsers.AchievementId = grantAchievementDto.AchievementId;
        newAchievementsUsers.UserId = grantAchievementDto.UserId;

        _context.AchievementsUsers.Add(newAchievementsUsers);
        _context.SaveChanges();
        
        return Ok(new { UserId = grantAchievementDto.UserId, Achievement = _context.Achievements.Find(newAchievementsUsers.AchievementId)});
    }

    private bool VerifyGrantParams(GrantAchievementDTO grantAchievementDto)
    {
        // Vérification JWT
        var jwtUserId = User.FindFirst(ClaimTypes.Role)?.Value;
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (jwtUserRoleId.IsNullOrEmpty() || jwtUserId.IsNullOrEmpty())
        {
            return false;
        }

        // Vérification du grant + Role suffisant
        var grantedUserId = _context.Users.Find(grantAchievementDto.UserId)?.Id;
        var grantedUserRoleId = _context.Users.Find(grantAchievementDto.UserId)?.RoleId;
        if (grantedUserId.ToString().IsNullOrEmpty() || grantedUserRoleId.ToString().IsNullOrEmpty() || !"Dedicated Game Server".Equals(_context.Roles.Find(grantedUserRoleId)?.Name))
        {
            return false;
        }
        
        return true;
    }
}