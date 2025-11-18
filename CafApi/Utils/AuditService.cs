using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

public class AuditService
{
    private readonly Db _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(Db db, IHttpContextAccessor http)
    {
        _db = db; _http = http;
    }

    public async Task LogAsync(string entidad, int? entidadId, string operacion, object? beforeObj = null, object? afterObj = null, string? extra = null)
    {
        var user = _http.HttpContext?.User;
        var actorId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorNombre = user?.Identity?.Name
                          ?? user?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                          ?? user?.FindFirst("unique_name")?.Value;
        var actorRol = user?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        using var cn = _db.GetConnection();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Audit.Log(ActorId, ActorNombre, ActorRol, Entidad, EntidadID, Operacion, DatosPrevios, DatosNuevos, IpRemota, Extra)
VALUES (@ActorId, @ActorNombre, @ActorRol, @Entidad, @EntidadID, @Operacion, @Prev, @New, @Ip, @Extra)";
        cmd.Parameters.AddWithValue("@ActorId", (object?)(int.TryParse(actorId, out var id) ? id : (int?)null) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ActorNombre", (object?)actorNombre ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ActorRol", (object?)actorRol ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Entidad", entidad);
        cmd.Parameters.AddWithValue("@EntidadID", (object?)entidadId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Operacion", operacion);
        cmd.Parameters.AddWithValue("@Prev", (object?)(beforeObj is null ? null : JsonSerializer.Serialize(beforeObj)) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@New", (object?)(afterObj is null ? null : JsonSerializer.Serialize(afterObj)) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Ip", (object?)_http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Extra", (object?)extra ?? DBNull.Value);

        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
}
