using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static T3Jailbreak.T3Jailbreak;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;
using static T3Jailbreak.Simon;

namespace T3Jailbreak;
public class JailPlayer
{
    // NOTA: NU UITA SA ADAUGI EVENT CHECK PT DAY-URI SI LR-URI
    public static void SetRebel(CCSPlayerController? player)
    {
        if (player == null)
            return;
        if (!player.PawnIsAlive)
            return;

        if (playerRebel.Contains(player))
            return;

        playerRebel.Add(player);
        Server.PrintToChatAll(Instance.Localizer["rebel.prefix"] + Instance.Localizer["player.is.rebel", player.PlayerName]);
        if (playerFreeday.Contains(player))
        {
            playerFreeday.Remove(player);
        }
        player.SetColor(Color.Red);
    }
    public static void ForgiveRebel(CCSPlayerController? player)
    {
        if (player == null) 
            return;

        if (player.PawnIsAlive && player.Team == CsTeam.Terrorist)
        {
            Server.PrintToChatAll(Instance.Localizer["rebel.prefix"] + Instance.Localizer["rebel.forgiven", player.PlayerName]);
            player.SetColor(Helpers.DefaultColor);

            if (playerRebel.Contains(player))
            {
                RebelList.Remove(player);
                playerRebel.Remove(player);
                player.RemoveWeapons();
                Server.NextFrame(() =>
                {
                    player.GiveNamedItem("weapon_knife");
                });
            }
        }
    }
    public static bool IsAliveRebel(CCSPlayerController? player)
    {
        var jailPlayer = JailPlayerFromPlayer(player);

        if (jailPlayer != null && player != null)
            return isRebel(player) && player.PawnIsAlive;

        return false;
    }
    public static bool isFreeday(CCSPlayerController? player)
    {
        return player!.Team == CsTeam.Terrorist && playerFreeday.Contains(player);
    }
    public static void GiveFreeday(CCSPlayerController? player)
    {
        if (player != null && player.isValid() && player.IsT())
        {
            if (playerFreeday.Contains(player))
                return;

            Server.PrintToChatAll(Instance.Localizer["freeday.prefix"] + Instance.Localizer["player.freeday", player.PlayerName]);
            player.SetColor(Color.Green);

            if (playerRebel.Contains(player))
            {
                RebelList.Remove(player);
                playerRebel.Remove(player);
            }
            playerFreeday.Add(player);
        }
    }
    public static void RemoveFreeday(CCSPlayerController? player)
    {
        if (player != null && player.isValid() && player.IsT())
        {
            playerFreeday.Remove(player);
            player.SetColor(Helpers.DefaultColor);
        }
    }
    public static List<CCSPlayerController> GetFreeday()
    {
        return playerFreeday.Where(player => playerFreeday.Contains(player) && player.isValid() && player.PawnIsAlive).ToList();
    }

    public static bool isRebel(CCSPlayerController? player)
    {
        return player!.Team == CsTeam.Terrorist && playerRebel.Contains(player);
    }
    public bool IsCuffed { get; set; } = false;
    public static Color LaserColor { get; set; } = Color.Cyan;
    public static Color MarkerColor { get; set; } = Color.Cyan;
    public static HashSet<CCSPlayerController> playerRebel = new HashSet<CCSPlayerController>();
    public static HashSet<CCSPlayerController> playerFreeday = new HashSet<CCSPlayerController>();
    public static Dictionary<CCSPlayerController, CCSPlayerController> RebelList = new Dictionary<CCSPlayerController, CCSPlayerController>();
};