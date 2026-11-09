using Aether.Graphics.Terrain;
using Aether.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Social;
using Terraria.UI;

namespace FreshUI;

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
/// under headers; and the window's actions (in game: achievements, invites, close, save and exit; on
/// the title menu: language, back).
/// </summary>
public static class SettingsTabs
{
    // FreshUI's own names, for tabs and groups the game has no text for (English only).
    private const string DisplayName = "Display";

    private const string GraphicsName = "Graphics";

    public static string Name(SettingsTab tab) => tab switch
    {
        SettingsTab.General => Lang.menu[114].Value,
        SettingsTab.Display => DisplayName,
        SettingsTab.Graphics => GraphicsName,
        SettingsTab.Effects => Language.GetTextValue("UI.Effects"),
        SettingsTab.Interface => Lang.menu[210].Value,
        SettingsTab.Volume => Lang.menu[65].Value,
        SettingsTab.Cursor => Lang.menu[218].Value,
        _ => Lang.menu[219].Value
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

    /// <summary>The window's actions, under its tabs.</summary>
    public static IEnumerable<(Func<string> Text, Action OnClick)> Actions(bool inGame)
    {
        if (!inGame)
        {
            yield return (() => Lang.menu[103].Value, delegate
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.menuMode = 1213;
            });
            yield return (() => Lang.menu[5].Value, MenuSettingsScreen.Back);
            yield break;
        }
        yield return (() => Lang.menu[131].Value, delegate
        {
            IngameOptions.Close();
            IngameFancyUI.OpenAchievements();
        });
        if (SocialAPI.Network != null && SocialAPI.Network.CanInvite())
        {
            yield return (() => Lang.menu[147].Value, delegate
            {
                IngameOptions.Close();
                SocialAPI.Network.OpenInviteInterface();
            });
        }
        yield return (() => Lang.menu[118].Value, delegate
        {
            IngameOptions.Close();
        });
        yield return (() => Lang.inter[35].Value, delegate
        {
            IngameOptions.Close();
            Main.menuMode = 10;
            Main.gameMenu = true;
            WorldGen.SaveAndQuit();
        });
    }

    public static void Build(SettingsTab tab, SettingsWindow window)
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

    private static string AetherOnOff(string key, bool enabled) => CachedText.Format(key, Language.GetTextValue(enabled ? "AetherGraphics.On" : "AetherGraphics.Off"));

    private static void General(SettingsWindow w)
    {
        w.Header(() => Language.GetTextValue("GameUI.Gameplay"));
        w.Toggle(() => Language.GetTextValue(Main.SettingsEnabled_AutoReuseAllItems ? "UI.AutoReuseAllOn" : "UI.AutoReuseAllOff"), () => Main.SettingsEnabled_AutoReuseAllItems = !Main.SettingsEnabled_AutoReuseAllItems);
        w.Toggle(() => DoorOpeningHelper.PreferenceSettings switch
        {
            DoorOpeningHelper.DoorAutoOpeningPreference.EnabledForEverything => Language.GetTextValue("UI.SmartDoorsEnabled"),
            DoorOpeningHelper.DoorAutoOpeningPreference.EnabledForGamepadOnly => Language.GetTextValue("UI.SmartDoorsGamepad"),
            _ => Language.GetTextValue("UI.SmartDoorsDisabled")
        }, DoorOpeningHelper.CyclePreferences);
        w.Toggle(() => Language.GetTextValue(Player.Settings.HoverControl == ButtonControlMode.Hold ? "UI.HoverControlSettingIsHold" : "UI.HoverControlSettingIsClick"), Player.Settings.CycleHoverControl);
        w.Header(() => "Pausing and Saving");
        w.Toggle(() => Lang.menu[Main.autoPause ? 69 : 70].Value, () => Main.autoPause = !Main.autoPause);
        w.Toggle(() => Language.GetTextValue(Main.SettingPlayWhenUnfocused ? "UI.PlayWhenUnfocusedOn" : "UI.PlayWhenUnfocusedOff"), () => Main.SettingPlayWhenUnfocused = !Main.SettingPlayWhenUnfocused);
        w.Toggle(() => Lang.menu[Main.autoSave ? 67 : 68].Value, () => Main.autoSave = !Main.autoSave);
        // The world map is loaded with the world, so it's only switched on the title menu, as in vanilla.
        if (!w.InGame)
        {
            w.Toggle(() => Lang.menu[Main.mapEnabled ? 112 : 113].Value, () => Main.mapEnabled = !Main.mapEnabled);
        }
        w.Header(() => "Multiplayer");
        w.Toggle(() => Lang.menu[Main.HidePassword ? 212 : 211].Value, () => Main.HidePassword = !Main.HidePassword);
    }

