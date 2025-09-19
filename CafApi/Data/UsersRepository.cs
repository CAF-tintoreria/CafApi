using System.Data;
using Microsoft.Data.SqlClient;
using CafApi.Models;
using CafApi.Utils;

namespace CafApi.Data
{
    public class UsersRepository
    {
        private readonly Db _db;
        public UsersRepository(Db db) => _db = db;

        public async Task<bool> ExistsNombreAsync(string username)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM dbo.Usuario WHERE Nombre = @nombre;";
            cmd.Parameters.Add(new SqlParameter("@nombre", username));
            await cn.OpenAsync();
            var r = await cmd.ExecuteScalarAsync();
            return r is not null;
        }

        public async Task<int> CreateAsync(UserModel u)
        {
            // Hash de la contraseña ANTES de insertar
            var hashed = PasswordHasher.Hash(u.Password);

            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO dbo.Usuario (Nombre, Rol, [Contraseña])
VALUES (@nombre, @rol, @pwd);
SELECT SCOPE_IDENTITY();";
            cmd.Parameters.Add(new SqlParameter("@nombre", u.Username));
            cmd.Parameters.Add(new SqlParameter("@rol", u.Role));
            cmd.Parameters.Add(new SqlParameter("@pwd", hashed));

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<IEnumerable<UserModel>> GetAllAsync(string? username = null)
        {
            var list = new List<UserModel>();
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
SELECT UsuarioID, Nombre, Rol, [Contraseña]
FROM dbo.Usuario
WHERE (@nombre IS NULL OR Nombre LIKE '%' + @nombre + '%')
ORDER BY UsuarioID DESC;";
            cmd.Parameters.Add(new SqlParameter("@nombre", (object?)username ?? DBNull.Value));
            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
            {
                list.Add(new UserModel
                {
                    UserId = rd.GetInt32(0),
                    Username = rd.GetString(1),
                    Role = rd.GetString(2),
                    Password = rd.GetString(3)
                });
            }
            return list;
        }

        public async Task<UserModel?> GetByIdAsync(int id)
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
                return new UserModel
                {
                    UserId = rd.GetInt32(0),
                    Username = rd.GetString(1),
                    Role = rd.GetString(2),
                    Password = rd.GetString(3)
                };
            }
            return null;
        }

        public async Task<bool> UpdateAsync(int id, UserModel u)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();

            var setPwd = u.Password is not null && u.Password != "";
            var pwdClause = setPwd ? ", [Contraseña] = @pwd" : "";

            cmd.CommandText = $@"
UPDATE dbo.Usuario
SET Nombre = @nombre,
    Rol = @rol
    {pwdClause}
WHERE UsuarioID = @id;";

            cmd.Parameters.Add(new SqlParameter("@id", id));
            cmd.Parameters.Add(new SqlParameter("@nombre", u.Username));
            cmd.Parameters.Add(new SqlParameter("@rol", u.Role));
            if (setPwd)
                cmd.Parameters.Add(new SqlParameter("@pwd", PasswordHasher.Hash(u.Password)));

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

        public async Task<UserModel?> GetByNombreAsync(string username)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
SELECT UsuarioID, Nombre, Rol, [Contraseña]
FROM dbo.Usuario
WHERE Nombre = @nombre;";
            cmd.Parameters.Add(new SqlParameter("@nombre", username));
            await cn.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            if (await rd.ReadAsync())
            {
                return new UserModel
                {
                    UserId = rd.GetInt32(0),
                    Username = rd.GetString(1),
                    Role = rd.GetString(2),
                    Password = rd.GetString(3) // hash almacenado
                };
            }
            return null;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            using var cn = _db.GetConnection();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
UPDATE dbo.Usuario
SET [Contraseña] = @pwd
WHERE UsuarioID = @id;";
            cmd.Parameters.Add(new SqlParameter("@pwd", newPasswordHash));
            cmd.Parameters.Add(new SqlParameter("@id", userId));
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
