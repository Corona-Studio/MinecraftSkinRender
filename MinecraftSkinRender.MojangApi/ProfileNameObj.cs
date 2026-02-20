using System.Text.Json.Serialization;

namespace MinecraftSkinRender.MojangApi;

public record ProfileNameObj
{
    [JsonPropertyName("id")]
    public string UUID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
