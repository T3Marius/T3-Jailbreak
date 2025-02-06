using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using static T3Jailbreak.Helpers;
using CounterStrikeSharp.API.Modules.Utils;
using static T3Jailbreak.T3Jailbreak;
using CounterStrikeSharp.API.Modules.Memory;
using static T3Jailbreak.JBDatabase;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Admin;

namespace T3Jailbreak;

public static class Commands
{
    public static JailAPI jailApi { get; set; } = new JailAPI();
    public static bool isBoxActive = false;
    private static readonly Dictionary<ulong, int> HealUsageTracker = new();
    private static int MaxHealUsagePerRound = Instance.Config.Prisoniers.HealCommandCountPerRound;
    public static void Load()
    {
        var AddCmd = Instance.AddCommand;
        var config = Instance.Config.Commands;
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        foreach (var cmd in config.Simon)
        {
            AddCmd($"css_{cmd}", "take simon", Command_Simon);
        }
        foreach (var cmd in config.UnSimon)
        {
            AddCmd($"css_{cmd}", "Gave up simon", Command_UnSimon);
        }
        foreach (var cmd in config.Deputy)
        {
            AddCmd($"css_{cmd}", "Take deputy", Command_Deputy);
        }
        foreach (var cmd in config.UnDeputy)
        {
            AddCmd($"css_{cmd}", "Gave up deputy", Command_UnDeputy);
        }
        foreach (var cmd in config.GunsMenu)
        {
            AddCmd($"css_{cmd}", "Open CT Guns Menu", Command_Guns);
        }
        foreach (var cmd in config.OpenCells)
        {
            AddCmd($"css_{cmd}", "Open Cells", Command_OpenCells);
        }
        foreach (var cmd in config.CloseCells)
        {
            AddCmd($"css_{cmd}", "Close Cells", Command_CloseCells);
        }
        foreach (var cmd in config.Box)
        {
            AddCmd($"css_{cmd}", "Starts box", Command_Box);
        }
        foreach (var cmd in config.Ding)
        {
            AddCmd($"css_{cmd}", "Fake box/ding", Command_Ding);
        }
        foreach (var cmd in config.ForgiveRebel)
        {
            AddCmd($"css_{cmd}", "Forgive Rebel", Command_ForgiveRebel);
        }
        foreach (var cmd in config.GiveFreeday)
        {
            AddCmd($"css_{cmd}", "Give freeday", Command_GiveFreeday);
        }
        foreach (var cmd in config.RemoveFreeday)
        {
            AddCmd($"css_{cmd}", "Remove Freeday", Command_RemoveFreeday);
        }
        foreach (var cmd in config.Heal)
        {
            AddCmd($"css_{cmd}", "Ask simon for heal", Command_Heal);
        }
        foreach (var cmd in config.GiveUp)
        {
            AddCmd($"css_{cmd}", "Ask simon for forgivness", Command_GiveUP);
        }
        foreach (var cmd in config.AdminCommands.SetSimon)
        {
            AddCmd($"css_{cmd}", "Set someone as simon", Command_SetSimon);
        }
        foreach (var cmd in config.AdminCommands.RemoveSimon)
        {
            AddCmd($"css_{cmd}", "Removes The Existing Simon", Command_RemoveSimon);
        }
        foreach (var cmd in config.SimonMenu)
        {
            AddCmd($"css_{cmd}", "Opens simon menu", Command_SimonMenu);
        }
        foreach (var cmd in config.SetColor)
        {
            AddCmd($"css_{cmd}", "Simon set a color for player", Command_SetColor);
        }
        foreach (var cmd in config.LRTop)
        {
            AddCmd($"css_{cmd}", "Shows top lr players", Command_TopLR);
        }
        foreach (var cmd in config.ExtendRoundTimeCommands)
        {
            AddCmd($"css_{cmd}", "Exted The Round Time", Command_Extend);
        }
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }
    public static void UnLoad()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }
    public static void Command_Extend(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (!(jailApi.IsSimon(player) || jailApi.IsDeputy(player) || AdminManager.PlayerHasPermissions(player, "@css/generic")))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["no.permission"]);
            return;
        }


        var manager = Instance.GetMenuManager();
        if (manager == null) 
            return;

        var menu = manager.CreateMenu(Instance.Localizer["extend<menu>"], isSubMenu: false);
        var customValues = new List<object> { 1, 2, 5, 10 };
        menu.AddSliderOption(" ", customValues: customValues, defaultValue: 1, onSlide: (p, option) =>
        {
            int value = (int)option.SliderValue!;
            ExtendRoundTime(value);
            manager.CloseMenu(p);

            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["round.extended", p.PlayerName, value]);
        });

        manager.OpenMainMenu(player, menu);
    }
    public static void Command_TopLR(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        Server.NextFrame(() =>
        {
            var manager = Instance.GetMenuManager();
            if (manager == null)
                return;

            var lrTopMenu = manager.CreateMenu(Instance.Localizer["lr.top<menu>"], isSubMenu: false);

            Task.Run(async () =>
            {
                var topPlayers = await GetTopPlayersAsync();

                if (!topPlayers.Any())
                {
                    Server.NextFrame(() =>
                    {
                        lrTopMenu.AddTextOption(Instance.Localizer["no.top.players"], selectable: false);
                        manager.OpenMainMenu(player, lrTopMenu);
                    });
                    return;
                }

                int rank = 1;

                Server.NextFrame(() =>
                {
                    foreach (var (PlayerName, Wins) in topPlayers)
                    {
                        string displayText = $"<font color='#FFFF00'>#{rank}.</font> {PlayerName} - <font color='#FFE4C4'>{Wins}</font> Wins";
                        lrTopMenu.AddTextOption(displayText, selectable: true);
                        rank++;
                    }
                    manager.OpenMainMenu(player, lrTopMenu);
                });
            });
        });
    }

    [CommandHelper(minArgs: 2, usage: "<playername> <color>")]
    public static void Command_SetColor(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (!jailApi.IsSimon(player) && !jailApi.IsDeputy(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon.using.command"]);
            return;
        }

        var targetPlayer = FindPlayer(info.ArgByIndex(1), player);
        if (targetPlayer == null || targetPlayer.Team != CsTeam.Terrorist || !targetPlayer.PawnIsAlive)
            return;

        if (JailPlayer.isRebel(targetPlayer))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["cannot.color.rebel", targetPlayer.PlayerName]);
            return;
        }

        var colorName = info.ArgByIndex(2);
        var systemColor = ParseColor(colorName);
        if (systemColor == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["invalid.color"]);
            return;
        }

        var targetPawn = targetPlayer.PlayerPawn.Value;
        if (targetPawn == null)
            return;

        targetPawn.RenderMode = RenderMode_t.kRenderTransColor;

        int renderColor = SystemColorToInteger(systemColor.Value);
        targetPawn.Render = systemColor.Value;

        Utilities.SetStateChanged(targetPawn, "CBaseModelEntity", "m_clrRender");

        char chatColor = GetChatColorForColorName(colorName);

        Server.PrintToChatAll(Instance.Localizer["jb.prefix"] +
            Instance.Localizer["simon.colored.player", targetPlayer.PlayerName, $" {chatColor}{colorName}{ChatColors.Default}"]);
    }

    public static void Command_SimonMenu(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (!jailApi.IsSimon(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon.using.command"]);
            return;
        }

        var manager = Instance.GetMenuManager();
        if (manager == null)
            return;

        var mainMenu = manager.CreateMenu(Instance.Localizer["simon<menu>"], isSubMenu: false);

        mainMenu.Add(Instance.Localizer["option<lasercolor>"], (p, option) =>
        {
            var laserColorMenu = manager.CreateMenu(Instance.Localizer["color<menu>"], isSubMenu: true);
            laserColorMenu.ParentMenu = mainMenu;

            var colorOptions = new Dictionary<string, System.Drawing.Color>
            {
               { "Red", System.Drawing.Color.Red },
               { "Green", System.Drawing.Color.Green },
               { "Blue", System.Drawing.Color.Blue },
               { "Cyan", System.Drawing.Color.Cyan },
               { "Yellow", System.Drawing.Color.Yellow },
               { "Purple", System.Drawing.Color.Purple },
               { "Brown", System.Drawing.Color.Brown }
            };

            foreach (var (name, color) in colorOptions)
            {
                string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                string displayName = $"<font color='{hexColor}'>{name}</font>";

                laserColorMenu.Add(displayName, (subP, subOption) =>
                {
                    Laser laser = new Laser();
                    laser.SaveColor(p, color); // Save the selected color for the laser
                    char chatColor = GetChatColorForColorName(name);

                    p.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["laser.color.selected", $" {chatColor}{name}{ChatColors.Default}"]);
                });
            }

            // Add the special RGB option with colored R, G, B letters
            string rgbDisplayName =
                "<font color='#0000FF'>R</font>" + // Blue R
                "<font color='#FFFF00'>G</font>" + // Yellow G
                "<font color='#FF0000'>B</font>";  // Red B

            laserColorMenu.Add(rgbDisplayName, (subP, subOption) =>
            {
                Laser laser = new Laser();
                laser.SaveColor(p, System.Drawing.Color.Empty); // RGB option uses no specific color, could customize if needed

                p.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["laser.color.selected", $"{ChatColors.Blue}R{ChatColors.Yellow}G{ChatColors.Red}B"]);
            });

            manager.OpenSubMenu(p, laserColorMenu);
        });
        mainMenu.Add(Instance.Localizer["option<markercolor>"], (p, option) =>
        {
            var markerColorMenu = manager.CreateMenu(Instance.Localizer["color<menu>"], isSubMenu: true);
            markerColorMenu.ParentMenu = mainMenu;

            var colorOptions = new Dictionary<string, System.Drawing.Color>
            {
               { "Red", System.Drawing.Color.Red },
               { "Green", System.Drawing.Color.Green },
               { "Blue", System.Drawing.Color.Blue },
               { "Cyan", System.Drawing.Color.Cyan },
               { "Yellow", System.Drawing.Color.Yellow },
               { "Purple", System.Drawing.Color.Purple },
               { "Brown", System.Drawing.Color.Brown }
            };

            foreach (var (name, color) in colorOptions)
            {
                string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                string displayName = $"<font color='{hexColor}'>{name}</font>";

                markerColorMenu.Add(displayName, (subP, subOption) =>
                {
                    var marker = new Circle();
                    marker.SaveColor(p, color);
                    char chatColor = GetChatColorForColorName(name);

                    p.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["marker.color.selected", $" {chatColor}{name}{ChatColors.Default}"]);
                });
            }

            string rgbDisplayName =
                "<font color='#0000FF'>R</font>" +
                "<font color='#FFFF00'>G</font>" +
                "<font color='#FF0000'>B</font>";

            markerColorMenu.Add(rgbDisplayName, (subP, subOption) =>
            {
                var marker = new Circle();
                marker.SaveColor(p, System.Drawing.Color.Empty);

                p.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["marker.color.selected", $"{ChatColors.Blue}R{ChatColors.Yellow}G{ChatColors.Red}B"]);
            });

            manager.OpenSubMenu(p, markerColorMenu);
        });
        mainMenu.Add(Instance.Localizer["option<freeday>"], (p, option) =>
        {
            var prisoniersSubMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
            prisoniersSubMenu.ParentMenu = mainMenu;

            var prisoniers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive);

            if (!prisoniers.Any())
            {
                prisoniersSubMenu.AddTextOption("<font color='#FFFF00'>" + Instance.Localizer["no.prisoniers.players"] + "</font>");
            }
            else
            {
                foreach (var prisonier in prisoniers)
                {
                    prisoniersSubMenu.Add(prisonier.PlayerName, (p, option) =>
                    {
                        p.ExecuteClientCommandFromServer($"css_{Instance.Config.Commands.GiveFreeday.First()} {prisonier.PlayerName}");
                        manager.CloseMenu(player);
                    });
                }
            }
            manager.OpenSubMenu(p, prisoniersSubMenu);
        });
        mainMenu.Add(Instance.Localizer["option<removefreeday>"], (p, option) =>
        {
            var prisoniersSubMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
            prisoniersSubMenu.ParentMenu = mainMenu;

            var freedayPlayers = Utilities.GetPlayers().Where(p => JailPlayer.isFreeday(p) && p.PawnIsAlive);

            if (!JailPlayer.GetFreeday().Any())
            {
                prisoniersSubMenu.AddTextOption("<font color='#FFFF00'>" + Instance.Localizer["no.freeday.players"] + "</font>");
            }
            else
            {
                foreach (var fdPlayer in freedayPlayers)
                {
                    prisoniersSubMenu.Add(fdPlayer.PlayerName, (p, option) =>
                    {
                        p.ExecuteClientCommandFromServer($"css_{Instance.Config.Commands.RemoveFreeday.First()} {fdPlayer.PlayerName}");
                        manager.CloseMenu(p);
                    });
                }
            }
            manager.OpenSubMenu(p, prisoniersSubMenu);
        });
        mainMenu.Add(Instance.Localizer["option<cells>"], (p, option) =>
        {
            var cellsSubMenu = manager.CreateMenu(Instance.Localizer["submenu<cells>"], isSubMenu: true);
            cellsSubMenu.ParentMenu = mainMenu;

            cellsSubMenu.Add(Instance.Localizer["option<opencells>"], (p, option) =>
            {
                ForceOpen();
            });
            cellsSubMenu.Add(Instance.Localizer["option<closecells>"], (p, option) =>
            {
                ForceClose();
            });
            manager.OpenSubMenu(player, cellsSubMenu);
        });
        mainMenu.Add(Instance.Localizer["option<box>"], (p, option) =>
        {
            var boxSubMenu = manager.CreateMenu(Instance.Localizer["submenu<box>"], isSubMenu: true);
            boxSubMenu.ParentMenu = mainMenu;

            boxSubMenu.AddBoolOption(Instance.Localizer["option<togglebox>"], defaultValue: false, (p, option) =>
            {
                if (option is IT3Option boolOption)
                {
                    bool boxEnabled = boolOption.OptionDisplay!.Contains("✔");

                    if (boxEnabled)
                    {
                        ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() = true;
                        Server.ExecuteCommand("sv_teamid_overhead 0");

                        Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["box.started"]);
                        Utilities.GetPlayers().ForEach(p => p.PlaySound(Instance.Config.Sounds.BoxSound));
                    }
                    else
                    {
                        ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() = false;
                        Server.ExecuteCommand("sv_teamid_overhead 1");

                        Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["box.ended"]);
                    }
                }
            });
            manager.OpenSubMenu(player, boxSubMenu);

        });
        mainMenu.Add(Instance.Localizer["option<setplayercolor>"], (p, option) =>
        {
            var prisoniersSubMenu = manager.CreateMenu(Instance.Localizer["lr<selectplayer>"], isSubMenu: true);
            prisoniersSubMenu.ParentMenu = mainMenu;

            var aliveT = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive && !JailPlayer.isRebel(p));

            if (!aliveT.Any())
            {
                prisoniersSubMenu.AddTextOption("<font color='#FFFF00'>" + Instance.Localizer["no.prisoniers.players"] + "</font>");
            }

            foreach (var t in aliveT)
            {
                prisoniersSubMenu.Add(t.PlayerName, (p, option) =>
                {
                    var colorSubMenu = manager.CreateMenu(Instance.Localizer["color<menu>"], isSubMenu: true);
                    colorSubMenu.ParentMenu = prisoniersSubMenu;

                    // List of basic colors to use in the menu
                    var basicColors = new List<string>
                    {
                      "Red", "Green", "Blue", "Yellow",
                      "Orange", "Pink", "Purple", "White",
                      "Black", "Gray", "Cyan", "Magenta", "Brown"
                    };

                    foreach (var colorName in basicColors)
                    {
                        var color = System.Drawing.Color.FromName(colorName);
                        if (!color.IsKnownColor) continue;

                        string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                        string displayName = $"<font color='{hexColor}'>{colorName}</font>";

                        colorSubMenu.Add(displayName, (p, option) =>
                        {
                            p.ExecuteClientCommandFromServer($"css_{Instance.Config.Commands.SetColor.First()} {t.PlayerName} {colorName}");
                        });
                    }

                    manager.OpenSubMenu(p, colorSubMenu);
                });
            }

            manager.OpenSubMenu(p, prisoniersSubMenu);
        });

        manager.OpenMainMenu(player, mainMenu);
    }
    public static void Command_RemoveSimon(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;
        if (LastRequest.GetTAlive() == true)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["lr.active"]);
            return;
        }
        if (LastRequest.isLrActive)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["lr.active"]);
            return;
        }

        if (!Instance.Config.Commands.AdminCommands.AdminPermissions.RemoveSimon.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["no.permission"]);
            return;
        }

        var currentSimon = jailApi.GetSimon();
        if (currentSimon == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["no.simon.remove"]);
            return;
        }

        jailApi.RemoveSimon();
        Server.PrintToChatAll(string.Format(Instance.Localizer["jb.prefix"] + Instance.Localizer["jailApi.removed.admin"], player.PlayerName));
    }

    [CommandHelper(minArgs: 1, usage: "<playername> (ONLY CT)")]
    public static void Command_SetSimon(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (LastRequest.GetTAlive() == true)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["lr.active"]);
            return;
        }

        if (LastRequest.isLrActive)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["lr.active"]);
            return;
        }

        if (!Instance.Config.Commands.AdminCommands.AdminPermissions.SetSimon.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["no.permission"]);
            return;
        }

        var targetArg = info.ArgByIndex(1);
        if (string.IsNullOrEmpty(targetArg))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["invalid.argument"]);
            return;
        }

        var targetPlayer = FindPlayer(targetArg, player);
        if (targetPlayer == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.not.found"]);
            return;
        }

        if (targetPlayer.Team == CsTeam.Terrorist)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.is.t"]);
            return;
        }

        var currentSimon = jailApi.GetSimon();
        if (currentSimon != null && jailApi.IsSimon(currentSimon))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.exists"]);
            return;
        }

        jailApi.SetSimon(targetPlayer.Slot);
        Server.PrintToChatAll(string.Format(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.setted.admin"], player.PlayerName, targetPlayer.PlayerName));
    }
    public static void Command_GiveUP(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (player.Team != CsTeam.Terrorist)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["need.to.be.t"]);
            return;
        }
        var simon = jailApi.GetSimon();
        if (simon == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["no.simon.forgivness"]);
            return;
        }
        if (!JailPlayer.isRebel(player))
        {
            Server.PrintToChatAll(Instance.Localizer["rebel.prefix"] + Instance.Localizer["need.to.be.rebel"]);
            return;
        }
        var manager = Instance.GetMenuManager();
        if (manager == null)
            return;

        var rebelMenu = manager.CreateMenu(Instance.Localizer["rebel<menu>", player.PlayerName], freezePlayer: false);
        rebelMenu.Add(Instance.Localizer["option<accept>"], (p, option) =>
        {
            if (Instance.Config.Commands.ForgiveRebel.Any())
            {
                string command = $"css_{Instance.Config.Commands.ForgiveRebel.First()} {player.PlayerName}";
                simon.ExecuteClientCommandFromServer(command);
            }
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.accepted.forgivness", simon.PlayerName, player.PlayerName]);
            manager.CloseMenu(simon);
        });
        rebelMenu.Add(Instance.Localizer["option<refuse>"], (player, option) =>
        {
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.refused.forgivness", simon.PlayerName, player.PlayerName]);
            manager.CloseMenu(simon);
        });
        manager.OpenMainMenu(simon, rebelMenu);
    }
    public static void Command_Heal(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;
        if (LastRequest.isLrActive)
            return;
        if (!player.PawnIsAlive)
            return;
        if (player.Team != CsTeam.Terrorist)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["need.to.be.t"]);
            return;
        }

        ulong playerSteamId = player.SteamID;
        if (!HealUsageTracker.TryGetValue(playerSteamId, out int usageCount))
        {
            usageCount = 0;
        }

        if (usageCount >= MaxHealUsagePerRound)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["heal.limit.reached"]);
            return;
        }

        var simon = jailApi.GetSimon();
        if (simon == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["no.simon"]);
            return;
        }
        if (player.PlayerPawn.Value?.Health >= 100)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["hp.full"]);
            return;
        }

        var manager = Instance.GetMenuManager();
        if (manager == null)
            return;

        var healMenu = manager.CreateMenu(Instance.Localizer["heal<menu>", player.PlayerName], freezePlayer: false);
        healMenu.Add(Instance.Localizer["option<accept>"], (p, option) =>
        {
            player.GiveNamedItem("weapon_healthshot");
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.accepted.heal", simon.PlayerName, player.PlayerName]);
            manager.CloseMenu(simon);

            HealUsageTracker[playerSteamId] = usageCount + 1;
        });
        healMenu.Add(Instance.Localizer["option<refuse>"], (player, option) =>
        {
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.refused.heal", simon.PlayerName, player.PlayerName]);
            manager.CloseMenu(simon);
        });
        manager.OpenMainMenu(simon, healMenu);
    }
    [CommandHelper(minArgs: 1, usage: "<playername>")]
    public static void Command_RemoveFreeday(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (!jailApi.IsSimon(player) && !jailApi.IsDeputy(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon.or.deputy.using.command"]);
            return;
        }

        var targetPlayer = FindPlayer(info.ArgByIndex(1), player);
        if (targetPlayer == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.not.found"]);
            return;
        }
        if (targetPlayer.Team == CsTeam.CounterTerrorist)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["cant.freeday.ct"]);
        }
        if (!JailPlayer.playerFreeday.Contains(targetPlayer))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.doesnt.have.freeday", targetPlayer.PlayerName]);
            return;
        }
        Server.PrintToChatAll(Instance.Localizer["freeday.prefix"] + Instance.Localizer["freeday.removed", player.PlayerName, targetPlayer.PlayerName]);
        JailPlayer.RemoveFreeday(targetPlayer);
    }
    [CommandHelper(minArgs: 1, usage: "<playername>")]
    public static void Command_GiveFreeday(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (!jailApi.IsSimon(player) && !jailApi.IsDeputy(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon.using.command"]);
            return;
        }

        var targetPlayer = FindPlayer(info.ArgByIndex(1), player);
        if (targetPlayer == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.not.found"]);
            return;
        }
        if (targetPlayer.Team == CsTeam.CounterTerrorist)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["cant.freeday.ct"]);
        }
        if (JailPlayer.playerFreeday.Contains(targetPlayer))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.has.freeday", targetPlayer.PlayerName]);
            return;
        }
        JailPlayer.GiveFreeday(targetPlayer);
    }

    [CommandHelper(minArgs: 1, usage: "<playername>")]
    public static void Command_ForgiveRebel(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;
        if (!jailApi.IsSimon(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon.using.command"]);
            return;
        }
        var targetPlayer = FindPlayer(info.ArgByIndex(1), player);

        if (targetPlayer == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["player.not.found"]);
            return;
        }
        if (!JailPlayer.isRebel(targetPlayer))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.rebel", targetPlayer.PlayerName]);
            return;
        }

        JailPlayer.ForgiveRebel(targetPlayer);
    }

    public static void Command_Ding(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (jailApi.IsSimon(player))
        {
            Utilities.GetPlayers().ForEach(p => p.PlaySound("sounds/jailbreak_sounds/40_volume/boxsound.vsnd_c"));
        }
        else if (player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist)
        {
            player.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon.using.command"]);
        }
    }
    public static void Command_Box(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (jailApi.IsSimon(player))
        {
            isBoxActive = !isBoxActive;

            if (isBoxActive)
            {

                ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() = true;
                Server.ExecuteCommand("sv_teamid_overhead 0");

                Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["box.started"]);
                Utilities.GetPlayers().ForEach(p => p.PlaySound(Instance.Config.Sounds.BoxSound));
            }
            else
            {
                Server.ExecuteCommand("sv_teamid_overhead 1");
                ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() = false;

                Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["box.ended"]);
            }
        }
        else if (player != null && (player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist))
        {
            player.PrintToChat(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon.using.command"]);
        }
    }

    public static void Command_OpenCells(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;
        if (!jailApi.IsSimon(player)) 
            return;
        ForceOpen();
    }
    public static void Command_CloseCells(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;
        if (!jailApi.IsSimon(player))
            return;
        ForceClose();
    }
    public static void Command_Guns(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (!SpecialDays.isActive(SpecialDaysType.FreeForAll) &&
            !SpecialDays.isActive(SpecialDaysType.Teleport) &&
            !SpecialDays.isActive(SpecialDaysType.DrunkDay) &&
            !SpecialDays.isActive(SpecialDaysType.WarDay) &&

            !(SpecialDays.isCountdownActive &&
              (SpecialDays.activeSpecialDay == SpecialDaysType.FreeForAll || SpecialDays.activeSpecialDay == SpecialDaysType.Teleport || SpecialDays.activeSpecialDay == SpecialDaysType.DrunkDay || SpecialDays.activeSpecialDay == SpecialDaysType.WarDay)) &&
            (player.Team == CsTeam.Terrorist || player.Team == CsTeam.Spectator))
            return;

        if (LastRequest.isLrActive) 
            return;

        var manager = Instance.GetMenuManager();
        if (manager == null)
            return;

        string steamId = player.SteamID.ToString();
        var weaponSettings = PlayerWeaponsSettingsManager.GetPlayerWeaponSettings(steamId);

        var menu = manager.CreateMenu(Instance.Localizer["guns<menu>"], isSubMenu: false);
        bool saveGuns = weaponSettings.SelectedWeapons.Any();

        menu.AddBoolOption(Instance.Localizer["guns.save"], defaultValue: saveGuns, (p, option) =>
        {
            if (option is IT3Option boolOption)
            {
                saveGuns = boolOption.OptionDisplay!.Contains("✔");
            }
        });

        foreach (var weaponEntry in Weapons.WeaponList)
        {
            string weaponKey = weaponEntry.Key;
            string weaponName = weaponEntry.Value;

            menu.Add(weaponName, (p, option) =>
            {
                var pistolsSubMenu = manager.CreateMenu(Instance.Localizer["guns.pistol"], isSubMenu: true);
                pistolsSubMenu.ParentMenu = menu;

                foreach (var pistolEntry in Weapons.PistolsList)
                {
                    string pistolKey = pistolEntry.Key;
                    string pistolName = pistolEntry.Value;

                    pistolsSubMenu.Add(pistolName, (pistolPlayer, pistolOption) =>
                    {
                        player.StripWeapons();
                        player.GiveNamedItem(weaponKey);
                        player.GiveNamedItem(pistolKey);

                        if (saveGuns)
                        {
                            weaponSettings.SelectedWeapons["primary"] = weaponKey;
                            weaponSettings.SelectedWeapons["secondary"] = pistolKey;
                            PlayerWeaponsSettingsManager.SetPlayerWeaponSettings(steamId, weaponSettings);
                        }
                        manager.CloseMenu(player);
                    });
                }
                manager.OpenSubMenu(player, pistolsSubMenu);
            });
        }

        manager.OpenMainMenu(player, menu);
    }

    public static void Command_Simon(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (SpecialDays.isSpecialDayActive)
            return;
        if (SpecialDays.isHnsCountDownActive || SpecialDays.isWarCountDownActive || SpecialDays.isCountdownActive)
            return;

        if (player.Team == CsTeam.Terrorist || player.Team == CsTeam.Spectator)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["require.ct"]);
            return;
        }
        if (LastRequest.isLrActive)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.not.allowed"]);
            return;
        }
        if (LastRequest.GetTAlive())
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.not.allowed"]);
            return;
        }
        var simon = jailApi.GetSimon();
        if (jailApi.IsSimon(simon))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.exists"]);
            return;
        }
        if (player != null && player.Team == CsTeam.CounterTerrorist)
        {
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.selected", player.PlayerName]);
            jailApi.SetSimon(player.Slot);
        }
    }
    public static void Command_UnSimon(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (player.Team == CsTeam.Terrorist || player.Team == CsTeam.Spectator)
            return;

        if (!jailApi.IsSimon(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.simon"]);
            return;
        }
        Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.gaveup", player.PlayerName]);
        jailApi.RemoveSimon();

        jailApi.RemoveSimonInterval();
        jailApi.RemoveDeputy();

        var currentSimon = jailApi.GetSimon();

        Instance.AddTimer(Instance.Config.Simon.SetSimonIfNotAny, () =>
        {
            // Check if there is already an existing Simon
            if (jailApi.IsSimon(currentSimon))
                return;

            var cts = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.IsValid && p.PawnIsAlive).ToList();
            if (cts.Count > 0)
            {
                var newSimon = cts[new Random().Next(cts.Count)];

                // Ensure the selected newSimon is still valid and alive
                if (newSimon.IsValid && newSimon.PawnIsAlive)
                {
                    Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.selected", newSimon.PlayerName]);
                    jailApi.SetSimon(newSimon.Slot);
                }
            }
        });
    }

    public static void Command_Deputy(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (player.Team == CsTeam.Terrorist || player.Team == CsTeam.Spectator)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["require.ct"]);
            return;
        }

        if (jailApi.GetSimon() == null)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.required"]);
            return;
        }

        if (jailApi.IsSimon(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["simon.cant.deputy"]);
            return;
        }
        var deputy = jailApi.GetDeputy();
        if (jailApi.IsDeputy(deputy))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["deputy.exists"]);
            return;
        }

        if (player.Team == CsTeam.CounterTerrorist)
        {
            jailApi.SetDeputy(player.Slot);
            Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["deputy.selected", player.PlayerName]);
        }
    }
    public static void Command_UnDeputy(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (player.Team == CsTeam.Terrorist || player.Team == CsTeam.Spectator)
            return;
        var deputy = jailApi.GetDeputy();
        if (!jailApi.IsDeputy(deputy))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["not.deputy"]);
            return;
        }
        Server.PrintToChatAll(Instance.Localizer["jb.prefix"] + Instance.Localizer["deputy.gaveup", player.PlayerName]);
        jailApi.RemoveDeputy();
    }
    public static HookResult OnTakeDamage(DynamicHook hook)
    {
        CEntityInstance entity = hook.GetParam<CEntityInstance>(0);
        CTakeDamageInfo info = hook.GetParam<CTakeDamageInfo>(1);

        var ability = info.Ability.Value;
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

        // Handle friendly fire logic: enable only for Terrorists
        if (isBoxActive && attacker.Team == victim.Team)
        {
            if (attacker.Team == CsTeam.CounterTerrorist) // Prevent FF for CT
            {
                return HookResult.Handled;
            }
            // Allow FF for Terrorists
        }

        return HookResult.Continue;
    }

    public static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Laser.ApplySavedColors();
        marker.ApplySavedColors();
        HealUsageTracker.Clear();

        return HookResult.Continue;
    }
    static Circle marker = new Circle();
}
