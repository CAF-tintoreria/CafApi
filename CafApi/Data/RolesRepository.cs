// Data/RolesRepository.cs
using CafApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

public class RolesRepository
{
    private readonly Db _db;
    public RolesRepository(Db db) => _db = db;

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        const string sql = "SELECT RolID, Nombre, EsSistema, Descripcion FROM dbo.Rol ORDER BY Nombre;";
        var list = new List<RoleDto>();
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new RoleDto
            {
                RolID = rd.GetInt32(0),
                Nombre = rd.GetString(1),
                EsSistema = rd.GetBoolean(2),
                Descripcion = rd.IsDBNull(3) ? null : rd.GetString(3)  
            });
        }
        return list;
    }

    public async Task<int?> GetRolIdByNombreAsync(string nombre)
    {
        const string sql = "SELECT RolID FROM dbo.Rol WHERE Nombre=@n;";
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@n", nombre));
        await cn.OpenAsync();
        var o = await cmd.ExecuteScalarAsync();
        return o == null ? (int?)null : Convert.ToInt32(o);
    }

    public async Task<bool> IsSistemaAsync(int rolId)
    {
        const string sql = "SELECT EsSistema FROM dbo.Rol WHERE RolID=@id;";
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@id", rolId));
        await cn.OpenAsync();
        var o = await cmd.ExecuteScalarAsync();
        return o != null && Convert.ToBoolean(o);
    }

  public async Task<int> CreateRolAsync(string nombre, string? descripcion = null)
    {
        const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.Rol WHERE Nombre=@n) THROW 50000, 'El rol ya existe.', 1;
INSERT INTO dbo.Rol(Nombre, EsSistema, Descripcion) VALUES(@n, 0, @d);
SELECT SCOPE_IDENTITY();";
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@n", nombre));
        cmd.Parameters.Add(new SqlParameter("@d", (object?)descripcion ?? DBNull.Value));
        await cn.OpenAsync();
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }


    public async Task<bool> UpdateRolAsync(int rolId, string nuevoNombre, string? descripcion)
    {
        const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.Rol WHERE RolID=@id AND EsSistema=1) THROW 50000, 'No se puede editar este rol.', 1;
UPDATE dbo.Rol SET Nombre=@n, Descripcion=@d WHERE RolID=@id;";
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@id", rolId));
        cmd.Parameters.Add(new SqlParameter("@n", nuevoNombre));
        cmd.Parameters.Add(new SqlParameter("@d", (object?)descripcion ?? DBNull.Value));
        await cn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteRolAsync(int rolId)
    {
        const string getSql = "SELECT Nombre, EsSistema FROM dbo.Rol WHERE RolID=@id;";
        const string checkUsersSql = "SELECT COUNT(1) FROM dbo.Usuario WHERE Rol = @nombre;";
        const string deleteSql = "DELETE FROM dbo.Rol WHERE RolID=@id;";

        using var cn = _db.GetConnection();
        await cn.OpenAsync();

        // 1) Traer rol
        string? nombre = null;
        bool esSistema = false;

        using (var getCmd = new SqlCommand(getSql, cn))
        {
            getCmd.Parameters.Add(new SqlParameter("@id", rolId));
            using var rd = await getCmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return false; // no existe
            nombre = rd.GetString(0);
            esSistema = rd.GetBoolean(1);
        }

        // 2) Bloquear Administrador SIEMPRE (por bandera o por nombre)
        if (esSistema || string.Equals(nombre, "Administrador", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No se puede eliminar el rol 'Administrador'.");

        // 3) Bloquear si hay usuarios con ese rol
        using (var chkCmd = new SqlCommand(checkUsersSql, cn))
        {
            chkCmd.Parameters.Add(new SqlParameter("@nombre", nombre));
            var count = Convert.ToInt32(await chkCmd.ExecuteScalarAsync());
            if (count > 0)
                throw new InvalidOperationException("No se puede eliminar un rol que está asignado a usuarios.");
        }

        // 4) Eliminar
        using (var delCmd = new SqlCommand(deleteSql, cn))
        {
            delCmd.Parameters.Add(new SqlParameter("@id", rolId));
            return await delCmd.ExecuteNonQueryAsync() > 0;
        }
    }

    public async Task<IEnumerable<string>> GetPermisosByRolAsync(int rolId)
    {
        const string sql = @"
SELECT p.Codigo
FROM dbo.RolPermiso rp
JOIN dbo.Permiso p ON p.PermisoID = rp.PermisoID
WHERE rp.RolID = @id
ORDER BY p.Codigo;";
        var list = new List<string>();
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@id", rolId));
        await cn.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync()) list.Add(rd.GetString(0));
        return list;
    }

    public async Task AssignPermisosAsync(int rolId, IEnumerable<string> codigos)
    {
        // admin = todos -> no necesitamos asignarle nada, pero tampoco lo bloqueamos aquí.
        const string tmp = "#codigos";
        const string sql = @"
CREATE TABLE #codigos(Codigo NVARCHAR(50) NOT NULL);
" + // bulk insert por parámetros
@"-- mapear codigos -> PermisoID
INSERT INTO dbo.RolPermiso(RolID, PermisoID)
SELECT @rolId, p.PermisoID
FROM dbo.Permiso p
JOIN #codigos c ON c.Codigo = p.Codigo
WHERE NOT EXISTS(
  SELECT 1 FROM dbo.RolPermiso rp WHERE rp.RolID=@rolId AND rp.PermisoID=p.PermisoID
);";
        using var cn = _db.GetConnection();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();

        // crear #codigos
        using (var create = new SqlCommand($"CREATE TABLE {tmp}(Codigo NVARCHAR(50) NOT NULL);", cn, tx))
            await create.ExecuteNonQueryAsync();

        // insertar filas temporales
        foreach (var c in codigos.Distinct())
        {
            using var ins = new SqlCommand($"INSERT INTO {tmp}(Codigo) VALUES(@c);", cn, tx);
            ins.Parameters.Add(new SqlParameter("@c", c));
            await ins.ExecuteNonQueryAsync();
        }

        // ejecutar mapeo
        using (var map = new SqlCommand(@"
INSERT INTO dbo.RolPermiso(RolID, PermisoID)
SELECT @rolId, p.PermisoID
FROM dbo.Permiso p
JOIN #codigos c ON c.Codigo = p.Codigo
WHERE NOT EXISTS(
  SELECT 1 FROM dbo.RolPermiso rp WHERE rp.RolID=@rolId AND rp.PermisoID=p.PermisoID
);", cn, tx))
        {
            map.Parameters.Add(new SqlParameter("@rolId", rolId));
            await map.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task RemovePermisoAsync(int rolId, string codigoPermiso)
    {
        const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.Rol WHERE RolID=@id AND EsSistema=1)
    THROW 50000, 'No se pueden modificar permisos del rol del sistema.', 1;

DELETE rp
FROM dbo.RolPermiso rp
JOIN dbo.Permiso p ON p.PermisoID = rp.PermisoID
WHERE rp.RolID = @id AND p.Codigo = @cod;";
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@id", rolId));
        cmd.Parameters.Add(new SqlParameter("@cod", codigoPermiso));
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<string>> GetAllPermisosAsync()
    {
        const string sql = "SELECT Codigo FROM dbo.Permiso ORDER BY Codigo;";
        var list = new List<string>();
        using var cn = _db.GetConnection();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync()) list.Add(rd.GetString(0));
        return list;
    }
}
