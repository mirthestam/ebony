using System.Text.Json;
using Ebony.Backends.MPD.Connection;
using Ebony.Core.Connection;
using Microsoft.Extensions.Logging;

namespace Ebony.App.Infrastructure;

public partial class DiskConnectionProfileSource(ILogger<DiskConnectionProfileSource> logger)
{
    private static readonly string ProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Ebony", "profiles");

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public async Task<IEnumerable<IConnectionProfile>> LoadAllAsync()
    {
        if (!Directory.Exists(ProfileDir)) return [];

        var profiles = new List<IConnectionProfile>();
        foreach (var file in Directory.GetFiles(ProfileDir, "*.json"))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                var data = await JsonSerializer.DeserializeAsync<ConnectionProfileData>(stream, SerializerOptions);
                
                IConnectionProfile? profile = data switch
                {
                    MpdConnectionProfileData mpdData => new ConnectionProfile
                    {
                        Id = mpdData.Id,
                        Name = mpdData.Name,
                        AutoConnect = mpdData.AutoConnect,
                        UseSocket = mpdData.UseSocket,
                        Socket = mpdData.Socket,
                        Host = mpdData.Host,
                        Port = mpdData.Port,
                        Password = mpdData.Password,
                        LastDbUpdate = mpdData.LastDbUpdate
                    },
                    _ => null
                };

                if (profile == null) continue;
                
                profile.Flags |= ConnectionFlags.Saved;
                profiles.Add(profile);
            }
            catch (Exception e)
            {
                LogFailedToLoadProfileFromFile(logger, e, file);
            }
        }
        return profiles;
    }

    public static async Task SaveAsync(IConnectionProfile profile)
    {
        Directory.CreateDirectory(ProfileDir);
        
        ConnectionProfileData data = profile switch
        {
            ConnectionProfile mpd => new MpdConnectionProfileData
            {
                Id = mpd.Id,
                Name = mpd.Name,
                AutoConnect = mpd.AutoConnect,
                UseSocket = mpd.UseSocket,
                Socket = mpd.Socket,
                Host = mpd.Host,
                Port = mpd.Port,
                Password = mpd.Password,
                LastDbUpdate = mpd.LastDbUpdate
            },
            _ => throw new NotSupportedException($"Profile type {profile.GetType()} is not supported for disk storage.")
        };

        var path = Path.Combine(ProfileDir, $"{profile.Id}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, data, SerializerOptions);
    }

    public static void Delete(Guid id)
    {
        var path = Path.Combine(ProfileDir, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);
    }

    [LoggerMessage(LogLevel.Error, "Failed to load profile from {file}")]
    static partial void LogFailedToLoadProfileFromFile(ILogger<DiskConnectionProfileSource> logger, Exception e, string file);
}