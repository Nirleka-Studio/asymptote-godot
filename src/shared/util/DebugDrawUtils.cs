using Asymptote.Shared.World.Entity;
using Godot;

namespace Asymptote.Util;

public class DebugDrawUtils
{
    public static void DebugDrawNpcEye(Npc npc)
    {
        var eyePos = npc.getEyePosition();
        var eyeDir = npc.getEyeVector();
        DebugDraw.DrawRing(eyePos, Vector3.Up, 0.5f, 16, Colors.Green);
        Vector3 arrowEnd = eyePos + (eyeDir * 2.0f);
        DebugDraw.DrawArrow(eyePos, arrowEnd, 0.3f, Colors.Red);
    }
}