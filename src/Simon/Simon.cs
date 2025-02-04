using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;
using static T3Jailbreak.Helpers;
using CounterStrikeSharp.API.Modules.Utils;
using static T3Jailbreak.T3Jailbreak;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace T3Jailbreak;

public partial class Simon
{
    public static readonly Dictionary<string, CPointWorldText> SimonHudTexts = new();
    public static readonly Dictionary<string, CPointWorldText> SimonHudStaticTexts = new();
    public static readonly Dictionary<string, bool> FrozenPrisoners = new();
    private static Dictionary<string, CCSPlayerController> HeldPrisoners = new();
    public Simon()
    {
        for (int p = 0; p < jailPlayers.Length; p++)
        {
            jailPlayers[p] = new JailPlayer();
        }
    }
    public static void Load()
    {
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        Instance.RegisterListener<Listeners.OnTick>(OnTick);
        Instance.RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnZeus, HookMode.Pre);
    }
    public static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        foreach (var p in Utilities.GetPlayers().Where(p => isSimon(p)))
        {
            p.SetTag(" ");
        }

        return HookResult.Continue;
    }
    public static HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        CCSPlayerController? simon = @event.Userid;
        if (simon == null)
            return HookResult.Continue;

        if (!isSimon(simon))
            return HookResult.Continue;

        var activeWeapon = simon.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon == null)
            return HookResult.Continue;

        if (activeWeapon.DesignerName.Contains("taser"))
        {
            activeWeapon.SetAmmo(100, 100);
        }

        return HookResult.Continue;
    }
    public static void OnTick()
    {
        foreach (var simon in Utilities.GetPlayers().Where(p => isSimon(p) && p.IsValid && p.PawnIsAlive))
        {
            string simonSteamId = simon.SteamID.ToString();

            if ((simon.Buttons & PlayerButtons.Attack2) != 0)
            {
                if (!HeldPrisoners.ContainsKey(simonSteamId))
                {
                    var prisoner = FindClosestFrozenPrisoner(simon);

                    if (prisoner != null)
                    {
                        // Start moving prisoner
                        HeldPrisoners[simonSteamId] = prisoner;
                    }
                }
                else
                {

                    var prisoner = HeldPrisoners[simonSteamId];

                    if (prisoner != null && prisoner.IsValid && prisoner.PawnIsAlive)
                    {
                        var simonPawn = simon.PlayerPawn.Value;
                        Vector simonPosition = simonPawn?.AbsOrigin!;
                        QAngle simonEyeAngles = simonPawn?.EyeAngles!;
                        Vector forward = new(), right = new(), up = new();
                        NativeAPI.AngleVectors(simonEyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

                        Vector prisonerNewPosition = simonPosition + forward * 50 + new Vector(0, 0, 10); 

                        prisoner.PlayerPawn.Value?.Teleport(prisonerNewPosition, simonEyeAngles, null);
                    }
                }
            }
            else
            {
                // If Attack2 is released, drop the prisoner
                if (HeldPrisoners.ContainsKey(simonSteamId))
                {
                    var prisoner = HeldPrisoners[simonSteamId];
                    HeldPrisoners.Remove(simonSteamId);
                }
            }
        }
    }
    public static HookResult OnZeus(DynamicHook hook)
    {
        var ent = hook.GetParam<CBaseEntity>(0);
        var prisoner = player(ent);

        if (prisoner == null)
            return HookResult.Continue;

        var info = hook.GetParam<CTakeDamageInfo>(1);
        CCSPlayerController? attacker = null;

        if (info.Attacker.Value != null)
        {
            var playerWhoAttacked = info.Attacker.Value.As<CCSPlayerPawn>();
            attacker = playerWhoAttacked.Controller.Value?.As<CCSPlayerController>();
        }

        if (!isSimon(attacker))
            return HookResult.Continue;

        if (info.BitsDamageType != DamageTypes_t.DMG_SHOCK)
            return HookResult.Continue;

        if (attacker == null)
            return HookResult.Continue;

        var weapon = attacker.Pawn.Value?.WeaponServices?.ActiveWeapon.Value;
        weapon.SetAmmo(10000, 10000);
        info.Damage = 0;

        string prisonerSteamId = prisoner.SteamID.ToString();

        if (FrozenPrisoners.ContainsKey(prisonerSteamId) && FrozenPrisoners[prisonerSteamId])
        {
            prisoner.Cuff();
            FrozenPrisoners[prisonerSteamId] = false;
        }
        else
        {
            prisoner.UnCuff();
            FrozenPrisoners[prisonerSteamId] = true;
        }

        return HookResult.Continue;
    }

    public static HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? attacker = @event.Attacker;
        CCSPlayerController? player = @event.Userid;

        if (attacker == null || player == null)
            return HookResult.Continue;

        var prisoniers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive).ToList();
        if (prisoniers.Count == 1)
            return HookResult.Continue;

        if (Simon.isSimon(player))
        {
            var deputy = Simon.GetDeputy();
            if (deputy != null && deputy.IsValid && deputy.PawnIsAlive)
            {
                foreach (var p in Utilities.GetPlayers())
                {
                    if (string.IsNullOrEmpty(Instance.Config.Sounds.SimonDeathSound))
                    {
                        p.PlaySound(Instance.Config.Sounds.SimonDeathSound);
                    }
                }
                Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.death.deputy.selected", deputy.PlayerName]);
                Simon.SetSimon(deputy.Slot);
            }
            else
            {
                var cts = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.IsValid && p.PawnIsAlive).ToList();
                if (cts.Count > 0)
                {
                    Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.death", attacker.PlayerName]);
                    foreach (var p in Utilities.GetPlayers())
                    {
                        p.PlaySound("sounds/jailbreaksounds/unsimon.vsnd_c");
                    }

                    var newSimon = cts[new Random().Next(cts.Count)];

                    Instance.AddTimer(5.0f, () =>
                    {
                        var simon = Simon.GetSimon();
                        if (Simon.isSimon(simon))
                            return;

                        if (newSimon.IsValid && newSimon.PawnIsAlive)
                        {
                            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.death.selected", newSimon.PlayerName]);
                            Simon.SetSimon(newSimon.Slot);
                        }
                    });
                }
            }
        }

        return HookResult.Continue;
    }

    public static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        RemoveSimonInterval();
        RemoveDeputy();
        foreach (var ct in Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist))
        {
            if (!SpecialDays.isSpecialDayActive)
            {
                ct.SetArmor(100);
            }
        }
        if (SpecialDays.isSpecialDayActive)
            return HookResult.Continue;

        if (SpecialDays.isCountdownActive)
            return HookResult.Continue;

        var prisoniers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive).ToList();
        if (prisoniers.Count == 1)
            return HookResult.Continue;

        Instance.AddTimer(Instance.Config.Simon.SetSimonIfNotAny, () =>
        {
            var currentSimon = GetSimon();
            if (!isSimon(currentSimon))
            {
                var cts = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.IsValid && p.PawnIsAlive).ToList();
                if (cts.Count > 0)
                {
                    var newSimon = cts[new Random().Next(cts.Count)];
                    Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.selected", newSimon.PlayerName]);
                    SetSimon(newSimon.Slot);
                }
            }
        });

        return HookResult.Continue;
    }
   /* public static HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var prisoner = @event.Userid;
        var simon = @event.Attacker;

        if (prisoner == null)
            return HookResult.Continue;
        if (simon == null)
            return HookResult.Continue;
        if (!isSimon(simon))
            return HookResult.Continue;

        var weapon = simon.FindWeapon("taser");

        if (weapon != null && weapon.DesignerName.Contains("taser", StringComparison.OrdinalIgnoreCase))
        {
            var jailPlayer = JailPlayerFromPlayer(prisoner);
            if (jailPlayer == null)
                return HookResult.Continue;

            if (!jailPlayer.IsCuffed)
            {
                jailPlayer.IsCuffed = true;
                prisoner.Freeze();
                Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["prisonier.cuffed", simon.PlayerName, prisoner.PlayerName]);
            }
            else
            {
                jailPlayer.IsCuffed = false;
                prisoner.UnFreeze();
                Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["prisonier.uncuffed", simon.PlayerName, prisoner.PlayerName]);
            }
        }

        return HookResult.Continue;
    }
   */

    public static void SetSimon(int slot)
    {
        if (SpecialDays.isSpecialDayActive)
            return;
        if (SpecialDays.isHnsCountDownActive || SpecialDays.isWarCountDownActive || SpecialDays.isCountdownActive)
            return;

        simonSlot = slot;
        var player = Utilities.GetPlayerFromSlot(simonSlot);
        if (player == null || !player.IsValid)
        {
            simonSlot = INVALID_SLOT;
            return;
        }
        if (player.IsBot || player.IsHLTV)
            return;

        if (!string.IsNullOrEmpty(Instance.Config.Models.SimonModel))
        {
            player.SetModel(Instance.Config.Models.SimonModel);
        }
        foreach (var p in Utilities.GetPlayers())
        {
            if (string.IsNullOrEmpty(Instance.Config.Sounds.SetSimonSound))
            {
                p.PlaySound(Instance.Config.Sounds.SetSimonSound);
            }
        }
        player.GiveWeapon("taser");
        player.SetTag(Instance.Localizer["simon.tag"]);
        player.SetColor(Color.Blue);
        UpdateSimonHud();
    }
    public static CCSPlayerController? GetDeputy()
    {
        if (deputySlot == INVALID_SLOT)
        {
            return null;
        }

        return Utilities.GetPlayerFromSlot(deputySlot);
    }

    public static void RemoveSimonInterval()
    {
        simonSlot = INVALID_SLOT;
        simonTimestamp = -1;
    }

    public static void RemoveSimon()
    {
        var player = Utilities.GetPlayerFromSlot(simonSlot);

        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            player.SetColor(DefaultColor);
            foreach (var p in Utilities.GetPlayers())
            {
                if (string.IsNullOrEmpty(Instance.Config.Sounds.SimonGaveUpSound))
                {
                    p.PlaySound(Instance.Config.Sounds.SimonGaveUpSound);
                }
            }
            if (!string.IsNullOrEmpty(Instance.Config.Models.GuardModel))
            {
                player.SetModel(Instance.Config.Models.GuardModel);
                player.SetTag(" ");
            }
        }
        RemoveSimonInterval();
        UpdateSimonHud();
    }

    public static void RemoveDeputy()
    {
        var player = Utilities.GetPlayerFromSlot(deputySlot);

        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!string.IsNullOrEmpty(Instance.Config.Models.GuardModel))
            {
                player.SetModel(Instance.Config.Models.GuardModel);
                player.SetTag(" ");
            }
        }

        deputySlot = INVALID_SLOT;
    }

    public static void RemoveIfSimon(CCSPlayerController? player)
    {
        if (isSimon(player))
        {
            RemoveSimon();
        }
    }
    public static void RemoveIfDeputy(CCSPlayerController? player)
    {
        if (isDeputy(player))
        {
            RemoveDeputy();
        }
    }

    public static CCSPlayerController? GetSimon()
    {
        if (simonSlot == INVALID_SLOT)
        {
            Instance.Logger.LogDebug("[Simon] No valid Simon slot.");
            return null;
        }

        var simonPlayer = Utilities.GetPlayerFromSlot(simonSlot);
        if (simonPlayer == null || !simonPlayer.IsValid || !simonPlayer.PawnIsAlive)
        {
            Instance.Logger.LogDebug($"[Simon] Simon slot {simonSlot} is invalid or dead.");
            return null;
        }

        return simonPlayer;
    }


    public static void SetDeputy(int slot)
    {
        deputySlot = slot;
        var player = Utilities.GetPlayerFromSlot(deputySlot);
        if (player == null || !player.IsValid)
        {
            deputySlot = INVALID_SLOT;
            return;
        }

        if (isSimon(player))
        {
            return;
        }
        if (!string.IsNullOrEmpty(Instance.Config.Models.DeputyModel))
        {
            player.SetModel(Instance.Config.Models.DeputyModel);
            player.SetTag("⭐Deputy⭐");
        }
    }
    public static JailPlayer? JailPlayerFromPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
        {
            return null;
        }

        return jailPlayers[player.Slot];
    }
    public static bool isSimon(CCSPlayerController? player)
    {
        if (player == null || !player.PawnIsAlive)
        {
            return false;
        }

        return player.Slot == simonSlot;
    }

    public static bool isDeputy(CCSPlayerController? player)
    {
        if (player == null || !player.PawnIsAlive)
        {
            return false;
        }

        return player.Slot == deputySlot;
    }
    public static void UpdateSimonHud()
    {
        try
        {
            var currentSimon = GetSimon();
            string simonName = currentSimon != null ? currentSimon.PlayerName : Instance.Localizer["hud.current.simon.none"];

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                    continue;

                string steamId = player.SteamID.ToString();

                if (SimonHudTexts.TryGetValue(steamId, out var hudText) && hudText.IsValid)
                {
                    hudText.AcceptInput("SetMessage", hudText, hudText, Instance.Localizer["hud.current.simon", simonName]);
                }
                else
                {
                    hudText = CreateHud(player, Instance.Localizer["hud.current.simon", simonName], 30, Color.LightGreen, "Verdana Bold", shiftX: -1.50f, shiftY: 3.55f);
                    SimonHudTexts[steamId] = hudText;
                }
            }
        }
        catch (Exception ex)
        {
            Instance.Logger.LogError($"[Simon HUD] Error while updating HUD: {ex.Message}");
        }
    }

    public const int INVALID_SLOT = -3;
    int colorSlot = -1;
    static long simonTimestamp = -1;
    public static int simonSlot = INVALID_SLOT;
    public static int deputySlot = INVALID_SLOT;
    public static JailPlayer[] jailPlayers = new JailPlayer[64];
}
