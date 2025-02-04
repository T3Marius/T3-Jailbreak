using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace T3Jailbreak;

public static class Entity
{
    public static int DrawLaser(Vector start, Vector end, float width, Color color)
    {
        CEnvBeam? laser = Utilities.CreateEntityByName<CEnvBeam>("env_beam");

        if (laser == null)
            return -1;

        laser.SetLaserColor(color);
        laser.Width = 2.0f;

        laser.MoveLaser(start, end);

        laser.DispatchSpawn();

        return (int)laser.Index;
    }
    static public void Remove(int index, String name)
    {
        CBaseEntity? ent = Utilities.GetEntityFromIndex<CBaseEntity>(index);

        if (ent != null && ent.DesignerName == name)
            ent.Remove();
    }
}
