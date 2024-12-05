using System;

public static class Extensions
{
    public static float LerpUnclamped(this float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public static float Clamp(this float value, float min, float max)
    {
        return Math.Clamp(value, min, max);
    }
}