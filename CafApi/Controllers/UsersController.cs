using CafApi.Data;
using CafApi.Models;
using CafApi.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CafApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UsersRepository _repo;
    private readonly JwtTokenService _tokenSvc;

    public UsersController(UsersRepository repo, JwtTokenService tokenSvc)
    {
        _repo = repo;
        _tokenSvc = tokenSvc;
    }

    static readonly HashSet<string> RolesValidos = new(StringComparer.OrdinalIgnoreCase)
        { "Administrador", "Ingreso" };

    private (bool ok, string? error) ValidarNuevo(UserModel u)
    {
        // nombre: >3 y <24
        if (string.IsNullOrWhiteSpace(u.Username) || u.Username.Length <= 3 || u.Username.Length >= 24)
            return (false, "El nombre debe tener más de 3 y menos de 24 caracteres.");
        // rol
        if (string.IsNullOrWhiteSpace(u.Role) || !RolesValidos.Contains(u.Role))
            return (false, "El rol debe ser 'Administrador' o 'Ingreso'.");
        // contraseña: 6–16
        if (string.IsNullOrEmpty(u.Password) || u.Password.Length < 6 || u.Password.Length > 16)
            return (false, "La contraseña debe tener entre 6 y 16 caracteres.");
        return (true, null);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] UserModel u)
    {
        try
        {
            // Validaciones de negocio
            var v = ValidarNuevo(u);
            if (!v.ok) return BadRequest(new { ok = false, message = v.error });

            // Unicidad de nombre
            if (await _repo.ExistsNombreAsync(u.Username))
                return Conflict(new { ok = false, message = "El nombre ya existe." });

            await _repo.CreateAsync(u);

            // Respuesta EXACTA solicitada
            return Ok(new { ok = true, message = "Usuario creado." });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo crear el usuario.", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] string? username)
    {
        try
        {
            var data = await _repo.GetAllAsync(username);
            return Ok(new { ok = true, message = "Listado obtenido.", data });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo obtener el listado.", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        try
        {
            var u = await _repo.GetByIdAsync(id);
            if (u is null) return NotFound(new { ok = false, message = "Usuario no encontrado." });
            return Ok(new { ok = true, message = "Usuario obtenido.", data = u });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo obtener el usuario.", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UserModel u)
    {
        try
        {
            // Validaciones mínimas para update (nombre y rol)
            if (string.IsNullOrWhiteSpace(u.Username) || u.Username.Length <= 3 || u.Username.Length >= 24)
                return BadRequest(new { ok = false, message = "El nombre debe tener más de 3 y menos de 24 caracteres." });
            if (!RolesValidos.Contains(u.Role ?? ""))
                return BadRequest(new { ok = false, message = "El rol debe ser 'Administrador' o 'Ingreso'." });

            // Si viene contraseña, validar longitud
            if (!string.IsNullOrEmpty(u.Password) && (u.Password.Length < 6 || u.Password.Length > 16))
                return BadRequest(new { ok = false, message = "La contraseña debe tener entre 6 y 16 caracteres." });

            var ok = await _repo.UpdateAsync(id, u);
            if (!ok) return NotFound(new { ok = false, message = "Usuario no encontrado." });

            return Ok(new { ok = true, message = "Usuario actualizado." });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo actualizar el usuario.", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var ok = await _repo.DeleteAsync(id);
            if (!ok) return NotFound(new { ok = false, message = "Usuario no encontrado." });
            return Ok(new { ok = true, message = "Usuario eliminado." });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo eliminar el usuario.", detail: ex.Message, statusCode: 500);
        }
    }
}
