using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Modules.Utils;
using static T3Jailbreak.Helpers;
using CounterStrikeSharp.API.Core.Capabilities;
using T3MenuSharedApi;
using static T3Jailbreak.JailPlayer;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Memory;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Timers;

namespace T3Jailbreak;

public class T3Jailbreak : BasePlugin, IPluginConfig<PluginConfig>
{
    // NOTA: NU UITA SA ADAUGI EVENT CHECK PT DAY-URI SI LR-URI
    // NOTA: ADAUGAT EVENT CHECK PT LR 
    public override string ModuleName => "[T3] Jailbreak";
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleVersion => "1.0";
    public static T3Jailbreak Instance { get; set; } = new T3Jailbreak();
    public PluginConfig Config { get; set; } = new PluginConfig();

    private int remainingCooldown;
    private bool isCountdownActive = false;
    private DateTime lastUpdateTime;
    private readonly WIN_LINUX<int> OnCollisionRulesChangedOffset = new WIN_LINUX<int>(173, 172);

    Circle marker = new Circle();
    static bool isEventActive = false;
    Laser laser = new Laser();

    public IT3MenuManager? MenuManager;
    public IT3MenuManager? GetMenuManager()
    {
        if (MenuManager == null)
            MenuManager = new PluginCapability<IT3MenuManager>("t3menu:manager").Get();

        return MenuManager;
    }
    public async void OnConfigParsed(PluginConfig config)
    {
        Config = config;

        await JBDatabase.CreateJailbreakTableAsync(config.Database);
    }