    private static void Display(SettingsWindow w)
    {
        w.Header(() => "Window");
        // Resizing the window mid-draw isn't safe, so these wait for the next update.
        w.Toggle(() => Lang.menu[Main.graphics.IsFullScreen ? 49 : 50].Value, () => Main.QueueMainThreadAction(Main.ToggleFullScreen));
        w.Toggle(() => CachedText.Concat(Lang.menu[51].Value, ": ", CachedText.Number(Main.PendingResolutionWidth), "x", CachedText.Number(Main.PendingResolutionHeight)), NextResolution);
        w.Header(() => "Frame Rate");
        w.Toggle(() => AetherOnOff(AetherGraphics.UncappedFps.Key, AetherGraphics.UncappedFps.Get()), AetherGraphics.ToggleUncapped);
        Flag(w, AetherGraphics.SmoothEntityMovement);
        w.Header(() => "Scale");
        w.Toggle(GuiScale.LabelText, GuiScale.Cycle);
        w.Slider(() => CachedText.Format("GameUI.GameZoom", CachedText.Number(Math.Round(Main.GameZoomTarget * 100f)), CachedText.Number(Math.Round(Main.GameViewMatrix.Zoom.X * 100f))), () => Main.GameZoomTarget - 1f, v => Main.GameZoomTarget = v + 1f, 10);
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

    private static void Graphics(SettingsWindow w)
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
        w.Toggle(() => Lang.menu[Main.BackgroundEnabled ? 100 : 101].Value, () => Main.BackgroundEnabled = !Main.BackgroundEnabled);
        w.Slider(() => CachedText.Concat(Lang.menu[52].Value, ": ", CachedText.Number(Main.bgScroll), "%"), () => Main.bgScroll / 100f, delegate(float v)
        {
            Main.bgScroll = (int)(v * 100f);
            Main.caveParallax = 1f - Main.bgScroll / 500f;
        }, 1);
        w.Header(() => "World");
        w.Toggle(() => Lang.menu[ChildSafety.Disabled ? 132 : 133].Value, () => ChildSafety.Disabled = !ChildSafety.Disabled);
        w.Toggle(() => Language.GetTextValue(Main.SettingsEnabled_TilesSwayInWind ? "UI.TilesSwayInWindOn" : "UI.TilesSwayInWindOff"), () => Main.SettingsEnabled_TilesSwayInWind = !Main.SettingsEnabled_TilesSwayInWind);
        w.Toggle(() => Lang.menu[Main.SettingsEnabled_MinersWobble ? 250 : 251].Value, () => Main.SettingsEnabled_MinersWobble = !Main.SettingsEnabled_MinersWobble);
    }

    private static void Flag(SettingsWindow w, AetherGraphics.Flag flag)
    {
        w.Toggle(() => AetherOnOff(flag.Key, flag.Get()), () => flag.Set(!flag.Get()));
    }

