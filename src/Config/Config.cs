using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Newtonsoft.Json;

namespace T3Jailbreak;

public class PluginConfig : BasePluginConfig
{
    public Database_Config Database { get; set; } = new Database_Config();
    public Simon_Config Simon { get; set; } = new Simon_Config();
    public Models_Config Models { get; set; } = new Models_Config();
    public LastRequest_Config LastRequest { get; set; } = new LastRequest_Config();
    public SpecialDays_Config SpecialDays { get; set; } = new SpecialDays_Config();
    public Commands_Config Commands { get; set; } = new Commands_Config();
    public BunnyHoop_Config BunnyHoop { get; set; } = new BunnyHoop_Config();
    public Prisoniers_Config Prisoniers { get; set; } = new Prisoniers_Config();
    public Guarians_Config Guardians { get; set; } = new Guarians_Config();
    public Sounds_Config Sounds { get; set; } = new Sounds_Config();

    public static string ConfigFilePath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", "T3-Jailbreak", "T3-Jailbreak.json");

    public void SaveConfig()
    {
        try
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
            Server.PrintToConsole("[T3-Jailbreak] Config file saved successfully.");
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[T3-Jailbreak] Error saving config file: {ex.Message}");
        }
    }

    public static PluginConfig LoadConfig()
    {
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                string json = File.ReadAllText(ConfigFilePath);
                Server.PrintToConsole("[T3-Jailbreak] Config file loaded successfully.");
                return JsonConvert.DeserializeObject<PluginConfig>(json) ?? new PluginConfig();
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[T3-Jailbreak] Error loading config file: {ex.Message}");
                return new PluginConfig();
            }
        }
        else
        {
            PluginConfig config = new PluginConfig();
            config.SaveConfig();
            return config;
        }
    }
}
public class Database_Config
{
    public string DatabaseHost { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string DatabaseUser { get; set; } = "";
    public string DatabasePassword { get; set; } = "";
    public uint DatabasePort { get; set; } = 3306;
}
public class Guarians_Config
{
    public List<string> VipFlags { get; set; } = ["@css/vip"];
    public List<string> GiveXHealthshot { get; set; } = [""];
}
public class Commands_Config
{
    public List<string> SimonMenu { get; set; } = ["wmenu", "smenu"];
    public List<string> Simon { get; set; } = ["s", "simon"];
    public List<string> UnSimon { get; set; } = ["us", "unsimon"];
    public List<string> Deputy { get; set; } = ["d", "deputy"];
    public List<string> UnDeputy { get; set; } = ["ud", "undeputy"];
    public List<string> Box { get; set; } = ["box"];
    public List<string> Ding { get; set; } = ["ding"];
    public List<string> GunsMenu { get; set; } = ["gun", "guns"];
    public List<string> OpenCells { get; set; } = ["open", "o"];
    public List<string> CloseCells { get; set; } = ["close", "c"];
    public List<string> ForgiveRebel { get; set; } = ["forgive", "pardon"];
    public List<string> GiveUp { get; set; } = ["giveup", "p"];
    public List<string> GiveFreeday { get; set; } = ["givefreeday", "freeday"];
    public List<string> RemoveFreeday { get; set; } = ["removefreeday", "unfreeday"];
    public List<string> LastRequest { get; set; } = ["lr", "lastrequest"];
    public List<string> SpecialDays { get; set; } = ["sd", "specialday"];
    public List<string> Heal { get; set; } = ["h", "heal"];
    public List<string> SetColor { get; set; } = ["color", "setcolor"];
    public List<string> LRTop { get; set; } = ["lrtop"];
    public List<string> QueueCommands { get; set; } = ["q", "queue"];
    public List<string> QueueListCommands { get; set; } = ["qlist", "queuelist"];
    public List<string> ExtendRoundTimeCommands { get; set; } = ["extend"];
    public List<string> CTBanCommands { get; set; } = ["ctban"];
    public AdminCommands_Config AdminCommands { get; set; } = new AdminCommands_Config();
}
public class AdminCommands_Config
{
    public List<string> SetSimon { get; set; } = ["sw", "ss", "setsimon"];
    public List<string> RemoveSimon { get; set; } = ["removes", "removesimon"];

    public AdminCommands_Permissions AdminPermissions { get; set; } = new AdminCommands_Permissions();

}
public class AdminCommands_Permissions
{
    public List<string> SetSimon { get; set; } = ["@css/generic"];
    public List<string> RemoveSimon { get; set; } = ["@css/generic"];
}
public class Simon_Config
{
    public float SetSimonIfNotAny { get; set; } = 10.0f;
}

public class BunnyHoop_Config
{
    public int BunnyHoopTimer { get; set; } = 30;
    public bool PrintToCenterHtml { get; set; } = true;
    public bool ShowChatMessages { get; set; } = true;
}
public class Models_Config
{
    public string SimonModel { get; set; } = "characters/models/nozb1/rus_police_player_model/rus_police_player_model.vmdl";
    public string DeputyModel { get; set; } = "characters/models/kolka/prisoner_guard/prisoner_guard_black/prisoner_guard_black3.vmdl";
    public string GuardModel { get; set; } = "characters/models/nozb1/policeman_player_model/policeman_player_model.vmdl";
    public string ArmsRaceKnifeModel { get; set; } = "weapons/nozb1/knife/blaine_spineedge/blaine_spineedge.vmdl";
}
public class SpecialDays_Config
{
    public int SDRoundsCountdown { get; set; } = 3;
    public float SdStartTimer { get; set; } = 15.0f;
    public float HideTimer { get; set; } = 120;
    public float WarTimer { get; set; } = 60;
    public float ZombieTimer { get; set; } = 60;
    public int ZombieHealth { get; set; } = 3500;
    public string ZombieModel { get; set; } = "";
    public List<string> AdminPermissions { get; set; } = ["@css/generic"];

    public class SpecialDays_Type
    {
        public bool FreeForAll { get; set; } = true;
        public bool OneInTheChamber { get; set; } = true;
        public bool NoScope { get; set; } = true;
        public bool Teleport { get; set; } = true;
        public bool ArmsRace { get; set; } = true;
        public bool HideAndSeek { get; set; } = true;
        public bool DrunkDay { get; set; } = true;
        public bool WarDay { get; set; } = true;
    }
    public SpecialDays_Type Type { get; set; } = new SpecialDays_Type();
}

public class LastRequest_Config
{
    public bool EnableCheatPunishment { get; set; } = true;
    public float LrStartTimer { get; set; } = 2.0f;

    public class LastRequest_Types
    {
        public bool KnifeFight { get; set; } = true;
        public bool ShotForShot { get; set; } = true;
        public bool NoScope { get; set; } = true;
        public bool MagForMag { get; set; } = true;
        public bool Dodgeball { get; set; } = true;
        public bool HeadShotOnly { get; set; } = true;
        public bool Rebel { get; set; } = true;
    }
    public LastRequest_Types Types { get; set; } = new LastRequest_Types();
}
public class Sounds_Config
{
    public string SetSimonSound { get; set; } = "";
    public string SimonDeathSound { get; set; } = "";
    public string SimonGaveUpSound { get; set; } = "";
}
public class Prisoniers_Config
{
    public List<string> SkipQueuePermissions { get; set; } = ["@css/vip"];
    public int HealCommandCountPerRound { get; set; } = 2;
}