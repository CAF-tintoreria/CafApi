using CafApi.Data;
using CafApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/estado-prenda")]
public class EstadoPrendaController : ControllerBase
{
    private readonly PrendaRepository _repo;
    public EstadoPrendaController(PrendaRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(new { ok = true, data = await _repo.GetEstadosPrendaAsync() });

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EstadoPrendaCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.EstadoActual))
            return BadRequest("EstadoActual es requerido.");
        var id = await _repo.CreateEstadoPrendaAsync(dto);
        return Ok(new { ok = true, id, message = "Estado creado." });
    }
}