using System.Diagnostics;
using System.Runtime.CompilerServices;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Legacy;

/// <summary>
/// Scrollbars the mouse wheel moves with momentum: a turn sets them gliding (velocity decaying
/// exponentially, stepped every frame) instead of jumping, and a smaller step than vanilla's that
/// grows to twice that a notch while the wheel spins fast.
/// </summary>
public static class MomentumScroll
{
    /// <summary>The share of vanilla's wheel step (the wheel's raw delta: 120 units a notch) a turn scrolls.</summary>
    public const float StepScale = 1f / 3f;

    // Per second: the glide's velocity falls to 1/e in 0.1 s, and a push's whole glide covers its step.
    private const float Friction = 10f;

    // How much more a turn scrolls while the bar already glides its way at a turn's speed or faster:
    // a single notch scrolls the step, a fast spin up to twice as far a notch.
    private const float MaxBoost = 2f;

    // Units per second below which a glide stops.
    private const float Rest = 4f;

    private sealed class Glide
    {
        public float Velocity;

        public long Last;
    }

    private static readonly ConditionalWeakTable<UIScrollbar, Glide> Glides = new();

    /// <summary>A wheel turn that would have set <paramref name="bar" />'s view position to <paramref name="target" />.</summary>
    public static void Push(UIScrollbar bar, float target)
    {
        Glide glide = Glides.GetOrCreateValue(bar);
        float step = (target - bar.ViewPosition) * StepScale;
        // Turning the other way stops the glide first.
        if (MathF.Sign(step) != MathF.Sign(glide.Velocity))
            glide.Velocity = 0f;
        // A turn's own speed; the faster the bar already glides, the more it adds, up to MaxBoost.
        float push = step * Friction;
        float boost = push == 0f ? 1f : 1f + (MaxBoost - 1f) * MathF.Min(1f, MathF.Abs(glide.Velocity / push));
        glide.Velocity += push * boost;
    }

    /// <summary>Moves <paramref name="bar" /> along its glide by the time since its last frame; dragging the handle stops it.</summary>
    public static void Step(UIScrollbar bar, bool dragging)
    {
        if (!Glides.TryGetValue(bar, out Glide? glide))
            return;
        long now = Stopwatch.GetTimestamp();
        float seconds = glide.Last == 0 ? 0f : MathF.Min(0.1f, (now - glide.Last) / (float)Stopwatch.Frequency);
        glide.Last = now;
        if (dragging)
        {
            glide.Velocity = 0f;
            return;
        }
        if (glide.Velocity == 0f || seconds <= 0f)
            return;
        float decay = MathF.Exp(-Friction * seconds);
        // The distance v·e^(−kt) covers over the frame.
        float distance = glide.Velocity * (1f - decay) / Friction;
        float before = bar.ViewPosition;
        bar.ViewPosition = before + distance;
        glide.Velocity *= decay;
        // Stopped by the end of the list (ViewPosition clamps), or slow enough to rest.
        if (MathF.Abs(bar.ViewPosition - before) < MathF.Abs(distance) * 0.5f || MathF.Abs(glide.Velocity) < Rest)
            glide.Velocity = 0f;
    }
}
