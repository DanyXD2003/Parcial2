using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.DTOs.Auth;
using Parcial2.Models;
using Parcial2.Services;

namespace Parcial2.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        var token = _jwt.GenerateToken(user);
        return Ok(new LoginResponseDto
        {
            Token     = token,
            Username  = user.Username,
            Role      = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return Conflict(new { message = "El nombre de usuario ya existe" });

        if (dto.Role != "Admin" && dto.Role != "Cashier")
            return BadRequest(new { message = "Rol inválido. Use 'Admin' o 'Cashier'" });

        var user = new User
        {
            Username     = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role         = dto.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Usuario creado", user.Username, user.Role });
    }
}
