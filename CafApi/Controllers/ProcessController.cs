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

    //GET
    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] string? name)
    {
        try
        {
            var data = await _repo.GetAllAsync();
            List<ProcessDTO> processList = data.Select(item => new ProcessDTO
            { 
                TypeProcess = item.TypeProcess.ToString(),
                Observations = item.Observations,
                ProcessID = item.ProcessID,
            }).ToList();

            if (!string.IsNullOrWhiteSpace(name) && Enum.TryParse(typeof(ETypeProcess), name, out _))
                processList.Where(p => p.TypeProcess.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(new { ok = true, processList });
        }
        catch (Exception)
        {
            return Problem500Dev("No se pudo obtener el listado de procesos.");
        }
    }

    //GET
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        try
        {
            var proc = await _repo.GetByIdAsync(id);
            if (proc is null) return NotFound(new { ok = false, message = "Proceso no encontrado." });

            ProcessDTO parsedProcess = new()
            {
                TypeProcess = proc.TypeProcess.ToString(),
                Observations = proc.Observations,
                ProcessID = proc.ProcessID,
            };

            return Ok(new { ok = true, data = parsedProcess });
        }
        catch (Exception)
        {
            return Problem500Dev("No se pudo obtener el proceso.");
        }
    }

    

    //POST
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ProcessCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TypeProcess) || !Enum.TryParse<ETypeProcess>(dto.TypeProcess, out var eTypeProcess))
            return BadRequest(new { ok = false, message = "TypeProcess es requerido." });
        if (dto.TypeProcess.Length > 80)
            return BadRequest(new { ok = false, message = "TypeProcess no puede superar 80 caracteres." });

        try
        {
            if (await _repo.ExistsByNameAsync(dto.TypeProcess))
                return Conflict(new { ok = false, message = "Ya existe un proceso con ese nombre." });

            var entity = new Process
            {
                TypeProcess = eTypeProcess,
                Observations = dto.Observations
            };

            var newId = await _repo.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = newId },
                new { ok = true, message = "Proceso creado correctamente." });
        }
        catch (SqlException)
        {
            return Problem500Dev("Error de base de datos al crear el proceso.");
        }
        catch (Exception)
        {
            return Problem500Dev("No se pudo crear el proceso.");
        }
    }

    //PUT
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ProcessUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TypeProcess) || !Enum.TryParse<ETypeProcess>(dto.TypeProcess, out var eTypeProcess))
            return BadRequest(new { ok = false, message = "TypeProcess es requerido." });

        if (dto.TypeProcess.Length > 80)
            return BadRequest(new { ok = false, message = "TypeProcess no puede superar 80 caracteres." });

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
                TypeProcess = eTypeProcess,
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

    //DELETE
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            if (await _repo.GetByIdAsync(id) is null)
                return NotFound(new { ok = false, message = "Proceso no encontrado." });

            var ok = await _repo.DeleteAsync(id);
            return ok
                ? Ok(new { ok = true, message = "Proceso eliminado correctamente." })
                : StatusCode(500, new { ok = false, message = "No se pudo eliminar." });
        }
        catch (SqlException)
        {
            return Problem500Dev("Error de base de datos al eliminar el proceso.");
        }
        catch (Exception)
        {
            return Problem500Dev("No se pudo eliminar el proceso.");
        }
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
