using CafApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafApi.Controllers;

[ApiController]
[Route("api/[controller]")]                 // => /api/processes
public class ProcesoController : ControllerBase
{
    // GET /api/processes?name=&onlyActive=
    [HttpGet]
    public ActionResult<IEnumerable<Process>> Get([FromQuery] string? name, [FromQuery] bool onlyActive = false)
    {
        IEnumerable<Process> q = ProcesoRepo.GetAll();

        if (onlyActive) q = q.Where(p => p.Activo);
        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(p => p.TypeProcess.Contains(name, StringComparison.OrdinalIgnoreCase));

        return Ok(q);
    }

    // GET /api/processes/5
    [HttpGet("{id:int}")]
    public ActionResult<Process> GetById(int id)
    {
        var p = ProcesoRepo.Get(id);
        return p is null ? NotFound() : Ok(p);
    }

    // POST /api/processes
    [HttpPost]
    public ActionResult<Process> Create([FromBody] Process model)
    {
        if (string.IsNullOrWhiteSpace(model.TipoProceso))
            ModelState.AddModelError(nameof(model.TipoProceso), "El tipo de proceso es obligatorio.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var dup = ProcesoRepo.GetAll().Any(x =>
            x.TipoProceso.Equals(model.TipoProceso, StringComparison.OrdinalIgnoreCase));
        if (dup) return Conflict(new { message = "Ya existe un proceso con ese nombre." });

        ProcesoRepo.Add(model);
        return CreatedAtAction(nameof(GetById), new { id = model.IdProceso }, model);
    }

    // PUT /api/processes/5
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] Process model)
    {
        if (id != model.IdProceso)
            return BadRequest(new { message = "El id de la ruta no coincide con el del cuerpo." });

        if (string.IsNullOrWhiteSpace(model.TipoProceso))
            ModelState.AddModelError(nameof(model.TipoProceso), "El tipo de proceso es obligatorio.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var ok = ProcesoRepo.Update(model);
        return ok ? NoContent() : NotFound();
    }

    // DELETE /api/processes/5  (baja lógica)
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var p = ProcesoRepo.Get(id);
        if (p is null) return NotFound();

        ProcesoRepo.SoftDelete(id); //
        return NoContent();
    }
}


