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

        public string CreateToken(int userId, string nombre, string rol)
        {
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            // OJO: en tu appsettings la clave se llama ExpireMinutes (no ExpMinutes)
            var expire = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 60;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, nombre),
                new Claim(ClaimTypes.Role, rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
