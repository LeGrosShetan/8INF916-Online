using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Online_API.Models;


[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private ApplicationDbContext _context;
    private IConfiguration _config;

    public UserController(ApplicationDbContext context,
        IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<User>> Register(UserRegistrationDto registrationDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        if (await EmailExists(registrationDto.Email))
            return BadRequest("Email already exists");
        
        if (await UsernameExists(registrationDto.Username))
            return BadRequest("Username already exists");

        using var hmac = new HMACSHA512();

        var user = new User
        {
            Email = registrationDto.Email,
            Username = registrationDto.Username,
            Password = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationDto.Password))),
            Salt = Convert.ToBase64String(hmac.Key),
            RoleId = 1
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(201);  // Created
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var user = _context.Users.SingleOrDefault(u => u.Username == model.Username);
        
        if (user == null || VerifyPasswordHash(model.Password, user.Password, user.Salt))
            return BadRequest("Username or password is incorrect");

        var token = GenerateJwtToken(user);
        return Ok(token);
    }

    [HttpGet("user")]
    [Authorize]
    public IActionResult GetUserIdFromJWT()
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (jwtUserId.IsNullOrEmpty() || jwtUserRoleId.IsNullOrEmpty())
        {
            return Unauthorized("Invalid JWT token");
        }
        
        var user = _context.Users.SingleOrDefault(userDB => userDB.Id.ToString().Equals(jwtUserId));
        if (user == null)
        {
            return BadRequest("User was not found");
        }

        return Ok(new {Id = user.Id, Username = user.Username});
    }
    
    [HttpGet("rank")]
    [Authorize]
    public IActionResult GetUserRank([FromBody] Guid UserId)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var user = _context.Users.Find(UserId);
        
        if (user == null)
            return BadRequest("User not found");

        var rank =_context.Ranks.Find(_context.UsersRanks.Find(UserId)?.RankId);

        if (rank == null) return NotFound("User is not ranked yet");
        
        return Ok(rank);
    }
    
    [HttpPost("updateRank")]
    [Authorize]
    public IActionResult SetUserRank([FromBody] UpdateModel updateModel)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (jwtUserRoleId.IsNullOrEmpty() ||
            !"Dedicated Game Server".Equals(_context.Roles.Find(Int32.Parse(jwtUserRoleId))?.Name))
        {
            return Unauthorized("Vous ne disposez pas des permissions nécessaires");
        }
        
        var user = _context.Users.Find(updateModel.UserId);
        
        if (user == null)
            return NotFound("User not found");

        var rank =_context.Ranks.Find(updateModel.RankId);

        if (rank == null) return NotFound("Rank doesnt exist");
        
        var UREntry = _context.UsersRanks.Find(updateModel.UserId);
        if (UREntry == null)
        {
            var newUsersRanks = new UsersRanks();
            newUsersRanks.UserId = updateModel.UserId;
            newUsersRanks.RankId = updateModel.RankId;
            
            _context.UsersRanks.Add(newUsersRanks);
        }
        else
        {
            UREntry.RankId = updateModel.RankId;
            _context.Entry(UREntry).State = EntityState.Modified;
        }

        _context.SaveChanges();
        
        return Ok(_context.UsersRanks.Find(updateModel.UserId));
    }
    
    private async Task<bool> EmailExists(string email)
    {
        return await _context.Users.AnyAsync(x => x.Email == email);
    }
    
    private async Task<bool> UsernameExists(string username)
    {
        return await _context.Users.AnyAsync(x => x.Username == username);
    }

    private string GenerateJwtToken(User user)
    {
        // TODO : Add global variable for SecretKey
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("6a29ce19b1c552a59bc3759e9dfd7c40f764e87cd74aa0a6adeabf180fb7f236c4dcf4f8eb97a806605d2d5cff1f6ca93be91f9cc9e28b7a815d25b58b35cd556ca66f559a3db228f1987f3f545bdfefab754c8d56d38b945385a4225367ed93e4b2384c5c0172486b75598da708907d47b282dcde0c532e3a55cb84e175191465129e86164785bf0f47c2e95979ef5a84ffe2176d9e0d7e7cdfef8a87be14d56759ddd8fffeb5f0528180a45cbc726010fddb8b9cf67e6c52aca36f058b897f2717deeb2806dfdb6a3479c7aca27180cb3a8bcfbf034a3d50d10510c6cc5fba997dc7fc776bc4dd7e5a4fc55db7fb9ceb26ac1c3d09a440309e2a50868f65c4c619ebfdee124a174e86129f0e46ee72903526447cdfe06358bffabe23683370de319203e807b3f2496532ad8525e4c22e2ee2f001140acadbd2ece7abd959ef"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.RoleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        var token = new JwtSecurityToken(_config["Jwt:Issuer"],
            _config["Jwt:Issuer"],
            claims: claims, 
            expires: DateTime.Now.AddHours(1), 
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
    {
        using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(storedSalt)))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(Convert.FromBase64String(storedHash));
        }
    }
}