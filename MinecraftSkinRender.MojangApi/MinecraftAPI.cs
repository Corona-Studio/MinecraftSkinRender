using Newtonsoft.Json;

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

        return JsonConvert.DeserializeObject<ProfileNameObj>(data);
    }

    /// <summary>
    /// 获取账户信息
    /// </summary>
    /// <param name="accessToken">token</param>
    /// <returns>账户信息</returns>
    public static async Task<MinecraftProfileObj?> GetMinecraftProfileAsync(string accessToken)
    {
        HttpRequestMessage message = new(HttpMethod.Get, Profile);
        message.Headers.Add("Authorization", $"Bearer {accessToken}");
        var data = await Client.SendAsync(message);
        var data1 = await data.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<MinecraftProfileObj>(data1); ;
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

        return JsonConvert.DeserializeObject<UserProfileObj>(data);
    }
}
