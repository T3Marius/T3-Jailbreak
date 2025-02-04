using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static T3Jailbreak.T3Jailbreak;

namespace T3Jailbreak;

public static class Queue
{
    private static readonly LinkedList<CCSPlayerController> QueueList = new();
    private const double MaxCtRatio = 0.5;

    public static void Load()
    {
        foreach (var cmd in Instance.Config.Commands.QueueCommands)
        {
            Instance.AddCommand($"css_{cmd}", "Enters the CT queue", Command_Queue);
        }
        foreach (var cmd in Instance.Config.Commands.QueueListCommands)
        {
            Instance.AddCommand($"css_{cmd}", "Shows queue list", Command_QueueList);
        }
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Pre);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.RegisterEventHandler<EventPlayerTeam>(OnJoinTeam, HookMode.Pre);
    }

    public static void Command_Queue(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (player.Team != CsTeam.Terrorist)
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["must.be.t"]);
            return;
        }

        // ✅ Remove unnecessary CT balance check: Players should always be allowed to enter the queue!

        bool hasSkipPermission = Instance.Config.Prisoniers.SkipQueuePermissions.Any(permission =>
            AdminManager.PlayerHasPermissions(player, permission));

        if (QueueList.Contains(player))
        {
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["queue.already.in"]);
            return;
        }

        if (hasSkipPermission)
        {
            var node = QueueList.First;
            while (node != null && AdminManager.PlayerHasPermissions(node.Value, Instance.Config.Prisoniers.SkipQueuePermissions.First()))
            {
                node = node.Next;
            }
            if (node != null)
            {
                QueueList.AddBefore(node, player);
            }
            else
            {
                QueueList.AddLast(player);
            }
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["vip.queue"]);
        }
        else
        {
            QueueList.AddLast(player);
            info.ReplyToCommand(Instance.Localizer["jb.prefix"] + Instance.Localizer["queue.added"]);
        }
    }

    public static void Command_QueueList(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        var manager = Instance.GetMenuManager();
        if (manager == null)
            return;

        var queueMenu = manager.CreateMenu(Instance.Localizer["queue<menu>"], isSubMenu: false);

        if (!QueueList.Any())
        {
            queueMenu.AddTextOption(Instance.Localizer["queue.empty"]);
        }

        int position = 1;
        foreach (var queuedPlayer in QueueList)
        {
            var queuemessage = $"<font color='#FFFF00'>#{position}.</font> {queuedPlayer.PlayerName}";
            queueMenu.Add(queuemessage, (p, option) => { });
            position++;
        }

        manager.OpenMainMenu(player, queueMenu);
    }

    private static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        var tPlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.Team == CsTeam.Terrorist).ToList();
        var ctPlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.Team == CsTeam.CounterTerrorist).ToList();

        int maxAllowedCTs = Math.Max(1, (int)Math.Floor(tPlayers.Count * MaxCtRatio));

        while (ctPlayers.Count < maxAllowedCTs && QueueList.Any())
        {
            var nextPlayer = QueueList.First!.Value;
            QueueList.RemoveFirst();

            if (nextPlayer == null || !nextPlayer.IsValid)
            {
                continue;
            }

            if (nextPlayer.Team == CsTeam.Terrorist)
            {
                nextPlayer.ChangeTeam(CsTeam.CounterTerrorist);

                if (nextPlayer.Team == CsTeam.CounterTerrorist)
                {
                    ctPlayers.Add(nextPlayer);
                }
            }
        }

        return HookResult.Continue;
    }


    private static HookResult OnJoinTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        int tCount = Utilities.GetPlayers().Count(p => p.Team == CsTeam.Terrorist);
        int ctCount = Utilities.GetPlayers().Count(p => p.Team == CsTeam.CounterTerrorist);
        int maxAllowedCTs = (int)Math.Floor(tCount * MaxCtRatio);

        if (@event.Team == (byte)CsTeam.CounterTerrorist)
        {
            if (ctCount >= maxAllowedCTs && tCount > 0)
            {
                player.ChangeTeam(CsTeam.Terrorist);
                return HookResult.Handled;
            }

            if (QueueList.Contains(player))
            {
                QueueList.Remove(player);
            }

            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    private static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        QueueList.Remove(player);
        return HookResult.Continue;
    }
}
