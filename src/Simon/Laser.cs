using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using System.Drawing;
using static T3Jailbreak.T3Jailbreak;
using static T3Jailbreak.Simon;
using static T3Jailbreak.Vec;
using T3Jailbreak;

public class Laser
{
    private static readonly Dictionary<int, Color> SavedLaserColors = new();
    private static int colorPhase = 0; // To track the current phase of the color cycle
    private static float lastCycleTime = 0.0f; // To track the time of the last color change

    public void RemoveMarker()
    {
        marker.Destroy();
    }

    public void Ping(CCSPlayerController? player, float x, float y, float z)
    {
        if (player != null && Instance.JailApi!.IsSimon(player) && player.PawnIsAlive)
        {
            RemoveMarker();

            marker.Draw(60.0f, 75.0f, x, y, z, Color.Red);
        }
    }

    public void LaserTick()
    {
        if (simonSlot == INVALID_SLOT)
            return;

        CCSPlayerController? simon = Utilities.GetPlayerFromSlot(simonSlot);
        if (simon == null)
            return;

        if (!simon.PawnIsAlive)
            return;

        bool useKey = (simon.Buttons & PlayerButtons.Use) == PlayerButtons.Use;

        CCSPlayerPawn? pawn = simon.Pawn();
        CPlayer_CameraServices? camera = pawn?.CameraServices;

        if (pawn == null || pawn.AbsOrigin == null)
            return;

        JailPlayer? jailPlayer = JailPlayerFromPlayer(simon);

        if (camera == null)
            return;

        // Check if the key is being held
        if (useKey)
        {
            // Color cycling logic
            CycleLaserColor(); // Cycle color on each tick

            // Proceed with the laser drawing
            Vector eye = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + camera.OldPlayerViewOffsetZ);

            Vector? eyeVector = simon.EyeVector();
            if (eyeVector == null)
                return;

            eyeVector = Scale(eyeVector, 3000);
            Vector end = Add(eye, eyeVector);

            Color selectedColor = GetSavedColor(simon) ?? Color.Cyan;
            if (selectedColor == Color.Empty)
            {
                selectedColor = GetDynamicRGBColor(); // Use dynamic color
            }

            laser.color = selectedColor;
            laser.Move(eye, end, 2.0f, selectedColor);
        }
        else
        {
            RemoveLaser();
        }
    }

    public void SaveColor(CCSPlayerController player, Color color)
    {
        int playerSlot = player.Slot;
        SavedLaserColors[playerSlot] = color;
    }

    private Color? GetSavedColor(CCSPlayerController player)
    {
        int playerSlot = player.Slot;
        return SavedLaserColors.TryGetValue(playerSlot, out var color) ? color : null;
    }

    public static void ApplySavedColors()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || player.Team == CsTeam.Spectator)
                continue;

            if (SavedLaserColors.TryGetValue(player.Slot, out var color))
            {
                Laser laser = new Laser();
                laser.SaveColor(player, color);
            }
        }
    }
    private void CycleLaserColor()
    {
        colorPhase = (colorPhase + 1) % 9;
    }

    private Color GetDynamicRGBColor()
    {
        switch (colorPhase)
        {
            case 0: // Red
                return Color.FromArgb(255, 0, 0);
            case 1: // Green
                return Color.FromArgb(0, 255, 0);
            case 2: // Blue
                return Color.FromArgb(0, 0, 255);
            case 3: // Yellow
                return Color.FromArgb(255, 255, 0);
            case 4: // Cyan
                return Color.FromArgb(0, 255, 255);
            case 5: // Magenta
                return Color.FromArgb(255, 0, 255);
            case 6: // Orange
                return Color.FromArgb(255, 165, 0);
            case 7: // Pink
                return Color.FromArgb(255, 192, 203);
            case 8: // Purple
                return Color.FromArgb(128, 0, 128);
            default:
                return Color.Cyan;
        }
    }

    public void RemoveLaser()
    {
        laser.Destroy();
    }

    public static readonly float LASER_TIME = 1.0f; // Timer interval for color change (1 second)
    Circle marker = new Circle();
    Line laser = new Line();
}

