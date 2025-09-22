using System.Security.Cryptography;
using System.Text;

namespace CafApi.Utils
{
    public class PasswordHasher
    {
        // Formato almacenado: PBKDF2$<iter>$<saltB64>$<hashB64>
        private const int Iterations = 100_000;
        private const int SaltSize = 16;  // 128 bits
        private const int KeySize = 32;   // 256 bits

        public static string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2") return false;

            var iter = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var hash = Convert.FromBase64String(parts[3]);

            var test = Rfc2898DeriveBytes.Pbkdf2(password, salt, iter, HashAlgorithmName.SHA256, hash.Length);
            return CryptographicOperations.FixedTimeEquals(hash, test);
        }
    }
}
