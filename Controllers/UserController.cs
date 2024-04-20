using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<User>> Register(UserRegistrationDto registrationDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        if (await UserExists(registrationDto.Email))
            return BadRequest("Email already exists");

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
        Console.WriteLine("$username recu : "+ model.Username +" mot de passe recu" +model.Password); // debug line  lol 
        if (user == null || VerifyPasswordHash(model.Password, user.Password, user.Salt))
            return BadRequest("Username or password is incorrect");

        var token = GenerateJwtToken(user);
        return Ok(token);
    }
    
    private async Task<bool> UserExists(string email)
    {
        return await _context.Users.AnyAsync(x => x.Email == email);
    }

    private string GenerateJwtToken(User user)
    {
        // TODO : Add global variable for SecretKey
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyHere"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.RoleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddHours(1), signingCredentials: credentials);
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