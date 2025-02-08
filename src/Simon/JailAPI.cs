using CounterStrikeSharp.API;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using JailAPI;
using Microsoft.Extensions.Logging;
using static T3Jailbreak.T3Jailbreak;
using static T3Jailbreak.Helpers;
using static T3Jailbreak.Simon;

namespace T3Jailbreak;

public class JailAPI : IJailAPI
{
    public void SetSimon(int slot)
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
        if (!string.IsNullOrEmpty(Instance.Config.Simon.SimonColor))
        {
            player.SetColor(Color.FromName(Instance.Config.Simon.SimonColor));
        }
        UpdateSimonHud();

        if (Instance.Config.Prisoniers.MuteXSecondsOnSimonSet > 0)
        {
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["t.muted.new.simon", Instance.Config.Prisoniers.MuteXSecondsOnSimonSet]);
            foreach (var t in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist))
            {
                foreach (var flag in Instance.Config.Prisoniers.SkipFlagForMute)
                {
                    if (AdminManager.PlayerHasPermissions(t, flag))
                    {
                        t.VoiceFlags = VoiceFlags.Normal;
                    }
                    else
                    {
                        t.VoiceFlags = VoiceFlags.Muted;
                    }
                }

                Instance.AddTimer(Instance.Config.Prisoniers.MuteXSecondsOnSimonSet, () =>
                {
                    t.VoiceFlags = VoiceFlags.Normal;
                    Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["t.can.talk"]);
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }
    }
    public CCSPlayerController? GetDeputy()
    {
        if (deputySlot == INVALID_SLOT)
        {
            return null;
        }

        return Utilities.GetPlayerFromSlot(deputySlot);
    }

    public void RemoveSimonInterval()
    {
        simonSlot = INVALID_SLOT;
        simonTimestamp = -1;
    }

    public void RemoveSimon()
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

    public void RemoveDeputy()
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

    public void RemoveIfSimon(CCSPlayerController? player)
    {
        if (IsSimon(player))
        {
            RemoveSimon();
        }
    }
    public void RemoveIfDeputy(CCSPlayerController? player)
    {
        if (IsDeputy(player))
        {
            RemoveDeputy();
        }
    }

    public CCSPlayerController? GetSimon()
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


    public void SetDeputy(int slot)
    {
        deputySlot = slot;
        var player = Utilities.GetPlayerFromSlot(deputySlot);
        if (player == null || !player.IsValid)
        {
            deputySlot = INVALID_SLOT;
            return;
        }

        if (IsSimon(player))
        {
            return;
        }
        if (!string.IsNullOrEmpty(Instance.Config.Models.DeputyModel))
        {
            player.SetModel(Instance.Config.Models.DeputyModel);
            player.SetTag("⭐Deputy⭐");
        }
    }
    public bool IsSimon(CCSPlayerController? player)
    {
        if (player == null || !player.PawnIsAlive)
        {
            return false;
        }

        return player.Slot == simonSlot;
    }
    public bool IsDeputy(CCSPlayerController? player)
    {
        if (player == null || !player.PawnIsAlive)
        {
            return false;
        }

        return player.Slot == deputySlot;
    }



    public const int INVALID_SLOT = -3;
    int colorSlot = -1;
    static long simonTimestamp = -1;
    public static int simonSlot = INVALID_SLOT;
    public static int deputySlot = INVALID_SLOT;
    public static JailPlayer[] jailPlayers = new JailPlayer[64];
}