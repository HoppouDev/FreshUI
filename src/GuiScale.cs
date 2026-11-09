using Terraria;

namespace FreshUI;

/// <summary>
/// A stepped GUI scale: Auto, or 1, 2, 3… screen pixels per pixel of Terraria's art (2×2
/// texels), so the UI scale is a half-step (0.5, 1, 1.5…) and every art pixel covers the same
/// number of screen pixels. The title menu and the game use the same scale.
/// </summary>
public static class GuiScale
{
    // In Terraria's config.json, with the game's own settings; 0 is Auto.
    private const string Key = "FreshUIGuiScale";

    private static bool _loaded;

    private static int _setting;

    private static string? _label;

    private static int _labelSetting = -1;

    private static int _labelMax = -1;

    /// <summary>0 (Auto), or screen pixels per art pixel.</summary>
    public static int Setting
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                // Until it's set, the vanilla UI scale's nearest step.
                int vanilla = (int)MathF.Round(Main.UIScaleWanted * 2f, MidpointRounding.AwayFromZero);
                _setting = Math.Max(0, Main.Configuration.Get(Key, vanilla));
            }
            return _setting;
        }
        set
        {
            _setting = value;
            _loaded = true;
            Main.Configuration.Put(Key, value);
        }
    }

    /// <summary>The largest scale that fits the screen (vanilla's UI scale limit, in steps).</summary>
    public static int Max => Math.Max(1, (int)MathF.Floor(Main.instance.UIScaleMax * 2f + 0.001f));

    /// <summary>Auto's scale</summary>
    public static int Auto => Math.Max(1, Max - 1);

    /// <summary>The scale in use: the setting, or <see cref="Auto" />'s, at most <see cref="Max" />.</summary>
    public static int Current => Setting <= 0 ? Auto : Math.Min(Setting, Max);

    public static float UIScale => Current / 2f;

    /// <summary>The next setting, cycling Auto, 1, 2… the largest, Auto.</summary>
    public static void Next()
    {
        int setting = Setting;
        Setting = setting <= 0 ? 1 : setting >= Max ? 0 : setting + 1;
        // Not mid-draw: the rest of the frame is drawn at the old scale.
        Main.QueueMainThreadAction(() =>
        {
            Main.UIScale = Main.UIScaleWanted;
            Main.Configuration.Save();
        });
    }

    // For the settings window's button (mixins can't hold the closures delegates compile to).
    public static readonly Func<string> LabelText = () => Label;

    public static readonly Action Cycle = Next;

    /// <summary>The setting's button text (kept until it changes; it's drawn every frame).</summary>
    public static string Label
    {
        get
        {
            int setting = Setting;
            int max = Max;
            if (_label == null || setting != _labelSetting || max != _labelMax)
            {
                _labelSetting = setting;
                _labelMax = max;
                _label = setting <= 0 ? $"GUI Scale: Auto ({Auto})" : $"GUI Scale: {Math.Min(setting, max)}";
            }
            return _label;
        }
    }
}
