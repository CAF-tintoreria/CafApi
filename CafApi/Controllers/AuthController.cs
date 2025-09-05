using CafApi.Data;
using CafApi.Models;
using CafApi.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CafApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UsuarioRepository _repo;
    private readonly JwtTokenService _jwt;

    public AuthController(UsuarioRepository repo, JwtTokenService jwt)
    {
        _repo = repo;
        _jwt = jwt;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        // dto.Nombre y dto.Contrasena (no "Password")
        var user = await _repo.GetByNombreAsync(dto.Nombre);
        if (user is null) return Unauthorized(new { ok = false, message = "Credenciales inválidas" });

        // Verificación: (password plano, hash almacenado)
        var okPwd = PasswordHasher.Verify(dto.Contrasena, user.Contrasena);
        if (!okPwd) return Unauthorized(new { ok = false, message = "Credenciales inválidas" });

        // Crear token con (id, nombre, rol)
        var token = _jwt.CreateToken(user.UsuarioID, user.Nombre, user.Rol);
        return Ok(new { ok = true, token });
    }

    [Authorize]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue(ClaimTypes.Name); // por si configuraste así los claims
        if (string.IsNullOrWhiteSpace(idStr))
            return Unauthorized(new { ok = false, message = "Agregar token de acceso" });

        if (!int.TryParse(idStr, out var id))
            return Unauthorized(new { ok = false, message = "No tenés permiso" });

        var user = await _repo.GetByIdAsync(id);
        if (user is null) return Unauthorized(new { ok = false, message = "No tenés permiso.." });

        var token = _jwt.CreateToken(user.UsuarioID, user.Nombre, user.Rol);
        return Ok(new { ok = true, token });
    }
}
