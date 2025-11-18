// Controllers/AuditController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[Authorize(Roles = "Administrador")] // o tu policy
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly Db _db;
    public AuditController(Db db) => _db = db;

    // GET /api/audit?entity=Cliente&entityId=7&page=1&pageSize=20&operation=...&actor=...&from=YYYY-MM-DD&to=YYYY-MM-DD
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? entity,           // <-- ahora opcional
        [FromQuery] int? entityId,
        [FromQuery] string? operation,
        [FromQuery] string? actor,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 1000)
    {
        if (page < 1 || pageSize is < 1 or > 5000)
            return BadRequest("paginación inválida");

        const string sql = @"
WITH q AS (
  SELECT AuditID, Fecha, ActorId, ActorNombre, ActorRol,
         Entidad, EntidadID, Operacion, DatosPrevios, DatosNuevos, IpRemota, Extra,
         ROW_NUMBER() OVER(ORDER BY Fecha DESC, AuditID DESC) rn
  FROM Audit.Log
  WHERE (@entidad IS NULL OR Entidad = @entidad)                 -- <- todas si viene NULL
    AND (@entidadId IS NULL OR EntidadID = @entidadId)
    AND (@operation IS NULL OR Operacion = @operation)
    AND (@actor IS NULL OR ActorNombre LIKE '%' + @actor + '%')
    AND (@from IS NULL OR Fecha >= @from)
    AND (@to   IS NULL OR Fecha <  DATEADD(day, 1, @to))         -- to exclusivo +1 día
)
SELECT * FROM q WHERE rn BETWEEN @fromRow AND @toRow;

SELECT COUNT(1)
FROM Audit.Log
WHERE (@entidad IS NULL OR Entidad = @entidad)
  AND (@entidadId IS NULL OR EntidadID = @entidadId)
  AND (@operation IS NULL OR Operacion = @operation)
  AND (@actor IS NULL OR ActorNombre LIKE '%' + @actor + '%')
  AND (@from IS NULL OR Fecha >= @from)
  AND (@to   IS NULL OR Fecha <  DATEADD(day, 1, @to));";

        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);

        cmd.Parameters.AddWithValue("@entidad", (object?)entity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@entidadId", (object?)entityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@operation", (object?)operation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actor", (object?)actor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@from", (object?)from ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@to", (object?)to ?? DBNull.Value);

        cmd.Parameters.AddWithValue("@fromRow", (page - 1) * pageSize + 1);
        cmd.Parameters.AddWithValue("@toRow", page * pageSize);

        await cn.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();

        var rows = new List<object>();
        while (await rd.ReadAsync())
        {
            rows.Add(new
            {
                auditID = rd.GetInt64(rd.GetOrdinal("AuditID")),
                fecha = rd.GetDateTime(rd.GetOrdinal("Fecha")),
                actorId = rd.IsDBNull(rd.GetOrdinal("ActorId")) ? (int?)null : rd.GetInt32(rd.GetOrdinal("ActorId")),
                actorNombre = rd.IsDBNull(rd.GetOrdinal("ActorNombre")) ? null : rd.GetString(rd.GetOrdinal("ActorNombre")),
                actorRol = rd.IsDBNull(rd.GetOrdinal("ActorRol")) ? null : rd.GetString(rd.GetOrdinal("ActorRol")),
                entidad = rd.GetString(rd.GetOrdinal("Entidad")),
                entidadID = rd.IsDBNull(rd.GetOrdinal("EntidadID")) ? (int?)null : rd.GetInt32(rd.GetOrdinal("EntidadID")),
                operacion = rd.GetString(rd.GetOrdinal("Operacion")),
                datosPrevios = rd.IsDBNull(rd.GetOrdinal("DatosPrevios")) ? null : rd.GetString(rd.GetOrdinal("DatosPrevios")),
                datosNuevos = rd.IsDBNull(rd.GetOrdinal("DatosNuevos")) ? null : rd.GetString(rd.GetOrdinal("DatosNuevos")),
                ipRemota = rd.IsDBNull(rd.GetOrdinal("IpRemota")) ? null : rd.GetString(rd.GetOrdinal("IpRemota")),
                extra = rd.IsDBNull(rd.GetOrdinal("Extra")) ? null : rd.GetString(rd.GetOrdinal("Extra")),
            });
        }

        int total = 0;
        if (await rd.NextResultAsync() && await rd.ReadAsync()) total = rd.GetInt32(0);

        return Ok(new { ok = true, data = rows, page, pageSize, total });
    }

}
