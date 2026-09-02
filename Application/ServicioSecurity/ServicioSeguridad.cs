using Core.Shared;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security;

// https://gist.github.com/hclewk/a41d937eba12c0388f70429a997cd7ec

namespace Core.ServicioSecurity;

internal class ServicioSeguridad : IServicio, IServicioSeguridad
{
    public ILogger<ServicioSeguridad> _logger;


    public ServicioSeguridad(ILogger<ServicioSeguridad> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, string> HashPassword(string password)
    {
        //string[] passwordHashed = new string[2];
        var passwordHashed = new Dictionary<string, string>();

        var salt = GenerateSalt();
        var hash = PBKDF2(password, salt);

        /* char[] delimiter = { ':' };
        string[] split = storedHash.Split(delimiter);
        int iterations = Int32.Parse(split[0]); */

        // passwordHashed[0] = Convert.ToBase64String(salt);
        // passwordHashed[1] = Convert.ToBase64String(hash);
        _logger.LogInformation("Contraseña hasheada correctamente.");
        passwordHashed.Add("salt", Convert.ToBase64String(salt));
        passwordHashed.Add("hash", Convert.ToBase64String(hash));
        _logger.LogInformation("SALT: ", passwordHashed.GetValueOrDefault("salt"));
        _logger.LogInformation("HASH: ", passwordHashed.GetValueOrDefault("hash"));

        return passwordHashed;
    }

    public bool ValidatePassword(SecureString password, string storedSalt, string storedHash)
    {
        _logger.LogInformation("Validando contraseña...");

        byte[] salt = Convert.FromBase64String(storedSalt);
        byte[] hash = Convert.FromBase64String(storedHash);
        
        var pass = new System.Net.NetworkCredential(string.Empty, password).Password;
        byte[] testHash = PBKDF2(pass, salt);
        _logger.LogInformation("Contraseña validada...");

        return SlowEquals(hash, testHash);
    }

    private byte[] PBKDF2(string password, byte[] salt, int iterations = 1000, int outputBytes = 24)
    {
        _logger.LogInformation("Encriptando la contraseña...");
        var pbkdf2 = new Rfc2898DeriveBytes(password: password,
                                            salt: salt,
                                            iterations: iterations,
                                            hashAlgorithm: HashAlgorithmName.SHA512);
        _logger.LogInformation("Contraseña encriptada...");

        return pbkdf2.GetBytes(outputBytes);
    }

    private byte[] GenerateSalt(int size=24)
    {
        _logger.LogInformation("Generando salt...");
        var mySalt = new byte[size];
        
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(mySalt);
        _logger.LogInformation("Salt generada correctamente...");

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
