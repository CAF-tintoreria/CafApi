using CafApi.Data;
using CafApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrendaController : ControllerBase
{
    private readonly PrendaRepository _repo;

    public enum UpdateEstadoResult
    {
        Updated = 1,
        NoChange = 0,
        NotFound = -1,
        Error = -2
    }
    public PrendaController(PrendaRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _repo.GetAllAsync();
            return Ok(new { ok = true, data });
        }
        catch (Exception)
        {
            return NotFound((new { ok = false, message = "No se pudo obtener el listado." }));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var data = await _repo.GetByIdAsync(id);
            return Ok(new { ok = true, data });
        }
        catch (Exception)
        {
            return NotFound((new { ok = false, message = "No se pudo obtener el listado." }));
        }
    }

    [HttpGet("{id:int}/estado")]
    public async Task<IActionResult> GetEstado(int id)
    {
        var dto = await _repo.GetEstadoByIdAsync(id);
        if (dto is null) return NotFound(new { ok = false, message = "Prenda no encontrada." });
        return Ok(new { ok = true, data = dto });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrendaCreateDto dto)
    {
        if (dto.FechaIngreso == default)
            ModelState.AddModelError(nameof(dto.FechaIngreso), "FechaIngreso debe ser una fecha válida.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var id = await _repo.CreateAsync(dto);
            return Ok(new { ok = true, id, message = "Prenda creada correctamente." });
        }
        catch (InvalidOperationException vex)
        {

            return BadRequest(new { ok = false, message = vex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo crear la prenda.", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PrendaUpdateDto dto)
    {
        if (id <= 0) return BadRequest("Id inválido.");
        if (dto.FechaIngreso == default)
            ModelState.AddModelError(nameof(dto.FechaIngreso), "FechaIngreso debe ser una fecha válida.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var affected = await _repo.UpdateAsync(id, dto);
            if (affected == 0)
                return NotFound(new { ok = false, message = "Prenda no encontrada." });
            return Ok(new { ok = true, message = "Prenda actualizada correctamente." });
        }
        catch (InvalidOperationException vex)
        {
            return BadRequest(new { ok = false, message = vex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo actualizar la prenda.", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] PrendaEstadoUpdateDto dto)
    {
        if (id <= 0) return BadRequest("Id inválido.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var code = await _repo.UpdateEstadoAsync(id, dto.EstadoPrendaID);

            // 1 = actualizado, 0 = ya tenía ese estado, -1 = prenda no existe
            if (code == 1)
                return Ok(new { ok = true, message = "Estado actualizado correctamente." });

            if (code == 0)
                return BadRequest(new { ok = false, message = "La prenda ya tiene ese estado." });

            if (code == -1)
                return NotFound(new { ok = false, message = "Prenda no encontrada." });

            return Problem(title: "No se pudo actualizar el estado.", statusCode: 500);
        }
        catch (Exception ex)
        {
            return Problem(title: "Error al actualizar el estado.", detail: ex.Message, statusCode: 500);
        }
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {

        try
        {
            var ok = await _repo.DeleteAsync(id);
            if (!ok) return NotFound(new { ok = false, message = "Prenda no encontrada." });
            return Ok(new { ok = true, message = "Prenda eliminada correctamente." });
        }
        catch (Exception ex)
        {
            return Problem(title: "No se pudo eliminar la prenda.", detail: ex.Message, statusCode: 500);
        }
    }


    [HttpGet("complete")]
    public async Task<IActionResult> GetAllFull()
    {
        try
        {
            var data = await _repo.GetAllWithClientAsync();
            return Ok(new { ok = true, data });
        }
        catch
        {
            return NotFound(new { ok = false, message = "No se pudo obtener el listado." });
        }
    }

    [HttpGet("{id:int}/complete")]
    public async Task<IActionResult> GetByIdFull(int id)
    {
        try
        {
            var data = await _repo.GetWithClientByIdAsync(id);
            return data is null
                ? NotFound(new { ok = false, message = "Prenda no encontrada." })
                : Ok(new { ok = true, data });
        }
        catch
        {
            return NotFound(new { ok = false, message = "No se pudo obtener la prenda." });
        }
    }
}
