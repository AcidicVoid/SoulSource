using FlaxEngine;

namespace SoulSource.Utils;

/// <summary>
/// Math Script.
/// </summary>
[Category("Utils")]
public class Math : Script
{
    public static float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4.0f * x * x * x : 1.0f - Mathf.Pow(-2.0f * x + 2.0f, 3.0f) / 2.0f;
    }
    

}