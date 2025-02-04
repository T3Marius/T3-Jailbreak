using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace T3Jailbreak;

class Line
{
    public void Move(Vector start, Vector end, float size, Color color)
    {
        if (laserIndex == -1)
            laserIndex = Entity.DrawLaser(start, end, size, color);
        else
            Helpers.MoveLaserByIndex(laserIndex, start, end);
    }

    public void Destroy()
    {
        if (laserIndex != -1)
        {
            Entity.Remove(laserIndex, "env_beam");
            laserIndex = -1;
        }
    }

    public void DestroyDelay(float life)
    {
        if (laserIndex != -1)
        {
            CBaseEntity? laser = Utilities.GetEntityFromIndex<CBaseEntity>(laserIndex);
            laser.RemoveDelay(life, "env_beam");
        }
    }

    public Color color = Color.Cyan;
    private int laserIndex = -1;
}

class Circle
{
    private static readonly Dictionary<int, Color> SavedMarkerColors = new();

    public Circle()
    {
        for (int l = 0; l < lines.Length; l++)
            lines[l] = new Line();
    }

    public void SaveColor(CCSPlayerController player, Color color)
    {
        if (player == null)
            return;

        SavedMarkerColors[player.Slot] = color; // Save the color for the player's marker
    }

    public Color? GetSavedColor(int playerSlot)
    {
        return SavedMarkerColors.TryGetValue(playerSlot, out var color) ? color : Color.Red;
    }

    static Vector AngleOnCircle(float angle, float r, Vector mid)
    {
        // {r * cos(x),r * sin(x)} + mid
        // NOTE: we offset Z so it doesn't clip into the ground
        return new Vector((float)(mid.X + (r * Math.Cos(angle))), (float)(mid.Y + (r * Math.Sin(angle))), mid.Z + 6.0f);
    }

    public void Draw(float life, float radius, float X, float Y, float Z, Color color)
    {
        Vector mid = new Vector(X, Y, Z);
        float step = (float)(2.0f * Math.PI) / lines.Length;
        var simon = Simon.GetSimon();
        if (simon == null)
            return;

        float angleOld = 0.0f;
        float angleCur = step;
        Color selectedColor = GetSavedColor(simon.Slot) ?? Color.Red;

        // If the color is empty, fall back to red
        if (selectedColor == Color.Empty)
            selectedColor = GetDynamicRGBColor();

        for (int l = 0; l < lines.Length; l++)
        {
            Vector start = AngleOnCircle(angleOld, radius, mid);
            Vector end = AngleOnCircle(angleCur, radius, mid);

            lines[l].color = selectedColor;
            lines[l].Move(start, end, 2.0f, selectedColor); // Use selected color for the circle
            lines[l].DestroyDelay(life);

            angleOld = angleCur;
            angleCur += step;
        }
    }
    public void ApplySavedColors()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || player.Team == CsTeam.Spectator)
                continue;

            if (SavedMarkerColors.TryGetValue(player.Slot, out var color))
            {
                for (int l = 0; l < lines.Length; l++)
                {
                    lines[l].color = color;
                }
            }
        }
    }
    public void Draw(float life, float radius, Vector vec, Color color)
    {
        Draw(life, radius, vec.X, vec.Y, vec.Z, color);
    }

    public void Destroy()
    {
        for (int l = 0; l < lines.Length; l++)
            lines[l].Destroy();
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
    private static int colorPhase = 0;
    Line[] lines = new Line[50];
}

