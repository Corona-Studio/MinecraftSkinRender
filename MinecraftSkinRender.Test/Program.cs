using System.Text;
using MinecraftSkinRender;
using MinecraftSkinRender.Image;
using MinecraftSkinRender.MojangApi;
using Newtonsoft.Json;
using SkiaSharp;

//Console.WriteLine("Download skin");

//var res = await MinecraftAPI.GetMinecraftProfileNameAsync("Color_yr");
//var res1 = await MinecraftAPI.GetUserProfile(res!.UUID);
//TexturesObj? obj = null;
//foreach (var item in res1!.properties)
//{
//    if (item.name == "textures")
//    {
//        var temp = Convert.FromBase64String(item.value);
//        var data = Encoding.UTF8.GetString(temp);
//        obj = JsonConvert.DeserializeObject<TexturesObj>(data);
//        break;
//    }
//}
//if (obj == null)
//{
//    Console.WriteLine("No have skin");
//    return;
//}
//if (obj!.textures.SKIN.url != null)
//{
//    var data = await MinecraftAPI.Client.GetByteArrayAsync(obj!.textures.SKIN.url);
//    File.WriteAllBytes("skin.png", data);
//}
//if (obj.textures.CAPE.url != null)
//{
//    var data = await MinecraftAPI.Client.GetByteArrayAsync(obj!.textures.CAPE.url);
//    File.WriteAllBytes("cape.png", data);
//}

using var skin = SKBitmap.Decode("skin.png");

//Skin2DHeadTypeA.MakeHeadImage(skin).SavePng("tempa.png");
//Skin2DHeadTypeB.MakeHeadImage(skin).SavePng("tempb.png");
//Skin3DHeadTypeA.MakeHeadImage(skin).SavePng("tempc.png");
//Skin3DHeadTypeB.MakeHeadImage(skin, 15, 65).SavePng("tempd.png");
Skin2DTypeA.MakeSkinImage(skin).SavePng("tempe.png");
Skin2DTypeB.MakeSkinImage(skin).SavePng("tempf.png");
Skin2DTypeA.MakeSkinImage(skin, SkinType.NewSlim).SavePng("tempg.png");
Skin2DTypeB.MakeSkinImage(skin, SkinType.NewSlim).SavePng("temph.png");
