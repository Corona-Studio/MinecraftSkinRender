using System.Text;
using System.Text.Json;

namespace MinecraftSkinRender.MojangApi;

public static class MinecraftAPI
{
    public static readonly HttpClient Client = new();

    public const string ProfileName = "https://api.mojang.com/users/profiles/minecraft";
    public const string Profile = "https://api.minecraftservices.com/minecraft/profile";
    public const string UserProfile = "https://sessionserver.mojang.com/session/minecraft/profile";

    /// <summary>
    /// 从游戏名字获取UUID
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static async Task<ProfileNameObj?> GetMinecraftProfileNameAsync(string name)
    {
        string url = $"{ProfileName}/{name}";
        var data = await Client.GetStringAsync(url);

        return JsonSerializer.Deserialize(data, JsonType.ProfileNameObj);
    }

    /// <summary>
    /// 获取账户信息
    /// </summary>
    /// <param name="accessToken">token</param>
    /// <returns>账户信息</returns>
    public static async Task<MinecraftProfileObj?> GetMinecraftProfileAsync(string accessToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, Profile);
        message.Headers.Add("Authorization", $"Bearer {accessToken}");
        var data = await Client.SendAsync(message);
        var data1 = await data.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(data1, JsonType.MinecraftProfileObj);
    }

    /// <summary>
    /// 获取皮肤信息
    /// </summary>
    /// <param name="uuid">uuid</param>
    /// <param name="url">网址</param>
    /// <returns>皮肤信息</returns>
    public static async Task<UserProfileObj?> GetUserProfile(string? uuid)
    {
        var url = $"{UserProfile}/{uuid}";
        var data = await Client.GetStringAsync(url);

        return JsonSerializer.Deserialize(data, JsonType.UserProfileObj);
    }

    /// <summary>
    /// 将base64字符串转为材质信息<br/>
    /// 从<see cref="UserProfileObj.PropertiesObj.Value"/>获取base64字符串
    /// </summary>
    /// <param name="base64">输入base64</param>
    /// <returns></returns>
    public static TexturesObj? Base64ToTexture(string base64)
    {
        var data = Convert.FromBase64String(base64);
        var data1 = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize(data1, JsonType.TexturesObj);
    }
}
