// Data/UsuarioRepository.cs
using System.Data;
using Microsoft.Data.SqlClient;
using CafApi.Models;
using CafApi.Utils;

namespace CafApi.Data
{
    public class UsuarioRepository
    {
        private readonly Db _db;
        public UsuarioRepository(Db db) => _db = db;



        public async Task<bool> ExistsNombreAsync(string nombre)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM dbo.Usuario WHERE Nombre = @nombre;";
            cmd.Parameters.Add(new SqlParameter("@nombre", nombre));
            await cn.OpenAsync();
            var r = await cmd.ExecuteScalarAsync();
            return r is not null;
        }

        public async Task<int> CreateAsync(Usuario u)
        {
            // Hash de la contraseña ANTES de insertar
            var hashed = PasswordHasher.Hash(u.Contrasena);

            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO dbo.Usuario (Nombre, Rol, [Contraseña])
VALUES (@nombre, @rol, @pwd);
SELECT SCOPE_IDENTITY();";
            cmd.Parameters.Add(new SqlParameter("@nombre", u.Nombre));
            cmd.Parameters.Add(new SqlParameter("@rol", u.Rol));
            cmd.Parameters.Add(new SqlParameter("@pwd", hashed));

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync(string? nombre = null)
        {
            var list = new List<Usuario>();
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
SELECT UsuarioID, Nombre, Rol, [Contraseña]
FROM dbo.Usuario
WHERE (@nombre IS NULL OR Nombre LIKE '%' + @nombre + '%')
ORDER BY UsuarioID DESC;";
            cmd.Parameters.Add(new SqlParameter("@nombre", (object?)nombre ?? DBNull.Value));
            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
            {
                list.Add(new Usuario
                {
                    UsuarioID = rd.GetInt32(0),
                    Nombre = rd.GetString(1),
                    Rol = rd.GetString(2),
                    Contrasena = rd.GetString(3) // valor hasheado
                });
            }
            return list;
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
SELECT UsuarioID, Nombre, Rol, [Contraseña]
FROM dbo.Usuario
WHERE UsuarioID = @id;";
            cmd.Parameters.Add(new SqlParameter("@id", id));
            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            if (await rd.ReadAsync())
            {
                return new Usuario
                {
                    UsuarioID = rd.GetInt32(0),
                    Nombre = rd.GetString(1),
                    Rol = rd.GetString(2),
                    Contrasena = rd.GetString(3)
                };
            }
            return null;
        }

        public async Task<bool> UpdateAsync(int id, Usuario u)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();

            var setPwd = u.Contrasena is not null && u.Contrasena != "";
            var pwdClause = setPwd ? ", [Contraseña] = @pwd" : "";

            cmd.CommandText = $@"
UPDATE dbo.Usuario
SET Nombre = @nombre,
    Rol = @rol
    {pwdClause}
WHERE UsuarioID = @id;";

            cmd.Parameters.Add(new SqlParameter("@id", id));
            cmd.Parameters.Add(new SqlParameter("@nombre", u.Nombre));
            cmd.Parameters.Add(new SqlParameter("@rol", u.Rol));
            if (setPwd)
                cmd.Parameters.Add(new SqlParameter("@pwd", PasswordHasher.Hash(u.Contrasena)));

            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM dbo.Usuario WHERE UsuarioID = @id;";
            cmd.Parameters.Add(new SqlParameter("@id", id));
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Usuario?> GetByNombreAsync(string nombre)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
SELECT UsuarioID, Nombre, Rol, [Contraseña]
FROM dbo.Usuario
WHERE Nombre = @nombre;";
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@nombre", nombre));
            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            if (await rd.ReadAsync())
            {
                return new Usuario
                {
                    UsuarioID = rd.GetInt32(0),
                    Nombre = rd.GetString(1),
                    Rol = rd.GetString(2),
                    Contrasena = rd.GetString(3) // hash almacenado
                };
            }
            return null;
        }
    }
}
