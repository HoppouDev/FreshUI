using System.Reflection;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The character creation (the game's <see cref="UICharacterCreation"/>: the categories, colour pickers with their
/// sliders and hex buttons, hair and clothing styles, voice, difficulty, name, the preview, Back and Create) in
/// FreshUI's components, by <see cref="GameUiMirror"/>: the game's screen keeps its state, handlers and layout; Gum
/// draws it and takes the input. Its buttons' names show by the cursor while hovered, as the game's; B goes back.
/// </summary>
internal sealed class CharacterCreationScreen : ContainerRuntime, IGumScreenUpdate
{
    public static readonly Func<bool> Shows = GumRoutes.State<UICharacterCreation>();

    // The game's hover texts: an element (a field, or one in an array field) and its text's key.
    private static readonly (string Field, int Index, string Key)[] HoverTexts =
    [
        ("_copyHexButton", -1, "UI.CopyColorToClipboard"), ("_pasteHexButton", -1, "UI.PasteColorFromClipboard"),
        ("_randomColorButton", -1, "UI.RandomizeColor"), ("_copyTemplateButton", -1, "UI.CopyPlayerToClipboard"),
        ("_pasteTemplateButton", -1, "UI.PastePlayerFromClipboard"), ("_randomizePlayerButton", -1, "UI.RandomizePlayer"),
        ("_previewArmorButton", 0, "UI.PreviewArmorNone"), ("_previewArmorButton", 1, "UI.PreviewArmorHallowed"),
        ("_previewArmorButton", 2, "UI.PreviewArmorSilver"), ("_previewArmorButton", 3, "UI.PreviewArmorFormal"),
        ("_previewArmorButton", 4, "UI.PreviewArmorSwimming"), ("_voicePrevious", -1, "UI.PlayerCreateVoicePrev"),
        ("_voiceNext", -1, "UI.PlayerCreateVoiceNext"), ("_voicePlay", -1, "UI.PlayerCreateVoicePlay"),
        ("_charInfoCategoryButton", -1, "UI.PlayerCreateCategoryInfo"), ("_clothingStylesCategoryButton", -1, "UI.PlayerCreateCategoryBodyStyle"),
        ("_hairStylesCategoryButton", -1, "UI.PlayerCreateCategoryHairStyle"), ("_colorPickers", 3, "UI.PlayerCreateCategoryHairColor"),
        ("_colorPickers", 4, "UI.PlayerCreateCategoryEyeColor"), ("_colorPickers", 5, "UI.PlayerCreateCategorySkinColor"),
        ("_colorPickers", 6, "UI.PlayerCreateCategoryShirtColor"), ("_colorPickers", 7, "UI.PlayerCreateCategoryUndershirtColor"),
        ("_colorPickers", 8, "UI.PlayerCreateCategoryPantsColor"), ("_colorPickers", 9, "UI.PlayerCreateCategoryShoesColor"),
    ];

    public static CharacterCreationScreen? Instance { get; private set; }

    private readonly GameUiMirror _mirror = new();
    private readonly TextRuntime _hover = new()
    {
        WidthUnits = DimensionUnitType.RelativeToChildren,
        HeightUnits = DimensionUnitType.RelativeToChildren,
        Width = 0,
        Height = 0,
        FontSize = 9.5f,
        HasEvents = false,
        Visible = false,
    };

    public CharacterCreationScreen()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Children.Add(_mirror);
        GumText.SetOutline(_hover, 0.75f, Color.Black);
        Children.Add(_hover);
        Instance = this;
    }

    public Gum.Forms.Controls.FrameworkElement? First => _mirror.First;

    public void UpdateScreen()
    {
        _mirror.Source = Main.MenuUI.CurrentState as UICharacterCreation;
        _mirror.Update();
        HoverText();
        if (UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock && _mirror.Source is IHaveBackButtonCommand back)
        {
            UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            back.HandleBackButtonUsage();
        }
    }

    // The hovered button's name by the cursor (the game's Draw shows it).
    private void HoverText()
    {
        string? text = null;
        if (_mirror.Source is UICharacterCreation screen)
        {
            foreach ((string field, int index, string key) in HoverTexts)
            {
                object? value = typeof(UICharacterCreation).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(screen);
                UIElement? element = index < 0 ? value as UIElement : value is Array array && index < array.Length ? array.GetValue(index) as UIElement : null;
                if (element?.IsMouseHovering == true)
                    text = Language.GetTextValue(key);
            }
        }
        bool shown = text != null;
        if (_hover.Visible != shown)
            _hover.Visible = shown;
        if (!shown)
            return;
        if (_hover.Text != text)
            _hover.Text = text;
        float x = MathF.Round(Main.mouseX / 2f + 8), y = MathF.Round(MathF.Min(Main.mouseY + 16, Main.screenHeight - 30) / 2f);
        if (_hover.X != x || _hover.Y != y)
            (_hover.X, _hover.Y) = (x, y);
    }
}
