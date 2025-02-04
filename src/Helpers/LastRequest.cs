using CounterStrikeSharp.API.Core;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static T3Jailbreak.T3Jailbreak;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace T3Jailbreak;

public static class LRHelper
{
    public enum TimerFlag
    {
        Drug,
        Beacon
    };
    public enum FadeFlags
    {
        FADE_IN,
        FADE_OUT,
        FADE_STAYOUT
    }
    public static void DisableCollision(CBaseEntity entity)
    {
        if (entity.Collision != null)
        {
            entity.Collision.SolidType = SolidType_t.SOLID_NONE;
            entity.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
        }
    }
    public static void EnableCollision(CBaseEntity entity)
    {
        if (entity.Collision != null)
        {
            entity.Collision.SolidType = SolidType_t.SOLID_BBOX;
            entity.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEFAULT;
        }
    }
    public static void GiveWeaponAmmo(CCSPlayerController player, string weaponName, int ammoToAdd)
    {
        var weapon = player.FindWeapon($"weapon_{weaponName}");
        if (weapon != null)
        {
            weapon.SetAmmo(weapon.Clip1 + ammoToAdd, weapon.Clip2); // Add ammo to the specified weapon
        }
    }
    public static bool IsValidPistol(string weaponName)
    {
        var allowedWeapons = new[]
        {
        "deagle", "p250", "revolver", "hkp2000", "tec9", "fiveseven", "glock", "elite"
    };

        return allowedWeapons.Contains(weaponName);
    }

    private static void ColorScreen(this CCSPlayerController player, Color color, float hold = 0.1f, float fade = 0.2f, FadeFlags flags = FadeFlags.FADE_IN, bool withPurge = true)
    {

    }
    static public void GiveWeapon(this CCSPlayerController? player, string name)
    {
        if (player != null)
        {
            player.GiveNamedItem($"weapon_{name}");
        }
    }
    public static void CreateLaserBeamBetweenPlayers(float time)
    {
        Vector? CTPlayerPosition = null, TPlayerPosition = null;

        // Loop through players and assign positions
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if (player.TeamNum == 3) // CT Team
                CTPlayerPosition = player.Pawn.Value?.AbsOrigin;

            if (player.TeamNum == 2) // T Team
                TPlayerPosition = player.Pawn.Value?.AbsOrigin;
        }

        if (CTPlayerPosition == null || TPlayerPosition == null)
        {
            Server.PrintToConsole("Error: One or both team positions are null. Laser beam cannot be created.");
            return;
        }

        CTPlayerPosition = new Vector(CTPlayerPosition.X, CTPlayerPosition.Y, CTPlayerPosition.Z + 50);
        TPlayerPosition = new Vector(TPlayerPosition.X, TPlayerPosition.Y, TPlayerPosition.Z + 50);

        float totalDistance = CalculateDistance(CTPlayerPosition, TPlayerPosition);

