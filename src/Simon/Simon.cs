using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;
using static T3Jailbreak.Helpers;
using CounterStrikeSharp.API.Modules.Utils;
using static T3Jailbreak.T3Jailbreak;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace T3Jailbreak;

public partial class Simon
{
    public static JailAPI jailApi { get; set; } = new JailAPI();
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
        foreach (var p in Utilities.GetPlayers().Where(p => jailApi.IsSimon(p)))
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

        if (!jailApi.IsSimon(simon))
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
        foreach (var simon in Utilities.GetPlayers().Where(p => jailApi.IsSimon(p) && p.IsValid && p.PawnIsAlive))
        {
            string simonSteamId = simon.SteamID.ToString();

            if ((simon.Buttons & PlayerButtons.Attack2) != 0)
            {
                if (!HeldPrisoners.ContainsKey(simonSteamId))
                {
                    var prisoner = FindClosestFrozenPrisoner(simon);

                    if (prisoner != null)
                    {
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

        if (!jailApi.IsSimon(attacker))
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

        if (jailApi.IsSimon(player))
        {
            var deputy = jailApi.GetDeputy();
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
                jailApi.SetSimon(deputy.Slot);
            }
            else
            {
                var cts = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.IsValid && p.PawnIsAlive).ToList();
                if (cts.Count > 0)
                {
                    Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.death", attacker.PlayerName]);
                    foreach (var p in Utilities.GetPlayers())
                    {
                        p.PlaySound(Instance.Config.Sounds.SimonDeathSound);
                    }

                    var newSimon = cts[new Random().Next(cts.Count)];

                    Instance.AddTimer(Instance.Config.Simon.SetSimonIfNotAny, () =>
                    {
                        var simon = jailApi.GetSimon();
                        if (jailApi.IsSimon(simon))
                            return;

                        if (newSimon.IsValid && newSimon.PawnIsAlive)
                        {
                            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.death.selected", newSimon.PlayerName]);
                            jailApi.SetSimon(newSimon.Slot);
                        }
                    });
                }
            }
        }

        return HookResult.Continue;
    }

    public static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        jailApi.RemoveSimonInterval();
        jailApi.RemoveDeputy();
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
            var currentSimon = jailApi.GetSimon();
            if (!jailApi.IsSimon(currentSimon))
            {
                var cts = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.IsValid && p.PawnIsAlive).ToList();
                if (cts.Count > 0)
                {
                    var newSimon = cts[new Random().Next(cts.Count)];
                    Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.selected", newSimon.PlayerName]);
                    jailApi.SetSimon(newSimon.Slot);
                }
            }
        });

        return HookResult.Continue;
    }

    public static JailPlayer? JailPlayerFromPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
        {
            return null;
        }

        return jailPlayers[player.Slot];
    }

    public static void UpdateSimonHud()
    {
        try
        {
            var currentSimon = jailApi.GetSimon();
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
