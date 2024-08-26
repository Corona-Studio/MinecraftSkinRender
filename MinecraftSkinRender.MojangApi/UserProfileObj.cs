using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftSkinRender.MojangApi;

/// <summary>
/// 皮肤信息
/// </summary>
/// <value></value>
public record UserProfileObj
{
    public record Properties
    {
        public string name { get; set; }
        public string value { get; set; }
        public string signature { get; set; }
    }
    public string id { get; set; }
    public string name { get; set; }
    public List<Properties> properties { get; set; }
}
