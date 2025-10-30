using System.Security.Cryptography;
using System.Text;

namespace Biedapp.Infrastructure.Configuration;
public static class EventStoreConfiguration
{
    /// <summary>
    /// Gets the default file path for events storage in AppData folder
    /// Windows: C:\Users\[Username]\AppData\Local\Biedapp\events.json
    /// macOS: /Users/[Username]/.local/share/Biedapp/events.json
    /// Linux: /home/[Username]/.local/share/Biedapp/events.json
    /// </summary>
    public static string GetDefaultEventsFilePath()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string biedAppFolder = Path.Combine(appDataFolder, "Biedapp");

        if (!Directory.Exists(biedAppFolder))
        {
            Directory.CreateDirectory(biedAppFolder);
        }

        return Path.Combine(biedAppFolder, "events.json");
    }

    /// <summary>
    /// Generates a machine and user-specific encryption key using SHA256
    /// This ensures the key is consistent for the same user on the same machine
    /// but different for different users or machines
    /// </summary>
    public static string GetEncryptionKey()
    {
        string machineName = Environment.MachineName;
        string userName = Environment.UserName;
        string seedString = $"Biedapp-{machineName}-{userName}-v1.0";
        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(seedString));
        return Convert.ToBase64String(keyBytes);
    }
}