    private static void Effects(SettingsWindow w)
    {
        w.Toggle(() => OnOff("GameUI.StormEffects", Main.UseStormEffects), () => Main.UseStormEffects = !Main.UseStormEffects);
        w.Toggle(() => OnOff("GameUI.ForegroundSunlightEffects", Main.ForegroundSunlightEffects), () => Main.ForegroundSunlightEffects = !Main.ForegroundSunlightEffects);
        w.Toggle(() => OnOff("GameUI.HeatDistortion", Main.UseHeatDistortion), () => Main.UseHeatDistortion = !Main.UseHeatDistortion);
        w.Toggle(() => CachedText.Format("GameUI.WaveQuality", Main.WaveQuality switch
        {
            1 => Language.GetTextValue("GameUI.QualityLow"),
            2 => Language.GetTextValue("GameUI.QualityMedium"),
            3 => Language.GetTextValue("GameUI.QualityHigh"),
            _ => Language.GetTextValue("GameUI.QualityOff")
        }), () => Main.WaveQuality = (Main.WaveQuality + 1) % 4);
        w.Toggle(() => OnOff("GameUI.ScreenShake", Main.UseScreenShake), () => Main.UseScreenShake = !Main.UseScreenShake);
        w.Toggle(() => Language.GetTextValue(Main.FlashyEffectsWorld ? "UI.FlashyEffectsWorldOn" : "UI.FlashyEffectsWorldOff"), () => Main.FlashyEffectsWorld = !Main.FlashyEffectsWorld);
    }

    private static void Interface(SettingsWindow w)
    {
        w.Header(() => "HUD");
        w.Toggle(() => CachedText.Format("UI.SelectHealthStyle", Language.GetTextValue(CachedText.Concat("UI.HealthManaStyle_", Main.ResourceSetsManager.ActiveSetKeyName))), Main.ResourceSetsManager.CycleResourceSet);
        w.Toggle(() => CachedText.Format("UI.SelectMapBorder", Language.GetTextValue(CachedText.Concat("UI.MinimapFrame_", Main.MinimapFrameManagerInstance.ActiveSelectionKeyName))), Main.MinimapFrameManagerInstance.CycleSelection);
        w.Slider(() => CachedText.Format("GameUI.MapScale", CachedText.Number(Math.Round(Main.MapScale * 100f))), () => (Main.MapScale - 0.5f) / 0.5f, v => Main.MapScale = v * 0.5f + 0.5f, 12);
        w.Toggle(() => CachedText.Concat(Lang.menu[123].Value, " ", Lang.menu[124 + Main.invasionProgressMode].Value), () => Main.invasionProgressMode = (Main.invasionProgressMode + 1) % 3);
        w.Toggle(() => Language.GetTextValue(BigProgressBarSystem.ShowText ? "UI.ShowBossLifeTextOn" : "UI.ShowBossLifeTextOff"), BigProgressBarSystem.ToggleShowText);
        w.Toggle(() => Lang.menu[Main.showItemText ? 71 : 72].Value, () => Main.showItemText = !Main.showItemText);
        w.Toggle(() => Lang.menu[Main.GamepadDisableInstructionsDisplay ? 241 : 242].Value, () => Main.GamepadDisableInstructionsDisplay = !Main.GamepadDisableInstructionsDisplay);
        w.Header(() => "Items and Building");
        w.Toggle(() => Lang.inter[ItemSlot.Options.HighlightNewItems ? 117 : 116].Value, () => ItemSlot.Options.HighlightNewItems = !ItemSlot.Options.HighlightNewItems);
        w.Toggle(() => Language.GetTextValue(Main.FlashyEffectsInterface ? "UI.FlashyEffectsInterfaceOn" : "UI.FlashyEffectsInterfaceOff"), () => Main.FlashyEffectsInterface = !Main.FlashyEffectsInterface);
        w.Toggle(() => Lang.menu[Main.placementPreview ? 128 : 129].Value, () => Main.placementPreview = !Main.placementPreview);
        w.Toggle(() => Lang.menu[Main.MouseShowBuildingGrid ? 229 : 230].Value, () => Main.MouseShowBuildingGrid = !Main.MouseShowBuildingGrid);
        w.Header(() => "Dialogue and Tooltips");
        w.Toggle(() => Main.DialoguePortraitPreference switch
        {
            Main.DialoguePortraitDrawOption.CloseUp => Language.GetTextValue("UI.PortraitsCloseUp"),
            Main.DialoguePortraitDrawOption.FullBodyRetro => Language.GetTextValue("UI.PortraitsFullBody"),
            Main.DialoguePortraitDrawOption.Disabled => Language.GetTextValue("UI.PortraitsDisabled"),
            _ => Language.GetTextValue("UI.PortraitsDetailed")
        }, Main.CycleNPCPortraitMode);
        w.Toggle(() => Language.GetTextValue(Main.SettingsEnabled_OpaqueBoxBehindTooltips ? "GameUI.HoverTextBoxesOn" : "GameUI.HoverTextBoxesOff"), () => Main.SettingsEnabled_OpaqueBoxBehindTooltips = !Main.SettingsEnabled_OpaqueBoxBehindTooltips);
    }

