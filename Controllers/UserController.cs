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
    
    /**
     * <summary>Tries to register a User through a UserRegistrationDTO</summary>
     * <param name="registrationDto">The User we want to register</param>
     * <exception cref="BadRequestResult">Thrown if the DTO is incomplete, or if a user with the same email/username exists in database</exception>
     * <returns>Status code 200 - OK</returns>
     */
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

    /**
     * <summary>Login to a User stored in database using a loginModel</summary>
     * <param name="loginModel">The model containing credentials</param>
     * <exception cref="BadRequestResult">Thrown if model is incomplete, or if Username & password combination does not match in database</exception>
     * <returns>Status code 200 - OK containing a JWT token freshly generated</returns>
     */
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginModel loginModel)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var user = _context.Users.SingleOrDefault(u => u.Username == loginModel.Username);
        
        if (user == null || VerifyPasswordHash(loginModel.Password, user.Password, user.Salt))
            return BadRequest("Username or password is incorrect");

        var token = GenerateJwtToken(user);
        return Ok(token);
    }

    /**
     * <summary>Retrieves an authenticated User's Id & username through it's JWT</summary>
     * <exception cref="UnauthorizedResult">Thrown if user has an invalid token</exception>
     * <exception cref="BadRequestResult">Thrown if User is not found</exception>
     * <returns>Status code 200 - OK containing UserId and Username</returns>
     */
    [HttpGet("user")]
    [Authorize]
    public IActionResult GetUserIdFromJWT()
    {
        var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(jwtUserId) || string.IsNullOrEmpty(jwtUserRoleId))
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
    
    /**
     * <summary>Retrieves a player rank from it's UserId</summary>
     * <param name="UserId">The Id of the player we want to get it's rank</param>
     * <exception cref="BadRequestResult">Thrown if UserId does not match a User</exception>
     * <exception cref="NotFoundResult">Thrown if User isn't ranked yet</exception>
     * <returns>Status code 200 - OK containing player's rank</returns>
     */
    [HttpGet("rank")]
    [Authorize]
    public IActionResult GetUserRank([FromBody] Guid UserId)
    {
        var user = _context.Users.Find(UserId);
        
        if (user == null)
            return BadRequest("User not found");

        var rank =_context.Ranks.Find(_context.UsersRanks.Find(UserId)?.RankId);

        if (rank == null) return NotFound("User is not ranked yet");
        
        return Ok(rank);
    }
    
    /**
     * <summary>Tries to update a player's rank using an UpdateModel</summary>
     * <param name="updateModel">Struct containing a PlayerId and a RankId corresponding to the new rank</param>
     * <exception cref="BadRequestResult">Thrown if updateModel is incomplete</exception>
     * <exception cref="UnauthorizedResult">Thrown if JWT Token doesnt have DGS accreditation</exception>
     * <exception cref="NotFoundResult">Thrown if User or Rank does not exist</exception>
     * <returns>User Id with it's new Rank</returns>
     */
    [HttpPost("updateRank")]
    [Authorize]
    public IActionResult SetUserRank([FromBody] UpdateModel updateModel)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var jwtUserRoleId = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(jwtUserRoleId) ||
            !"Dedicated Game Server".Equals(_context.Roles.Find(Int32.Parse(jwtUserRoleId))?.Name))
        {
            return Unauthorized("Invalid permissions");
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
    
    /**
     * <summary>Checks if a User with the same email is stored in database</summary>
     * <param name="email">The email to search among Users</param>
     * <returns>True if such a User is found, false otherwise</returns>
     */
    private async Task<bool> EmailExists(string email)
    {
        return await _context.Users.AnyAsync(x => x.Email == email);
    }
    
    /**
     * <summary>Checks if a User with the same username is stored in database</summary>
     * <param name="username">The username to search among Users</param>
     * <returns>True if such a User is found, false otherwise</returns>
     */
    private async Task<bool> UsernameExists(string username)
    {
        return await _context.Users.AnyAsync(x => x.Username == username);
    }

    /**
     * <summary>Generates a JWT Token for a given User using it's Id & RoleId</summary>
     * <param name="user">The user linked to the token's creation</param>
     * <returns>A JWT Token expiring 24 hours later from it's creation</returns>
     */
    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SymmetricSecurityKey"]));
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
            expires: DateTime.Now.AddHours(24), 
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /**
     * <summary>Compare an input password with a hashed password linked to a salt</summary>
     * <param name="password">The input password</param>
     * <param name="storedHash">Stored hashed password to challenge</param>
     * <param name="storedSalt">Stored salt linked to storedHash</param>
     * <returns>True if encrypted password corresponds to stored hash, false otherwise</returns>
     */
    private bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
    {
        using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(storedSalt)))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(Convert.FromBase64String(storedHash));
        }
    }
}