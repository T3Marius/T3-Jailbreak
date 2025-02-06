using CounterStrikeSharp.API;
using static T3Jailbreak.T3Jailbreak;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static T3Jailbreak.LRHelper;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;
using JailAPI;

namespace T3Jailbreak
{
    public enum LastRequestType
    {
        KnifeFight,// done
        ShotForShot, // done
        MagForMag, // done
        Rebel, // done
        OnlyHeadshot,
        NoScope, // done
        Dodgeball// done
    }
    public static class LastRequest
    {
        public static Timer? LrPrepTimer { get; set; }
        private static bool isCountdownActive = false;
        private static DateTime lastUpdateTime;
        private static float remainingCountdown = 0f;

        private static JailAPI jailApi { get; set; } = new JailAPI();

        public static bool isLrActive = false;
        public static bool isLrEnded = false;
        public static bool isLrStarted = false;
        public static LastRequestType? activeLastRequest;
        public static string? activeKnifeFightEffect;

        private static Dictionary<CCSPlayerController, int> playerAmmo = new Dictionary<CCSPlayerController, int>();
        public static CCSPlayerController? terrorist;
        public static CCSPlayerController? ct;
        public static CCSPlayerController? currentShooter;
        private static Line? laserline;
        public static void ActivateLastRequest(LastRequestType type)
        {
            isLrActive = true;
            isLrStarted = true;
            activeLastRequest = type;
        }
        public static void DeactivateLastRequest()
        {
            isLrActive = false;
            isLrStarted = false;
            activeLastRequest = null;
        }
        public static bool isActive(LastRequestType type)
        {
            return isLrActive && activeLastRequest == type;
        }
        public static void Load()
        {
            Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            Instance.RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
            Instance.RegisterListener<Listeners.OnTick>(OnTick);
            Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            Instance.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
            var config = Instance.Config.Commands;
            var AddCmd = Instance.AddCommand;

            foreach (var cmd in config.LastRequest)
            {
                AddCmd($"css_{cmd}", "Last Request", Command_LastRequest);
            }
        }
        public static void ResetLrState()
        {
            isLrActive = false;
            isLrEnded = false;
            isLrStarted = false;
        }
        public static void StartDodgeballLR(CCSPlayerController terrorist, CCSPlayerController ct, string ctName)
        {
            StartLrWithDelay(terrorist, ct, LastRequestType.Dodgeball, () =>
            {
                isLrActive = true;
                isLrStarted = true;
                LastRequest.terrorist = terrorist;
                LastRequest.ct = ct;
                ActivateLastRequest(LastRequestType.Dodgeball);

                if (laserline == null)
                    laserline = new Line();

                terrorist.SetHealth(1);
                ct.SetHealth(1);

                Server.ExecuteCommand("sv_cheats 1");
                Server.ExecuteCommand("sv_infinite_ammo 1");

                terrorist.GiveWeapon("decoy");
                ct.GiveWeapon("decoy");

                string lrName = GetLocalizedLastRequestName(LastRequestType.Dodgeball);
                string terroristName = terrorist.PlayerName;

                ct.AddTimer(TimerFlag.Beacon, Instance.AddTimer(2.0f, () => ct.Beacon(), TimerFlags.REPEAT));
                terrorist.AddTimer(TimerFlag.Beacon, Instance.AddTimer(2.0f, () => terrorist.Beacon(), TimerFlags.REPEAT));

                Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.started.with.no.type", terroristName, lrName, ctName]);
            });
        }
        public static void StartOnlyHeadShot(CCSPlayerController terrorist, CCSPlayerController ct, string choice, string ctName)
        {
            StartLrWithDelay(terrorist, ct, LastRequestType.OnlyHeadshot, () =>
            {
                isLrActive = true;
                isLrStarted = true;
                LastRequest.terrorist = terrorist;
                LastRequest.ct = ct;
                ActivateLastRequest(LastRequestType.OnlyHeadshot);
                string weaponRestrict = "";

                terrorist.SetHealth(100);
                ct.SetHealth(100);

                switch (choice)
                {
                    case "ak47":
                        weaponRestrict = "ak47";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "deagle":
                        weaponRestrict = "deagle";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "awp":
                        weaponRestrict = "awp";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "ssg08":
                        weaponRestrict = "ssg08";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "fiveseven":
                        weaponRestrict = "fiveseven";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "tec9":
                        weaponRestrict = "tec9";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                }
                terrorist.GiveWeapon(weaponRestrict);
                ct.GiveWeapon(weaponRestrict);

                string lrName = GetLocalizedLastRequestName(LastRequestType.OnlyHeadshot);
                string terroristName = terrorist.PlayerName;

                ct.AddTimer(TimerFlag.Beacon, Instance.AddTimer(2.0f, () => ct.Beacon(), TimerFlags.REPEAT));
                terrorist.AddTimer(TimerFlag.Beacon, Instance.AddTimer(2.0f, () => terrorist.Beacon(), TimerFlags.REPEAT));

                Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.started.with", terroristName, lrName, choice, ctName]);

            });
        }
        public static void StartNoScopeLR(CCSPlayerController terrorist, CCSPlayerController ct, string choice, string ctName)
        {
            StartLrWithDelay(terrorist, ct, LastRequestType.NoScope, () =>
            {
                isLrActive = true;
                isLrStarted = true;
                LastRequest.terrorist = terrorist;
                LastRequest.ct = ct;
                ActivateLastRequest(LastRequestType.NoScope);
                string weaponRestrict = "";

                terrorist.SetHealth(100);
                ct.SetHealth(100);

                switch (choice)
                {
                    case "awp":
                        weaponRestrict = "awp";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "ssg08":
                        weaponRestrict = "ssg08";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "g3sg1":
                        weaponRestrict = "g3sg1";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "scar20":
                        weaponRestrict = "scar20";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                }
                terrorist.GiveWeapon(weaponRestrict);
                ct.GiveWeapon(weaponRestrict);

                StartNoScope();

                string lrName = GetLocalizedLastRequestName(LastRequestType.NoScope);
                string terroristName = terrorist.PlayerName;

                ct.AddTimer(TimerFlag.Beacon, Instance.AddTimer(2.0f, () => ct.Beacon(), TimerFlags.REPEAT));
                terrorist.AddTimer(TimerFlag.Beacon, Instance.AddTimer(2.0f, () => terrorist.Beacon(), TimerFlags.REPEAT));

                Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.started.with", terroristName, lrName, choice, ctName]);
            });
        }
        public static void StartShotForShotLR(CCSPlayerController terrorist, CCSPlayerController ct, string choice, string ctName)
        {
            StartLrWithDelay(terrorist, ct, LastRequestType.ShotForShot, () =>
            {
                isLrActive = true;
                isLrStarted = true;
                LastRequest.terrorist = terrorist;
                LastRequest.ct = ct;
                ActivateLastRequest(LastRequestType.ShotForShot);
                string weaponRestrict = "";

                switch (choice)
                {
                    case "deagle":
                        weaponRestrict = "deagle";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "p250":
                        weaponRestrict = "p250";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "revolver":
                        weaponRestrict = "revolver";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "usp_silencer":
                        weaponRestrict = "usp_silencer";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "hkp2000":
                        weaponRestrict = "hkp2000";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "tec9":
                        weaponRestrict = "tec9";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "fiveseven":
                        weaponRestrict = "fiveseven";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "glock":
                        weaponRestrict = "glock";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                    case "elite":
                        weaponRestrict = "elite";
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;
                }

                terrorist.StripWeaponsFull();
                ct.StripWeaponsFull();

                terrorist.GiveWeapon(weaponRestrict);
                ct.GiveWeapon(weaponRestrict);

                var terroristWeapon = terrorist.FindWeapon($"weapon_{weaponRestrict}");
                var ctWeapon = ct.FindWeapon($"weapon_{weaponRestrict}");

                if (terroristWeapon != null)
                    terroristWeapon.SetAmmo(0, 0);
                if (ctWeapon != null)
                    ctWeapon.SetAmmo(0, 0);

                currentShooter = PickRandomShooter(terrorist, ct);
                ReloadClip(currentShooter, 1);

                string lrName = GetLocalizedLastRequestName(LastRequestType.ShotForShot);
                string terroristName = terrorist.PlayerName;

                ct.AddTimer(LRHelper.TimerFlag.Beacon, Instance.AddTimer(2.0f, () => ct.Beacon(), TimerFlags.REPEAT));
                terrorist.AddTimer(LRHelper.TimerFlag.Beacon, Instance.AddTimer(2.0f, () => terrorist.Beacon(), TimerFlags.REPEAT));

                Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.started.with", terroristName, lrName, choice, ctName]);
            });
        }
        public static void StartMagForMagLR(CCSPlayerController terrorist, CCSPlayerController ct, string choice, string ctName)
        {
            StartLrWithDelay(terrorist, ct, LastRequestType.MagForMag, () =>
            {
                isLrActive = true;
                isLrStarted = true;
                LastRequest.terrorist = terrorist;
                LastRequest.ct = ct;
                ActivateLastRequest(LastRequestType.MagForMag);
                string weaponRestrict = "";

                int clipSize = 0; // Default clip size

                switch (choice)
                {
                    case "deagle":
                        weaponRestrict = "deagle";
                        clipSize = 7;
                        break;
                    case "p250":
                        weaponRestrict = "p250";
                        clipSize = 13;
                        break;
                    case "usp_silencer":
                        weaponRestrict = "usp_silencer";
                        clipSize = 12;
                        break;
                    case "hkp2000":
                        weaponRestrict = "hkp2000";
                        clipSize = 13;
                        break;
                    case "tec9":
                        weaponRestrict = "tec9";
                        clipSize = 18;
                        break;
                    case "fiveseven":
                        weaponRestrict = "fiveseven";
                        clipSize = 20;
                        break;
                    case "glock":
                        weaponRestrict = "glock";
                        clipSize = 20;
                        break;
                    case "elite":
                        weaponRestrict = "elite";
                        clipSize = 30;
                        break;
                    default:
                        Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + "Invalid weapon choice!");
                        return;
                }

                terrorist.StripWeaponsFull();
                ct.StripWeaponsFull();

                terrorist.GiveWeapon(weaponRestrict);
                ct.GiveWeapon(weaponRestrict);

                var terroristWeapon = terrorist.FindWeapon($"weapon_{weaponRestrict}");
                var ctWeapon = ct.FindWeapon($"weapon_{weaponRestrict}");

                if (terroristWeapon != null)
                    terroristWeapon.SetAmmo(0, 0); // Start with empty clip
                if (ctWeapon != null)
                    ctWeapon.SetAmmo(0, 0); // Start with empty clip

                // Pick a random shooter to start with a full clip
                currentShooter = PickRandomShooter(terrorist, ct);
                ReloadClip(currentShooter, clipSize);

                string lrName = GetLocalizedLastRequestName(LastRequestType.MagForMag);
                string terroristName = terrorist.PlayerName;

                ct.AddTimer(LRHelper.TimerFlag.Beacon, Instance.AddTimer(2.0f, () => ct.Beacon(), TimerFlags.REPEAT));
                terrorist.AddTimer(LRHelper.TimerFlag.Beacon, Instance.AddTimer(2.0f, () => terrorist.Beacon(), TimerFlags.REPEAT));

                Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.started.with", terroristName, lrName, choice, ctName]);
            });
        }
        public static void StartRebelLR(CCSPlayerController terrorist, IEnumerable<CCSPlayerController> cts)
        {
            LastRequest.terrorist = terrorist;
            ActivateLastRequest(LastRequestType.Rebel);

            JailPlayer.playerRebel.Add(terrorist);
            terrorist.StripWeapons();
            terrorist.GiveNamedItem("weapon_negev");
            terrorist.SetHealth(500);
            terrorist.SetColor(Color.Red);

            string lrName = GetLocalizedLastRequestName(LastRequestType.Rebel);
            string terroristName = terrorist.PlayerName;

            Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.started.rebel", terroristName, lrName]);

            foreach (var ct in cts)
            {
                if (ct.PawnIsAlive && ct.Team == CsTeam.CounterTerrorist)
                {
                    ct.SetHealth(100);
                }
            }
        }
        public static void StartKnifeFightLR(CCSPlayerController terrorist, CCSPlayerController ct, string effect, string ctName)
        {
            StartLrWithDelay(terrorist, ct, LastRequestType.KnifeFight, () =>
            {
                isLrStarted = true;
                isLrActive = true;
                LastRequest.terrorist = terrorist;
                LastRequest.ct = ct;
                ActivateLastRequest(LastRequestType.KnifeFight);

                activeKnifeFightEffect = effect;

                switch (effect)
                {
                    case "speed":
                        Speed(terrorist, ct);
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        DisableCollision(terrorist);
                        DisableCollision(ct);
                        break;

                    case "gravity":
                        Gravity(terrorist, ct);
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        DisableCollision(terrorist);
                        DisableCollision(ct);
                        break;

                    case "drug":
                        Drug(terrorist, ct);
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;

                    case "vanilla":
                        terrorist.SetHealth(100);
                        ct.SetHealth(100);
                        break;

                    case "onehit":
                        terrorist.SetHealth(1);
                        ct.SetHealth(1);
                        break;
                }
                terrorist.GiveNamedItem("weapon_knife");
                ct.GiveNamedItem("weapon_knife");
                string lrName = GetLocalizedLastRequestName(LastRequestType.KnifeFight);
                string terroristName = terrorist.PlayerName;

                ct.AddTimer(LRHelper.TimerFlag.Beacon, Instance.AddTimer(2.0f, () => ct.Beacon(), TimerFlags.REPEAT));
                terrorist.AddTimer(LRHelper.TimerFlag.Beacon, Instance.AddTimer(2.0f, () => terrorist.Beacon(), TimerFlags.REPEAT));

                Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.started.with", terroristName, lrName, effect, ctName]);
            });
        }
        private static readonly Dictionary<LastRequestType, string> LastRequestLocalizerKeys = new Dictionary<LastRequestType, string>
        {
            { LastRequestType.KnifeFight, "lr.knife_fight" },
            { LastRequestType.ShotForShot, "lr.shot_for_shot" },
            { LastRequestType.MagForMag, "lr_mag_for_mag" },
            { LastRequestType.Rebel, "lr_rebel" },
            { LastRequestType.OnlyHeadshot, "lr_only_headshot" },
            { LastRequestType.NoScope, "lr.no_scope" },
            { LastRequestType.Dodgeball, "lr_dodge_ball" }
        };