    private static void Volume(SettingsWindow w)
    {
        w.Slider(() => CachedText.Concat(Lang.menu[99].Value, " ", CachedText.Number(Math.Round(Main.musicVolume * 100f)), "%"), () => Main.musicVolume, v => Main.musicVolume = v, 2);
        w.Slider(() => CachedText.Concat(Lang.menu[98].Value, " ", CachedText.Number(Math.Round(Main.soundVolume * 100f)), "%"), () => Main.soundVolume, v => Main.soundVolume = v, 3);
        w.Slider(() => CachedText.Concat(Lang.menu[119].Value, " ", CachedText.Number(Math.Round(Main.ambientVolume * 100f)), "%"), () => Main.ambientVolume, v => Main.ambientVolume = v, 4);
    }

    private static void Cursor(SettingsWindow w)
    {
        w.Header(() => Lang.menu[64].Value);
        ColorBar(w, 5, 25, DelegateMethods.ColorLerp_HSL_H, () => Main.mouseColorSlider.Hue, v => Main.mouseColorSlider.Hue = v, () => Main.mouseColorSlider.GetHSLVector());
        ColorBar(w, 6, 25, DelegateMethods.ColorLerp_HSL_S, () => Main.mouseColorSlider.Saturation, v => Main.mouseColorSlider.Saturation = v, () => Main.mouseColorSlider.GetHSLVector());
        // The cursor's own luminance is kept at 0.15 or more, so it stays visible.
        ColorBar(w, 7, 25, DelegateMethods.ColorLerp_HSL_L, () => Utils.GetLerpValue(0.15f, 1f, Main.mouseColorSlider.Luminance, clamped: true), v => Main.mouseColorSlider.Luminance = v * 0.85f + 0.15f, delegate
        {
            Vector3 hsl = Main.mouseColorSlider.GetHSLVector();
            hsl.Z = Utils.GetLerpValue(0.15f, 1f, hsl.Z, clamped: true);
            return hsl;
        });
        w.Header(() => Lang.menu[217].Value);
        ColorBar(w, 5, 252, DelegateMethods.ColorLerp_HSL_H, () => Main.mouseBorderColorSlider.Hue, v => Main.mouseBorderColorSlider.Hue = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        ColorBar(w, 6, 252, DelegateMethods.ColorLerp_HSL_S, () => Main.mouseBorderColorSlider.Saturation, v => Main.mouseBorderColorSlider.Saturation = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        ColorBar(w, 7, 252, DelegateMethods.ColorLerp_HSL_L, () => Main.mouseBorderColorSlider.Luminance, v => Main.mouseBorderColorSlider.Luminance = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        ColorBar(w, 8, 252, DelegateMethods.ColorLerp_HSL_O, () => Main.mouseBorderColorSlider.Alpha, v => Main.mouseBorderColorSlider.Alpha = v, () => Main.mouseBorderColorSlider.GetHSLVector());
        w.Header(() => Lang.menu[218].Value);
        w.Toggle(() => LockOnHelper.UseMode switch
        {
            LockOnHelper.LockOnMode.FocusTarget => Lang.menu[232].Value,
            LockOnHelper.LockOnMode.TargetClosest => Lang.menu[233].Value,
            LockOnHelper.LockOnMode.ThreeDS => Lang.menu[234].Value,
            _ => ""
        }, LockOnHelper.CycleUseModes);
        w.Toggle(() => Lang.menu[Player.SmartCursorSettings.SmartBlocksEnabled ? 215 : 216].Value, () => Player.SmartCursorSettings.SmartBlocksEnabled = !Player.SmartCursorSettings.SmartBlocksEnabled);
        w.Toggle(() => Lang.menu[Main.cSmartCursorModeIsToggleAndNotHold ? 121 : 122].Value, () => Main.cSmartCursorModeIsToggleAndNotHold = !Main.cSmartCursorModeIsToggleAndNotHold);
        w.Toggle(() => Lang.menu[Player.SmartCursorSettings.SmartAxeAfterPickaxe ? 214 : 213].Value, () => Player.SmartCursorSettings.SmartAxeAfterPickaxe = !Player.SmartCursorSettings.SmartAxeAfterPickaxe);
    }

    /// <summary>
    /// One of the cursor's colour bars. In game, while focused it sets <c>menuMode</c> to 25 (cursor) or
    /// 252 (border): that's how vanilla's gamepad handling knows which colour it's adjusting. (On the
    /// title menu <c>menuMode</c> picks the screen, so it's left alone there.)
    /// </summary>
    private static void ColorBar(SettingsWindow w, int gamepadFeature, int menuMode, Utils.ColorLerpMethod ramp, Func<float> getValue, Action<float> setValue, Func<Vector3> rampColor)
    {
        SettingsSliderRow row = w.Slider(() => "", getValue, setValue, gamepadFeature, ramp);
        row.Tag = menuMode;
        row.Slider.BeforeDraw = () => DelegateMethods.v3_1 = rampColor();
        if (w.InGame)
        {
            row.Slider.WhileFocused = () => Main.menuMode = menuMode;
        }
    }

    /// <summary>The cursor tab's per-frame work (vanilla's): the gamepad edits Main.hBar etc., so keep them on the bars, and apply the colours.</summary>
    public static void UpdateCursorColors(SettingsWindow w)
    {
        Main.mouseColorSlider.ApplyToMainLegacyBars();
        bool cursorFocused = false;
        foreach (SettingsSliderRow row in w.SliderRows)
        {
            cursorFocused |= row.IsFocused && row.Tag == 25;
        }
        if (PlayerInput.UsingGamepad && !cursorFocused)
        {
            Main.mouseBorderColorSlider.ApplyToMainLegacyBars();
        }
        if (Main.mouseColorSlider.Luminance < 0.15f)
        {
            Main.mouseColorSlider.Luminance = 0.15f;
        }
        Main.mouseColor = Main.mouseColorSlider.GetColor();
        Main.MouseBorderColor = Main.mouseBorderColorSlider.GetColor();
    }

    private static void Controls(SettingsWindow w)
    {
        w.Button(() => Lang.menu[222].Value, OpenKeybinds(w.InGame));
        w.Toggle(() => Lang.menu[Main.ReversedUpDownArmorSetBonuses ? 220 : 221].Value, () => Main.ReversedUpDownArmorSetBonuses = !Main.ReversedUpDownArmorSetBonuses);
        w.Toggle(() => ItemSlot.Options.DisableQuickTrash ? Lang.menu[253].Value : Lang.menu[ItemSlot.Options.DisableLeftShiftTrashCan ? 224 : 223].Value, CycleQuickTrash);
    }

    private static Action OpenKeybinds(bool inGame) => inGame
        ? delegate
        {
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
