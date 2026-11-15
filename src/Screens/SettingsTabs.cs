using Aether.Graphics.Terrain;
using Aether.UI;
using FreshUI.Components;
using FreshUI.Hooks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>The settings window's tabs, in order.</summary>
public enum SettingsTab
{
    General,
    Display,
    Graphics,
    Effects,
    Interface,
    Volume,
    Cursor,
    Controls
}

/// <summary>
/// What's on each of the settings window's tabs: every setting of vanilla's in-game window and of the
/// title menu's settings pages (and Aether's graphics page), each once, with the same texts, grouped
/// under headers; and the window's actions (Back; on the title menu, Language too). In game, the rest
/// of what vanilla's settings window had (achievements, invites, save and exit) is the pause menu's.
/// </summary>
public static class SettingsTabs
{
    // FreshUI's own names, for tabs and groups the game has no text for (English only).
    private const string DisplayName = "Display";

    private const string GraphicsName = "Graphics";

    /// <summary>The open tab, kept between openings and shared by the title menu and the game.</summary>
    public static SettingsTab Tab;

    /// <summary>Opens a tab (a click on it), with the menus' tick.</summary>
    public static void Open(SettingsTab tab)
    {
        if (Tab != tab)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        Tab = tab;
    }

    public static string Name(SettingsTab tab) => tab switch
    {
        SettingsTab.General => Lang.menu[114].Value,
        SettingsTab.Display => DisplayName,
        SettingsTab.Graphics => GraphicsName,
        SettingsTab.Effects => Language.GetTextValue("UI.Effects"),
        SettingsTab.Interface => Lang.menu[210].Value,
        SettingsTab.Volume => Lang.menu[65].Value,
        SettingsTab.Cursor => Lang.menu[218].Value,
        _ => Lang.menu[222].Value
    };

    /// <summary>The tab a title-menu settings page's <c>menuMode</c> becomes (code elsewhere still opens them by mode).</summary>
    public static SettingsTab? ForMenuMode(int menuMode) => menuMode switch
    {
        112 => SettingsTab.General,
        111 => SettingsTab.Display,
        1111 or 28 or 44110 => SettingsTab.Graphics,
        2008 => SettingsTab.Effects,
        1112 => SettingsTab.Interface,
        26 => SettingsTab.Volume,
        1125 or 25 or 252 => SettingsTab.Cursor,
        1127 => SettingsTab.Controls,
        _ => null
    };

