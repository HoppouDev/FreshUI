using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The dresser's clothes window (Aether's <see cref="UIDresserWindow"/>, the game's <c>Main.DrawClothesWindow</c>)
/// in FreshUI's style: a <see cref="FreshPanel"/> with the chosen part's <see cref="FreshColorPicker"/>, the
/// voice's pitch as a <see cref="FreshSlider"/> in 21 steps, Change and Cancel, the player as they'd look on a
/// <see cref="FreshPanelButton"/> that cycles the clothing style, a voice button under it that cycles the voice
/// (sounding it), and the six parts (shirt, undershirt, pants, shoes, eyes, skin) as buttons, the chosen one lit.
/// The player and the parts' icons are the game's art.
/// </summary>
internal sealed class DresserWindow : ContainerRuntime
{
    private static readonly Func<bool> Asked = GumRoutes.GameScreen<UIDresserWindow>();
    private static readonly Func<Color[]> OldClothesColor = StylistWindow.MainField<Color[]>("oldClothesColor");
    private static readonly Func<int> OldClothesStyle = StylistWindow.MainField<int>("oldClothesStyle");
    private static readonly Func<int> OldVoiceStyle = StylistWindow.MainField<int>("oldVoiceStyle");
    private static readonly Color ChoiceColor = new(255, 231, 128);
    private const float PartsLeft = 141, PartSize = 26, PartPitch = 28;
    private static readonly (int Column, int Row)[] PartCells = [(0, 0), (1, 0), (0, 1), (1, 1), (2, 0), (2, 1)];

    private readonly FreshColorPicker _colors = new() { MinLightness = 0.15f, X = 11, Y = 8 };
    private readonly FreshSlider _pitch = new() { Minimum = 0, Maximum = 1, IsSnapToTickEnabled = true, TicksFrequency = 0.05 };
    private readonly FreshValueButton _change = new() { Color = ChoiceColor }, _cancel = new() { Color = ChoiceColor };
    private readonly FreshPanelButton _preview = new() { Text = "" }, _voice = new() { Text = "" };
    private readonly FreshPanelButton[] _parts = new FreshPanelButton[6];
    private bool _syncingPitch;
    private int _voiceShown = -1;

