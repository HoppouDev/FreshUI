using System.Diagnostics;
using System.Runtime.CompilerServices;
using Terraria.GameContent.UI.Elements;

namespace FreshUI;

/// <summary>
/// Scrolling the mouse wheel moves with momentum: a turn sets a glide going (velocity decaying
/// exponentially, stepped every frame) instead of jumping, and a fast spin scrolls up to twice as far a
/// notch. The game's scrollbars take a smaller step than vanilla's (<see cref="StepScale"/>); Gum's
/// (FreshScrollViewer) use a <see cref="Glide"/> of their own.
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

    /// <summary>One scroller's glide: wheel turns push it, and each frame advances it.</summary>
    public sealed class Glide
    {
        private float _velocity;
        private long _last;

        public bool IsMoving => _velocity != 0f;

        /// <summary>A wheel turn that scrolls <paramref name="step"/> units (positive: further down the content).</summary>
        public void Push(float step)
        {
            if (_velocity == 0f)
                _last = Stopwatch.GetTimestamp();
            // Turning the other way stops the glide first.
            if (MathF.Sign(step) != MathF.Sign(_velocity))
                _velocity = 0f;
            // A turn's own speed; the faster it already glides, the more it adds, up to MaxBoost.
            float push = step * Friction;
            float boost = push == 0f ? 1f : 1f + (MaxBoost - 1f) * MathF.Min(1f, MathF.Abs(_velocity / push));
            _velocity += push * boost;
        }

        public void Stop() => _velocity = 0f;

        /// <summary>How far to move for the time since the last frame (0 while still); report the move to <see cref="Moved"/>.</summary>
        public float Advance()
        {
            long now = Stopwatch.GetTimestamp();
            float seconds = _last == 0 ? 0f : MathF.Min(0.1f, (now - _last) / (float)Stopwatch.Frequency);
            _last = now;
            if (_velocity == 0f || seconds <= 0f)
                return 0f;
            float decay = MathF.Exp(-Friction * seconds);
            _velocity *= decay;
            // The distance v·e^(−kt) covers over the frame.
            return _velocity / decay * (1f - decay) / Friction;
        }

        /// <summary>Stops at the content's end (it moved much less than asked), or once slow enough to rest.</summary>
        public void Moved(float requested, float actual)
        {
            if (MathF.Abs(actual) < MathF.Abs(requested) * 0.5f || MathF.Abs(_velocity) < Rest)
                _velocity = 0f;
        }
    }

    private static readonly ConditionalWeakTable<UIScrollbar, Glide> Glides = new();

    /// <summary>A wheel turn that would have set <paramref name="bar" />'s view position to <paramref name="target" />.</summary>
    public static void Push(UIScrollbar bar, float target) =>
        Glides.GetOrCreateValue(bar).Push((target - bar.ViewPosition) * StepScale);

    /// <summary>Moves <paramref name="bar" /> along its glide by the time since its last frame; dragging the handle stops it.</summary>
    public static void Step(UIScrollbar bar, bool dragging)
    {
        if (!Glides.TryGetValue(bar, out Glide? glide))
            return;
        float distance = glide.Advance();
        if (dragging)
        {
            glide.Stop();
            return;
        }
        if (distance == 0f)
            return;
        float before = bar.ViewPosition;
        bar.ViewPosition = before + distance;
        glide.Moved(distance, bar.ViewPosition - before);
    }
}
