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
    private readonly UsersRepository _repo;
    private readonly JwtTokenService _jwt;
    private readonly RolesRepository _roles;

    public AuthController(UsersRepository repo, JwtTokenService jwt, RolesRepository roles)
    {
        _repo = repo;
        _jwt = jwt;
        _roles = roles;
    }

    // Controllers/AuthController.cs
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? dto, [FromServices] ILogger<AuthController> logger)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { ok = false, message = "Body inválido. Enviá { username, password } como JSON." });

        try
        {
            var user = await _repo.GetByNombreAsync(dto.Username); // usa dbo.Usuario  :contentReference[oaicite:0]{index=0}
            if (user is null) return Unauthorized(new { ok = false, message = "Credenciales inválidas" });

            var okPwd = PasswordHasher.Verify(dto.Password, user.Password);
            if (!okPwd) return Unauthorized(new { ok = false, message = "Credenciales inválidas" });

            // ⬇️ Acá suelen aparecer las diferencias entre local/prod
            var permisos = await GetPermisosDelRolAsync(user.Role); // usa Rol/RolPermiso/Permiso  :contentReference[oaicite:1]{index=1}

            var token = _jwt.CreateToken(user.UserId, user.Username, user.Role, permisos); // lee Jwt:* de config  :contentReference[oaicite:2]{index=2}

            return Ok(new { ok = true, token, username = user.Username, rol = user.Role, permisos });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en /api/auth/login para {User}", dto.Username);
            // (opcional) mensaje más explícito en dev
            return StatusCode(500, new { ok = false, message = "Error interno en login." });
        }
    }


    [Authorize]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(idStr))
            return Unauthorized(new { ok = false, message = "Agregar token de acceso" });

        if (!int.TryParse(idStr, out var id))
            return Unauthorized(new { ok = false, message = "No tenés permiso" });

        var user = await _repo.GetByIdAsync(id);
        if (user is null)
            return Unauthorized(new { ok = false, message = "No tenés permiso.." });

        var permisos = await GetPermisosDelRolAsync(user.Role);

        var token = _jwt.CreateToken(user.UserId, user.Username, user.Role, permisos);

        // ⬅️ devolvemos también los datos del usuario
        return Ok(new
        {
            ok = true,
            token,
            username = user.Username,
            rol = user.Role,
            permisos
        });
    }


    // --- Helpers ---
    private async Task<IEnumerable<string>> GetPermisosDelRolAsync(string? nombreRol)
    {
        if (string.IsNullOrWhiteSpace(nombreRol))
            return Enumerable.Empty<string>();

        var rolId = await _roles.GetRolIdByNombreAsync(nombreRol);
        if (rolId is int id)
            return await _roles.GetPermisosByRolAsync(id);

        // Si el rol no existe en DB, no tiramos 500: devolvemos sin permisos.
        return Enumerable.Empty<string>();
    }
}