        public static string GetLocalizedLastRequestName(LastRequestType lrType)
        {
            if (LastRequestLocalizerKeys.TryGetValue(lrType, out var localizerKey))
            {
                return Instance.Localizer[localizerKey];
            }
            return lrType.ToString();
        }
        public static bool GetTAlive()
        {
            int aliveTerrorists = Utilities.GetPlayers().Count(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive);

            return aliveTerrorists == 1;
        }

        public static void Command_LastRequest(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null)
                return;

            if (player.Team == CsTeam.CounterTerrorist)
            {
                info.ReplyToCommand(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.cannot.ct"]);
                return;
            }
            if (SpecialDays.isSpecialDayActive)
                return;

            if (JailPlayer.isRebel(player))
            {
                info.ReplyToCommand(Instance.Localizer["lr.prefix"] + Instance.Localizer["rebel.cant.lr"]);
                return;
            }
            if (JailPlayer.IsAliveRebel(player))
            {
                info.ReplyToCommand(Instance.Localizer["lr.prefix"] + Instance.Localizer["rebel.cant.lr"]);
                return;
            }
            if (!GetTAlive())
            {
                info.ReplyToCommand(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.cannot.start"]);
                return;
            }
            if (isLrActive)
                return;

            if (SpecialDays.isCountdownActive)
                return;

            player.VoiceFlags = VoiceFlags.Normal;

            var simon = jailApi.GetSimon();
            if (simon != null)
            {
                jailApi.RemoveSimon();
            }

            var manager = Instance.GetMenuManager();
            if (manager == null)
                return;

            var mainMenu = manager.CreateMenu(Instance.Localizer["lr<menu>"], isSubMenu: false);
            if (Instance.Config.LastRequest.Types.KnifeFight)
            {
                mainMenu.Add(Instance.Localizer["option<knifefight>"], (p, option) =>
                {
                    var ctSelectionMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
                    ctSelectionMenu.ParentMenu = mainMenu;

                    var aliveCTs = Utilities.GetPlayers().Where(ct => ct.Team == CsTeam.CounterTerrorist && ct.PawnIsAlive);
                    if (!aliveCTs.Any())
                    {
                        ctSelectionMenu.AddTextOption(Instance.Localizer["lr.no.ct"]);
                    }
                    foreach (var ct in aliveCTs)
                    {
                        ctSelectionMenu.Add(ct.PlayerName, (selectedPlayer, selectedOption) =>
                        {
                            var typeMenu = manager.CreateMenu(Instance.Localizer["lr.knifetype<menu>"], isSubMenu: true);
                            typeMenu.ParentMenu = ctSelectionMenu;

                            typeMenu.Add(Instance.Localizer["type<speed>"], (p, option) =>
                            {
                                StartKnifeFightLR(player, ct, "speed", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            typeMenu.Add(Instance.Localizer["type<gravity>"], (p, option) =>
                            {
                                StartKnifeFightLR(player, ct, "gravity", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            typeMenu.Add(Instance.Localizer["type<drug>"], (p, option) =>
                            {
                                StartKnifeFightLR(player, ct, "drug", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            typeMenu.Add(Instance.Localizer["type<vanilla>"], (p, option) =>
                            {
                                StartKnifeFightLR(player, ct, "vanilla", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            typeMenu.Add(Instance.Localizer["type<onehit>"], (p, option) =>
                            {
                                StartKnifeFightLR(player, ct, "onehit", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            manager.OpenSubMenu(player, typeMenu);
                        });
                    }
                    manager.OpenSubMenu(player, ctSelectionMenu);
                });
                if (Instance.Config.LastRequest.Types.NoScope)
                {
                    mainMenu.Add(Instance.Localizer["option<noscope>"], (p, option) =>
                    {
                        var ctSelectionMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
                        ctSelectionMenu.ParentMenu = mainMenu;

                        var aliveCTs = Utilities.GetPlayers().Where(ct => ct.Team == CsTeam.CounterTerrorist && ct.PawnIsAlive);
                        if (!aliveCTs.Any())
                        {
                            ctSelectionMenu.AddTextOption(Instance.Localizer["lr.no.ct"]);
                        }
                        foreach (var ct in aliveCTs)
                        {
                            ctSelectionMenu.Add(ct.PlayerName, (p, option) =>
                            {
                                var typeMenu = manager.CreateMenu(Instance.Localizer["lr.noscopetype<menu>"], isSubMenu: true);
                                typeMenu.ParentMenu = ctSelectionMenu;

                                typeMenu.Add(Instance.Localizer["AWP"], (p, option) =>
                                {
                                    StartNoScopeLR(p, ct, "awp", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                typeMenu.Add(Instance.Localizer["SSG08"], (p, option) =>
                                {
                                    StartNoScopeLR(p, ct, "ssg08", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                typeMenu.Add(Instance.Localizer["G3SG1"], (p, option) =>
                                {
                                    StartNoScopeLR(p, ct, "g3sg1", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                typeMenu.Add(Instance.Localizer["SCAR-20"], (p, option) =>
                                {
                                    StartNoScopeLR(p, ct, "scar20", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                manager.OpenSubMenu(p, typeMenu);
                            });
                        }
                        manager.OpenSubMenu(p, ctSelectionMenu);
                    });
                }
                if (Instance.Config.LastRequest.Types.Dodgeball)
                {
                    mainMenu.Add(Instance.Localizer["option<dodgeball>"], (p, option) =>
                    {
                        var ctSelectionMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
                        ctSelectionMenu.ParentMenu = mainMenu;

                        var aliveCTs = Utilities.GetPlayers().Where(ct => ct.Team == CsTeam.CounterTerrorist && ct.PawnIsAlive);
                        if (!aliveCTs.Any())
                        {
                            ctSelectionMenu.AddTextOption(Instance.Localizer["lr.no.ct"]);
                        }
                        foreach (var ct in aliveCTs)
                        {
                            ctSelectionMenu.Add(ct.PlayerName, (p, option) =>
                            {
                                StartDodgeballLR(p, ct, ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                        }
                        manager.OpenSubMenu(p, ctSelectionMenu);
                    });
                }
            }
            if (Instance.Config.LastRequest.Types.ShotForShot)
            {
                mainMenu.Add(Instance.Localizer["option<shotforshot>"], (p, option) =>
                {
                    var ctSelectionMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
                    ctSelectionMenu.ParentMenu = mainMenu;

                    var aliveCTs = Utilities.GetPlayers().Where(ct => ct.Team == CsTeam.CounterTerrorist && ct.PawnIsAlive);
                    if (!aliveCTs.Any())
                    {
                        ctSelectionMenu.AddTextOption(Instance.Localizer["lr.no.ct"]);
                    }
                    foreach (var ct in aliveCTs)
                    {
                        ctSelectionMenu.Add(ct.PlayerName, (p, option) =>
                        {
                            var weaponSelectionMenu = manager.CreateMenu(Instance.Localizer["lr.shotforshottype<menu>"], isSubMenu: true);
                            weaponSelectionMenu.ParentMenu = ctSelectionMenu;

                            weaponSelectionMenu.Add(Instance.Localizer["Deagle"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "deagle", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            weaponSelectionMenu.Add(Instance.Localizer["P250"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "p250", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            weaponSelectionMenu.Add(Instance.Localizer["Revolver"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "revolver", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            weaponSelectionMenu.Add(Instance.Localizer["P2000"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "hkp2000", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            weaponSelectionMenu.Add(Instance.Localizer["TEC-9"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "tec9", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            weaponSelectionMenu.Add(Instance.Localizer["Five-Seven"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "fiveseven", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            weaponSelectionMenu.Add(Instance.Localizer["Glock"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "glock", ct.PlayerName);
                                manager.CloseMenu(p);
                            });
                            weaponSelectionMenu.Add(Instance.Localizer["Dual Berettas"], (p, option) =>
                            {
                                StartShotForShotLR(p, ct, "elite", ct.PlayerName);
                                manager.CloseMenu(p);
                            });

                            manager.OpenSubMenu(p, weaponSelectionMenu);
                        });
                    }
                    manager.OpenSubMenu(p, ctSelectionMenu);
                });
                if (Instance.Config.LastRequest.Types.MagForMag)
                {
                    mainMenu.Add(Instance.Localizer["option<magformag>"], (p, option) =>
                    {
                        var ctSelectionMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
                        ctSelectionMenu.ParentMenu = mainMenu;

                        var aliveCTs = Utilities.GetPlayers().Where(ct => ct.Team == CsTeam.CounterTerrorist && ct.PawnIsAlive);
                        if (!aliveCTs.Any())
                        {
                            ctSelectionMenu.AddTextOption(Instance.Localizer["lr.no.ct"]);
                        }
                        foreach (var ct in aliveCTs)
                        {
                            ctSelectionMenu.Add(ct.PlayerName, (p, option) =>
                            {
                                var weaponSelectionMenu = manager.CreateMenu(Instance.Localizer["lr.magformagtype<menu>"], isSubMenu: true);
                                weaponSelectionMenu.ParentMenu = ctSelectionMenu;

                                weaponSelectionMenu.Add(Instance.Localizer["Deagle"], (p, option) =>
                                {
                                    StartMagForMagLR(p, ct, "deagle", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                weaponSelectionMenu.Add(Instance.Localizer["P250"], (p, option) =>
                                {
                                    StartMagForMagLR(p, ct, "p250", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                weaponSelectionMenu.Add(Instance.Localizer["P2000"], (p, option) =>
                                {
                                    StartMagForMagLR(p, ct, "hkp2000", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                weaponSelectionMenu.Add(Instance.Localizer["TEC-9"], (p, option) =>
                                {
                                    StartMagForMagLR(p, ct, "tec9", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                weaponSelectionMenu.Add(Instance.Localizer["Five-Seven"], (p, option) =>
                                {
                                    StartMagForMagLR(p, ct, "fiveseven", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                weaponSelectionMenu.Add(Instance.Localizer["Glock"], (p, option) =>
                                {
                                    StartMagForMagLR(p, ct, "glock", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });
                                weaponSelectionMenu.Add(Instance.Localizer["Dual Berettas"], (p, option) =>
                                {
                                    StartMagForMagLR(p, ct, "elite", ct.PlayerName);
                                    manager.CloseMenu(p);
                                });

                                manager.OpenSubMenu(p, weaponSelectionMenu);
                            });
                        }
                        manager.OpenSubMenu(p, ctSelectionMenu);
                    });
                }
                if (Instance.Config.LastRequest.Types.Rebel)
                {
                    mainMenu.Add(Instance.Localizer["option<rebel>"], (p, option) =>
                    {
                        var ctSelectionMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
                        ctSelectionMenu.ParentMenu = mainMenu;

                        var aliveCt = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive).ToList();
                        if (!aliveCt.Any())
                        {
                            ctSelectionMenu.AddTextOption(Instance.Localizer["lr.no.ct"]);
                        }
                        else if (aliveCt.Count < 3)
                        {
                            ctSelectionMenu.AddTextOption(Instance.Localizer["min.3.ct"]);
                        }
                        else
                        {
                            StartRebelLR(p, aliveCt);
                            manager.CloseMenu(p);
                        }
                    });
                }
                if (Instance.Config.LastRequest.Types.HeadShotOnly)
                {
                    mainMenu.Add(Instance.Localizer["option<onlyheadshot>"], (p, option) =>
                    {
                        var ctSelectionMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
                        ctSelectionMenu.ParentMenu = mainMenu;

                        var aliveCt = Utilities.GetPlayers().Where(ct => ct.Team == CsTeam.CounterTerrorist && ct.PawnIsAlive).ToList();
                        if (!aliveCt.Any())
                        {
                            ctSelectionMenu.AddTextOption(Instance.Localizer["lr.no.ct"]);
                        }
                        else
                        {
                            foreach (var ct in aliveCt)
                            {
                                ctSelectionMenu.Add(ct.PlayerName, (p, option) =>
                                {
                                    var weaponSelectionMenu = manager.CreateMenu(Instance.Localizer["lr.headshotonly<menu>"], isSubMenu: true);
                                    weaponSelectionMenu.ParentMenu = ctSelectionMenu;

                                    weaponSelectionMenu.Add(Instance.Localizer["Deagle"], (p, option) =>
                                    {
                                        StartOnlyHeadShot(p, ct, "deagle", ct.PlayerName);
                                        manager.CloseMenu(p);
                                    });
                                    weaponSelectionMenu.Add(Instance.Localizer["AWP"], (p, option) =>
                                    {
                                        StartOnlyHeadShot(p, ct, "awp", ct.PlayerName);
                                        manager.CloseMenu(p);
                                    });
                                    weaponSelectionMenu.Add(Instance.Localizer["SSG08"], (p, option) =>
                                    {
                                        StartOnlyHeadShot(p, ct, "ssg08", ct.PlayerName);
                                        manager.CloseMenu(p);
                                    });
                                    weaponSelectionMenu.Add(Instance.Localizer["TEC-9"], (p, option) =>
                                    {
                                        StartOnlyHeadShot(p, ct, "tec9", ct.PlayerName);
                                        manager.CloseMenu(p);
                                    });
                                    weaponSelectionMenu.Add(Instance.Localizer["Five Seven"], (p, option) =>
                                    {
                                        StartOnlyHeadShot(p, ct, "fiveseven", ct.PlayerName);
                                        manager.CloseMenu(p);
                                    });
                                    weaponSelectionMenu.Add(Instance.Localizer["AK-47"], (p, option) =>
                                    {
                                        StartOnlyHeadShot(p, ct, "ak47", ct.PlayerName);
                                        manager.CloseMenu(p);
                                    });

                                    manager.OpenSubMenu(p, weaponSelectionMenu);
                                });
                            }
                        }

                        manager.OpenSubMenu(p, ctSelectionMenu);
                    });
                }

            }

            manager.OpenMainMenu(player, mainMenu);
        }
        public static void ReloadClip(CCSPlayerController player, int clipSize)
        {
            var weapon = player.Pawn?.Value?.WeaponServices?.ActiveWeapon?.Get()?.As<CCSWeaponBase>();
            if (weapon == null) return;

            var weaponName = weapon.DesignerName.ToLower();
            if (IsValidPistol(weaponName))
            {
                weapon.SetAmmo(clipSize, 0); // Set Clip1 to `clipSize` and Clip2 (reserve) to 0
                playerAmmo[player] = clipSize;
            }
        }

        private static bool IsValidPistol(string weaponName)
        {
            var allowedWeapons = new[]
            {
               "weapon_deagle", "weapon_p250", "weapon_revolver", "weapon_usp_silencer", "weapon_hkp2000",
               "weapon_tec9", "weapon_fiveseven", "weapon_glock", "weapon_elite"
            };

            return allowedWeapons.Contains(weaponName);
        }
        public static void DecrementAmmo(CCSPlayerController player)
        {
            if (playerAmmo.ContainsKey(player))
            {
                playerAmmo[player]--;
                if (playerAmmo[player] <= 0)
                {
                    playerAmmo[player] = 0;
                }
            }
        }

        private static int GetClipSizeForWeapon(string weaponName)
        {
            return weaponName switch
            {
                "weapon_deagle" => 7,
                "weapon_p250" => 13,
                "weapon_revolver" => 8,
                "weapon_usp_silencer" => 12,
                "weapon_hkp2000" => 13,
                "weapon_tec9" => 18,
                "weapon_fiveseven" => 20,
                "weapon_glock" => 20,
                "weapon_elite" => 30,
                _ => 0
            };
        }
        public static void Speed(CCSPlayerController? T, CCSPlayerController? CT)
        {
            if (T == null || CT == null)
                return;

            T.PlayerPawn.Value!.VelocityModifier = 2.75f;
            CT.PlayerPawn.Value!.VelocityModifier = 2.75f;

            Utilities.SetStateChanged(CT, "CCSPlayerPawn", "m_flVelocityModifier");
            Utilities.SetStateChanged(T, "CCSPlayerPawn", "m_flVelocityModifier");
        }
        public static void Gravity(CCSPlayerController? T, CCSPlayerController? CT)
        {
            if (T == null || CT == null)
                return;

            T.PlayerPawn.Value!.GravityScale = 0.5f;
            CT.PlayerPawn.Value!.GravityScale = 0.5f;
        }
        public static void Drug(CCSPlayerController? T, CCSPlayerController? CT)
        {
            if (T == null || CT == null)
                return;

            T.Drug(1);
            CT.Drug(1);
        }
        public static void ResetPlayerAttributes(CCSPlayerController? T, CCSPlayerController? CT, LastRequestType? lrType, string? effect = null)
        {
            if (T == null || CT == null || lrType == null)
                return;

            T.StripWeapons();
            CT.StripWeapons();

            T.SetHealth(100);
            CT.SetHealth(100);

            switch (lrType)
            {
                case LastRequestType.KnifeFight:
                    ResetKnifeFightAttributes(T, CT, effect);
                    break;

                case LastRequestType.NoScope:
                    T.StripWeapons();
                    CT.StripWeapons();
                    break;

                case LastRequestType.Dodgeball:
                    Server.ExecuteCommand("sv_cheats 0");
                    Server.ExecuteCommand("sv_infinite_ammo 0");
                    break;

            }

            T.RemoveTimer(LRHelper.TimerFlag.Beacon);
            CT.RemoveTimer(LRHelper.TimerFlag.Beacon);

            Utilities.SetStateChanged(T, "CCSPlayerPawn", "m_flVelocityModifier");
            Utilities.SetStateChanged(CT, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        public static async Task EndLastRequest(CCSPlayerController? T, CCSPlayerController? CT, CCSPlayerController? winner)
        {
            if (activeLastRequest.HasValue)
            {
                ResetPlayerAttributes(T, CT, activeLastRequest, activeKnifeFightEffect);
            }
            else
            {
                ResetPlayerAttributes(T, CT, activeLastRequest);
            }

            isLrStarted = false;

            if (winner != null)
            {
                string winnerName = winner.PlayerName; // Capture on the main thread

                // Perform database query
                int totalWins = await JBDatabase.GetPlayerWinsAsync(winner.PlayerName);

                // Output message to chat on the main thread
                Server.NextFrame(() =>
                {
                    Server.PrintToChatAll(
                        Instance.Localizer["lr.prefix"] +
                        Instance.Localizer["lr.ended", winnerName, totalWins]
                    );
                });
            }
        }
        private static void ResetKnifeFightAttributes(CCSPlayerController T, CCSPlayerController CT, string? effect)
        {
            switch (effect)
            {
                case "speed":
                    T.PlayerPawn.Value!.VelocityModifier = 1.0f;
                    CT.PlayerPawn.Value!.VelocityModifier = 1.0f;
                    EnableCollision(T);
                    EnableCollision(CT);
                    break;

                case "gravity":
                    T.PlayerPawn.Value!.GravityScale = 1.0f;
                    CT.PlayerPawn.Value!.GravityScale = 1.0f;
                    EnableCollision(T);
                    EnableCollision(CT);
                    break;

                case "drug":
                    T.KillDrug();
                    CT.KillDrug();
                    break;

                case "onehit":
                    T.SetHealth(100);
                    CT.SetHealth(100);
                    break;

                case "vanilla":
                    break;

                default:
                    Server.PrintToConsole($"Unknown KnifeFight effect: {effect}");
                    break;
            }
        }

        private static HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            if (victim == null)
                return HookResult.Continue;

            if (isLrActive && (victim == terrorist || victim == ct))
            {
                CCSPlayerController? winner = (victim == terrorist) ? ct : terrorist;
                CCSPlayerController? loser = (victim == terrorist) ? terrorist : ct;

                Server.NextFrame(() =>
                {
                    if (winner != null && loser != null)
                    {
                        string winnerName = winner.PlayerName;
                        string loserName = loser.PlayerName;

                        Task.Run(async () =>
                        {
                            await JBDatabase.UpdatePlayerStatsAsync(winnerName, true);
                            await JBDatabase.UpdatePlayerStatsAsync(loserName, false);
                        });
                    }
                    Server.NextFrame(() =>
                    {
                        EndLastRequest(terrorist, ct, winner).Wait();
                        ResetLrState();
                    });
                    ResetLrState();
                });
            }
            return HookResult.Continue;
        }
        public static HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;

            if (victim == null || attacker == null)
                return HookResult.Continue;

            if (isLrActive)
            {
                var activeWeapon = attacker.Pawn?.Value?.WeaponServices?.ActiveWeapon;
                var vData = activeWeapon?.Get()?.As<CCSWeaponBase>().VData;

                if (vData == null)
                    return HookResult.Continue;

                if (isActive(LastRequestType.KnifeFight))
                {
                    if (vData.WeaponType != CSWeaponType.WEAPONTYPE_KNIFE)
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                    }
                }
                else if (isActive(LastRequestType.NoScope))
                {
                    // allow only sniper damage
                    var allowedWeapons = new[] { "weapon_awp", "weapon_ssg08", "weapon_g3sg1", "weapon_scar20" };
                    if (!allowedWeapons.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                    }
                }
                else if (isActive(LastRequestType.Dodgeball))
                {
                    if (activeWeapon?.Value?.DesignerName.ToLower() != "weapon_decoy")
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                    }
                }
                else if (isActive(LastRequestType.ShotForShot))
                {
                    var allowedWeapons = new[] { "weapon_deagle", "weapon_p250", "weapon_revolver", "weapon_usp_silencer",
                        "weapon_hkp2000", "weapon_tec9",
                        "weapon_fiveseven", "weapon_glock", "weapon_elite" };
                    if (!allowedWeapons.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                    }
                }
                else if (isActive(LastRequestType.MagForMag))
                {
                    var allowedWeapons = new[] { "weapon_deagle", "weapon_p250", "weapon_revolver", "weapon_usp_silencer",
                        "weapon_hkp2000", "weapon_tec9",
                        "weapon_fiveseven", "weapon_glock", "weapon_elite" };
                    if (!allowedWeapons.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                    }
                }
                else if (isActive(LastRequestType.OnlyHeadshot))
                {
                    var allowedWeapons = new[] { "weapon_ak47", "weapon_deagle", "weapon_ssg08", "weapon_awp", "weapon_tec9", "weapon_fiveseven" };

                    if (!allowedWeapons.Contains(activeWeapon?.Value?.DesignerName.ToLower()))
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                        return HookResult.Handled;
                    }

                    if (@event.Hitgroup != 1)
                    {
                        victim.PlayerPawn!.Value!.Health += @event.DmgHealth;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                        victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                        return HookResult.Handled;
                    }
                }

            }
            return HookResult.Continue;
        }
        public static HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            if ((!isActive(LastRequestType.ShotForShot) && !isActive(LastRequestType.MagForMag)) || currentShooter == null)
                return HookResult.Continue;

            var shooter = @event.Userid;

            var activeWeapon = shooter?.Pawn?.Value?.WeaponServices?.ActiveWeapon?.Get()?.As<CCSWeaponBase>();
            if (activeWeapon == null || activeWeapon.DesignerName.ToLower() == "weapon_knife")
                return HookResult.Continue;

            var weaponName = activeWeapon.DesignerName.ToLower();
            if (!IsValidPistol(weaponName))
                return HookResult.Continue;

            if (shooter != currentShooter)
                return HookResult.Continue;

            if (!playerAmmo.ContainsKey(currentShooter))
            {
                playerAmmo[currentShooter] = 0;
            }

            if (isActive(LastRequestType.ShotForShot))
            {
                DecrementAmmo(currentShooter);

                if (playerAmmo[currentShooter] <= 0)
                {
                    currentShooter = (currentShooter == terrorist) ? ct : terrorist;
                    ReloadClip(currentShooter!, 1);
                }
            }
            else if (isActive(LastRequestType.MagForMag))
            {
                DecrementAmmo(currentShooter);

                if (playerAmmo[currentShooter] <= 0)
                {
                    currentShooter = (currentShooter == terrorist) ? ct : terrorist;

                    int clipSize = GetClipSizeForWeapon(activeWeapon.DesignerName.ToLower());
                    ReloadClip(currentShooter!, clipSize);
                }
            }

            return HookResult.Continue;
        }


        private static string ctName = "";
        private static string lrName = "";

        public static void StartLrWithDelay(CCSPlayerController terrorist, CCSPlayerController ct, LastRequestType lrType, Action startLrAction)
        {
            if (lrType == LastRequestType.Rebel)
            {
                startLrAction.Invoke();
                return;
            }
            remainingCountdown = Instance.Config.LastRequest.LrStartTimer;
            isCountdownActive = true;
            lastUpdateTime = DateTime.Now;

            lrName = GetLocalizedLastRequestName(lrType);
            ctName = ct?.PlayerName ?? "N/A";

            string terroristName = terrorist.PlayerName;
            terrorist.StripWeaponsFull();
            ct!.StripWeaponsFull();

            Server.PrintToChatAll(Instance.Localizer["lr.prefix"] + Instance.Localizer["lr.preparing", terroristName, lrName, remainingCountdown]);

            LrPrepTimer = Instance.AddTimer(remainingCountdown, () =>
            {
                startLrAction.Invoke();
                LrPrepTimer?.Kill();
                LrPrepTimer = null;
                isCountdownActive = false;
            });
        }

        public static void OnTick()
        {
            if (!isCountdownActive || LrPrepTimer == null) return;

            if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
            {
                remainingCountdown = Math.Max(0, remainingCountdown - 1);
                lastUpdateTime = DateTime.Now;
            }

            string countdownMessage = Instance.Localizer["lr.countdown", lrName, remainingCountdown.ToString("0"), ctName];

            foreach (var player in Utilities.GetPlayers())
            {
                player.PrintToCenterHtml(countdownMessage);
            }

            if (remainingCountdown <= 0)
            {
                isCountdownActive = false;
                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterHtml("");
                }
            }
        }
        public static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var disconnectingPlayer = @event.Userid;

            if (isLrActive && (disconnectingPlayer == terrorist || disconnectingPlayer == ct))
            {
                CCSPlayerController? winner = (disconnectingPlayer == terrorist) ? ct : terrorist;

                Server.NextFrame(() =>
                {
                    if (winner != null)
                    {

                        Task.Run(async () =>
                        {
                            string winnerName = winner.PlayerName;
                            await JBDatabase.UpdatePlayerStatsAsync(winnerName, true);
                        });

                        Server.NextFrame(() =>
                        {
                            EndLastRequest(terrorist, ct, winner).Wait();
                            ResetLrState();
                        });
                    }
                    else
                    {
                        ResetLrState();
                    }
                });
            }

            return HookResult.Continue;
        }

        public static bool NoScopeIsActive() => isActive(LastRequestType.NoScope);
        public static bool KnifeFightIsActive() => isActive(LastRequestType.KnifeFight);
        public static bool ShotForShotIsActive() => isActive(LastRequestType.ShotForShot);
        public static bool MagForMagIsActive() => isActive(LastRequestType.MagForMag);
        public static bool RebelIsActive() => isActive(LastRequestType.Rebel);
        public static bool OnlyHeadShotIsActive() => isActive(LastRequestType.OnlyHeadshot);
    }
}