// Controllers/PreciosController.cs
using CafApi.Data;
using CafApi.Models;
using CafApi.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CafApi.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class PreciosController : ControllerBase
{
    private readonly PrecioClienteRepository _repo;
    private readonly ClientsRepository _clients;
    private readonly AuditService _audit;

    public PreciosController(PrecioClienteRepository repo, ClientsRepository clients, AuditService audit)
    {
        _repo = repo;
        _clients = clients;
        _audit = audit;
    }

    private static readonly string[] UnidadesValidas = { "Kg", "Unidad" };

    // Devuelve un mensaje de error si los datos no son validos, o null si estan bien
    private static string? Validar(string tipoPrenda, string unidadCobro, decimal precio, List<int> procesoIds)
    {
        if (string.IsNullOrWhiteSpace(tipoPrenda))
            return "El tipo de prenda es requerido.";
        if (tipoPrenda.Trim().Length > 50)
            return "El tipo de prenda no puede superar los 50 caracteres.";
        if (!UnidadesValidas.Contains(unidadCobro))
            return "La unidad debe ser 'Kg' o 'Unidad'.";
        if (precio < 0)
            return "El precio no puede ser negativo.";
        if (procesoIds is null || procesoIds.Count == 0)
            return "Elegí al menos un proceso.";
        return null;
    }

    // ---------- Precios de un cliente ----------
    [HttpGet("clients/{clienteId:int}/precios")]
    public async Task<ActionResult> GetByCliente(int clienteId)
    {
        try
        {
            var data = await _repo.GetByClienteAsync(clienteId);
            return Ok(new { ok = true, data });
        }
        catch (Exception)
        {
            return StatusCode(500, new { ok = false, message = "No se pudieron obtener los precios." });
        }
    }

    // ---------- Cantidad de precios por cliente (para la columna de la lista) ----------
    [HttpGet("precios/conteo")]
    public async Task<ActionResult> Conteo()
    {
        try
        {
            var data = await _repo.CountByClienteAsync();
            return Ok(new { ok = true, data });
        }
        catch (Exception)
        {
            return StatusCode(500, new { ok = false, message = "No se pudo obtener el conteo de precios." });
        }
    }

    // ---------- Sugerencias de tipos de prenda ya escritos ----------
    [HttpGet("precios/tipos-prenda")]
    public async Task<ActionResult> TiposPrenda()
    {
        try
        {
            var data = await _repo.GetTiposPrendaAsync();
            return Ok(new { ok = true, data });
        }
        catch (Exception)
        {
            return StatusCode(500, new { ok = false, message = "No se pudieron obtener los tipos de prenda." });
        }
    }

    // ---------- Crear un precio para un cliente ----------
    [Authorize(Policy = "Clientes.Editar")]
    [HttpPost("clients/{clienteId:int}/precios")]
    public async Task<ActionResult> Create(int clienteId, [FromBody] PrecioClienteCreateDto dto)
    {
        var error = Validar(dto.TipoPrenda, dto.UnidadCobro, dto.Precio, dto.ProcesoIds);
        if (error is not null)
            return BadRequest(new { ok = false, message = error });

        try
        {
            var cliente = await _clients.GetByIdAsync(clienteId);
            if (cliente is null)
                return NotFound(new { ok = false, message = "Cliente no encontrado." });

            dto.TipoPrenda = dto.TipoPrenda.Trim();
            dto.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();

            var newId = await _repo.CreateAsync(clienteId, dto);
            var created = await _repo.GetByIdAsync(newId);

            // AUDIT
            await _audit.LogAsync(
                entidad: "PrecioCliente", entidadId: newId, operacion: "CREATE",
                beforeObj: null,
                afterObj: created,
                extra: $"Precio creado para el cliente #{clienteId}"
            );

            return Ok(new { ok = true, message = "Precio creado correctamente.", data = created });
        }
        catch (SqlException) { return StatusCode(500, new { ok = false, message = "Error de base de datos al crear el precio. Revisá que los procesos elegidos existan." }); }
        catch (Exception) { return StatusCode(500, new { ok = false, message = "No se pudo crear el precio." }); }
    }

    // ---------- Editar un precio ----------
    [Authorize(Policy = "Clientes.Editar")]
    [HttpPut("precios/{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] PrecioClienteUpdateDto dto)
    {
        var error = Validar(dto.TipoPrenda, dto.UnidadCobro, dto.Precio, dto.ProcesoIds);
        if (error is not null)
            return BadRequest(new { ok = false, message = error });

        try
        {
            var before = await _repo.GetByIdAsync(id);
            if (before is null)
                return NotFound(new { ok = false, message = "Precio no encontrado." });

            dto.TipoPrenda = dto.TipoPrenda.Trim();
            dto.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();

            var updated = await _repo.UpdateAsync(id, dto);
            if (!updated)
                return StatusCode(500, new { ok = false, message = "No se pudo actualizar el precio." });

            var after = await _repo.GetByIdAsync(id);

            // AUDIT
            await _audit.LogAsync(
                entidad: "PrecioCliente", entidadId: id, operacion: "UPDATE",
                beforeObj: before,
                afterObj: after,
                extra: $"Precio #{id} actualizado (cliente #{before.ClienteID})"
            );

            return Ok(new { ok = true, message = "Precio editado correctamente.", data = after });
        }
        catch (SqlException) { return StatusCode(500, new { ok = false, message = "Error de base de datos al editar el precio. Revisá que los procesos elegidos existan." }); }
        catch (Exception) { return StatusCode(500, new { ok = false, message = "No se pudo editar el precio." }); }
    }

    // ---------- Eliminar un precio ----------
    [Authorize(Policy = "Clientes.Editar")]
    [HttpDelete("precios/{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var before = await _repo.GetByIdAsync(id);
            if (before is null)
                return NotFound(new { ok = false, message = "Precio no encontrado." });

            var deleted = await _repo.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { ok = false, message = "No se pudo eliminar el precio." });

            // AUDIT
            await _audit.LogAsync(
                entidad: "PrecioCliente", entidadId: id, operacion: "DELETE",
                beforeObj: before,
                afterObj: null,
                extra: $"Precio #{id} eliminado (cliente #{before.ClienteID})"
            );

            return Ok(new { ok = true, message = "Precio eliminado correctamente." });
        }
        catch (SqlException) { return StatusCode(500, new { ok = false, message = "Error de base de datos al eliminar el precio." }); }
        catch (Exception) { return StatusCode(500, new { ok = false, message = "No se pudo eliminar el precio." }); }
    }
}
