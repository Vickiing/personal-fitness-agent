using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PersonalFitnessAgent.Infrastructure.Sqlite;

/// <summary>Optional authenticated encryption for user-controlled JSON backups.</summary>
public static class BackupEncryption
{
    private const int FormatVersion = 1;
    private const int Iterations = 310_000;
    private const int SaltBytes = 16;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;

    public static string Encrypt(string plaintext, string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase) || passphrase.Length < 10) throw new ArgumentException("Passphrase must contain at least 10 characters.", nameof(passphrase));
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, 32);
        var input = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[input.Length];
        var tag = new byte[TagBytes];
        try
        {
            using var aes = new AesGcm(key, TagBytes);
            aes.Encrypt(nonce, input, ciphertext, tag);
            return JsonSerializer.Serialize(new EncryptedBackup("fitness-agent-aesgcm", FormatVersion, Iterations,
                Convert.ToBase64String(salt), Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(ciphertext)));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(input);
        }
    }

    public static string Decrypt(string envelopeJson, string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase) || passphrase.Length < 10) throw new ArgumentException("Passphrase must contain at least 10 characters.", nameof(passphrase));
        var envelope = JsonSerializer.Deserialize<EncryptedBackup>(envelopeJson)
            ?? throw new InvalidDataException("Encrypted backup is invalid.");
        if (envelope.Format != "fitness-agent-aesgcm" || envelope.Version != FormatVersion || envelope.Iterations != Iterations)
            throw new InvalidDataException("Encrypted backup format is unsupported.");
        var salt = Convert.FromBase64String(envelope.Salt);
        var nonce = Convert.FromBase64String(envelope.Nonce);
        var tag = Convert.FromBase64String(envelope.Tag);
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
        if (salt.Length != SaltBytes || nonce.Length != NonceBytes || tag.Length != TagBytes)
            throw new InvalidDataException("Encrypted backup parameters are invalid.");
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, 32);
        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, TagBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (AuthenticationTagMismatchException error)
        {
            throw new CryptographicException("Passphrase is incorrect or backup was changed.", error);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static bool IsEncrypted(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("Format", out var format) && format.GetString() == "fitness-agent-aesgcm";
        }
        catch (JsonException) { return false; }
    }

    private sealed record EncryptedBackup(string Format, int Version, int Iterations, string Salt, string Nonce, string Tag, string Ciphertext);
}
