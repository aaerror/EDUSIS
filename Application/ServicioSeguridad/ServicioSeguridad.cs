using Core.Shared;
using System.Security.Cryptography;

// https://gist.github.com/hclewk/a41d937eba12c0388f70429a997cd7ec

namespace Core.ServicioSeguridad;

public class ServicioSeguridad : IServicio, IServicioSeguridad
{
    public string HashPassword(string password)
    {
        var salt = GenerarSalt(24);
        byte[] hash = PBKDF2(password, salt, 500, 24);

        /* char[] delimiter = { ':' };
        string[] split = storedHash.Split(delimiter);
        int iterations = Int32.Parse(split[0]); */

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool ValidatePassword(string password, string storedSalt, string storedHash)
    {
        byte[] salt = Convert.FromBase64String(storedSalt);
        byte[] hash = Convert.FromBase64String(storedHash);
        byte[] testHash = PBKDF2(password, salt, 500, hash.Length);

        return SlowEquals(hash, testHash);
    }

    private byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
    {
        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt);
        pbkdf2.IterationCount = iterations;
        return pbkdf2.GetBytes(outputBytes);
    }

    private byte[] GenerarSalt(int size)
    {
        using RNGCryptoServiceProvider rng = new();
        var mySalt = new byte[size];
        rng.GetBytes(mySalt);

        return mySalt;
    }

    private bool SlowEquals(byte[] a, byte[] b)
    {
        uint diff = (uint) a.Length ^ (uint) b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            diff |= (uint)(a[i] ^ b[i]);
        }

        return diff == 0;
    }
}
