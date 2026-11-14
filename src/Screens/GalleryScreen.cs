using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace FreshUI.Screens;

/// <summary>
/// The widget gallery (<see cref="GumGallery"/>): every FreshUI component in a grid of panels, as Gum's
/// own all-controls showcase lays out Gum's, under a toolbar with the integer UI scale. Edit a
/// component or this screen under <c>./aether gallery</c> and hot reload rebuilds it.
/// </summary>
public sealed class GalleryScreen : ContainerRuntime
{
    // The grid, in UI pixels.
    private const float CellWidth = 150, CellHeight = 104, Gap = 6, Margin = 8, ToolbarHeight = 22;
    private const int Columns = 4;

    private static readonly Color Backdrop = new(23, 25, 48);
    private static readonly Color PanelColor = new Color(63, 82, 151) * 0.7f;
    private static readonly Color Muted = new(170, 175, 200);

    private readonly ContainerRuntime _grid;
    // The controls, in one spatial-navigation zone: the gamepad's focus moves to the nearest control in
    // the direction pressed.
    private readonly InteractiveGue _zone;
    private static Gum.Forms.Controls.FrameworkElement? _first;
    private FreshSlider? _scale;

    /// <summary>The gallery that shows, if one has been built.</summary>
    public static GalleryScreen? Current { get; private set; }

    /// <summary>The first control (the gamepad's starting point).</summary>
    public static Gum.Forms.Controls.FrameworkElement? First() => _first;

    public GalleryScreen()
    {
        WidthUnits = DimensionUnitType.RelativeToParent;
        HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Children.Add(FreshStyle.Backdrop(Backdrop));
        _zone = GumGamepad.SpatialZone();
        Children.Add(_zone);
        BuildToolbar();

        var page = new FreshScrollViewer();
        page.Visual.X = Margin;
        page.Visual.Y = ToolbarHeight + Margin;
        page.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        page.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        page.Visual.Width = -2 * Margin;
        page.Visual.Height = -(ToolbarHeight + 2 * Margin);
        _zone.Children.Add(page.Visual);
        _grid = new ContainerRuntime
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = Columns * (CellWidth + Gap) - Gap,
            HasEvents = false,
        };
        page.InnerPanel.Children.Add(_grid);