    /// <summary>The window's actions, under its tabs: Back (to the pause menu, in game), and Language on the title menu.</summary>
    public static IEnumerable<(Func<string> Text, Action OnClick)> Actions(bool inGame)
    {
        if (inGame)
        {
            yield return (() => Lang.menu[5].Value, PauseMenu.Back);
            yield break;
        }
        yield return (() => Lang.menu[103].Value, delegate
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            Main.menuMode = 1213;
        });
        yield return (() => Lang.menu[5].Value, MenuSettingsScreen.Back);
    }

    public static void Build(SettingsTab tab, SettingsScreen window)
    {
        switch (tab)
        {
        case SettingsTab.General:
            General(window);
            break;
        case SettingsTab.Display:
            Display(window);
            break;
        case SettingsTab.Graphics:
            Graphics(window);
            break;
        case SettingsTab.Effects:
            Effects(window);
            break;
        case SettingsTab.Interface:
            Interface(window);
            break;
        case SettingsTab.Volume:
            Volume(window);
            break;
        case SettingsTab.Cursor:
            Cursor(window);
            break;
        case SettingsTab.Controls:
            Controls(window);
            break;
        }
    }

    private static string OnOff(string key, bool enabled) => CachedText.Format(key, Language.GetTextValue(enabled ? "GameUI.Enabled" : "GameUI.Disabled"));

    // FreshUI's own name for a setting the game names only by its action ("Go Windowed").
    private const string FullscreenName = "Fullscreen";

    /// <summary>
    /// An on/off setting's name from the game's texts for its two states ("Autopause On", "Autopause Off"):
    /// what they share, up to a word's end, without a trailing colon.
    /// </summary>
    internal static string Pair(string on, string off)
    {
        int shared = 0;
        while (shared < on.Length && shared < off.Length && on[shared] == off[shared])
            shared++;
        // Back to a word's end ("Autosave O" from "On"/"Off" is "Autosave").
        if (shared < on.Length && shared < off.Length)
        {
            while (shared > 0 && on[shared - 1] != ' ')
                shared--;
        }
        string name = on[..shared].TrimEnd(' ', ':');
        return name.Length > 0 ? name : on;
    }

    /// <summary>An on/off setting's name from the game's "Name: {0}" text for it.</summary>
    internal static string FormatName(string key) => Language.GetTextValue(key, "").TrimEnd(' ', ':');

    private static string AetherOnOff(string key, bool enabled) => CachedText.Format(key, Language.GetTextValue(enabled ? "AetherGraphics.On" : "AetherGraphics.Off"));

    private static void General(SettingsScreen w)
    {
        w.Header(() => Language.GetTextValue("GameUI.Gameplay"));
        w.Switch(Pair(Language.GetTextValue("UI.AutoReuseAllOn"), Language.GetTextValue("UI.AutoReuseAllOff")), () => Main.SettingsEnabled_AutoReuseAllItems, () => Main.SettingsEnabled_AutoReuseAllItems = !Main.SettingsEnabled_AutoReuseAllItems);
        w.Toggle(() => DoorOpeningHelper.PreferenceSettings switch
        {
            DoorOpeningHelper.DoorAutoOpeningPreference.EnabledForEverything => Language.GetTextValue("UI.SmartDoorsEnabled"),
            DoorOpeningHelper.DoorAutoOpeningPreference.EnabledForGamepadOnly => Language.GetTextValue("UI.SmartDoorsGamepad"),
            _ => Language.GetTextValue("UI.SmartDoorsDisabled")
        }, DoorOpeningHelper.CyclePreferences);
        w.Toggle(() => Language.GetTextValue(Player.Settings.HoverControl == ButtonControlMode.Hold ? "UI.HoverControlSettingIsHold" : "UI.HoverControlSettingIsClick"), Player.Settings.CycleHoverControl);
        w.Header(() => "Pausing and Saving");
        w.Switch(Pair(Lang.menu[69].Value, Lang.menu[70].Value), () => Main.autoPause, () => Main.autoPause = !Main.autoPause);
        w.Switch(Pair(Language.GetTextValue("UI.PlayWhenUnfocusedOn"), Language.GetTextValue("UI.PlayWhenUnfocusedOff")), () => !Main.SettingPlayWhenUnfocused, () => Main.SettingPlayWhenUnfocused = !Main.SettingPlayWhenUnfocused);
        w.Switch(Pair(Lang.menu[67].Value, Lang.menu[68].Value), () => Main.autoSave, () => Main.autoSave = !Main.autoSave);
        // The world map is loaded with the world, so it's only switched on the title menu, as in vanilla.
        if (!w.InGame)
        {
            w.Switch(Pair(Lang.menu[112].Value, Lang.menu[113].Value), () => Main.mapEnabled, () => Main.mapEnabled = !Main.mapEnabled);
        }
        w.Header(() => "Multiplayer");
        w.Toggle(() => Lang.menu[Main.HidePassword ? 212 : 211].Value, () => Main.HidePassword = !Main.HidePassword);

    }

    private static void Display(SettingsScreen w)
    {
        w.Header(() => "Window");
        // Resizing the window mid-draw isn't safe, so these wait for the next update.
        w.Switch(FullscreenName, () => Main.graphics.IsFullScreen, () => Main.QueueMainThreadAction(Main.ToggleFullScreen));
        w.Toggle(() => CachedText.Concat(Lang.menu[51].Value, ": ", CachedText.Number(Main.PendingResolutionWidth), "x", CachedText.Number(Main.PendingResolutionHeight)), NextResolution);
        w.Header(() => "Frame Rate");
        w.Switch(FormatName(AetherGraphics.UncappedFps.Key), () => AetherGraphics.UncappedFps.Get(), AetherGraphics.ToggleUncapped);
        Flag(w, AetherGraphics.SmoothEntityMovement);
        w.Header(() => "Scale");
        w.Toggle(GuiScale.LabelText, GuiScale.Cycle);
        w.Slider(() => CachedText.Format("GameUI.GameZoom", CachedText.Number(Math.Round(Main.GameZoomTarget * 100f)), CachedText.Number(Math.Round(Main.GameViewMatrix.Zoom.X * 100f))), () => Main.GameZoomTarget - 1f, v => Main.GameZoomTarget = v + 1f);
    }

    // The next display mode, applied once the frame is done (as vanilla's in-game window does).
    private static void NextResolution()
    {
        int current = 0;
        for (int i = 0; i < Main.numDisplayModes; i++)
        {
            if (Main.displayWidth[i] == Main.PendingResolutionWidth && Main.displayHeight[i] == Main.PendingResolutionHeight)
            {
                current = i;
                break;
            }
        }
        int next = (current + 1) % Math.Max(1, Main.numDisplayModes);
        Main.PendingResolutionWidth = Main.displayWidth[next];
        Main.PendingResolutionHeight = Main.displayHeight[next];
        Main.QueueMainThreadAction(() => Main.SetResolution(Main.PendingResolutionWidth, Main.PendingResolutionHeight));
    }

    private static void Graphics(SettingsScreen w)
    {
        w.Header(() => "Lighting");
        w.Toggle(() => Language.GetTextValue(CachedText.Concat("UI.LightMode_", CachedText.Name(Lighting.Mode))), Lighting.NextLightMode);
        w.Toggle(() => Lang.menu[59 + Main.qaStyle].Value, delegate
        {
            if (++Main.qaStyle > 3)
            {
                Main.qaStyle = 0;
            }
        });
        Flag(w, AetherGraphics.BackgroundLighting);
        w.Header(() => "Terrain");
        w.Toggle(() => CachedText.Format("AetherGraphics.Renderer", Language.GetTextValue(CachedText.Concat("AetherGraphics.", CachedText.Name(AetherGraphics.GetTerrainMode())))), () => AetherGraphics.SetTerrainMode((RendererMode)(((int)AetherGraphics.GetTerrainMode() + 1) % 3)));
        w.Toggle(() => CachedText.Format("AetherGraphics.Quality", Language.GetTextValue(CachedText.Concat("AetherGraphics.", CachedText.Name(AetherGraphics.GetTerrainQuality())))), () => AetherGraphics.SetTerrainQuality(AetherGraphics.GetTerrainQuality() == RendererQuality.Standard ? RendererQuality.Performance : RendererQuality.Standard));
        Flag(w, AetherGraphics.ComputeTerrain);
        Flag(w, AetherGraphics.FastTerrain);
        w.Header(() => "Background");
        w.Switch(Pair(Lang.menu[100].Value, Lang.menu[101].Value), () => Main.BackgroundEnabled, () => Main.BackgroundEnabled = !Main.BackgroundEnabled);
        w.Slider(() => CachedText.Concat(Lang.menu[52].Value, ": ", CachedText.Number(Main.bgScroll), "%"), () => Main.bgScroll / 100f, delegate(float v)
        {
            Main.bgScroll = (int)(v * 100f);
            Main.caveParallax = 1f - Main.bgScroll / 500f;
        });
        w.Header(() => "World");
        w.Switch(Pair(Lang.menu[132].Value, Lang.menu[133].Value), () => ChildSafety.Disabled, () => ChildSafety.Disabled = !ChildSafety.Disabled);
        w.Switch(Pair(Language.GetTextValue("UI.TilesSwayInWindOn"), Language.GetTextValue("UI.TilesSwayInWindOff")), () => Main.SettingsEnabled_TilesSwayInWind, () => Main.SettingsEnabled_TilesSwayInWind = !Main.SettingsEnabled_TilesSwayInWind);
        w.Switch(Pair(Lang.menu[250].Value, Lang.menu[251].Value), () => Main.SettingsEnabled_MinersWobble, () => Main.SettingsEnabled_MinersWobble = !Main.SettingsEnabled_MinersWobble);
    }

    private static void Flag(SettingsScreen w, AetherGraphics.Flag flag)
    {
        w.Switch(FormatName(flag.Key), () => flag.Get(), () => flag.Set(!flag.Get()));
    }

    private static void Effects(SettingsScreen w)
    {
        w.Switch(FormatName("GameUI.StormEffects"), () => Main.UseStormEffects, () => Main.UseStormEffects = !Main.UseStormEffects);
        w.Switch(FormatName("GameUI.ForegroundSunlightEffects"), () => Main.ForegroundSunlightEffects, () => Main.ForegroundSunlightEffects = !Main.ForegroundSunlightEffects);
        w.Switch(FormatName("GameUI.HeatDistortion"), () => Main.UseHeatDistortion, () => Main.UseHeatDistortion = !Main.UseHeatDistortion);
        w.Toggle(() => CachedText.Format("GameUI.WaveQuality", Main.WaveQuality switch
        {
            1 => Language.GetTextValue("GameUI.QualityLow"),
            2 => Language.GetTextValue("GameUI.QualityMedium"),
            3 => Language.GetTextValue("GameUI.QualityHigh"),
            _ => Language.GetTextValue("GameUI.QualityOff")
        }), () => Main.WaveQuality = (Main.WaveQuality + 1) % 4);
        w.Switch(FormatName("GameUI.ScreenShake"), () => Main.UseScreenShake, () => Main.UseScreenShake = !Main.UseScreenShake);
        w.Switch(Pair(Language.GetTextValue("UI.FlashyEffectsWorldOn"), Language.GetTextValue("UI.FlashyEffectsWorldOff")), () => Main.FlashyEffectsWorld, () => Main.FlashyEffectsWorld = !Main.FlashyEffectsWorld);
    }

    private static void Interface(SettingsScreen w)
    {
        w.Header(() => "HUD");
        w.Toggle(() => CachedText.Format("UI.SelectHealthStyle", Language.GetTextValue(CachedText.Concat("UI.HealthManaStyle_", Main.ResourceSetsManager.ActiveSetKeyName))), Main.ResourceSetsManager.CycleResourceSet);
        w.Toggle(() => CachedText.Format("UI.SelectMapBorder", Language.GetTextValue(CachedText.Concat("UI.MinimapFrame_", Main.MinimapFrameManagerInstance.ActiveSelectionKeyName))), Main.MinimapFrameManagerInstance.CycleSelection);
        w.Slider(() => CachedText.Format("GameUI.MapScale", CachedText.Number(Math.Round(Main.MapScale * 100f))), () => (Main.MapScale - 0.5f) / 0.5f, v => Main.MapScale = v * 0.5f + 0.5f);
        w.Cycle(() => Lang.menu[123].Value, () => Lang.menu[124 + Main.invasionProgressMode].Value, () => Main.invasionProgressMode = (Main.invasionProgressMode + 1) % 3);
        w.Switch(Pair(Language.GetTextValue("UI.ShowBossLifeTextOn"), Language.GetTextValue("UI.ShowBossLifeTextOff")), () => BigProgressBarSystem.ShowText, BigProgressBarSystem.ToggleShowText);
        w.Switch(Pair(Lang.menu[71].Value, Lang.menu[72].Value), () => Main.showItemText, () => Main.showItemText = !Main.showItemText);
        w.Switch(Pair(Lang.menu[241].Value, Lang.menu[242].Value), () => !Main.GamepadDisableInstructionsDisplay, () => Main.GamepadDisableInstructionsDisplay = !Main.GamepadDisableInstructionsDisplay);
        w.Header(() => "Items and Building");
        w.Switch(Pair(Lang.inter[117].Value, Lang.inter[116].Value), () => ItemSlot.Options.HighlightNewItems, () => ItemSlot.Options.HighlightNewItems = !ItemSlot.Options.HighlightNewItems);
        w.Switch(Pair(Language.GetTextValue("UI.FlashyEffectsInterfaceOn"), Language.GetTextValue("UI.FlashyEffectsInterfaceOff")), () => Main.FlashyEffectsInterface, () => Main.FlashyEffectsInterface = !Main.FlashyEffectsInterface);
        w.Switch(Pair(Lang.menu[128].Value, Lang.menu[129].Value), () => Main.placementPreview, () => Main.placementPreview = !Main.placementPreview);
        w.Switch(Pair(Lang.menu[229].Value, Lang.menu[230].Value), () => Main.MouseShowBuildingGrid, () => Main.MouseShowBuildingGrid = !Main.MouseShowBuildingGrid);
        w.Header(() => "Dialogue and Tooltips");
        w.Toggle(() => Main.DialoguePortraitPreference switch
        {
            Main.DialoguePortraitDrawOption.CloseUp => Language.GetTextValue("UI.PortraitsCloseUp"),
            Main.DialoguePortraitDrawOption.FullBodyRetro => Language.GetTextValue("UI.PortraitsFullBody"),
            Main.DialoguePortraitDrawOption.Disabled => Language.GetTextValue("UI.PortraitsDisabled"),
            _ => Language.GetTextValue("UI.PortraitsDetailed")
        }, Main.CycleNPCPortraitMode);
        w.Switch(Pair(Language.GetTextValue("GameUI.HoverTextBoxesOn"), Language.GetTextValue("GameUI.HoverTextBoxesOff")), () => Main.SettingsEnabled_OpaqueBoxBehindTooltips, () => Main.SettingsEnabled_OpaqueBoxBehindTooltips = !Main.SettingsEnabled_OpaqueBoxBehindTooltips);
    }

    private static void Volume(SettingsScreen w)
    {
        w.Slider(() => CachedText.Concat(Lang.menu[99].Value, " ", CachedText.Number(Math.Round(Main.musicVolume * 100f)), "%"), () => Main.musicVolume, v => Main.musicVolume = v);
        w.Slider(() => CachedText.Concat(Lang.menu[98].Value, " ", CachedText.Number(Math.Round(Main.soundVolume * 100f)), "%"), () => Main.soundVolume, v => Main.soundVolume = v);
        w.Slider(() => CachedText.Concat(Lang.menu[119].Value, " ", CachedText.Number(Math.Round(Main.ambientVolume * 100f)), "%"), () => Main.ambientVolume, v => Main.ambientVolume = v);
    }

    private static void Cursor(SettingsScreen w)
    {
        w.Header(() => Lang.menu[64].Value);
        ColorBar(w, DelegateMethods.ColorLerp_HSL_H, () => Main.mouseColorSlider.Hue, v => Main.mouseColorSlider.Hue = v, () => Main.mouseColorSlider.GetHSLVector());
        ColorBar(w, DelegateMethods.ColorLerp_HSL_S, () => Main.mouseColorSlider.Saturation, v => Main.mouseColorSlider.Saturation = v, () => Main.mouseColorSlider.GetHSLVector());
        // The cursor's own luminance is kept at 0.15 or more, so it stays visible.
        ColorBar(w, DelegateMethods.ColorLerp_HSL_L, () => Utils.GetLerpValue(0.15f, 1f, Main.mouseColorSlider.Luminance, clamped: true), v => Main.mouseColorSlider.Luminance = v * 0.85f + 0.15f, delegate
        {
            Vector3 hsl = Main.mouseColorSlider.GetHSLVector();
            hsl.Z = Utils.GetLerpValue(0.15f, 1f, hsl.Z, clamped: true);
            return hsl;
        });
        w.Header(() => Lang.menu[217].Value);
        ColorBar(w, DelegateMethods.ColorLerp_HSL_H, () => Main.mouseBorderColorSlider.Hue, v => Main.mouseBorderColorSlider.Hue = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        ColorBar(w, DelegateMethods.ColorLerp_HSL_S, () => Main.mouseBorderColorSlider.Saturation, v => Main.mouseBorderColorSlider.Saturation = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        ColorBar(w, DelegateMethods.ColorLerp_HSL_L, () => Main.mouseBorderColorSlider.Luminance, v => Main.mouseBorderColorSlider.Luminance = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        ColorBar(w, DelegateMethods.ColorLerp_HSL_O, () => Main.mouseBorderColorSlider.Alpha, v => Main.mouseBorderColorSlider.Alpha = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        w.Header(() => Lang.menu[218].Value);
        w.Toggle(() => LockOnHelper.UseMode switch
        {
            LockOnHelper.LockOnMode.FocusTarget => Lang.menu[232].Value,
            LockOnHelper.LockOnMode.TargetClosest => Lang.menu[233].Value,
            LockOnHelper.LockOnMode.ThreeDS => Lang.menu[234].Value,
            _ => ""
        }, LockOnHelper.CycleUseModes);
        w.Switch(Pair(Lang.menu[215].Value, Lang.menu[216].Value), () => Player.SmartCursorSettings.SmartBlocksEnabled, () => Player.SmartCursorSettings.SmartBlocksEnabled = !Player.SmartCursorSettings.SmartBlocksEnabled);
        w.Toggle(() => Lang.menu[Main.cSmartCursorModeIsToggleAndNotHold ? 121 : 122].Value, () => Main.cSmartCursorModeIsToggleAndNotHold = !Main.cSmartCursorModeIsToggleAndNotHold);

        w.Toggle(() => Lang.menu[Player.SmartCursorSettings.SmartAxeAfterPickaxe ? 214 : 213].Value, () => Player.SmartCursorSettings.SmartAxeAfterPickaxe = !Player.SmartCursorSettings.SmartAxeAfterPickaxe);

    }

    /// <summary>One of the cursor's colour bars: its track shows <paramref name="ramp"/> for the colour as it is (<paramref name="rampColor"/>, HSL).</summary>
    private static void ColorBar(SettingsScreen w, Utils.ColorLerpMethod ramp, Func<float> getValue, Action<float> setValue, Func<Vector3> rampColor) =>
        w.ColorBar(getValue, setValue, percent =>
        {
            DelegateMethods.v3_1 = rampColor();
            return ramp(percent);
        });

    /// <summary>The cursor tab's per-update work (vanilla's): keep the legacy bars in step, and apply the colours.</summary>
    public static void UpdateCursorColors()
    {
        Main.mouseColorSlider.ApplyToMainLegacyBars();
        if (Main.mouseColorSlider.Luminance < 0.15f)
        {
            Main.mouseColorSlider.Luminance = 0.15f;
        }
        Main.mouseColor = Main.mouseColorSlider.GetColor();
        Main.MouseBorderColor = Main.mouseBorderColorSlider.GetColor();
    }

    private static void Controls(SettingsScreen w)
    {
        w.Header(() => Lang.menu[219].Value);
        // The input profile (as the game's key bindings menu): its bindings are the profile's.
        w.Cycle(() => ProfileName, () => PlayerInput.CurrentProfile.ShowName, NextProfile);
        w.Toggle(() => Lang.menu[Main.ReversedUpDownArmorSetBonuses ? 220 : 221].Value, () => Main.ReversedUpDownArmorSetBonuses = !Main.ReversedUpDownArmorSetBonuses);
        w.Toggle(() => ItemSlot.Options.DisableQuickTrash ? Lang.menu[253].Value : Lang.menu[ItemSlot.Options.DisableLeftShiftTrashCan ? 224 : 223].Value, CycleQuickTrash);
        // The keyboard's bindings in the game's key bindings menu's groups; the gamepad's are still in that menu.
        foreach ((int title, string[] bindings) in KeyboardGroups)
        {
            w.Header(() => Lang.menu[title].Value);
            foreach (string bind in bindings)
                Binding(w, bind);
        }
        w.Header(() => GamepadName);
        w.Button(() => GamepadBindingsName, OpenKeybinds(w.InGame));
    }

    // FreshUI's own names (English, as the group headers).
    private const string ProfileName = "Profile", GamepadName = "Gamepad", GamepadBindingsName = "Gamepad Bindings";

    // The game's key bindings menu's keyboard groups (gameplay, map, hotbar) by their titles; "sp" entries are its other rows.
    private static readonly (int Title, string[] Bindings)[] KeyboardGroups =
    [
        (164, ["MouseLeft", "MouseRight", "Up", "Down", "Left", "Right", "Jump", "Grapple", "SmartSelect", "SmartCursor",
            "QuickMount", "QuickHeal", "QuickMana", "QuickBuff", "Throw", "Inventory", "ToggleCreativeMenu", "ViewZoomIn", "ViewZoomOut", "Loadout1",
            "Loadout2", "Loadout3", "NextLoadout", "PreviousLoadout", "ToggleCameraMode", "ArmorSetAbility", "Dash", "sp20", "sp21", "sp9"]),
        (165, ["MapZoomIn", "MapZoomOut", "MapAlphaUp", "MapAlphaDown", "MapFull", "MapStyle", "sp11"]),
        (166, ["HotbarMinus", "HotbarPlus", "Hotbar1", "Hotbar2", "Hotbar3", "Hotbar4", "Hotbar5", "Hotbar6", "Hotbar7", "Hotbar8",
            "Hotbar9", "Hotbar10", "sp10"]),
    ];

    // One row of a keyboard group: a binding (its name, and its keys, which a click listens to replace), or one of the group's other rows.
    private static void Binding(SettingsScreen w, string bind)
    {
        switch (bind)
        {
        case "sp9":
            w.Button(() => Lang.menu[86].Value, () => PlayerInput.CurrentProfile.CopyGameplaySettingsFrom(PlayerInput.OriginalProfiles[CopyableProfile], InputMode.Keyboard));
            return;
        case "sp10":
            w.Button(() => Lang.menu[86].Value, () => PlayerInput.CurrentProfile.CopyHotbarSettingsFrom(PlayerInput.OriginalProfiles[CopyableProfile], InputMode.Keyboard));
            return;
        case "sp11":
            w.Button(() => Lang.menu[86].Value, () => PlayerInput.CurrentProfile.CopyMapSettingsFrom(PlayerInput.OriginalProfiles[CopyableProfile], InputMode.Keyboard));
            return;
        case "sp20":
            w.Switch(Language.GetTextValue("UI.DoubleTapDash"), () => Player.Settings.DashControl == Player.Settings.DashPreference.AllowDoubleTap,
                () => Player.Settings.DashControl = Player.Settings.DashControl == Player.Settings.DashPreference.AllowDoubleTap
                    ? Player.Settings.DashPreference.OnlyThroughHotkeys : Player.Settings.DashPreference.AllowDoubleTap);
            return;
        case "sp21":
            w.Cycle(() => Language.GetTextValue("UI.PanControlMode"), () => Language.GetTextValue(CachedText.Concat("UI.PanControlMode_", CachedText.Name(PlayerInput.CurrentProfile.PanControl_Keyboard))),
                () => Utils.CycleControlControl_OffOnClickHold(ref PlayerInput.CurrentProfile.PanControl_Keyboard));
            return;
        }
        // The game's own names and key texts for the binding (its key bindings menu's row, kept for them).
        var row = new Terraria.GameContent.UI.Elements.UIKeybindingListItem(bind, InputMode.Keyboard, Color.White);
        Func<string> name = FriendlyName.CreateDelegate<Func<string>>(row);
        Func<List<string>, string> keys = KeysText.CreateDelegate<Func<List<string>, string>>(row);
        List<string> Keys() => PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus[bind];
        w.Value(name, () => Keys().Count == 0 ? Lang.menu[195].Value : keys(Keys()), delegate
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            if (PlayerInput.ListeningTrigger != bind)
                PlayerInput.ListenFor(PlayerInput.CurrentProfile.AllowEditing ? bind : null, InputMode.Keyboard);
        }, lit: () => PlayerInput.ListeningTrigger == bind, color: () => Keys().Count == 0 ? new Color(110, 110, 110) : FreshStyle.MutedText);
    }

    private static readonly System.Reflection.MethodInfo FriendlyName = typeof(Terraria.GameContent.UI.Elements.UIKeybindingListItem)
        .GetMethod("GetFriendlyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    private static readonly System.Reflection.MethodInfo KeysText = typeof(Terraria.GameContent.UI.Elements.UIKeybindingListItem)
        .GetMethod("GenInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    // The game's own profile to reset a group from: the current one if it's one of the game's, else its default.
    private static string CopyableProfile => PlayerInput.OriginalProfiles.ContainsKey(PlayerInput.CurrentProfile.Name) ? PlayerInput.CurrentProfile.Name : "Redigit's Pick";

    private static void NextProfile()
    {
        List<string> names = PlayerInput.Profiles.Keys.ToList();
        PlayerInput.SetSelectedProfile(names[(names.IndexOf(PlayerInput.CurrentProfile.Name) + 1) % names.Count]);
    }

    private static Action OpenKeybinds(bool inGame) => inGame
        ? delegate
        {
            // Back to the settings when the key bindings close.
            PauseMenu.ReturnAfter(PauseMenu.View.Settings);
            IngameOptions.Close();
            IngameFancyUI.OpenKeybinds();
        }
        : delegate
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            Main.menuMode = 888;
            Main.MenuUI.SetState(Main.ManageControlsMenu);
        };

    // Quick trash: shift-click trashes → shift-click doesn't trash → off → back on.
    private static void CycleQuickTrash()
    {
        if (ItemSlot.Options.DisableQuickTrash)
        {
            ItemSlot.Options.DisableQuickTrash = false;
            ItemSlot.Options.DisableLeftShiftTrashCan = true;
        }
        else if (ItemSlot.Options.DisableLeftShiftTrashCan)
        {
            ItemSlot.Options.DisableLeftShiftTrashCan = false;
        }
        else
        {
            ItemSlot.Options.DisableQuickTrash = true;
            ItemSlot.Options.DisableLeftShiftTrashCan = false;
        }
    }
}
