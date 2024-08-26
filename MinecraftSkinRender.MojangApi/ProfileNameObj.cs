using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MinecraftSkinRender.MojangApi;

public record ProfileNameObj
{
    [JsonProperty("id")]
    public string UUID { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
}
