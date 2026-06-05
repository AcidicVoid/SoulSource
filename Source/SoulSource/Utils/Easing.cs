using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game.Utils;

/// <summary>
/// Helper functions for easing transition
/// </summary>
public static class Easing
{
    /// <summary>
    /// Contains easing function names
    /// </summary>
    public enum Easings
    {
        EaseInSine,   EaseOutSine,   EaseInOutSine,
        EaseInQuad,   EaseOutQuad,   EaseInOutQuad,
        EaseInCubic,  EaseOutCubic,  EaseInOutCubic, EaseInOutCubicNeg,
        EaseInQuart,  EaseOutQuart,  EaseInOutQuart,
        EaseInQuint,  EaseOutQuint,  EaseInOutQuint,
        EaseInExpo,   EaseOutExpo,   EaseInOutExpo,
        EaseInCirc,   EaseOutCirc,   EaseInOutCirc,
        EaseInBack,   EaseOutBack,   EaseInOutBack,
        EaseInBounce, EaseOutBounce, EaseInOutBounce
    }
    
    /// <summary>
    /// Dictionary containing easing function references
    /// </summary>
    private static readonly Dictionary<Easings, Func<float, float>> EasingMap = 
        new() {
            { Easings.EaseInSine,        EaseInSine        },
            { Easings.EaseOutSine,       EaseOutSine       },
            { Easings.EaseInOutSine,     EaseInOutSine     },
            { Easings.EaseInQuad,        EaseInQuad        },
            { Easings.EaseOutQuad,       EaseOutQuad       },
            { Easings.EaseInOutQuad,     EaseInOutQuad     },
            { Easings.EaseInCubic,       EaseInCubic       },
            { Easings.EaseOutCubic,      EaseOutCubic      },
            { Easings.EaseInOutCubic,    EaseInOutCubic    },
            { Easings.EaseInOutCubicNeg, EaseInOutCubicNeg },
            { Easings.EaseInQuart,       EaseInQuart       },
            { Easings.EaseOutQuart,      EaseOutQuart      },
            { Easings.EaseInOutQuart,    EaseInOutQuart    },
            { Easings.EaseInQuint,       EaseInQuint       },
            { Easings.EaseInOutQuint,    EaseInOutQuint    },
            { Easings.EaseOutQuint,      EaseOutQuint      },
            { Easings.EaseInExpo,        EaseInExpo        },
            { Easings.EaseOutExpo,       EaseOutExpo       },
            { Easings.EaseInOutExpo,     EaseInOutExpo     },
            { Easings.EaseInCirc,        EaseInCirc        },
            { Easings.EaseOutCirc,       EaseOutCirc       },
            { Easings.EaseInOutCirc,     EaseInOutCirc     },
            { Easings.EaseInBack,        EaseInBack        },
            { Easings.EaseOutBack,       EaseOutBack       },
            { Easings.EaseInOutBack,     EaseInOutBack     },
            { Easings.EaseInBounce,      EaseInBounce      },
            { Easings.EaseOutBounce,     EaseOutBounce     },
            { Easings.EaseInOutBounce,   EaseInOutBounce   },
        };

    /// <summary>
    /// Streamlined method, calls easing method based on enum
    /// </summary>
    /// <param name="easing">enum determining the function to call</param>
    /// <param name="x">input value</param>
    /// <returns>input value with easing applied</returns>
    public static float Ease(Easings easing, float x)
    {
        if (EasingMap.TryGetValue(easing, out var func))
        {
            return func(x);
        }
        return x; // Fallback to linear
    }
    
    // Easing Sine
    public static float EaseInSine(float x)
    {
        return 1 - Mathf.Cos((x * Mathf.Pi) / 2);
    }

    public static float EaseOutSine(float x)
    {
        return Mathf.Sin((x * Mathf.Pi) / 2);
    }

    public static float EaseInOutSine(float x)
    {
        return -(Mathf.Cos(Mathf.Pi * x) - 1) / 2;
    }

    // Easing Quad
    public static float EaseInQuad(float x)
    {
        return x * x;
    }

    public static float EaseOutQuad(float x)
    {
        return 1 - (1 - x) * (1 - x);
    }

    public static float EaseInOutQuad(float x)
    {
        return x < 0.5 ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2;
    }

    // Easing Cubic
    public static float EaseInCubic(float x)
    {
        return x * x * x;
    }

    public static float EaseOutCubic(float x)
    {
        return 1 - Mathf.Pow(1 - x, 3);
    }

