using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public IActionResult Authenticate([FromBody] LoginModel model)
    {
        var user = _context.Users.SingleOrDefault(u => u.Username == model.Username);

        if (user == null || !VerifyPasswordHash(model.Password, user.Password, user.Salt))
            return BadRequest("Username or password is incorrect");

        var token = GenerateJwtToken(user);
        return Ok(token);
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyHere"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
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