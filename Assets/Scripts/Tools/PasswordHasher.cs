using System.Security.Cryptography;
using System.Text;
/// <summary>
/// 哈希加密密码存储
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string rawPassword)
    {
        if (string.IsNullOrEmpty(rawPassword))
            return "";

        using SHA256 sha256 = SHA256.Create();

        byte[] bytes = Encoding.UTF8.GetBytes(rawPassword);
        byte[] hash = sha256.ComputeHash(bytes);

        StringBuilder builder = new StringBuilder();

        foreach (byte b in hash)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    public static bool Verify(string rawPassword, string passwordHash)
    {
        return Hash(rawPassword) == passwordHash;
    }
}