    public static float EaseInOutCubic(float x)
    {
        return x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
    }
    
    public static float EaseInOutCubicNeg(float x)
    {
        int n = x >= 0f ? 1 : -1;
        x = Mathf.Abs(x);
        return (x < 0.5f ? 4.0f * x * x * x : 1.0f - Mathf.Pow(-2.0f * x + 2.0f, 3.0f) / 2.0f) * n;
    }

    // Easing Quart
    public static float EaseInQuart(float x)
    {
        return x * x * x * x;
    }

    public static float EaseOutQuart(float x)
    {
        return 1 - Mathf.Pow(1 - x, 4);
    }

    public static float EaseInOutQuart(float x)
    {
        return x < 0.5 ? 8 * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 4) / 2;
    }

    // Easing Quint
    public static float EaseInQuint(float x)
    {
        return x * x * x * x * x;
    }

    public static float EaseOutQuint(float x)
    {
        return 1 - Mathf.Pow(1 - x, 5);
    }

    public static float EaseInOutQuint(float x)
    {
        return x < 0.5 ? 16 * x * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 5) / 2;
    }

    // Easing Expo
    public static float EaseInExpo(float x)
    {
        return x == 0 ? 0 : Mathf.Pow(2, 10 * x - 10);
    }

    public static float EaseOutExpo(float x)
    {
        return Mathf.Approximately(x, 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * x);
    }

    public static float EaseInOutExpo(float x)
    {
        return x == 0f ? 0f : Mathf.Approximately(x, 1f) ? 1f : x < 0.5f 
            ? Mathf.Pow(2f, 20f * x - 10f) / 2f 
            : (2 - Mathf.Pow(2f, -20f * x + 10f)) / 2f;
    }

    // Easing Circ
    public static float EaseInCirc(float x)
    {
        return 1 - Mathf.Sqrt(1 - Mathf.Pow(x, 2));
    }

    public static float EaseOutCirc(float x)
    {
        return Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
    }

    public static float EaseInOutCirc(float x)
    {
        return x < 0.5
            ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * x, 2))) / 2
            : (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2;
    }

    // Easing Back
    public static float EaseInBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * x * x * x - c1 * x * x;
    }

    public static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }

    public static float EaseInOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        return x < 0.5f
            ? (Mathf.Pow(2f * x, 2f) * ((c2 + 1f) * 2 * x - c2)) / 2f
            : (Mathf.Pow(2f * x - 2f, 2f) * ((c2 + 1f) * (x * 2f - 2f) + c2) + 2f) / 2f;
    }

    // Easing Elastic
    public static float EaseInElastic(float x)
    {
        const float c4 = (2 * Mathf.Pi) / 3f;
        return x switch
        {
            0f => 0f,
            1f => 1f,
             _ => -Mathf.Pow(2, 10f * x - 10f) * Mathf.Sin((x * 10f - 10.75f) * c4)
        };
    }

    public static float EaseOutElastic(float x)
    {
        const float c4 = (2 * Mathf.Pi) / 3f;
        return x switch
        {
            0f => 0f,
            1f => 1f,
             _ => Mathf.Pow(2, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f
        };
    }

    public static float EaseInOutElastic(float x)
    {
        const float c5 = (2f * Mathf.Pi) / 4.5f;
        return x == 0f ? 0f : Mathf.Approximately(x, 1f) ? 1f : x < 0.5f
            ? -(Mathf.Pow(2,  20f * x - 10f) * Mathf.Sin((20f * x - 11.125f) * c5)) / 2f
            :  (Mathf.Pow(2, -20f * x + 10f) * Mathf.Sin((20f * x - 11.125f) * c5)) / 2f + 1f;
    }

    // Easing Bounce
    public static float EaseInBounce(float x)
    {
        return 1 - EaseOutBounce(1 - x);
    }

    public static float EaseOutBounce(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (x < 1 / d1)
            return n1 * x * x;
        if (x < 2 / d1)
        {
            x -= 1.5f / d1;
            return n1 * x * x + 0.75f;
        }
        if (x < 2.5 / d1)
        {
            x -= 2.25f / d1;
            return n1 * x * x + 0.9375f;
        }
        x -= 2.625f / d1;
        return n1 * x * x + 0.984375f;
    }

    public static float EaseInOutBounce(float x)
    {
        return x < 0.5
            ? (1 - EaseOutBounce(1 - 2 * x)) / 2
            : (1 + EaseOutBounce(2 * x - 1)) / 2;
    }
}