using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Screens;

/// <summary>
/// What's shown while the player is dead (Aether's <see cref="UIDeathScreen"/>, its state and the skip on jump
/// worked out by its <c>UpdateState</c>, run here): "You were slain", the coins dropped and the respawn
/// countdown as Gum text centred on the screen, fading with the death colour, and the respawn skip prompt
/// (with the jump button's glyph, chat text) drawn by the game in a game view.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> DeathAsked = GumRoutes.GameScreen<UIDeathScreen>();
    private static readonly Func<UIDeathScreen, UIPlacedText> DeathTitle = Member<UIDeathScreen, UIPlacedText>(typeof(UIDeathScreen), "_title");
    private static readonly Func<UIDeathScreen, UIPlacedText> DeathCoins = Member<UIDeathScreen, UIPlacedText>(typeof(UIDeathScreen), "_coins");
    private static readonly Func<UIDeathScreen, UIPlacedText> DeathCountdown = Member<UIDeathScreen, UIPlacedText>(typeof(UIDeathScreen), "_countdown");
    private static readonly Func<UIDeathScreen, UIRespawnSkipPrompt> DeathSkip = Member<UIDeathScreen, UIRespawnSkipPrompt>(typeof(UIDeathScreen), "_skip");

    private ContainerRuntime? _death;
    private TextRuntime? _deathTitle, _deathCoins, _deathCountdown;
    private bool _deathActivated;

    private void PresentDeath()
    {
        bool shown = IsReady && DeathAsked() && Main.LocalPlayer.dead;
        if (_death == null)
        {
            if (!shown)
                return;
            CreateDeath();
        }
        if (_death!.Visible != shown)
            _death.Visible = shown;
        if (!shown)
            return;
        if (!_deathActivated)
        {
            _deathActivated = true;
            UIDeathScreen.Instance.Activate();
        }
        UIDeathScreen.Instance.UpdateState();
        DeathLine(_deathTitle!, DeathTitle(UIDeathScreen.Instance));
        DeathLine(_deathCoins!, DeathCoins(UIDeathScreen.Instance));
        DeathLine(_deathCountdown!, DeathCountdown(UIDeathScreen.Instance));
    }

    private void CreateDeath()
    {
        _death = new ContainerRuntime { WidthUnits = DimensionUnitType.RelativeToParent, HeightUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = 0, HasEvents = false };
        // In the HUD before its children: Gum registers what's added to an element already shown.
        Children.Insert(Children.IndexOf(_over), _death);
        _deathTitle = DeathText(26, bold: true);
        _deathCoins = DeathText(10, bold: false);
        _deathCountdown = DeathText(18, bold: true);
        _death.Children.Add(new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (batch, _) => DeathSkip(UIDeathScreen.Instance).Draw(batch),
        });
    }

    private TextRuntime DeathText(float size, bool bold)
    {
        var text = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = size,
            XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center,
            HasEvents = false,
            Visible = false,
        };
        if (bold)
            GumText.SetFontWeight(text, GumText.BoldWeight);
        GumText.SetOutline(text, bold ? 1 : 0.5f, Color.Black);
        _death!.Children.Add(text);
        return text;
    }

    // A line as the game places it (centred, its top at its position's), in its fading colour.
    private static void DeathLine(TextRuntime text, UIPlacedText line)
    {
        bool shown = line.Text != null;
        if (text.Visible != shown)
            text.Visible = shown;
        if (!shown)
            return;
        if (text.Text != line.Text)
            text.Text = line.Text;
        float y = MathF.Round(line.Position.Y / 2);
        if (text.Y != y)
            text.Y = y;
        if (text.Color != line.Color)
            text.Color = line.Color;
    }
}
