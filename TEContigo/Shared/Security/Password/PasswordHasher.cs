using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace TEContigo.Shared.Security.Password
{
    public class PasswordHasher : IPasswordHasher
    {
        public async Task<string> HashAsync(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            var argon2 = new Argon2id(
                Encoding.UTF8.GetBytes(password));

            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 2;
            argon2.Iterations = 4;
            argon2.MemorySize = 65536;

            byte[] hash = await argon2.GetBytesAsync(32);

            return $"$argon2id$v=19$m=65536,t=4,p=2${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public async Task<bool> VerifyAsync(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('$');

                if (parts.Length != 6)
                    return false;

                var parameters = parts[3];

                var salt = Convert.FromBase64String(parts[4]);
                var expectedHash = Convert.FromBase64String(parts[5]);

                var parameterParts = parameters.Split(',');

                var memory = int.Parse(
                    parameterParts[0].Split('=')[1]);

                var iterations = int.Parse(
                    parameterParts[1].Split('=')[1]);

                var parallelism = int.Parse(
                    parameterParts[2].Split('=')[1]);

                var argon2 = new Argon2id(
                    Encoding.UTF8.GetBytes(password));

                argon2.Salt = salt;
                argon2.MemorySize = memory;
                argon2.Iterations = iterations;
                argon2.DegreeOfParallelism = parallelism;

                var actualHash =
                    await argon2.GetBytesAsync(expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(
                    actualHash,
                    expectedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
