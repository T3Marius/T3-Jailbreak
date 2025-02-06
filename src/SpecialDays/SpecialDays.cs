using CounterStrikeSharp.API.Core;
using static T3Jailbreak.T3Jailbreak;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using static T3Jailbreak.Weapons;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace T3Jailbreak;

public enum SpecialDaysType
{
    ZombieCT, // done
    ZombieT, // done
    OneInTheChamber, // done
    Teleport, // done
    WarDay, // done
    DrunkDay, // done
    HideNSeek, // done
    NoScope, // done
    ArmsRace, // done
    FreeForAll, // done
}

public static class SpecialDays
{
    public static bool isSpecialDayActive = false;
    public static bool isSpecialDayStarted = false;
    public static bool armsrace_PlayerWon = false;
    public static bool isSpecialDayEnded = false;
    private static bool allowGunsDuringCountdown = false;
    public static float hidenseekCountDown;
    private static DateTime lastHideSeekUpdateTime;
    public static bool hidenseekCountDownActive;
    public static bool allowSpecialDay = true;
    private static int roundsSinceLastSpecialDay = 0;

    private static JailAPI JailApi { get; set; } = new JailAPI();

    private static int roundsRequiredForSpecialDay => Instance.Config.SpecialDays.SDRoundsCountdown;
    private static readonly Dictionary<CCSPlayerController, int> PlayerWeaponProgress = new();
    private static readonly Dictionary<CCSPlayerController, bool> ArmsRaceKnifeUsers = new();

    public static SpecialDaysType? activeSpecialDay;
    public static SpecialDaysType? nextSpecialDay;

    public static Timer? SDPrepTimer;
    public static bool isCountdownActive = false;
    private static DateTime lastUpdateTime;
    private static float remainingCountdown = 0f;

    public static Timer? HNSPrepTimer;
    public static bool isHnsCountDownActive = false;
    public static DateTime lastHnsUpdateTime;
    private static float remainingHideCountdown = 0f;

    public static Timer? WarPrepTimer;
    public static bool isWarCountDownActive = false;
    public static DateTime lastWarUpdateTime;
    public static float remainingWarCountdown;

    public static Timer? ZombiePrepTimer;
    public static bool isZombieCountdownActive = false;
    public static DateTime lastZombieUpdateTime;
    public static float remainingZombieCountdown;

    private static readonly string[] allowedSniperWeapons =
    {
        "weapon_awp", "weapon_ssg08", "weapon_g3sg1", "weapon_scar20"
    };
    public static bool cooldownActive()
    {
        return isCountdownActive;
    }
    public static bool NoScopeDayIsActive() => isActive(SpecialDaysType.NoScope);
    public static bool FreeForAllDayIsActive() => isActive(SpecialDaysType.FreeForAll);
    public static bool OneInTheChamberDayIsActive() => isActive(SpecialDaysType.OneInTheChamber);
    public static bool TeleportDayIsActive() => isActive(SpecialDaysType.Teleport);
    public static bool ArmsRaceDayIsActive() => isActive(SpecialDaysType.ArmsRace);
    public static bool HideNSeekDayIsActive() => isActive(SpecialDaysType.HideNSeek);
    public static bool DrunkDayIsActive() => isActive(SpecialDaysType.DrunkDay);
    public static bool WarDayActive() => isActive(SpecialDaysType.WarDay);
    public static bool ZombieDayActive() => isActive(SpecialDaysType.ZombieCT);

    public static bool isActive(SpecialDaysType type)
    {
        return isSpecialDayActive && activeSpecialDay == type;
    }

    private static readonly Dictionary<SpecialDaysType, string> SpecialDaysLocalizerKeys = new Dictionary<SpecialDaysType, string>
    {
        { SpecialDaysType.ZombieCT, "sd.zombie" }, // done
        { SpecialDaysType.OneInTheChamber, "sd.one_in_the_chamber" }, // done
        { SpecialDaysType.Teleport, "sd.teleport" }, // done
        { SpecialDaysType.WarDay, "sd.war" }, // done
        { SpecialDaysType.DrunkDay, "sd.drunk_day" }, // done
        { SpecialDaysType.HideNSeek, "sd.hide_n_seek" }, // done
        { SpecialDaysType.NoScope, "sd.no_scope" }, // done
        { SpecialDaysType.ArmsRace, "sd.arms_race" }, // done
        { SpecialDaysType.FreeForAll, "sd.free_for_all" } // done
    };

