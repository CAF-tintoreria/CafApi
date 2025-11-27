// Controllers/ProcessController.cs
using CafApi.Constants.Enum;
using CafApi.Data;
using CafApi.Models;
using CafApi.Models.DTO;
using CafApi.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CafApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProcessController : ControllerBase
{
    private readonly ProcessRepository _repo;
    private readonly JwtTokenService _tokenSvc;

    public ProcessController(ProcessRepository repo, JwtTokenService tokenSvc)
    {
        _repo = repo;
        _tokenSvc = tokenSvc;
    }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] string? name)
    {
        try
        {
            var data = await _repo.GetAllAsync();
            var processList = data.Select(item => new ProcessDTO
            {
                TypeProcess = item.TypeProcess,
                Observations = item.Observations,
                ProcessID = item.ProcessID,
            }).ToList();

            if (!string.IsNullOrWhiteSpace(name))
                processList = processList
                    .Where(p => p.TypeProcess.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return Ok(new { ok = true, data = processList });
        }
        catch
        {
            return Problem500Dev("No se pudo obtener el listado de procesos.");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        try
        {
            var proc = await _repo.GetByIdAsync(id);
            if (proc is null) return NotFound(new { ok = false, message = "Proceso no encontrado." });

            var parsedProcess = new ProcessDTO
            {
                TypeProcess = proc.TypeProcess,
                Observations = proc.Observations,
                ProcessID = proc.ProcessID,
            };

            return Ok(new { ok = true, data = parsedProcess });
        }
        catch
        {
            return Problem500Dev("No se pudo obtener el proceso.");
        }
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ProcessCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TypeProcess))
            return BadRequest(new { ok = false, message = "TypeProcess es requerido." });

        if (dto.TypeProcess.Length > 50)
            return BadRequest(new { ok = false, message = "TypeProcess no puede superar 50 caracteres." });

        if (dto.Observations?.Length > 250)
            return BadRequest(new { ok = false, message = "Observations no puede superar 250 caracteres." });

        try
        {
            if (await _repo.ExistsByNameAsync(dto.TypeProcess))
                return Conflict(new { ok = false, message = "Ya existe un proceso con ese nombre." });

            var entity = new Process
            {
                TypeProcess = dto.TypeProcess,
                Observations = dto.Observations
            };

            var newId = await _repo.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = newId },
                new { ok = true, message = "Proceso creado correctamente." });
        }
        catch (SqlException ex)
        {
#if DEBUG
            return StatusCode(500, new { ok = false, message = "DB error al crear el proceso.", detail = ex.Message });
#else
    return Problem500Dev("Error de base de datos al crear el proceso.");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            return StatusCode(500, new { ok = false, message = "No se pudo crear el proceso.", detail = ex.InnerException?.Message ?? ex.Message });
#else
        return Problem500Dev("No se pudo crear el proceso.");
#endif
        }
    }




    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ProcessUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TypeProcess))
            return BadRequest(new { ok = false, message = "TypeProcess es requerido." });

        if (dto.TypeProcess.Length > 50)
            return BadRequest(new { ok = false, message = "TypeProcess no puede superar 50 caracteres." });

        if (dto.Observations?.Length > 250)
            return BadRequest(new { ok = false, message = "Observations no puede superar 250 caracteres." });

        try
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { ok = false, message = "Proceso no encontrado." });

            if (await _repo.ExistsByNameAsync(dto.TypeProcess, excludeId: id))
                return Conflict(new { ok = false, message = "Ya existe otro proceso con ese nombre." });

            var entity = new Process
            {
                ProcessID = id,
                TypeProcess = dto.TypeProcess,
                Observations = dto.Observations
            };

            var ok = await _repo.UpdateAsync(entity);
            return ok
                ? Ok(new { ok = true, message = "Proceso editado correctamente." })
                : StatusCode(500, new { ok = false, message = "No se pudo actualizar." });
        }
        catch (SqlException)
        {
            return Problem500Dev("Error de base de datos al actualizar el proceso.");
        }
        catch (Exception)
        {
            return Problem500Dev("No se pudo actualizar el proceso.");
        }
    }
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { ok = false, message = "Proceso no encontrado." });

            var refs = await _repo.GetReferenceCountsAsync(id);

            if (refs.muestras > 0 || refs.prendasActuales > 0)
            {
                return Conflict(new
                {
                    ok = false,
                    code = "PROCESS_HAS_RELATIONS",
                    message =
                        $"No se puede eliminar: el proceso está asociado a {refs.muestras} muestras y {refs.prendasActuales} prendas."
                });
            }

            var ok = await _repo.DeleteAsync(id);
            return ok
                ? Ok(new { ok = true, message = "Proceso eliminado correctamente." })
                : StatusCode(500, new { ok = false, message = "No se pudo eliminar." });
        }
        // safety net por si aparece otra FK que no contemplamos
        catch (SqlException ex) when (ex.Number == 547) // FK violation
        {
            return Conflict(new
            {
                ok = false,
                code = "FK_CONSTRAINT",
                message = "No se puede eliminar porque el proceso está asociado a otros registros (muestras y/o prendas)."
#if DEBUG
                ,
                detail = ex.Message
#endif
            });
        }
        catch
        {
            return Problem500Dev("No se pudo eliminar el proceso.");
        }
    }


    [HttpDelete("{id:int}/procesos/{procesoId:int}")]
    public async Task<IActionResult> RemoveProceso(int id, int procesoId)
    {
        try
        {
            var ok = await _repo.RemoveProcesoFromPrendaAsync(id, procesoId);
            return ok ? Ok(new { ok = true, message = "Relación eliminada." })
                      : NotFound(new { ok = false, message = "Relación no encontrada o prenda inexistente." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { ok = false, message = ex.Message });
        }

    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions()
    {
        var data = (await _repo.GetAllAsync())
            .Select(p => new
            {
                procesoID = p.ProcessID,
                nombre = p.TypeProcess  // string
            })
            .OrderBy(p => p.nombre)
            .ToList();

        return Ok(new { ok = true, data });
    }

    private ObjectResult Problem500Dev(string userMessage)
    {
#if DEBUG
        return StatusCode(500, new { ok = false, message = userMessage /*, detail = ex.Message */});
#else
        return StatusCode(500, new { ok = false, message = userMessage });
#endif
    }


}
