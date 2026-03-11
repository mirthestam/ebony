using System.Text.Json.Serialization;

namespace Ebony.App.Infrastructure;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MpdConnectionProfileData), typeDiscriminator: "mpd")]
public abstract class ConnectionProfileData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool AutoConnect { get; set; }
}