        Cell(0, 0, "Button, with an icon", Buttons);
        Cell(1, 0, "Label", Labels);
        Cell(2, 0, "Icon button", IconButtons);
        Cell(3, 0, "Item slot", ItemSlots);
        Cell(0, 1, "Toggle switch", ToggleSwitches);
        Cell(1, 1, "Slider", Sliders);
        Cell(2, 1, "Scroll area (smooth)", stack => ScrollArea(stack, smooth: true));
        Cell(3, 1, "Scroll area (stepped)", stack => ScrollArea(stack, smooth: false));
        Cell(0, 2, "Panel button", PanelButtons);
        Cell(1, 2, "Window", Window);
        Cell(2, 2, "Colour bars", ColorBars);
        Cell(3, 2, "Game view", GameView);
        Cell(0, 3, "Option buttons", OptionButtons);
        Cell(1, 3, "Text field", TextFields);
        _grid.Height = 4 * (CellHeight + Gap) - Gap;
        // Gum's spatial navigation passes over controls inside another candidate, and from the toolbar
        // the page (a scroll area) is one: down goes to the first control instead.
        _scale!.SpatialNavigationDown = _first;
        Current = this;
    }

    // ---------- layout ----------

    private void BuildToolbar()
    {
        // An odd height: the 9-pixel slider centres on whole pixels.
        var bar = Row(ToolbarHeight - 1, Margin, 1);
        _zone.Children.Add(bar);
        bar.Children.Add(Text("FreshUI widgets", 10, weight: 700).Visual);
        bar.Children.Add(Spacer(16));
        bar.Children.Add(Text("UI scale", 8, Muted).Visual);

        var scale = new FreshSlider
        {
            Minimum = 1,
            Maximum = Math.Max(UiPixels.Scale, GuiScale.Max),
            IsSnapToTickEnabled = true,
            TicksFrequency = 1,
            Value = UiPixels.Scale,
        };
        scale.Visual.Width = 60;
        FreshLabel value = Text($"{UiPixels.Scale}×", 8);
        // Screen pixels per UI pixel for the gallery only; the game's GUI scale setting stays as it was.
        scale.ValueChanged += (_, _) =>
        {
            GumHost.ScaleOverride = (int)Math.Round(scale.Value);
            value.Text = $"{GumHost.ScaleOverride}×";
        };
        bar.Children.Add(scale.Visual);
        bar.Children.Add(value.Visual);
        _scale = scale;
    }

    private void Cell(int column, int row, string title, Action<ContainerRuntime> fill)
    {
        var panel = new ContainerRuntime
        {
            X = column * (CellWidth + Gap),
            Y = row * (CellHeight + Gap),
            Width = CellWidth,
            Height = CellHeight,
            HasEvents = false,
        };
        panel.Children.Add(FreshPanel.Slice(FreshStyle.PanelBackground, PanelColor));
        panel.Children.Add(FreshPanel.Slice(FreshStyle.PanelBorder, Color.Black));
        var stack = new ContainerRuntime
        {
            X = 6,
            Y = 5,
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = -12,
            Height = 0,
            ChildrenLayout = ChildrenLayout.TopToBottomStack,
            StackSpacing = 4,
            HasEvents = false,
        };
        // In a row of whole pixels: text is a fractional height, and pixel art stacked below it would
        // land between pixels.
        var heading = Row(10);
        heading.Children.Add(Text(title, 8, Muted).Visual);
        stack.Children.Add(heading);
        fill(stack);
        panel.Children.Add(stack);
        _grid.Children.Add(panel);
    }

    // A row of items centred on its middle, as tall as given: a control and its label line up. Give it
    // a height of the same parity as its pixel art (odd for the 9-pixel switches and sliders), so the art
    // centres on whole pixels.
    private static ContainerRuntime Row(float height, float x = 0, float y = 0)
    {
        var row = new ContainerRuntime
        {
            X = x,
            Y = y,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.Absolute,
            Width = 0,
            Height = height,
            ChildrenLayout = ChildrenLayout.LeftToRightStack,
            StackSpacing = 4,
            HasEvents = false,
        };
        ((System.Collections.Specialized.INotifyCollectionChanged)row.Children).CollectionChanged += (_, e) =>
        {
            foreach (GraphicalUiElement item in e.NewItems ?? Array.Empty<GraphicalUiElement>())
            {
                item.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                item.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
                item.Y = 0;
            }
        };
        return row;
    }

    // Gum's buttons are as tall as their text plus Height; these are a fixed size.
    private static void Size(Gum.Forms.Controls.FrameworkElement control, float width, float height)
    {
        control.Visual.WidthUnits = DimensionUnitType.Absolute;
        control.Visual.HeightUnits = DimensionUnitType.Absolute;
        control.Visual.Width = width;
        control.Visual.Height = height;
    }

    private static ContainerRuntime Spacer(float width) => new() { Width = width, Height = 1, HasEvents = false };

    private static FreshLabel Text(string text, float size, Color? color = null, float weight = 0)
    {
        var label = new FreshLabel { Text = text, FontSize = size, OutlineWidth = 0.5f };
        if (color is Color c)
            label.Color = c;
        if (weight > 0)
            label.FontWeight = weight;
        return label;
    }

    // ---------- the components ----------

    private static void Buttons(ContainerRuntime stack)
    {
        int clicks = 0;
        var button = new FreshButton { Text = "Click me", FontSize = 8 };
        Size(button, 80, 16);
        button.Click += (_, _) => button.Text = $"Clicked {++clicks}×";
        _first = button;
        FreshTooltip.Attach(button.Visual, () => "A tooltip follows the cursor");
        var disabled = new FreshButton { Text = "Disabled", FontSize = 8, IsEnabled = false };
        Size(disabled, 80, 16);
        stack.Children.Add(button.Visual);
        stack.Children.Add(disabled.Visual);

        // With an icon: the game's 2×2-texel art at half a UI pixel per texel.
        var row = Row(16);
        var heal = new FreshIconTextButton { Text = "Heal", FontSize = 8, Icon = GumTextures.Load("Images/Heart") };
        Size(heal, 50, 16);
        var buy = new FreshIconTextButton { Text = "Buy", FontSize = 8, Icon = GumTextures.Load("Images/Item_73"), IsEnabled = false };
        Size(buy, 50, 16);
        row.Children.Add(heal.Visual);
        row.Children.Add(buy.Visual);
        stack.Children.Add(row);
    }

    private static void Labels(ContainerRuntime stack)
    {
        stack.Children.Add(Text("Regular text", 9).Visual);
        stack.Children.Add(Text("Bold text", 9, weight: 700).Visual);
        var italic = Text("Italic text", 9);
        italic.IsItalic = true;
        stack.Children.Add(italic.Visual);
        var fancy = Text("Gradient & shadow", 11, weight: 600);
        fancy.Gradient = (new Color(255, 240, 150), new Color(230, 140, 40));
        fancy.HasShadow = true;
        stack.Children.Add(fancy.Visual);
    }

    private static void IconButtons(ContainerRuntime stack)
    {
        var row = Row(12);
        Texture2D unlocked = GumTextures.Load("Images/Lock_1"), locked = GumTextures.Load("Images/Lock_0");
        var toggle = new FreshIconButton
        {
            IsToggle = true,
            Look = new FreshIconButtonLook(unlocked, new Rectangle(0, 0, 24, unlocked.Height), unlocked, new Rectangle(26, 0, 24, unlocked.Height)),
            CheckedLook = new FreshIconButtonLook(locked, new Rectangle(0, 0, 24, locked.Height), locked, new Rectangle(26, 0, 24, locked.Height)),
            HoverOverlayColor = new Color(255, 231, 69),
        };
        FreshTooltip.Attach(toggle.Visual, () => toggle.IsChecked ? "Locked" : "Unlocked");
        row.Children.Add(toggle.Visual);
        row.Children.Add(Text("A toggle, with a hover outline", 8, Muted).Visual);
        stack.Children.Add(row);
    }

    private static void ItemSlots(ContainerRuntime stack)
    {
        var row = Row(22);
        (int Type, int Stack, int Context, int Index, bool Favourite)[] slots =
        [
            (ItemID.CopperShortsword, 1, 0, 0, false),
            (ItemID.Torch, 65, 0, 15, false),
            (ItemID.GoldCoin, 12, 1, 50, false),
            (ItemID.WoodenArrow, 250, 2, 54, false),
            (ItemID.Mushroom, 6, 0, 16, true),
            (0, 0, 6, 0, false),
        ];
        foreach ((int type, int count, int context, int index, bool favourite) in slots)
        {
            var item = new Item();
            if (type > 0)
            {
                item.SetDefaults(type);
                item.stack = count;
                item.favorited = favourite;
            }
            var slot = new FreshItemSlot();
            slot.SetItem(item, context, index, FreshItemSlot.DefaultScale);
            row.Children.Add(slot.Visual);
        }
        row.StackSpacing = 1;
        stack.Children.Add(row);
        stack.Children.Add(Text("Hotbar, stack, coin, ammo, favourite, trash", 7, Muted).Visual);
    }

    private static void ToggleSwitches(ContainerRuntime stack)
    {
        (string Label, bool On, bool Enabled)[] switches =
        [
            ("Off", false, true),
            ("On", true, true),
            ("Disabled off", false, false),
            ("Disabled on", true, false),
        ];
        foreach ((string label, bool on, bool enabled) in switches)
        {
            var row = Row(11);
            var toggle = new FreshToggleSwitch { IsChecked = on, IsEnabled = enabled, Text = label };
            FreshLabel text = Text(label, 8);
            toggle.Checked += (_, _) => text.Text = "On";
            toggle.Unchecked += (_, _) => text.Text = "Off";
            row.Children.Add(toggle.Visual);
            row.Children.Add(text.Visual);
            stack.Children.Add(row);
        }
    }

    private static void Sliders(ContainerRuntime stack)
    {
        var row = Row(11);
        var smooth = new FreshSlider { Minimum = 0, Maximum = 100, Value = 40 };
        FreshLabel value = Text("40%", 8);
        smooth.ValueChanged += (_, _) => value.Text = $"{smooth.Value:0}%";
        row.Children.Add(smooth.Visual);
        row.Children.Add(value.Visual);
        stack.Children.Add(row);

        var stepped = Row(11);
        var ticks = new FreshSlider { Minimum = 1, Maximum = 5, IsSnapToTickEnabled = true, TicksFrequency = 1, Value = 3 };
        FreshLabel tick = Text("3 of 5", 8);
        ticks.ValueChanged += (_, _) => tick.Text = $"{ticks.Value:0} of 5";
        stepped.Children.Add(ticks.Visual);
        stepped.Children.Add(tick.Visual);
        stack.Children.Add(stepped);

        var disabled = new FreshSlider { Minimum = 0, Maximum = 1, Value = 0.7, IsEnabled = false };
        stack.Children.Add(disabled.Visual);
    }

    private static void PanelButtons(ContainerRuntime stack)
    {
        var row = Row(14);
        foreach ((string text, bool chosen, bool enabled) in new[] { ("At rest", false, true), ("Chosen", true, true), ("Disabled", false, false) })
        {
            var button = new FreshPanelButton { Text = text, FontSize = 8, IsChosen = chosen, IsEnabled = enabled };
            Size(button, 44, 14);
            row.Children.Add(button.Visual);
        }
        row.StackSpacing = 2;
        stack.Children.Add(row);
        // A settings row: its text at the left, its state changing as it's clicked.
        bool on = true;
        var setting = new FreshPanelButton { Text = "Autosave On", FontSize = 8 };
        setting.AlignTextLeft(5);
        Size(setting, 136, 14);
        setting.Click += (_, _) => setting.Text = (on = !on) ? "Autosave On" : "Autosave Off";
        stack.Children.Add(setting.Visual);
    }

    private static void Window(ContainerRuntime stack)
    {
        // Room above for the title tab, which sits over the window's top edge.
        var area = new ContainerRuntime { Width = 136, Height = 72, HasEvents = false };
        var window = new FreshWindow("Title") { Y = FreshWindow.TitleLift, Width = 136, Height = 52 };
        window.Add(new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, HeightUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = 0 });
        var text = Text("A window's panels under its title", 7, Muted);
        text.Visual.X = 6;
        text.Visual.Y = 30;
        window.Children.Add(text.Visual);
        area.Children.Add(window);
        stack.Children.Add(area);
        window.Layout();
    }

    private static void ColorBars(ContainerRuntime stack)
    {
        // Hue, saturation and lightness, each bar's track the colour along it with the others as they are.
        float[] hsl = [0.6f, 0.8f, 0.5f];
        Color Color(int part, float value)
        {
            float[] at = (float[])hsl.Clone();
            at[part] = value;
            return Main.hslToRgb(at[0], at[1], at[2]);
        }
        var swatch = FreshStyle.Backdrop(Color(0, hsl[0]));
        swatch.WidthUnits = swatch.HeightUnits = DimensionUnitType.Absolute;
        swatch.Width = 20;
        swatch.Height = 20;
        var bars = new List<FreshSlider>();
        for (int i = 0; i < 3; i++)
        {
            int part = i;
            var bar = new FreshSlider { Minimum = 0, Maximum = 1, Value = hsl[part] };
            bar.Ramp = value => Color(part, value);
            bar.ValueChanged += (_, _) =>
            {
                hsl[part] = (float)bar.Value;
                swatch.FillColor = Color(part, hsl[part]);
                foreach (FreshSlider other in bars)
                    other.UpdateRamp();
            };
            bars.Add(bar);
            var row = Row(11);
            row.Children.Add(bar.Visual);
            if (i == 0)
                row.Children.Add(swatch);
            stack.Children.Add(row);
        }
    }

    private static void GameView(ContainerRuntime stack)
    {
        // Chat text with its tags (an item icon, a colour) and coins, drawn by the game, under a Gum label
        // that overlaps it: Gum's own drawing order holds around the game's.
        var area = new ContainerRuntime { Width = 136, Height = 40, HasEvents = false };
        var view = new FreshGameView { Width = 136, Height = 40 };
        view.Draw = (batch, box) =>
        {
            Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(batch, Terraria.GameContent.FontAssets.MouseText.Value,
                "[i:29] A [c/FF8040:tagged] line", new Vector2(box.X, box.Y), Color.White, 0f, Vector2.Zero, Vector2.One);
            Terraria.UI.ItemSlot.DrawMoney(batch, "", box.X - 10, box.Y + 10, [1, 2, 3, 4], horizontal: true);
        };
        area.Children.Add(view);
        var over = Text("Gum over it", 8, FreshStyle.Gold);
        over.Visual.X = 90;
        over.Visual.Y = 2;
        area.Children.Add(over.Visual);
        stack.Children.Add(area);
    }

    private static void OptionButtons(ContainerRuntime stack)
    {
        // As the world creation's sizes and evils: an icon and the option's colour, one chosen at a time.
        (string Text, string Icon, Color Color)[][] groups =
        [
            [("Small", "IconSizeSmall", Color.Cyan), ("Medium", "IconSizeMedium", Color.Lerp(Color.Cyan, Color.LimeGreen, 0.5f)), ("Large", "IconSizeLarge", Color.LimeGreen)],
            [("Random", "IconEvilRandom", Color.White), ("Corrupt", "IconEvilCorruption", Color.MediumPurple), ("Crimson", "IconEvilCrimson", Color.IndianRed)],
        ];
        for (int g = 0; g < groups.Length; g++)
        {
            if (g > 0)
                stack.Children.Add(new FreshSeparator());
            var row = Row(17);
            row.StackSpacing = 2;
            var buttons = new List<FreshPanelButton>();
            foreach ((string text, string icon, Color color) in groups[g])
            {
                var button = new FreshPanelButton { Text = text, FontSize = 8, TextColor = color, IsChosen = buttons.Count == 1 };
                button.SetIcon(GumTextures.Load("Images/UI/WorldCreation/" + icon));
                Size(button, 44, 17);
                button.Click += (_, _) => buttons.ForEach(other => other.IsChosen = other == button);
                buttons.Add(button);
                row.Children.Add(button.Visual);
            }
            stack.Children.Add(row);
        }
    }

    private static void TextFields(ContainerRuntime stack)
    {
        var name = new FreshTextBox { Label = "Name:", Text = "Brilliant Isle" };
        name.Visual.Width = 136;
        var seed = new FreshTextBox { Label = "Seed:", Placeholder = "<Leave Blank For Random>" };
        seed.Visual.Width = 136;
        stack.Children.Add(name.Visual);
        stack.Children.Add(seed.Visual);
    }

    private static void ScrollArea(ContainerRuntime stack, bool smooth)
    {
        var area = new FreshScrollViewer { SmoothScrolling = smooth };
        area.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        area.Visual.Width = 0;
        area.Visual.Height = 76;
        var lines = new ContainerRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            ChildrenLayout = ChildrenLayout.TopToBottomStack,
            StackSpacing = 2,
            HasEvents = false,
        };
        for (int i = 1; i <= 30; i++)
            lines.Children.Add(Text($"Line {i}", 8, i % 5 == 0 ? new Color(255, 231, 69) : null).Visual);
        area.InnerPanel.Children.Add(lines);
        stack.Children.Add(area.Visual);
    }
}
