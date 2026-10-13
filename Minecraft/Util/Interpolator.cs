using BoboEngine;

namespace Minecraft.Util;

public static class Interpolater
{
    private static readonly Dictionary<EasingType, Func<float, float>> functions = new()
    {
         {EasingType.CONSTANT, (float x) => 0.0f},
         {EasingType.LINEAR, (float x) => x },
         // Only implementing these 
    };


    /// <summary>
    /// Samples 
    /// </summary>
    /// <param name="e"> The easing type to use</param>
    /// <param name="x"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static float Sample(EasingType e, float x)
    {
        if (!functions.TryGetValue(e, out Func<float, float>? func)) throw new NotImplementedException($"Easing type '{e}' has not been implemented yet!");

        return func(x);
    }

    public static float Sample(string e, float x)
    {
        if (!Enum.TryParse(e, true, out EasingType easingType)) throw new InvalidEasingType($"Could not find easing type: '{e}'!");


        return Sample(easingType, x);
    }

    /// <summary>
    /// Used with <see cref="Interpolater"/> 
    /// </summary>
    public enum EasingType
    {
        /// <summary>
        /// x = 0
        /// </summary>
        CONSTANT,
        /// <summary>
        /// x = y
        /// </summary>
        LINEAR,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_BACK,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_BOUNCE,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_CIRC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_CUBIC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_ELASTIC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_EXPO,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_QUAD,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_QUART,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_QUINT,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_SINE,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_BACK,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_BOUNCE,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_CIRC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_CUBIC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_ELASTIC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_EXPO,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_QUAD,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_QUART,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_QUINT,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        IN_OUT_SINE,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_BACK,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_BOUNCE,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_CIRC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_CUBIC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_ELASTIC,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_EXPO,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_QUAD,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_QUART,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_QUINT,
        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        OUT_SINE
    }

    public class InvalidEasingType(string message) : Exception(message){}
}

public class CubicBezier
{
    public readonly float ax;
    public readonly float ay;
    public readonly float bx;
    public readonly float by;
    public Float2 A => new(ax, ay);
    public Float2 B => new(bx, by);

    public CubicBezier(float ax, float ay, float bx, float by)
    {
        if (ax < 0 || ax > 1 || bx < 0 || by < 0) throw new InvalidRangeException("ax or bx are out of range! Must be between 0 and 1 to be valid!");

        this.ax = ax;
        this.ay = ay;
        this.bx = bx;
        this.by = by;
    }

    /// <summary>
    /// Samples the curve like a path
    /// </summary>
    /// <param name="t">How far along the path to go -- the "time".</param>
    /// <returns>The point at t</returns>
    public Float2 Sample(float t)
    {
        // Desmos: https://www.desmos.com/calculator/gzu9qzilc4
        var l01 = A*t; // Same as Float2.Lerp(Float2.zero, A, t);
        var l02 = Float2.Lerp(A, B, t);
        var l03 = Float2.Lerp(B, Float2.one, t);

        var l11 = Float2.Lerp(l01, l02, t);
        var l12 = Float2.Lerp(l02, l03, t);

        var r = Float2.Lerp(l11, l12, t);

        return r;
    }

    /// <summary>
    /// Samples the curve like a one to one function
    /// Uses an interative approach due to the nature of bezier curves
    /// </summary>
    /// <param name="x">The x coordinate to sample</param>
    /// <returns>The "y" of the curve at x</returns>
    public float Apply(float x) => Sample(SolveT(x)).y;

    private float SolveT(float x)
    {
        float t = x;

        // SampleGradient not implemented yet...

        //for (int i = 0; i < 4; ++i)
        //{
        //    float error = Sample(t).x - x;
        //    if (Math.Abs(error) < 1.0E-5F)
        //    {
        //        return t;
        //    }   

        //    float gradient = SampleGradient(t).x;
        //    if (gradient < 1.0E-5F)
        //    {
        //        break;
        //    }

        //    t -= Math.Clamp(error / gradient, -0.25F, 0.25F);
        //}

        return SolveTBisect(x, t);
    }

    private float SolveTBisect(float x, float initialT)
    {
        float t0 = 0.0F;
        float t1 = 1.0F;

        float t;
        for (t = initialT; t0 < t1; t = (t1 + t0) / 2.0F)
        {
            float error = Sample(t).x - x;
            if (Math.Abs(error) < 1.0E-5F)
            {
                return t;
            }

            if (error < 0.0F)
            {
                t0 = t;
            }
            else
            {
                t1 = t;
            }
        }

        return t;
    }
    public class InvalidRangeException(string message) : Exception(message) { }
}