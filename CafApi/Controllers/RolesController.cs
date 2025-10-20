// Controllers/RolesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly RolesRepository _repo;
    public RolesController(RolesRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(new { ok = true, data = await _repo.GetAllAsync() });

    [HttpGet("permisos")]
    public async Task<IActionResult> GetPermisosCatalog()
        => Ok(new { ok = true, data = await _repo.GetAllPermisosAsync() });

    [HttpGet("{rolId:int}/permisos")]
    public async Task<IActionResult> GetPermisosDeRol(int rolId)
        => Ok(new { ok = true, data = await _repo.GetPermisosByRolAsync(rolId) });

    public record RolCreateDto(string Nombre, string? Descripcion);

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RolCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { ok = false, message = "Nombre es requerido." });

        var desc = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        var id = await _repo.CreateRolAsync(dto.Nombre.Trim(), desc);
        return Ok(new { ok = true, id, message = "Rol creado." });
    }

    public record RolUpdateDto(string Nombre, string? Descripcion);

    [Authorize(Roles = "Administrador")]
    [HttpPut("{rolId:int}")]
    public async Task<IActionResult> Update(int rolId, [FromBody] RolUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { ok = false, message = "Nombre es requerido." });

        var desc = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        var ok = await _repo.UpdateRolAsync(rolId, dto.Nombre.Trim(), desc);
        return ok ? Ok(new { ok = true, message = "Rol actualizado." })
                  : NotFound(new { ok = false, message = "Rol no encontrado." });
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{rolId:int}")]
    public async Task<IActionResult> Delete(int rolId)
    {
        try
        {
            var ok = await _repo.DeleteRolAsync(rolId);
            return ok
                ? Ok(new { ok = true, message = "Rol eliminado." })
                : NotFound(new { ok = false, message = "Rol no encontrado." });
        }
        catch (InvalidOperationException ex)
        {
            // ej: "No se puede eliminar el rol 'Administrador'." o "rol asignado a usuarios"
            return BadRequest(new { ok = false, message = ex.Message });
        }
    }

    public record AsignarPermisosDto(IEnumerable<string> Codigos);

    [Authorize(Roles = "Administrador")]
    [HttpPost("{rolId:int}/permisos")]
    public async Task<IActionResult> Assign(int rolId, [FromBody] AsignarPermisosDto dto)
    {
        await _repo.AssignPermisosAsync(rolId, dto.Codigos ?? Enumerable.Empty<string>());
        return Ok(new { ok = true, message = "Permisos asignados." });
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{rolId:int}/permisos/{codigo}")]
    public async Task<IActionResult> Revoke(int rolId, string codigo)
    {
        await _repo.RemovePermisoAsync(rolId, codigo);
        return Ok(new { ok = true, message = "Permiso quitado." });
    }
}