    public DresserWindow()
    {
        HasEvents = false;
        Visible = false;
        Width = 231;
        Height = 82;
        Children.Add(new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, HeightUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = 0 });
        _colors.Get = () => new Vector3(Main.hBar, Main.sBar, Main.lBar);
        _colors.Set = hsl =>
        {
            (Main.hBar, Main.sBar, Main.lBar) = (hsl.X, hsl.Y, hsl.Z);
            Main.selColor = Main.hslToRgb(hsl.X, hsl.Y, hsl.Z);
            SetPartColor(Main.selClothes, Main.selColor);
        };
        Children.Add(_colors);
        _pitch.Visual.X = 11;
        _pitch.Visual.Y = 8 + 3 * FreshColorPicker.RowHeight;
        _pitch.Visual.Width = FreshColorPicker.BarWidth;
        _pitch.ValueChanged += (_, _) =>
        {
            if (_syncingPitch)
                return;
            Main.LocalPlayer.voicePitchOffset = Utils.Remap((float)Math.Round(_pitch.Value * 20) / 20f, 0f, 1f, -1f, 1f);
            SampleVoice();
        };
        Children.Add(_pitch.Visual);
        _change.Visual.X = 11;
        _cancel.Visual.X = 76;
        _change.Visual.Y = _cancel.Visual.Y = 64;
        _change.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) Main.SaveClothesWindow(); };
        _cancel.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) Main.CancelClothesWindow(); };
        Children.Add(_change.Visual);
        Children.Add(_cancel.Visual);
        Size(_preview, 110, 5, 25, 29);
        Size(_voice, 110, 36, 25, 23);
        _preview.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) CycleStyle(); };
        _voice.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) CycleVoice(); };
        Children.Add(_preview.Visual);
        Children.Add(_voice.Visual);
        for (int part = 0; part < 6; part++)
        {
            int index = part;
            var button = new FreshPanelButton { Text = "" };
            Size(button, PartsLeft + PartCells[part].Column * PartPitch, 5 + PartCells[part].Row * PartPitch, PartSize, PartSize);
            button.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) ChoosePart(index); };
            _parts[part] = button;
            Children.Add(button.Visual);
        }
        Children.Add(new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = DrawArt,
        });
    }

    private static void Size(FreshPanelButton button, float x, float y, float width, float height)
    {
        button.Visual.WidthUnits = button.Visual.HeightUnits = DimensionUnitType.Absolute;
        (button.Visual.X, button.Visual.Y, button.Visual.Width, button.Visual.Height) = (x, y, width, height);
    }

    /// <summary>Each frame, at draw time.</summary>
    public void Present()
    {
        bool shown = HudScreen.IsReady && Asked();
        if (Visible != shown)
            Visible = shown;
        if (!shown)
            return;
        Player player = Main.LocalPlayer;
        float x = MathF.Round((GraphicalUiElement.CanvasWidth - Width) / 2), y = MathF.Round(GraphicalUiElement.CanvasHeight / 2 + 30);
        if (X != x || Y != y)
            (X, Y) = (x, y);
        if (HudPoints.Hovered(this))
            player.mouseInterface = true;
        Main.selColor = PartColor(Main.selClothes);
        // Choosing a part resets the bars to -1; they start from its colour then.
        if (Main.hBar == -1f || Main.sBar == -1f || Main.lBar == -1f)
        {
            Vector3 hsl = Main.rgbToHsl(Main.selColor);
            (Main.hBar, Main.sBar, Main.lBar) = (hsl.X, hsl.Y, hsl.Z);
        }
        _colors.Sync();
        for (int row = 0; row < 3; row++)
        {
            HudPoints.Place(_colors.Bars[row].Visual, 2800 + row, out bool focused);
            // The game's gamepad moves a focused colour bar.
            if (focused)
                UILinkPointNavigator.Shortcuts.OPTIONS_BUTTON_SPECIALFEATURE = 5 + row;
        }
        float pitch = Utils.Remap(player.voicePitchOffset, -1f, 1f, 0f, 1f);
        if (MathF.Abs((float)_pitch.Value - pitch) > 1e-4f)
        {
            _syncingPitch = true;
            _pitch.Value = pitch;
            _syncingPitch = false;
        }
        HudPoints.Place(_pitch.Visual, 2812, out _);
        if (!Main.mouseLeft)
            Main.blockMouse = false;

        bool changed = Changed();
        SetText(_change, Language.GetTextValue("GameUI.Change"));
        SetText(_cancel, Language.GetTextValue("GameUI.Cancel"));
        _change.IsEnabled = changed;
        Main.npcChatFocus1 = Choice(_change, 2803, Main.SaveClothesWindow);
        Main.npcChatFocus2 = Choice(_cancel, 2804, () => Main.CancelClothesWindow());

        if (HudPoints.Place(_preview.Visual, 2805, out bool previewFocused))
            CycleStyle();
        _preview.IsLit = previewFocused;
        if (HudPoints.Place(_voice.Visual, 2813, out bool voiceFocused))
            CycleVoice();
        _voice.IsLit = voiceFocused;
        if (_voiceShown != player.voiceVariant)
        {
            _voiceShown = player.voiceVariant;
            _voice.SetIcon(GumTextures.Load("Images/UI/CharCreation/Voice"), null, PlayerVoiceID.Sets.Colors[player.voiceVariant]);
            _voice.CentreIcon();
        }
        for (int part = 0; part < 6; part++)
        {
            _parts[part].IsChosen = Main.selClothes == part;
            if (HudPoints.Place(_parts[part].Visual, 2806 + part, out bool focused))
                ChoosePart(part);
            _parts[part].IsLit = focused;
        }
    }

    private static void SetText(FreshValueButton button, string text)
    {
        if (button.Text != text)
            button.Text = text;
    }

    // A choice on the gamepad's point (A acts); true while hovered or focused (the game's focus fields).
    private static bool Choice(FreshValueButton button, int point, Action act)
    {
        bool acted = HudPoints.Place(button.Visual, point, out bool focused);
        button.IsLit = focused && button.IsEnabled;
        if (!button.IsEnabled)
            return false;
        if (acted)
            act();
        return focused || HudPoints.Hovered(button.Visual);
    }

    private static void ChoosePart(int part)
    {
        Main.selClothes = part;
        SoundEngine.PlaySound(12);
        Main.hBar = Main.sBar = Main.lBar = -1f;
    }

    private static void CycleStyle()
    {
        SoundEngine.PlaySound(12);
        CycleClothing(Main.LocalPlayer);
    }

    private static readonly Action<Player> CycleClothing = (Action<Player>)Delegate.CreateDelegate(typeof(Action<Player>),
        typeof(Main).GetMethod("CycleClothingStyle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!);

    private static void CycleVoice()
    {
        Main.CycleVoiceStyle(Main.LocalPlayer, 1);
        SampleVoice();
    }

    private static Color PartColor(int part)
    {
        Player player = Main.LocalPlayer;
        return part switch
        {
            0 => player.shirtColor,
            1 => player.underShirtColor,
            2 => player.pantsColor,
            3 => player.shoeColor,
            4 => player.eyeColor,
            _ => player.skinColor,
        };
    }

    private static void SetPartColor(int part, Color color)
    {
        Player player = Main.LocalPlayer;
        switch (part)
        {
            case 0: player.shirtColor = color; break;
            case 1: player.underShirtColor = color; break;
            case 2: player.pantsColor = color; break;
            case 3: player.shoeColor = color; break;
            case 4: player.eyeColor = color; break;
            default: player.skinColor = color; break;
        }
    }

    private static bool Changed()
    {
        Player player = Main.LocalPlayer;
        Color[] old = OldClothesColor();
        for (int i = 0; i < 6; i++)
        {
            if (old[i] != PartColor(i))
                return true;
        }
        return OldVoiceStyle() != player.voiceVariant || OldClothesStyle() != player.skinVariant || Main.oldVoicePitch != player.voicePitchOffset;
    }

    // A hurt sound in the player's (new) voice, from the preview dummy, as the game's window.
    private static void SampleVoice()
    {
        Player player = Main.LocalPlayer;
        Player dummy = Main.dresserInterfaceDummy;
        int voice = dummy.voiceVariant;
        Vector2 position = dummy.position;
        dummy.voiceVariant = player.voiceVariant;
        dummy.voicePitchOffset = player.voicePitchOffset;
        dummy.position = new Vector2(-1f, -1f);
        dummy.PlayHurtSound();
        dummy.position = position;
        dummy.voiceVariant = voice;
        dummy.voicePitchOffset = voice;
    }

    // The game's art: each part's icon in its colour, and the player as they'd look on the preview.
    private void DrawArt(SpriteBatch batch, Rectangle area)
    {
        for (int part = 0; part < 6; part++)
        {
            Rectangle cell = HudPoints.Units(_parts[part].Visual);
            Texture2D icon = TextureAssets.Clothes[part].Value;
            var at = new Vector2(cell.Center.X - icon.Width / 2, cell.Center.Y - icon.Height / 2);
            batch.Draw(icon, at, PartColor(part));
            if (part == 4)
                batch.Draw(GumTextures.Load("Images/UI/CharCreation/ColorEyeBack"), at, Color.White);
        }
        Player player = Main.LocalPlayer;
        Player dummy = Main.dresserInterfaceDummy;
        dummy.skinVariant = player.skinVariant;
        dummy.eyeColor = player.eyeColor;
        dummy.hairColor = player.hairColor;
        dummy.hair = player.hair;
        dummy.skinColor = player.skinColor;
        dummy.shirtColor = player.shirtColor;
        dummy.underShirtColor = player.underShirtColor;
        dummy.shoeColor = player.shoeColor;
        dummy.pantsColor = player.pantsColor;
        Rectangle preview = HudPoints.Units(_preview.Visual);
        dummy.Bottom = Main.screenPosition + new Vector2(preview.Center.X, preview.Bottom - 8);
        dummy.direction = -1;
        dummy.gravDir = 1f;
        dummy.PlayerFrame();
        dummy.socialIgnoreLight = true;
        // As the game's window: the player drawn immediately (its shaders), in the view's transform.
        Matrix transform = GumHost.BatchTransform(batch);
        batch.End();
        batch.Begin(SpriteSortMode.Immediate, null, FreshUI.Legacy.Sharp.Snapped, null, null, null, transform);
        Main.PlayerRenderer.DrawPlayer(Main.Camera, dummy, dummy.position, dummy.fullRotation, dummy.fullRotationOrigin);
    }
}
