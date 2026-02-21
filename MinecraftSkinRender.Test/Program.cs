using MinecraftSkinRender;
using MinecraftSkinRender.Image;
using SkiaSharp;

await SkinDownloader.Download();

using var skin = SKBitmap.Decode("skin.png");
using var cape = SKBitmap.Decode("cape.png");

Skin2DHeadTypeA.MakeHeadImage(skin).SavePng("tempa.png");
Skin2DHeadTypeB.MakeHeadImage(skin).SavePng("tempb.png");
Skin3DHeadTypeA.MakeHeadImage(skin).SavePng("tempc.png");
Skin3DHeadTypeB.MakeHeadImage(skin, 0, 15).SavePng("tempd.png");
Skin2DTypeA.MakeSkinImage(skin).SavePng("tempe.png");
Skin2DTypeB.MakeSkinImage(skin).SavePng("tempf.png");
Skin2DTypeA.MakeSkinImage(skin, SkinType.NewSlim).SavePng("tempg.png");
Skin2DTypeB.MakeSkinImage(skin, SkinType.NewSlim).SavePng("temph.png");
Cape2DTypaA.MakeCapeImage(cape).SavePng("tempi.png");
