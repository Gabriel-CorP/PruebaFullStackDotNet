using System.Security.Cryptography;
using System.Text;
using Ventas.Application.Common.Interfaces;

namespace Ventas.Infrastructure.Security;

/// <summary>
/// Hash = SHA-512(password + salt), idéntico a HASHBYTES('SHA2_512', password + salt) del script SQL.
/// Salt único por usuario (GUID) para que dos contraseñas iguales no generen el mismo hash.
/// </summary>
public sealed class Sha512PasswordHasher : IPasswordHasher
{
    public string GenerateSalt() => Guid.NewGuid().ToString();

    public byte[] Hash(string password, string salt) =>
        SHA512.HashData(Encoding.UTF8.GetBytes(password + salt));

    public bool Verify(string password, string salt, byte[] expectedHash) =>
        CryptographicOperations.FixedTimeEquals(Hash(password, salt), expectedHash);
}