    public static string GetLocalizedSpecialDayName(SpecialDaysType type)
    {
        return SpecialDaysLocalizerKeys.TryGetValue(type, out var localizerKey)
            ? Instance.Localizer[localizerKey]
            : type.ToString();
    }

    public static void Load()
    {
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        Instance.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        Instance.RegisterListener<Listeners.OnTick>(OnTick);

        foreach (var cmd in Instance.Config.Commands.SpecialDays)
        {
            Instance.AddCommand($"css_{cmd}", "Opens the Special Days Menu", Command_SpecialDays);
        }
    }
    public static void UnLoad()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }
    public static HookResult OnTakeDamage(DynamicHook hook)
    {

        if (isActive(SpecialDaysType.ArmsRace))
        {
            CEntityInstance entity = hook.GetParam<CEntityInstance>(0);
            CTakeDamageInfo info = hook.GetParam<CTakeDamageInfo>(1);

            var ability = info.Ability?.Value;

            if (ability == null)
            {
                return HookResult.Continue;
            }

            if (entity.DesignerName != "player")
            {
                return HookResult.Continue;
            }
            var attacker = new CCSPlayerController(ability.Handle);
            var victim = new CCSPlayerController(entity.Handle);
        }

        return HookResult.Continue;
    }

    public static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        roundsSinceLastSpecialDay++;
        armsrace_PlayerWon = false;

        if (nextSpecialDay.HasValue)
        {
            activeSpecialDay = nextSpecialDay;
            nextSpecialDay = null;
            StartSpecialDay(activeSpecialDay.Value);

            roundsSinceLastSpecialDay = 0;
        }
        return HookResult.Continue;
    }


    public static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (isSpecialDayActive && activeSpecialDay == SpecialDaysType.ArmsRace)
        {
            var winner = Utilities.GetPlayers().FirstOrDefault(p =>
                PlayerWeaponProgress.TryGetValue(p, out int weaponIndex) &&
                weaponIndex == ArmsRaceWeaponOrder.Count - 1);

            if (winner != null)
            {
                Server.PrintToChatAll(Instance.Localizer["sd.prefix"] + Instance.Localizer["sd.arms_race.winner", winner.PlayerName]);
            }
            else
            {
                Server.PrintToChatAll(Instance.Localizer["sd.prefix"] + Instance.Localizer["sd.arms_race.draw"]);
            }
        }

        if (isSpecialDayActive)
        {
            DeactivateSpecialDay();
            Server.ExecuteCommand("sv_teamid_overhead 1");
            Server.ExecuteCommand("mp_randomspawn 0");
            PlayerWeaponProgress.Clear();
        }
        return HookResult.Continue;
    }

    public static void StartSpecialDay(SpecialDaysType type)
    {
        switch (type)
        {
            case SpecialDaysType.NoScope:
                StartNoScopeDay();
                break;
            case SpecialDaysType.FreeForAll:
                StartFreeForAllDay();
                break;
            case SpecialDaysType.OneInTheChamber:
                StartOneInTheChamberDay();
                break;
            case SpecialDaysType.Teleport:
                StartTeleportDay();
                break;
            case SpecialDaysType.ArmsRace:
                StartArmsRaceDay();
                break;
            case SpecialDaysType.HideNSeek:
                StartHNSDay();
                break;
            case SpecialDaysType.DrunkDay:
                StartDrunkDay();
                break;
            case SpecialDaysType.WarDay:
                StartWarDay();
                break;
                /*
            case SpecialDaysType.ZombieCT:
                StartCTZombieDay();
                break;
            case SpecialDaysType.ZombieT:
                StartTZombieDay();
                break;
                */

        }
    }
    /*
    public static void StartCTZombieDay()
    {
        if (isSpecialDayActive)
            return;

        StartZombieDayCountdown(CsTeam.CounterTerrorist, () =>
        {
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            activeSpecialDay = SpecialDaysType.ZombieCT;


            foreach (var ct in Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive))
            {
                ct.UnFreeze();
            }
        });
    }
    public static void StartTZombieDay()
    {
        if (isSpecialDayActive)
            return;

        StartZombieDayCountdown(CsTeam.Terrorist, () =>
        {
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            activeSpecialDay = SpecialDaysType.ZombieT;

            foreach (var t in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive))
            {
                t.UnFreeze();
            }
        });
    }
    */
    public static void StartWarDay()
    {
        if (isSpecialDayActive)
            return;

        StartWARDayCountdown(() =>
        {
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            activeSpecialDay = SpecialDaysType.WarDay;

            foreach (var p in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive))
            {
                p.UnFreeze();
                Helpers.ForceOpen();
            }
        });
    }
    public static void StartDrunkDay()
    {
        if (isSpecialDayActive)
            return;

        StartSDWithDelay(SpecialDaysType.DrunkDay, () =>
        {
            // da play la sound-ul de drunk day: GO GO GO
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            activeSpecialDay = SpecialDaysType.DrunkDay;

            foreach (var p in Utilities.GetPlayers().Where(p => p.PawnIsAlive))
            {
                p.Drug(1);
            }
            ActivateFriendlyFire();
        });
    }
    public static void StartHNSDay()
    {
        if (isSpecialDayActive)
            return;

        StartHSNDayCountdown(() =>
        {
            var ct = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive);
            var pr = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive);

            foreach (var p in pr)
            {
                p.Freeze();
            }
            foreach (var c in ct)
            {
                c.UnFreeze();
                c.SetHealth(2500);
            }
        });
    }
    public static void StartTeleportDay()
    {
        if (isSpecialDayActive)
            return;

        StartSDWithDelay(SpecialDaysType.Teleport, () =>
        {
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            activeSpecialDay = SpecialDaysType.Teleport;
            ActivateFriendlyFire();

        });
    }
    public static void StartOneInTheChamberDay()
    {
        if (isSpecialDayActive)
            return;

        StartSDWithDelay(SpecialDaysType.OneInTheChamber, () =>
        {
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            ActivateFriendlyFire();

            foreach (var player in Utilities.GetPlayers())
            {
                player.StripWeapons();
                player.GiveNamedItem("weapon_deagle");
                player.GiveNamedItem("weapon_knife");

                var deagle = player.FindWeapon("weapon_deagle");
                if (deagle != null)
                {
                    deagle.SetAmmo(1, 0);
                    SetDeagleDamage(player, 999);
                }
            }
        });
    }
    public static void StartArmsRaceDay()
    {
        if (isSpecialDayActive)
            return;

        StartSDWithDelay(SpecialDaysType.ArmsRace, () =>
        {
            isSpecialDayActive = true;
            Server.ExecuteCommand("mp_randomspawn 1");
            isSpecialDayStarted = true;
            activeSpecialDay = SpecialDaysType.ArmsRace;

            foreach (var player in Utilities.GetPlayers())
            {
                PlayerWeaponProgress[player] = 0;
                player.StripWeaponsFull();
                player.GiveNamedItem(ArmsRaceWeaponOrder[0]);
            }
            ActivateFriendlyFire();
            ActivateRespawn();
        });
    }
    public static void StartNoScopeDay()
    {
        if (isSpecialDayActive)
            return;

        StartSDWithDelay(SpecialDaysType.NoScope, () =>
        {
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            ActivateFriendlyFire();

            string selectedWeapon = allowedSniperWeapons[new Random().Next(allowedSniperWeapons.Length)];
            foreach (var player in Utilities.GetPlayers())
            {
                Instance.AddTimer(0.5f, () =>
                {
                    player.GiveNamedItem(selectedWeapon);
                });
            }

            LRHelper.StartSDNoScope();
        });
    }
    public static void StartFreeForAllDay()
    {
        if (isSpecialDayActive)
            return;

        StartSDWithDelay(SpecialDaysType.FreeForAll, () =>
        {
            isSpecialDayActive = true;
            isSpecialDayStarted = true;
            activeSpecialDay = SpecialDaysType.FreeForAll;

            ActivateFriendlyFire();

            foreach (var player in Utilities.GetPlayers())
            {

            }
        });
    }
    private static void SetDeagleDamage(CCSPlayerController player, int damage)
    {
        var weapon = player.Pawn?.Value?.WeaponServices?.ActiveWeapon;
        if (weapon == null || weapon.Value == null)
            return;

        if (weapon.Value.DesignerName.ToLower() != "weapon_deagle")
            return;

        var vData = weapon.Get()?.As<CCSWeaponBase>().VData;
        if (vData == null)
            return;

        vData.Damage = damage;
    }
    public static void Command_SpecialDays(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;
        var days = Instance.Config.SpecialDays;
        if (roundsSinceLastSpecialDay < roundsRequiredForSpecialDay)
        {
            int roundsLeft = roundsRequiredForSpecialDay - roundsSinceLastSpecialDay;
            info.ReplyToCommand(Instance.Localizer["sd.prefix"] + Instance.Localizer["special_day.cooldown", roundsLeft.ToString()]);
            return;
        }

        bool hasPermission = Instance.Config.SpecialDays.AdminPermissions.Any(permission =>
            AdminManager.PlayerHasPermissions(player, permission));
        bool isSimon = jailApi.IsSimon(player);

        if (!hasPermission && !isSimon)
        {
            info.ReplyToCommand(Instance.Localizer["special_day.no_permission"]);
            return;
        }

        if (isSpecialDayActive || nextSpecialDay.HasValue)
        {
            return;
        }

        var manager = Instance.GetMenuManager();
        if (manager == null)
            return;

        var specialDayMenu = manager.CreateMenu(Instance.Localizer["special_day<menu>"], isSubMenu: false);

        if (days.Type.NoScope)
        {
            specialDayMenu.Add(Instance.Localizer["sd.noscope<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.NoScope);
                manager.CloseMenu(player);
            });
        }
        if (days.Type.FreeForAll)
        {
            specialDayMenu.Add(Instance.Localizer["sd.ffa<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.FreeForAll);
                manager.CloseMenu(p);
            });
        }
        if (days.Type.OneInTheChamber)
        {
            specialDayMenu.Add(Instance.Localizer["sd.oitc<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.OneInTheChamber);
                manager.CloseMenu(p);
            });
        }
        if (days.Type.Teleport)
        {
            specialDayMenu.Add(Instance.Localizer["sd.teleport<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.Teleport);
                manager.CloseMenu(p);
            });
        }
        if (days.Type.ArmsRace)
        {
            specialDayMenu.Add(Instance.Localizer["sd.armsrace<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.ArmsRace);
                manager.CloseMenu(p);
            });
        }
        if (days.Type.HideAndSeek)
        {
            specialDayMenu.Add(Instance.Localizer["sd.hidenseek<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.HideNSeek);
                manager.CloseMenu(p);
            });
        }
        if (days.Type.DrunkDay)
        {
            specialDayMenu.Add(Instance.Localizer["sd.drunk<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.DrunkDay);
                manager.CloseMenu(p);
            });
        }
        if (days.Type.WarDay)
        {
            specialDayMenu.Add(Instance.Localizer["sd.war<option>"], (p, option) =>
            {
                ActivateSpecialDay(SpecialDaysType.WarDay);
                manager.CloseMenu(p);
            });
        }
        /*if (days.Type.ZombieDay)
        {
            specialDayMenu.Add(Instance.Localizer["sd.zombie<option>"], (p, option) =>
            {
                var subMenu = manager.CreateMenu(Instance.Localizer["sd.zombie<selectteam>"], isSubMenu: true);
                subMenu.ParentMenu = specialDayMenu;

                subMenu.Add("Prizonieri", (p, opiton) =>
                {
                    ActivateSpecialDay(SpecialDaysType.ZombieT);
                    manager.CloseMenu(p);
                });
                subMenu.Add("Gardieni", (p, option) =>
                {
                    ActivateSpecialDay(SpecialDaysType.ZombieCT);
                    manager.CloseMenu(p);
                });

                manager.OpenSubMenu(p, subMenu);
            });
        }
        */
        manager.OpenMainMenu(player, specialDayMenu);
    }
    public static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !ArmsRaceDayIsActive())
            return HookResult.Continue;

        if (PlayerWeaponProgress.TryGetValue(player, out int weaponIndex))
        {
            if (weaponIndex >= 0 && weaponIndex < ArmsRaceWeaponOrder.Count)
            {
                player.StripWeaponsFull();
                player.GiveNamedItem(ArmsRaceWeaponOrder[weaponIndex]);

                // Check if the weapon is a knife and update the flag
                ArmsRaceKnifeUsers[player] = ArmsRaceWeaponOrder[weaponIndex] == "weapon_knife";
            }
        }
        else
        {
            PlayerWeaponProgress[player] = 0;
            player.StripWeaponsFull();
            player.GiveNamedItem(ArmsRaceWeaponOrder[0]);

            // Ensure the knife flag is cleared on spawn
            ArmsRaceKnifeUsers[player] = false;
        }

        return HookResult.Continue;
    }

    public static HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        if (victim == null || attacker == null)
            return HookResult.Continue;

        var victimPos = victim.PlayerPawn.Value?.AbsOrigin;
        var attackerPos = attacker.PlayerPawn.Value?.AbsOrigin;

        if (isSpecialDayActive)
        {
            var activeWeapon = attacker.Pawn?.Value?.WeaponServices?.ActiveWeapon;
            var vData = activeWeapon?.Get()?.As<CCSWeaponBase>().VData;

            if (vData == null)
                return HookResult.Continue;

            if (isActive(SpecialDaysType.NoScope))
            {
                var allowedWeapons = new[] { "weapon_awp", "weapon_ssg08", "weapon_g3sg1", "weapon_scar20" };
                if (!allowedWeapons.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                {
                    victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                    Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                    victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                    Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                }
            }
            else if (isActive(SpecialDaysType.OneInTheChamber))
            {
                var allowedWeapon = new[] { "weapon_knife", "weapon_deagle" };
                if (!allowedWeapon.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                {
                    victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                    Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                    victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                    Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                }
            }

            else if (isActive(SpecialDaysType.Teleport))
            {

                if (victimPos != null && attackerPos != null)
                {
                    var tempLocationVictim = new Vector(victimPos.X, victimPos.Y, victimPos.Z);
                    var tempLocationAttacker = new Vector(attackerPos.X, attackerPos.Y, attackerPos.Z);

                    victim.PlayerPawn.Value?.Teleport(tempLocationAttacker, null);
                    attacker.PlayerPawn.Value?.Teleport(tempLocationVictim, null);
                }
            }
            else if (isActive(SpecialDaysType.ZombieCT))
            {
                var allowedWeapon = new[] { "weapon_knife" };

                if (attacker.Team == CsTeam.CounterTerrorist)
                {
                    if (!allowedWeapon.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                    }
                }
            }
            else if (isActive(SpecialDaysType.ZombieT))
            {
                var allowedWeapon = new[] { "weapon_knife" };

                if (attacker.Team == CsTeam.Terrorist)
                {
                    if (!allowedWeapon.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                    }
                }
            }
            else if (isCountdownActive)
            {
                victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
            }
        }
        return HookResult.Continue;
    }
    public static void OnTick()
    {
        if (isCountdownActive && SDPrepTimer != null)
        {
            if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
            {
                remainingCountdown = Math.Max(0, remainingCountdown - 1);
                lastUpdateTime = DateTime.Now;
            }

            if (remainingCountdown > 0)
            {
                string countdownMessage = Instance.Localizer["sd.countdown",
                    activeSpecialDayName,
                    remainingCountdown.ToString("0")];

                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterHtml(countdownMessage);
                }
            }
            else
            {
                isCountdownActive = false;
                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterHtml(" ");
                }
            }
        }

        if (isHnsCountDownActive && HNSPrepTimer != null)
        {
            if ((DateTime.Now - lastHnsUpdateTime).TotalSeconds >= 1)
            {
                remainingHideCountdown = Math.Max(0, remainingHideCountdown - 1);
                lastHnsUpdateTime = DateTime.Now;
            }

            if (remainingHideCountdown > 0)
            {
                string countdownMessage = Instance.Localizer["hns.prep.time",
                    remainingHideCountdown.ToString("0")];

                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterHtml(countdownMessage);
                }
            }
            else
            {
                isHnsCountDownActive = false;
                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterHtml(" ");
                }
            }
        }
        if (isWarCountDownActive && WarPrepTimer != null)
        {
            if ((DateTime.Now - lastWarUpdateTime).TotalSeconds >= 1)
            {
                remainingWarCountdown = Math.Max(0, remainingWarCountdown - 1);
                lastWarUpdateTime = DateTime.Now;
            }

            if (remainingWarCountdown > 0)
            {
                string countdownMessage = Instance.Localizer["war.prep.time",
                   remainingWarCountdown.ToString("0")];

                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterHtml(countdownMessage);
                }
            }
            else
            {
                isWarCountDownActive = false;
                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterHtml(" ");
                }
            }
        }
    }
    public static void ActivateSpecialDay(SpecialDaysType type)
    {
        if (isSpecialDayActive || nextSpecialDay.HasValue)
            return;

        nextSpecialDay = type;
        string sdName = GetLocalizedSpecialDayName(type);
        Server.PrintToChatAll(Instance.Localizer["sd.prefix"] + Instance.Localizer["special_day.next", sdName]);
        foreach (var p in Utilities.GetPlayers())
        {
            p.PrintToCenter(Instance.Localizer["special_day.next.center", sdName]);
        }
    }

    public static void DeactivateSpecialDay()
    {
        isSpecialDayActive = false;
        isSpecialDayEnded = true;
        isSpecialDayStarted = false;
        activeSpecialDay = null;

        DisableDrug();
        DeactivateFriendlyFire();
        DezactivateRespawn();

    }
    public static HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        if (victim == null || attacker == null)
            return HookResult.Continue;

        var activeWeapon = attacker.Pawn?.Value?.WeaponServices?.ActiveWeapon?.Get()?.As<CCSWeaponBase>();
        string? weaponName = activeWeapon?.DesignerName.ToLower();

        if (isActive(SpecialDaysType.OneInTheChamber))
        {
            if (weaponName == "weapon_deagle")
            {
                var deagle = attacker.FindWeapon("weapon_deagle");
                if (deagle != null)

                    deagle.SetAmmo(deagle.Clip1 + 1, deagle.Clip2);
            }

            else if (weaponName == "weapon_knife")
            {
                var deagle = attacker.FindWeapon("weapon_deagle");
                if (deagle != null)
                    deagle.SetAmmo(deagle.Clip1 + 1, deagle.Clip2);
            }
        }
        else if (isActive(SpecialDaysType.ArmsRace))
        {
            if (PlayerWeaponProgress.TryGetValue(attacker, out int attackerWeaponIndex))
            {
                if (attackerWeaponIndex + 1 < ArmsRaceWeaponOrder.Count)
                {
                    PlayerWeaponProgress[attacker] = attackerWeaponIndex + 1;
                    attacker.StripWeaponsFull();
                    attacker.GiveNamedItem(ArmsRaceWeaponOrder[attackerWeaponIndex + 1]);

                    if (ArmsRaceWeaponOrder[attackerWeaponIndex + 1] == "weapon_knife")
                    {
                        var knife = attacker.FindWeapon("weapon_knife");
                        if (knife != null)
                        {
                            Server.NextFrame(() =>
                            {
                                UpdateModel(attacker, knife, Instance.Config.Models.ArmsRaceKnifeModel, true);
                                Instance.Logger.LogInformation($"[DEBUG] Knife model set to: {Instance.Config.Models.ArmsRaceKnifeModel}");
                            });
                            ArmsRaceKnifeUsers[attacker] = true;
                        }
                        else
                        {
                            Instance.Logger.LogInformation("[ERROR] Arms Race - Knife weapon not found for attacker.");
                        }
                    }
                    else
                    {
                        ArmsRaceKnifeUsers[attacker] = false;
                    }
                }
                else
                {
                    if (ArmsRaceWeaponOrder[attackerWeaponIndex] == "weapon_knife")
                    {
                        armsrace_PlayerWon = true;
                        EndRound();
                        return HookResult.Continue;
                    }
                }
            }
            else
            {
                PlayerWeaponProgress[attacker] = 0;
                attacker.StripWeaponsFull();
                attacker.GiveNamedItem(ArmsRaceWeaponOrder[0]);
            }
        }
        return HookResult.Continue;
    }
    public static void StartHSNDayCountdown(Action startsdAction)
    {
        remainingHideCountdown = Instance.Config.SpecialDays.HideTimer;
        isHnsCountDownActive = true;
        lastHideSeekUpdateTime = DateTime.Now;
        Helpers.ForceOpen();

        var ct = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive);

        foreach (var c in ct)
        {
            c.Freeze();
        }
        HNSPrepTimer = Instance.AddTimer(remainingHideCountdown, () =>
        {
            isSpecialDayStarted = true;

            startsdAction.Invoke();
            HNSPrepTimer?.Kill();
            HNSPrepTimer = null;
            isHnsCountDownActive = false;
        });
    }
    public static void StartWARDayCountdown(Action startsdAction)
    {
        remainingWarCountdown = Instance.Config.SpecialDays.WarTimer;
        isWarCountDownActive = true;
        lastWarUpdateTime = DateTime.Now;
        var ps = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive);

        foreach (var t in ps)
        {
            t.Freeze();
        }

        WarPrepTimer = Instance.AddTimer(remainingWarCountdown, () =>
        {
            isSpecialDayStarted = true;

            startsdAction.Invoke();
            WarPrepTimer?.Kill();
            WarPrepTimer = null;
            isWarCountDownActive = false;
        });
    }
    /*
    public static void StartZombieDayCountdown(CsTeam zombieTeam, Action startsdAction)
    {
        remainingZombieCountdown = Instance.Config.SpecialDays.ZombieTimer;
        isZombieCountdownActive = true;
        lastZombieUpdateTime = DateTime.Now;
        var zombie = Utilities.GetPlayers().Where(p => p.Team == zombieTeam && p.PawnIsAlive);

        Helpers.ForceOpen();

        foreach (var zm in zombie)
        {
            zm.Freeze();
            Server.NextFrame(() =>
            {
                if (!string.IsNullOrEmpty(Instance.Config.SpecialDays.ZombieModel))
                {
                    zm.SetModel(Instance.Config.SpecialDays.ZombieModel);
                };
            });
            zm.SetHealth(Instance.Config.SpecialDays.ZombieHealth);
        }

        ZombiePrepTimer = Instance.AddTimer(remainingZombieCountdown, () =>
        {
            isSpecialDayStarted = true;
            startsdAction.Invoke();
            ZombiePrepTimer?.Kill();
            ZombiePrepTimer = null;
            isZombieCountdownActive = false;
        });
    }
    */
    private static string activeSpecialDayName = "";
    private static void StartSDWithDelay(SpecialDaysType type, Action startSdAction)
    {
        remainingCountdown = Instance.Config.SpecialDays.SdStartTimer;
        isCountdownActive = true;
        if (nextSpecialDay == SpecialDaysType.FreeForAll)
        {
            activeSpecialDay = SpecialDaysType.FreeForAll;
        }
        if (nextSpecialDay == SpecialDaysType.Teleport)
        {
            activeSpecialDay = SpecialDaysType.Teleport;
        }
        if (nextSpecialDay == SpecialDaysType.DrunkDay)
        {
            activeSpecialDay = SpecialDaysType.DrunkDay;
        }

        Server.ExecuteCommand("sv_teamid_overhead 0");
        activeSpecialDayName = GetLocalizedSpecialDayName(type);
        lastUpdateTime = DateTime.Now;
        Helpers.ForceOpen();
        foreach (var p in Utilities.GetPlayers())
        {
            p.GiveNamedItem("weapon_revolver");
            Instance.AddTimer(0.8f, () =>
            {
                p.StripWeaponsFull();
            });
        }

        SDPrepTimer = Instance.AddTimer(remainingCountdown, () =>
        {
            startSdAction.Invoke();

            isSpecialDayStarted = true;

            SDPrepTimer?.Kill();
            SDPrepTimer = null;
            isCountdownActive = false;
        });
    }
    private static void ActivateFriendlyFire()
    {
        ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() = true;
    }

    private static void DeactivateFriendlyFire()
    {
        ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() = false;
    }
    private static void ActivateRespawn()
    {
        ConVar.Find("mp_respawn_on_death_t")?.SetValue(true);
        ConVar.Find("mp_respawn_on_death_ct")?.SetValue(true);
    }
    private static void DezactivateRespawn()
    {
        ConVar.Find("mp_respawn_on_death_t")?.SetValue(false);
        ConVar.Find("mp_respawn_on_death_ct")?.SetValue(false);
    }
    private static void DisableDrug()
    {
        foreach (var p in Utilities.GetPlayers().Where(p => p.PawnIsAlive))
        {
            p.KillDrug();
        }
    }
    private static void EndRound()
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
        if (gameRules == null)
            return;

        RoundEndReason endReason = RoundEndReason.RoundDraw;

        gameRules.TerminateRound(5.0f, endReason);
    }
}