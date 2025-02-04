using Newtonsoft.Json;

namespace T3Jailbreak;
public static class PlayerWeaponsSettingsManager
{
    private static string? settingsFilePath;
    private static Dictionary<string, PlayerWeaponSettings> playerWeapons = new Dictionary<string, PlayerWeaponSettings>();

    public static void Initialize(string basePath)
    {
        settingsFilePath = Path.Combine(basePath, "player_guns.json");
        LoadSettings();
    }

    private static void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            string json = File.ReadAllText(settingsFilePath);
            playerWeapons = JsonConvert.DeserializeObject<Dictionary<string, PlayerWeaponSettings>>(json) ?? new Dictionary<string, PlayerWeaponSettings>();
        }
        else
        {
            SaveSettings();
        }
    }

    public static void SaveSettings()
    {
        var json = JsonConvert.SerializeObject(playerWeapons, Formatting.Indented);
        File.WriteAllText(settingsFilePath!, json);
    }

    public static PlayerWeaponSettings GetPlayerWeaponSettings(string steamId)
    {
        if (!playerWeapons.TryGetValue(steamId, out var settings))
        {
            settings = new PlayerWeaponSettings { SteamID = steamId };
            playerWeapons[steamId] = settings;
        }
        return settings;
    }

    public static void SetPlayerWeaponSettings(string steamId, PlayerWeaponSettings settings)
    {
        playerWeapons[steamId] = settings;
        SaveSettings();
    }
}
public class PlayerWeaponSettings
{
    public string? SteamID { get; set; }
    public Dictionary<string, string> SelectedWeapons { get; set; } = new Dictionary<string, string>();

    public PlayerWeaponSettings()
    {
        SelectedWeapons = new Dictionary<string, string>();
    }
}
