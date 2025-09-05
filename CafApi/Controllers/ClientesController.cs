// Controllers/ClientesController.cs
using CafApi.Data;
using CafApi.Models;
using CafApi.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CafApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ClienteRepository _repo;
    private readonly JwtTokenService _tokenSvc;
    public ClientesController(ClienteRepository repo, JwtTokenService tokenSvc)
    {
        _repo = repo;
        _tokenSvc = tokenSvc;
    }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] string? nombre)
    {
        try
        {
            var data = await _repo.GetAllAsync(nombre);
            return Ok(new { ok = true, data });
        }
        catch (Exception )
        {
            return Problem500Dev("No se pudo obtener el listado.");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        try
        {
            var cli = await _repo.GetByIdAsync(id);
            return cli is null
                ? NotFound(new { ok = false, message = "Cliente no encontrado." })
                : Ok(new { ok = true, data = cli });
        }
        catch (Exception)
        {
            return Problem500Dev("No se pudo obtener el cliente.");
        }
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ClienteCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { ok = false, message = "Nombre es requerido." });
        if (dto.Nombre.Length > 24)
            return BadRequest(new { ok = false, message = "El nombre no puede superar los 24 caracteres." });

        try
        {
            if (await _repo.ExistsByNombreAsync(dto.Nombre))
                return Conflict(new { ok = false, message = "Ya existe un cliente con ese Nombre." });

            var newId = await _repo.CreateAsync(dto);
            var created = await _repo.GetByIdAsync(newId);

            return CreatedAtAction(nameof(GetById), new { id = newId },
                new { ok = true, message = "Cliente creado correctamente."});
        }
        catch (SqlException) { return Problem500Dev("Error de base de datos al crear el cliente."); }
        catch (Exception) { return Problem500Dev("No se pudo crear el cliente."); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ClienteUpdateDto dto)
    {
        if (dto.Nombre.Length > 24)
            return BadRequest(new { ok = false, message = "El nombre no puede superar los 24 caracteres." });

        try
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { ok = false, message = "Cliente no encontrado." });

            if (await _repo.ExistsByNombreAsync(dto.Nombre, id))
                return Conflict(new { ok = false, message = "Ya existe otro cliente con ese Nombre." });

            var entity = new Cliente
            {
                ClienteID = id,
                Nombre = dto.Nombre,
                Contacto = dto.Contacto,
                Estado = dto.Estado
            };

            var ok = await _repo.UpdateAsync(entity);
            return ok
                ? Ok(new { ok = true, message = "Cliente editado correctamente." })
                : StatusCode(500, new { ok = false, message = "No se pudo actualizar." });
        }
        catch (SqlException) { return Problem500Dev("Error de base de datos al actualizar el cliente."); }
        catch (Exception) { return Problem500Dev("No se pudo actualizar el cliente."); }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            if (await _repo.GetByIdAsync(id) is null)
                return NotFound(new { ok = false, message = "Cliente no encontrado." });

            var ok = await _repo.DeleteAsync(id);
            return ok
                ? Ok(new { ok = true, message = "Cliente eliminado correctamente." })
                : StatusCode(500, new { ok = false, message = "No se pudo eliminar." });
        }
        catch (SqlException) { return Problem500Dev("Error de base de datos al eliminar el cliente."); }
        catch (Exception) { return Problem500Dev("No se pudo eliminar el cliente."); }
    }

    // Mostrar detalle solo en Debug mientras terminás de ajustar
    private ObjectResult Problem500Dev(string userMessage)
    {
#if DEBUG
        return StatusCode(500, new { ok = false, message = userMessage /*, detail = ex.Message */});
#else
        return StatusCode(500, new { ok = false, message = userMessage });
#endif
    }
}
