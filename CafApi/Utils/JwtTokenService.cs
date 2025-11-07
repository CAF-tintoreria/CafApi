// Utils/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CafApi.Utils
{
    public class JwtTokenService
    {
        private readonly IConfiguration _config;
        public JwtTokenService(IConfiguration config) => _config = config;

        // ⬅️  Nuevo parámetro opcional: permisos
        public string CreateToken(int userId, string nombre, string rol, IEnumerable<string>? permisos = null)
        {
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expire = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 300;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claims base
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, nombre),

                // 👇 asegura que [Authorize(Roles="...")] funcione
                new Claim(ClaimTypes.Role, rol),

                // 👇 opcional pero muy útil: que NameIdentifier sea el id
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // ⬅️  Agregar un claim "perm" por cada permiso
            if (permisos is not null)
            {
                foreach (var p in permisos)
                {
                    if (!string.IsNullOrWhiteSpace(p))
                        claims.Add(new Claim("perm", p.Trim()));
                }
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expire),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
