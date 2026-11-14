using System.Reflection;
using Aether.UI.Game;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.GameInput;

namespace FreshUI.Screens;

/// <summary>
/// The player's life and mana at the top right (Aether's <see cref="UIResourceBars"/>, in each of the game's
/// styles it lays out: classic hearts and stars, the framed ones, the bars), each piece a Gum sprite where
/// the style places it and its "Life"/"Mana" texts Gum text; hovering life or mana shows its numbers, as the
/// game's. The layout stays Aether's (<c>UpdateState</c>, run here as the routed screen no longer runs it).
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> ResourceBarsAsked = GumRoutes.GameScreen<UIResourceBars>();
    private static readonly FieldInfo LifeGroup = typeof(UIResourceBars).GetField("_life", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ManaGroup = typeof(UIResourceBars).GetField("_mana", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ResourceTexts = typeof(UIResourceBars).GetField("_texts", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private ContainerRuntime? _resources;
    private readonly List<SpriteRuntime> _resourceSprites = [];
    private readonly TextRuntime[] _resourceTexts = new TextRuntime[4];
    private UIResourceGroup? _lifeGroup, _manaGroup;
    private UIResourceText[]? _resourceTextSources;

    private void PresentResourceBars()
    {
        bool shown = IsReady && ResourceBarsAsked();
        if (_resources == null)
        {
            if (!shown)
                return;
            CreateResourceBars();
        }
        if (_resources!.Visible != shown)
            _resources.Visible = shown;
        if (!shown)
            return;
        UIResourceBars.Instance.UpdateState();
        int used = 0;
        foreach (UIResourceGroup group in new[] { _lifeGroup!, _manaGroup! })
        {
            foreach (ResourcePiece piece in group.Pieces)
                Piece(used++, piece);
        }
        for (int i = used; i < _resourceSprites.Count; i++)
        {
            if (_resourceSprites[i].Visible)
                _resourceSprites[i].Visible = false;
        }
        for (int i = 0; i < _resourceTexts.Length; i++)
            ResourceText(_resourceTexts[i], _resourceTextSources![i]);
        // Hovering life or mana shows its numbers, as the game's.
        if (!PlayerInput.IgnoreMouseInterface)
        {
            var mouse = new Vector2(Main.mouseX, Main.mouseY);
            if (_lifeGroup!.ContainsPoint(mouse))
                CommonResourceBarMethods.DrawLifeMouseOver();
            else if (_manaGroup!.ContainsPoint(mouse))
                CommonResourceBarMethods.DrawManaMouseOver();
        }
    }

    private void CreateResourceBars()
    {
        _resources = new ContainerRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            HasEvents = false,
        };
        Children.Insert(0, _resources);
        // The screen's parts, made as the game would before it first draws it.
        UIResourceBars.Instance.Activate();
        _lifeGroup = (UIResourceGroup)LifeGroup.GetValue(UIResourceBars.Instance)!;
        _manaGroup = (UIResourceGroup)ManaGroup.GetValue(UIResourceBars.Instance)!;
        _resourceTextSources = (UIResourceText[])ResourceTexts.GetValue(UIResourceBars.Instance)!;
        for (int i = 0; i < _resourceTexts.Length; i++)
        {
            var text = new TextRuntime
            {
                WidthUnits = DimensionUnitType.RelativeToChildren,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
                FontSize = 10,
                HasEvents = false,
                Visible = false,
            };
            GumText.SetOutline(text, 0.5f, Color.Black);
            _resourceTexts[i] = text;
            _resources.Children.Add(text);
        }
    }

    // A piece as the game draws it (its position, origin and scale in UI units, two a UI pixel).
    private void Piece(int index, ResourcePiece piece)
    {
        if (index == _resourceSprites.Count)
        {
            var created = new SpriteRuntime
            {
                TextureAddress = Gum.Managers.TextureAddress.Custom,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
            };
            _resourceSprites.Add(created);
            // Under the texts.
            _resources!.Children.Insert(_resourceSprites.Count - 1, created);
        }
        SpriteRuntime sprite = _resourceSprites[index];
        float x = (piece.Position.X - piece.Origin.X * piece.Scale) / 2, y = (piece.Position.Y - piece.Origin.Y * piece.Scale) / 2;
        float width = piece.Source.Width * piece.Scale / 2, height = piece.Source.Height * piece.Scale / 2;
        if (sprite.Texture != piece.Texture)
            sprite.Texture = piece.Texture;
        if (sprite.TextureLeft != piece.Source.X || sprite.TextureTop != piece.Source.Y || sprite.TextureWidth != piece.Source.Width || sprite.TextureHeight != piece.Source.Height)
        {
            sprite.TextureLeft = piece.Source.X;
            sprite.TextureTop = piece.Source.Y;
            sprite.TextureWidth = piece.Source.Width;
            sprite.TextureHeight = piece.Source.Height;
        }
        if (sprite.X != x || sprite.Y != y || sprite.Width != width || sprite.Height != height)
            (sprite.X, sprite.Y, sprite.Width, sprite.Height) = (x, y, width, height);
        if (sprite.Color != piece.Color)
            sprite.Color = piece.Color;
        if (!sprite.Visible)
            sprite.Visible = true;
    }

    private static void ResourceText(TextRuntime text, UIResourceText source)
    {
        bool shown = source.Text != null;
        if (text.Visible != shown)
            text.Visible = shown;
        if (!shown)
            return;
        if (text.Text != source.Text)
            text.Text = source.Text;
        var origin = source.RightAligned ? RenderingLibrary.Graphics.HorizontalAlignment.Right : RenderingLibrary.Graphics.HorizontalAlignment.Left;
        if (text.XOrigin != origin)
            text.XOrigin = origin;
        float x = MathF.Round(source.Position.X / 2), y = MathF.Round(source.Position.Y / 2);
        if (text.X != x || text.Y != y)
            (text.X, text.Y) = (x, y);
        var color = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
        if (text.Color != color)
            text.Color = color;
    }
}
