using CounterStrikeSharp.API.Modules.Utils;

namespace T3Jailbreak;
static public class Vec
{
    // TODO: should we have versions not in place?
    static public Vector Scale(Vector vec, float t)
    {
        return new Vector(vec.X * t, vec.Y * t, vec.Z * t);
    }

    static public Vector Add(Vector v1, Vector v2)
    {
        return new Vector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
    }

    static public Vector Sub(Vector v1, Vector v2)
    {
        return new Vector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
    }

    static public Vector Normalize(Vector v1)
    {
        float length = (float)Math.Sqrt((v1.X * v1.X) + (v1.Y * v1.Y) + (v1.Z * v1.Z));

        return new Vector(v1.X / length, v1.Y / length, v1.Z / length);
    }
}