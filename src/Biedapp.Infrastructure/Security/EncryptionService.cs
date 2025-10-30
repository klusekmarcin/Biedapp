using System.Security.Cryptography;
using System.Text;

namespace Biedapp.Infrastructure.Security;
public class EncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new ArgumentException("Encryption key cannot be null or empty", nameof(base64Key));

        byte[] keyBytes = Convert.FromBase64String(base64Key);

        if (keyBytes.Length != 32)
            throw new ArgumentException("Encryption key must be 32 bytes (256 bits)", nameof(base64Key));

        _key = keyBytes;

        // Derive IV from key using SHA256 (first 16 bytes)
        _iv = SHA256.HashData(keyBytes).Take(16).ToArray();
    }

    /// <summary>
    /// Encrypts plain text using AES-256-CBC
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to encrypt data", ex);
        }
    }

    /// <summary>
    /// Decrypts cipher text using AES-256-CBC
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException)
        {
            // File might not be encrypted or corrupted - return empty JSON array
            return "[]";
        }
        catch (CryptographicException)
        {
            // Decryption failed - possibly wrong key or corrupted data
            return "[]";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt data", ex);
        }
    }
}
