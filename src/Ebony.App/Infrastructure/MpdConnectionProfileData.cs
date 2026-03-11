namespace Ebony.App.Infrastructure;

public class MpdConnectionProfileData : ConnectionProfileData
{
    public string Socket { get; set; } = "/run/mpd/socket";
    public bool UseSocket { get; set; } = false;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 6600;
    public string Password { get; set; } = string.Empty;
    public long LastDbUpdate { get; set; }
}