    public static bool EventActive()
    {
        return isEventActive;
    }
    public override void Load(bool hotReload)
    {
        Instance = this;
        RegisterEventsAndListeners();
        AddTimer(Laser.LASER_TIME, laser.LaserTick, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);

    }
    public override void Unload(bool hotReload)
    {
        Commands.UnLoad();
        SpecialDays.UnLoad();
    }
    public void RegisterEventsAndListeners()
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventTeamchangePending>(OnSwitchTeam, HookMode.Pre);
        RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
        AddCommandListener("jointeam", OnJoinTeam, HookMode.Pre);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventBulletImpact>(OnBulletImpact);
        RegisterListener<OnTick>(OnTick);
        RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);

        Config.SaveConfig();
        PlayerWeaponsSettingsManager.Initialize(Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "plugins", "T3-Jailbreak"));
        LastRequest.Load();
        SpecialDays.Load();
        Queue.Load();
        Simon.Load();
        Commands.Load();
        PluginConfig.LoadConfig();
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {

        if (!@event.Userid!.IsValid)
        {
            return HookResult.Continue;
        }

        CCSPlayerController player = @event.Userid;

        if (player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        if (!player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }

        CHandle<CCSPlayerPawn> pawn = player.PlayerPawn;

        Server.NextFrame(() => PlayerSpawnNextFrame(player, pawn));

        return HookResult.Continue;
    }
    public HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        const double maxCtRatio = 0.5;

        var ctPlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.Team == CsTeam.CounterTerrorist);
        var tPlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.Team == CsTeam.Terrorist);

        int ctCount = ctPlayers.Count();
        int tCount = tPlayers.Count();

        int maxAllowedCTs = Math.Max(1, (int)Math.Floor(tCount * maxCtRatio));

        if (player.Team == CsTeam.CounterTerrorist)
        {
            if (ctCount >= maxAllowedCTs && tCount > 0)
            {
                player.ChangeTeam(CsTeam.Terrorist);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }

        if (player.Team == CsTeam.Spectator)
        {
            player.ChangeTeam(CsTeam.Terrorist);
            return HookResult.Handled;
        }

        if (player.Team != CsTeam.Terrorist)
        {
            player.SwitchTeam(CsTeam.Terrorist);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }


    public HookResult OnSwitchTeam(EventTeamchangePending @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null)
            return HookResult.Continue;
        
        player.ChangeTeam(CsTeam.Terrorist);

        return HookResult.Continue;
    }
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        Laser.ApplySavedColors();
        marker.ApplySavedColors();

        return HookResult.Continue;
    }
    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        CCSPlayerController? rebel = @event.Userid;
        var weapon = @event.Weapon;

        if (LastRequest.isLrActive)
            return HookResult.Continue;

        if (SpecialDays.isSpecialDayActive)
            return HookResult.Continue;

        if (rebel == null)
            return HookResult.Continue;

        if (weapon != null && weapon.Contains("knife", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        if (weapon != null && weapon.Contains("healthshot", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        if (weapon != null && weapon.Contains("hegrenade", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        if (weapon != null && weapon.Contains("smoke", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        if (weapon != null && weapon.Contains("decoy", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        if (weapon != null && weapon.Contains("flashbang", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        if (weapon != null && weapon.Contains("molotov", StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        if (rebel.Team == CsTeam.Terrorist && !isRebel(rebel))
        {
            SetRebel(rebel);
            if (!RebelList.ContainsKey(rebel))
            {
                RebelList.Add(rebel, rebel);
            }
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        // creeaza un cerc unde da simon-u ping.
        CCSPlayerController? player = @event.Userid;

        if (player == null)
            return HookResult.Continue;

        if (player.PawnIsAlive)
        {
            laser.Ping(player, @event.X, @event.Y, @event.Z);
        }

        return HookResult.Continue;
    }
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        if (victim == null || attacker == null)
            return HookResult.Continue;

        if (LastRequest.isLrActive)
            return HookResult.Continue;

        if (SpecialDays.isSpecialDayActive)
            return HookResult.Continue;

        if (victim.Team == CsTeam.CounterTerrorist && attacker.Team == CsTeam.Terrorist)
        {
            if (RebelList.TryGetValue(attacker, out var existingVictim) && existingVictim == victim)
            {
                return HookResult.Continue;
            }

            SetRebel(attacker);

            RebelList[attacker] = victim;
        }

        return HookResult.Continue;
    }

    public HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        //remove weapon when simon shoot at it
        CCSPlayerController? shooter = @event.Userid;
        if (shooter == null)
            return HookResult.Continue;

        if (!Simon.isSimon(shooter))
            return HookResult.Continue;

        var impactPosition = new CounterStrikeSharp.API.Modules.Utils.Vector(@event.X, @event.Y, @event.Z);

        var allWeapons = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon");

        var targetedWeapon = allWeapons
            .Where(entity => entity.IsValid && entity.DesignerName.StartsWith("weapon_") &&
                             (entity.OwnerEntity == null || !entity.OwnerEntity.IsValid))
            .OrderBy(entity =>
            {
                double distance = GetDistance(entity.AbsOrigin!, impactPosition);
                return distance;
            })
            .FirstOrDefault();

        if (targetedWeapon != null)
        {
            double distance = GetDistance(targetedWeapon.AbsOrigin!, impactPosition);

            if (distance <= 50.0f)
            {
                targetedWeapon.Remove();
            }
        }
        return HookResult.Continue;
    }
    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(Config.Models.GuardModel);
        manifest.AddResource(Config.Models.SimonModel);
        manifest.AddResource(Config.Models.DeputyModel);
        manifest.AddResource(Config.Models.ArmsRaceKnifeModel);
    }
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (Simon.isSimon(player))
        {
            Server.PrintToChatAll(Localizer["jb.prefix"] + Localizer["simon.disconnected"]);
            Simon.RemoveSimon();
        }

        return HookResult.Continue;
    }
    public void OnTick()
    {
        laser.LaserTick();
        BunnyHoopTick();
        LRHelper.StartNoScope();
        LRHelper.StartSDNoScope();
        /*if (LastRequest.GetTAlive()) // nu uita sa adaugi asta <<<<
        {
            var T = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive);
            foreach (var player in T)
            {
                var manager = GetMenuManager();
                if (manager == null)
                    return;

                var menu = manager.CreateMenu(Localizer["start.lr.question"], isSubMenu: false);

                menu.Add(Localizer["option<yes>"], (p, option) =>
                {
                    p.ExecuteClientCommandFromServer($"css_{Config.Commands.LastRequest}");
                });
                menu.Add(Localizer["option<no>"], (p, option) =>
                {
                    manager.CloseMenu(p);
                });
                manager.OpenMainMenu(player, menu);
            }
        }
        */
    }
    public void BunnyHoopTick()
    {
        if (!isCountdownActive) return;

        if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
        {
            remainingCooldown = Math.Max(0, remainingCooldown - 1);
            lastUpdateTime = DateTime.Now;
        }

        string countdownMessage = Localizer["bunnyhoop_disabled_center", remainingCooldown];

        foreach (var player in Utilities.GetPlayers())
        {
            if (Config.BunnyHoop.PrintToCenterHtml)
            {
                player.PrintToCenterHtml(countdownMessage);
            }
        }

        if (remainingCooldown <= 0)
        {
            EnableBunnyHoop();
            isCountdownActive = false;
        }
    }
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() = false;
        const double maxCtRatio = 0.5;

        var ctPlayers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist).ToList();
        var tPlayers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist).ToList();

        int ctCount = ctPlayers.Count;
        int tCount = tPlayers.Count;

        int maxAllowedCTs = (int)Math.Floor(tCount * maxCtRatio);

        if (ctCount > maxAllowedCTs && tCount > 0)
        {
            int excessCTs = ctCount - Math.Max(maxAllowedCTs, 1);

            for (int i = 0; i < excessCTs; i++)
            {
                var playerToMove = ctPlayers[i];
                playerToMove.ChangeTeam(CsTeam.Terrorist);
            }
        }

        foreach (var p in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist))
        {
            RebelList.Remove(p);
            playerRebel.Remove(p);
            p.StripWeapons();
        }

        var TPlayers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && !p.IsBot && !p.IsHLTV);
        Server.PrintToChatAll(Localizer["jb.prefix"] + Localizer["t.muted"]);
        AddTimer(0.5f, () =>
        {
            foreach (var p in TPlayers)
            {
                if (AdminManager.PlayerHasPermissions(p, "@css/generic"))
                {
                    p.VoiceFlags = VoiceFlags.Normal;
                }
                else
                {
                    p.VoiceFlags = VoiceFlags.Muted;
                }
            }
        });
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || player.Team == CsTeam.Terrorist || player.Team == CsTeam.Spectator)
                continue;

            string steamId = player.SteamID.ToString();
            var weaponSettings = PlayerWeaponsSettingsManager.GetPlayerWeaponSettings(steamId);

            if (weaponSettings.SelectedWeapons.TryGetValue("primary", out string? primaryWeapon) &&
                weaponSettings.SelectedWeapons.TryGetValue("secondary", out string? secondaryWeapon))
            {
                try
                {
                    player.StripWeapons();
                    player.GiveNamedItem(primaryWeapon);
                    player.GiveNamedItem(secondaryWeapon);

                }
                catch (Exception ex)
                {
                    Instance.Logger.LogError($"[EntrySounds] Error giving weapons to player {steamId}: {ex.Message}");
                }
            }
            foreach (var flag in Config.Guardians.VipFlags)
            {
                if (AdminManager.PlayerHasPermissions(player, flag))
                {
                    player.GiveWeapon("healthshot");
                    player.PrintToChat(Localizer["jb.prefix"] + Localizer["You got healthshot for beign vip!"]);
                }
            }
        }
        if (SpecialDays.cooldownActive())
        {
            EnableBunnyHoop();
        }
        else
        {
            DisableBunnyHoop();
        }
        remainingCooldown = Config.BunnyHoop.BunnyHoopTimer;
        isCountdownActive = true;
        lastUpdateTime = DateTime.Now;

        return HookResult.Continue;
    }
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        isCountdownActive = false;
        var TPlayers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist);

        foreach (var p in TPlayers)
        {
            // unmute all T on round end
            p.VoiceFlags = VoiceFlags.Normal;
        }
        Server.PrintToChatAll(Localizer["jb.prefix"] + Localizer["t.unmuted"]);
        return HookResult.Continue;
    }
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? attacker = @event.Attacker;
        CCSPlayerController? player = @event.Userid;

        if (attacker == null || player == null)
            return HookResult.Continue;

        if (LastRequest.isLrActive)
            return HookResult.Continue;

        if (SpecialDays.isSpecialDayActive)
            return HookResult.Continue;

        var alivePlayers = Utilities.GetPlayers()
                                    .Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive)
                                    .ToList();

        if (alivePlayers.Count == 1)
        {
            var lastTerrorist = alivePlayers.First();
            lastTerrorist.VoiceFlags = VoiceFlags.Normal;
        }
        if (attacker.Team == CsTeam.CounterTerrorist && isRebel(player))
        {
            // announce rebel was killed by CT

            RebelList.Remove(player);
            playerRebel.Remove(player);
            
            Server.PrintToChatAll(Localizer["rebel.prefix"] + Localizer["rebel.death", player.PlayerName, attacker.PlayerName]);
        }
        return HookResult.Continue;
    }
    private void PlayerSpawnNextFrame(CCSPlayerController player, CHandle<CCSPlayerPawn> pawn)
    {
        // Changes the player's collision to 16, allowing the player to pass through other players while still take damage from bullets and knife attacks
        pawn.Value!.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;

        // Changes the player's CollisionAttribute to the collision type used for dissolving objects 
        pawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;

        // Updates the CollisionRulesChanged for the specific player
        VirtualFunctionVoid<nint> collisionRulesChanged = new VirtualFunctionVoid<nint>(pawn.Value.Handle, OnCollisionRulesChangedOffset.Get());

        // Invokes the updated CollisionRulesChanged information to ensure the player's collision is correctly set
        collisionRulesChanged.Invoke(pawn.Value.Handle);
    }
}
internal static class IsValid
{
    // Returns true if the player's index is valid
    public static bool PlayerIndex(uint playerIndex)
    {
        // If the player's index is 0 then execute this section
        if (playerIndex == 0)
        {
            return false;
        }

        // If the client's index value is not within the range it should be then execute this section
        if (!(1 <= playerIndex && playerIndex <= Server.MaxPlayers))
        {
            return false;
        }

        return true;
    }
}
public class WIN_LINUX<T>
{
    [JsonPropertyName("Windows")]
    public T Windows { get; private set; }

    [JsonPropertyName("Linux")]
    public T Linux { get; private set; }

    public WIN_LINUX(T windows, T linux)
    {
        this.Windows = windows;
        this.Linux = linux;
    }

    public T Get()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return this.Windows;
        }
        else
        {
            return this.Linux;
        }
    }
}
