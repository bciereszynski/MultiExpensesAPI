using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MultiExpensesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableCors("AllowAll")]
public class AuthController(AppDbContext context, PasswordHasher<User> passwordHasher, IConfiguration configuration) : Controller
{
    [HttpPost("Register")]
    public IActionResult Register([FromBody] PostUserDto userDto)
    {
        if (context.Users.Any(u => u.Email == userDto.Email))
        {
            return BadRequest("Email already in use.");
        }

        var hashedPassword = passwordHasher.HashPassword(null, userDto.Password);

        var newUser = new User()
        {
            Email = userDto.Email,
            Password = hashedPassword,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(newUser);
        context.SaveChanges();

        var token = GenerateJwtToken(newUser);

        return Ok(new { Token = token });
    }

    [HttpPost("Login")]
    public IActionResult Login([FromBody] PostUserDto userDto)
    {
        var user = context.Users.SingleOrDefault(u => u.Email == userDto.Email);
        if (user == null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.Password, userDto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid credentials.");
        }

        var token = GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(double.Parse(configuration["Jwt:ExpirationHours"]!)),
            signingCredentials: creds);


        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
