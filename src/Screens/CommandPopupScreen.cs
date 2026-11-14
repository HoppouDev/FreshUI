using FreshUI.Components;
using FreshUI.GumUi;
using FreshUI.Legacy;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Screens;

/// <summary>
/// The chat's command completion (<see cref="CommandPopup"/>'s state) in the menus' panels, above the
/// chat input and under the word being completed: the suggestions, the selected one gold (the mouse
/// selects one by hovering, completes it by clicking, and the wheel scrolls the list), its description
/// in a panel beside it, or, with nothing to suggest, the usage of the next arguments or the parse error.
/// </summary>
public sealed class CommandPopupScreen : ContainerRuntime, IGumScreenUpdate
{
    // In UI pixels: two of the chat's UI units each.
    private const float RowHeight = 12, PaddingX = 5, PaddingY = 3, FontSize = 9, Gap = 1;
    // The input's text starts this far in, and the popup's bottom is this far above the screen's (the chat's own units halved).
    private const float InputX = ChatInput.InputX / 2f, Bottom = 19;

    private static readonly Color TextColor = new(200, 200, 200);
    private static readonly Color ErrorColor = new(255, 85, 85);

    private readonly FreshPanel _list;
    private readonly List<(InteractiveGue Row, FreshLabel Label)> _rows = [];
    private readonly FreshPanel _tooltip;
    private readonly FreshLabel _tooltipText;

    public CommandPopupScreen()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        _list = new FreshPanel { HasEvents = true };
        _list.MouseWheelScroll += (_, args) =>
        {
            CommandPopup.ScrollBy(-Math.Sign(Gum.Forms.Controls.FrameworkElement.MainCursor.ZVelocity));
            args.Handled = true;
        };
        Children.Add(_list);
        for (int i = 0; i < CommandPopup.MaxRows; i++)
        {
            int row = i;
            var area = new ContainerRuntime { X = PaddingX - 2, Y = PaddingY + i * RowHeight, Height = RowHeight, HasEvents = true };
            area.WidthUnits = DimensionUnitType.RelativeToParent;
            area.Width = -2 * (PaddingX - 2);
            var label = new FreshLabel { FontSize = FontSize, OutlineWidth = 0.75f };
            label.Visual.X = 2;
            label.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            label.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            area.Children.Add(label.Visual);
            area.RollOn += (_, _) => CommandPopup.Hover(row);
            area.Click += (_, _) => CommandPopup.Click(row);
            _rows.Add((area, label));
            _list.Children.Add(area);
        }
        _tooltip = new FreshPanel { Height = RowHeight + 2 * PaddingY };
        _tooltipText = new FreshLabel { FontSize = FontSize, OutlineWidth = 0.75f };
        _tooltipText.Visual.X = PaddingX;
        _tooltipText.Visual.Y = PaddingY + RowHeight / 2;
        _tooltipText.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        _tooltip.Children.Add(_tooltipText.Visual);
        Children.Add(_tooltip);
    }

    /// <summary>Whether there's anything to show: the chat is open with suggestions, a usage or an error for it.</summary>
    public static bool Shows => Main.drawingPlayerChat && !Main.gameMenu && CommandPopup.Lines.Count > 0;

    public void UpdateScreen()
    {
        IReadOnlyList<(string Text, bool Error)> lines = CommandPopup.Lines;
        float width = 0;
        for (int i = 0; i < _rows.Count; i++)
        {
            (InteractiveGue row, FreshLabel label) = _rows[i];
            row.Visible = i < lines.Count;
            if (!row.Visible)
                continue;
            (string text, bool error) = lines[i];
            if (label.Text != text)
                label.Text = text;
            label.Color = error ? ErrorColor : i == CommandPopup.SelectedRow ? FreshStyle.Gold : TextColor;
            width = MathF.Max(width, label.Visual.AbsoluteWidth);
        }
        width = MathF.Ceiling(width) + 2 * PaddingX;
        float height = lines.Count * RowHeight + 2 * PaddingY;
        float canvasWidth = Gum.GumService.Default.CanvasWidth;
        // Under the word being completed (from the chat's own measure of its text), on screen.
        _list.X = MathF.Floor(Math.Clamp(InputX + CommandPopup.AnchorX / 2 - PaddingX, 0, MathF.Max(0, canvasWidth - width)));
        _list.Y = MathF.Floor(Gum.GumService.Default.CanvasHeight - Bottom - height);
        _list.Width = width;
        _list.Height = height;
        // For the chat's own update: the wheel over the list scrolls it, not the chat.
        CommandPopup.Bounds = new Rectangle((int)(_list.X * 2), (int)(_list.Y * 2), (int)(width * 2), (int)(height * 2));

        string? tooltip = CommandPopup.SelectedTooltip;
        _tooltip.Visible = tooltip != null;
        if (tooltip != null)
        {
            if (_tooltipText.Text != tooltip)
                _tooltipText.Text = tooltip;
            float tooltipWidth = MathF.Ceiling(_tooltipText.Visual.AbsoluteWidth) + 2 * PaddingX;
            _tooltip.Width = tooltipWidth;
            _tooltip.X = MathF.Min(_list.X + width + Gap, MathF.Max(0, canvasWidth - tooltipWidth));
            _tooltip.Y = _list.Y + CommandPopup.SelectedRow * RowHeight;
        }
    }
}
