using Newtonsoft.Json;

namespace MinecraftSkinRender.MojangApi;

public record ProfileNameObj
{
    [JsonProperty("id")]
    public string UUID { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
}