        if (totalDistance > 100.0f)
        {
            var (beamId, beam) = DrawLrLaser(CTPlayerPosition, TPlayerPosition, Color.Red, time, 2.0f);
            if (beam == null)
            {
                Server.PrintToConsole("Error: Failed to create beam.");
            }
        }
    }
    public static int GetClipSizeForWeapon(string weaponName)
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
            _ => 7,
        };
    }
    public static CCSPlayerController PickRandomShooter(CCSPlayerController t, CCSPlayerController ct)
    {
        var random = new Random();
        return random.Next(0, 2) == 0 ? t : ct;
    }
    public static void ReloadClip(CCSPlayerController player, int clipSize)
    {
        var weapon = player.FindWeapon("weapon_deagle"); // Replace with dynamic weapon if necessary
        if (weapon != null)
        {
            weapon.SetAmmo(clipSize, 0); // Reload clip with given size, no reserve
            player.PrintToChat($"{Instance.Localizer["lr.prefix"]} Reloaded! It's your turn!");
        }
    }

    private static readonly Vector VectorZero = new Vector(0, 0, 0);
    private static readonly QAngle RotationZero = new QAngle(0, 0, 0);
    public static (int, CBeam) DrawLrLaser(Vector startPos, Vector endPos, Color color, float life, float width)
    {
        if (startPos == null || endPos == null)
            return (-1, null!);

        CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");

        if (beam == null)
            return (-1, null!);

        beam.Render = color;
        beam.Width = width;

        beam.Teleport(startPos, RotationZero, VectorZero);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();

        Instance.AddTimer(life, () => { if (beam != null && beam.IsValid) beam.Remove(); });

        return ((int)beam.Index, beam);
    }
    public static void TeleportLrLaser(CBeam? laser, Vector start, Vector end)
    {
        if (laser == null || !laser.IsValid) return;
        // set pos
        laser.Teleport(start, RotationZero, VectorZero);
        // end pos
        // NOTE: we cant just move the whole vec
        laser.EndPos.X = end.X;
        laser.EndPos.Y = end.Y;
        laser.EndPos.Z = end.Z;
        Utilities.SetStateChanged(laser, "CBeam", "m_vecEndPos");
    }
    public static float CalculateDistance(Vector point1, Vector point2)
    {
        float dx = point2.X - point1.X;
        float dy = point2.Y - point1.Y;
        float dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    static public CBasePlayerWeapon? FindWeapon(this CCSPlayerController? player, string name)
    {
        // only care if player is alive
        if (player == null)
        {
            return null;
        }

        CCSPlayerPawn? pawn = player.Pawn();

        if (pawn == null)
        {
            return null;
        }

        var weapons = pawn.WeaponServices?.MyWeapons;

        if (weapons == null)
        {
            return null;
        }

        foreach (var weaponOpt in weapons)
        {
            CBasePlayerWeapon? weapon = weaponOpt.Value;

            if (weapon == null)
            {
                continue;
            }

            if (weapon.DesignerName.Contains(name))
            {
                return weapon;
            }
        }

        return null;
    }
    private const float radiusIncrement = 20.0f;
    private const int lines = 20;
    private const float initialRadius = 20.0f;
    private static readonly Dictionary<CCSPlayerController, Dictionary<TimerFlag, Timer>> PlayerTimers = [];
    private static readonly float[] g_DrugAngles = [0.0f, 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 20.0f, 15.0f, 10.0f, 5.0f, 0.0f, -5.0f, -10.0f, -15.0f, -20.0f, -25.0f, -20.0f, -15.0f, -10.0f, -5.0f];
    public static void Timer_Drug(this CCSPlayerController player)
    {
        if (player.PlayerPawn.Value?.AbsRotation is not QAngle playerRotation)
            return;

        Random _random = new Random();
        playerRotation.Z = g_DrugAngles[_random.Next(g_DrugAngles.Length)];
        player.PlayerPawn.Value!.Teleport(null, playerRotation, null);
        player.ColorScreen(
        Color.FromArgb(_random.Next(256), _random.Next(256), _random.Next(256), 128),
        255, 255, FadeFlags.FADE_OUT
        );
    }
    public static void Drug(this CCSPlayerController player, int value)
    {
        CounterStrikeSharp.API.Modules.Timers.Timer timer = Instance.AddTimer(1.0f, () => player.Timer_Drug(), TimerFlags.REPEAT);
        player.AddTimer(TimerFlag.Drug, timer);
    }
    public static void AddTimer(this CCSPlayerController player, TimerFlag timerflag, CounterStrikeSharp.API.Modules.Timers.Timer timer)
    {
        player.RemoveTimer(timerflag);

        if (!PlayerTimers.TryGetValue(player, out Dictionary<TimerFlag, Timer>? timers))
        {
            timers = [];
            PlayerTimers[player] = timers;
        }

        timers[timerflag] = timer;
    }
    public static void RemoveTimer(this CCSPlayerController player, TimerFlag timerflag)
    {
        if (PlayerTimers.TryGetValue(player, out Dictionary<TimerFlag, Timer>? timers))
        {
            if (timers.TryGetValue(timerflag, out Timer? timer))
            {
                timer.Kill();
                timers.Remove(timerflag);

                if (timers.Count == 0)
                {
                    PlayerTimers.Remove(player);
                }
            }
        }
    }
    public static void KillDrug(this CCSPlayerController player)
    {
        if (player.PlayerPawn.Value?.AbsRotation is not QAngle playerRotation)
        {
            return;
        }

        player.RemoveTimer(TimerFlag.Drug);
        playerRotation.Z = 0.0f;
        player.PlayerPawn.Value!.Teleport(null, playerRotation, null);
        player.ColorScreen(
            Color.FromArgb(0, 0, 0, 0), 1536, 1536, FadeFlags.FADE_IN);
    }
    private static CBeam? CreateAndDrawBeam(Vector start, Vector end, Color color, float life, float width)
    {
        CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");

        if (beam != null)
        {
            beam.Render = color;
            beam.Width = width;
            beam.Teleport(start, new QAngle(), new Vector());
            beam.EndPos.X = end.X;
            beam.EndPos.Y = end.Y;
            beam.EndPos.Z = end.Z;
            beam.DispatchSpawn();
            Instance.AddTimer(life, () => beam.Remove());
        }

        return beam;
    }
    public static void Beacon(this CCSPlayerController player)
    {
        Vector? absOrigin = player.PlayerPawn.Value?.AbsOrigin;

        if (absOrigin == null)
        {
            return;
        }

        float step = (float)(2 * Math.PI) / lines;
        float angle = 0.0f;
        Color teamColor = player.TeamNum == 2 ? Color.Red : Color.Blue;

        List<CBeam> beams = [];

        for (int i = 0; i < lines; i++)
        {
            Vector start = CalculateCirclePoint(angle, initialRadius, absOrigin);
            angle += step;
            Vector end = CalculateCirclePoint(angle, initialRadius, absOrigin);

            CBeam? beam = CreateAndDrawBeam(start, end, teamColor, 1.0f, 2.0f);

            if (beam != null)
            {
                beams.Add(beam);
            }
        }

        float elapsed = 0.0f;

        Instance.AddTimer(0.1f, () =>
        {
            if (elapsed >= 0.9f)
            {
                return;
            }

            MoveBeams(beams, absOrigin, angle, step, radiusIncrement, elapsed);
            elapsed += 0.1f;
        }, TimerFlags.REPEAT);

        player.ExecuteClientCommand($"play sounds/tools/sfm/beep.vsnd_c");
    }
    static public void SetAmmo(this CBasePlayerWeapon? weapon, int clip, int reserve)
    {
        if (weapon == null)
            return;

        CCSWeaponBaseVData? weaponData = weapon.As<CCSWeaponBase>().VData;


        if (weaponData != null)
        {
            if (reserve > weaponData.PrimaryReserveAmmoMax)
            {
                weaponData.PrimaryReserveAmmoMax = reserve;
            }
        }

        if (clip != -1)
        {
            weapon.Clip1 = clip;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        }

        if (reserve != -1)
        {
            weapon.ReserveAmmo[0] = reserve;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        }
    }
    private static void MoveBeams(List<CBeam> beams, Vector mid, float angle, float step, float radiusIncrement, float elapsed)
    {
        float radius = initialRadius + (radiusIncrement * (elapsed / 0.1f));

        foreach (CBeam beam in beams)
        {
            Vector start = CalculateCirclePoint(angle, radius, mid);
            angle += step;
            Vector end = CalculateCirclePoint(angle, radius, mid);
            TeleportLaser(beam, start, end);
        }
    }
    private static void TeleportLaser(CBeam beam, Vector start, Vector end)
    {
        if (beam != null && beam.IsValid)
        {
            beam.Teleport(start, new QAngle(), new Vector());
            beam.EndPos.X = end.X;
            beam.EndPos.Y = end.Y;
            beam.EndPos.Z = end.Z;
            Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
        }
    }
    private static Vector CalculateCirclePoint(float angle, float radius, Vector mid)
    {
        return new Vector(
            (float)(mid.X + (radius * Math.Cos(angle))),
            (float)(mid.Y + (radius * Math.Sin(angle))),
            mid.Z + 6.0f
        );
    }
    public static void StartNoScope()
    {
        var T = LastRequest.terrorist;
        var CT = LastRequest.ct;

        if (CT == null || T == null)
            return;
        if (!LastRequest.NoScopeIsActive())
            return;

        var activeCTWeapon = CT.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon.Value;
        var activeTWeapon = T.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeCTWeapon != null && activeTWeapon != null)
        {
            activeCTWeapon.NextSecondaryAttackTick = Server.TickCount + 99999;
            activeTWeapon.NextSecondaryAttackTick = Server.TickCount + 99999;
        }
    }
    public static void StartSDNoScope()
    {
        if (!SpecialDays.NoScopeDayIsActive())
            return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.PawnIsAlive)
            {
                var activeWeapon = player.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon.Value;
                if (activeWeapon != null)
                {
                    activeWeapon.NextSecondaryAttackTick = Server.TickCount + 99999;
                }
            }
        }
    }

    public static void EndNoScope()
    {
        var T = LastRequest.terrorist;
        var CT = LastRequest.ct;

        if (CT == null || T == null)
            return;
        if (!LastRequest.NoScopeIsActive())
            return;

        var activeCTWeapon = CT.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon.Value;
        var activeTWeapon = T.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeCTWeapon != null && activeTWeapon != null)
        {
            activeCTWeapon.NextSecondaryAttackTick = Server.TickCount;
            activeTWeapon.NextSecondaryAttackTick = Server.TickCount;
        }
    }
    public static void ShotForShotDeagle()
    {

    }
}