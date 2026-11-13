using FreshUI.Components;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Aether.Testing;
using Gum;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;

namespace FreshUI.GumUi;

/// <summary>
/// The live Gum element tree (the <see cref="GumScreens" />), for Aether's remote UI automation:
/// <c>game_ui_layout</c> lists it as the <c>gum</c> tree, separate from Terraria's retained UI, and
/// <c>game_ui_action</c> acts on its components by ID. Each element's ID (<c>gum:N</c>) stays the same
/// for as long as it exists. A control (a button, a slot) is one component, its visual's parts left out.
/// Bounds are back-buffer pixels: [x, y, width, height].
/// </summary>
public sealed class GumUiTree : IUiTree
{
    private const string Prefix = "gum:";

    private static readonly ConditionalWeakTable<GraphicalUiElement, StrongBox<int>> Ids = new();

    private static readonly Dictionary<int, WeakReference<GraphicalUiElement>> ById = [];

    private static readonly FieldInfo? ClickHandlers =
        typeof(InteractiveGue).GetField("Click", BindingFlags.NonPublic | BindingFlags.Instance);

    private static int _nextId = 1;

    public string Id => "gum";

    public string Owner => Startup.ModId;

    public IEnumerable<JsonObject> Components(bool interactiveOnly)
    {
        var components = new List<JsonObject>();
        if (GumHost.IsInitialized)
        {
            foreach (GraphicalUiElement root in new[] { GumService.Default.Root, GumService.Default.PopupRoot, GumService.Default.ModalRoot })
            {
                Walk(root, null, interactiveOnly, components);
            }
        }
        return components;
    }

    public bool TryGetBounds(string componentId, out Rectangle bounds)
    {
        bool found = TryFind(componentId, out GraphicalUiElement? element);
        bounds = found ? Bounds(element!) : Rectangle.Empty;
        return found;
    }

    public bool TryAct(string componentId, string action, JsonNode? value, out string error)
    {
        if (!TryFind(componentId, out GraphicalUiElement? element))
        {
            error = "stale UI component; request the current layout";
            return false;
        }
        if (!Actions(element!).Contains(action))
        {
            error = "the component does not currently support that action";
            return false;
        }
        FrameworkElement? control = ControlOf(element!);
        if (control is FreshItemSlot { HudAction: { } act } && action is "activate" or "secondary")
        {
            bool acted = act(action == "secondary");
            error = acted ? "" : "the slot cannot be used right now";
            return acted;
        }
        switch (action)
        {
        case "activate":
            ((InteractiveGue)element!).CallClick();
            break;
        case "secondary":
            ((InteractiveGue)element!).CallRightClick();
            break;
        case "set_value":
            var range = (RangeBase)control!;
            range.Value = range.Minimum + value!.GetValue<double>() * (range.Maximum - range.Minimum);
            break;
        case "set_text":
            control!.GetType().GetProperty("Text", typeof(string))!.SetValue(control, value!.GetValue<string>());
            break;
        }
        error = "";
        return true;
    }

    private static bool TryFind(string componentId, out GraphicalUiElement? element)
    {
        element = null;
        return componentId.StartsWith(Prefix, StringComparison.Ordinal) &&
            int.TryParse(componentId.AsSpan(Prefix.Length), out int id) &&
            ById.TryGetValue(id, out WeakReference<GraphicalUiElement>? reference) &&
            reference.TryGetTarget(out element) && IsShown(element);
    }

    private static string IdOf(GraphicalUiElement element)
    {
        StrongBox<int> id = Ids.GetValue(element, _ => new StrongBox<int>(_nextId++));
        ById[id.Value] = new WeakReference<GraphicalUiElement>(element);
        return Prefix + id.Value;
    }

    /// <summary>The element's back-buffer bounds.</summary>
    private static Rectangle Bounds(GraphicalUiElement element)
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;
        camera.WorldToScreen(element.AbsoluteLeft, element.AbsoluteTop, out float left, out float top);
        camera.WorldToScreen(element.AbsoluteLeft + element.AbsoluteWidth, element.AbsoluteTop + element.AbsoluteHeight,
            out float right, out float bottom);
        // Halves round the same way everywhere, so evenly spaced elements stay evenly spaced.
        int x = RoundHalfUp(left), y = RoundHalfUp(top);
        return new Rectangle(x, y, RoundHalfUp(right) - x, RoundHalfUp(bottom) - y);
    }

    // With a little slack, as layout's floats land a hair either side of a half.
    private static int RoundHalfUp(float value) => (int)MathF.Floor(value + 0.5f + 0.001f);

    private static bool IsShown(GraphicalUiElement element)
    {
        for (GraphicalUiElement? at = element; at != null; at = at.Parent as GraphicalUiElement)
        {
            if (!at.Visible)
            {
                return false;
            }
        }
        return true;
    }

    private static FrameworkElement? ControlOf(GraphicalUiElement element) =>
        (element as InteractiveGue)?.FormsControlAsObject as FrameworkElement;

    /// <summary>What can be done to the element, in Aether's action names.</summary>
    private static List<string> Actions(GraphicalUiElement element)
    {
        var actions = new List<string>();
        FrameworkElement? control = ControlOf(element);
        if (element is not InteractiveGue interactive || !IsShown(element) || control is { IsEnabled: false })
        {
            return actions;
        }
        bool clickable = control is FreshItemSlot { HudAction: not null } || interactive.HasEvents && (control is ButtonBase or ListBoxItem or FreshItemSlot or FreshIconButton ||
            ClickHandlers?.GetValue(interactive) != null);
        if (clickable)
        {
            actions.Add("activate");
            actions.Add("secondary");
        }
        if (control is RangeBase)
        {
            actions.Add("set_value");
        }
        if (control is TextBoxBase)
        {
            actions.Add("set_text");
        }
        return actions;
    }

    // Controls whose visual's parts are the control itself, not content to list.
    private static bool IsLeafControl(FrameworkElement? control) =>
        control is ButtonBase or Label or RangeBase or TextBoxBase or FreshItemSlot or FreshIconButton;

    private static void Walk(GraphicalUiElement element, string? parent, bool interactiveOnly, List<JsonObject> components)
    {
        if (!element.Visible)
        {
            return;
        }
        string id = IdOf(element);
        List<string> actions = Actions(element);
        if (!interactiveOnly || actions.Count > 0)
        {
            components.Add(Describe(element, id, parent, actions));
        }
        if (IsLeafControl(ControlOf(element)))
        {
            return;
        }
        foreach (GraphicalUiElement child in element.Children.ToArray())
        {
            Walk(child, id, interactiveOnly, components);
        }
    }

    private static JsonObject Describe(GraphicalUiElement element, string id, string? parent, List<string> actions)
    {
        FrameworkElement? control = ControlOf(element);
        Rectangle bounds = Bounds(element);
        var component = new JsonObject
        {
            ["id"] = id,
            ["parent"] = parent,
            ["screen"] = GumScreens.ScreenOf(element),
            ["type"] = (control?.GetType() ?? element.GetType()).Name,
            ["label"] = TextOf(element, control) ?? (string.IsNullOrEmpty(element.Name) ? null : element.Name),
            ["bounds"] = new JsonArray(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            ["value"] = Value(control),
        };
        if (control is { IsEnabled: false })
        {
            component["enabled"] = false;
        }
        component["actions"] = new JsonArray(actions.Select(action => (JsonNode)action).ToArray());
        return component;
    }

    private static string? TextOf(GraphicalUiElement element, FrameworkElement? control)
    {
        if (element is TextRuntime text)
        {
            return text.Text;
        }
        return control?.GetType().GetProperty("Text", typeof(string))?.GetValue(control) as string;
    }

    // What a control is showing, beyond its text.
    private static JsonNode? Value(FrameworkElement? control) => control switch
    {
        FreshItemSlot slot => new JsonObject
        {
            ["kind"] = slot.Kind.ToString(),
            ["item"] = slot.Item?.Name,
            ["count"] = slot.Count,
            ["hotbarIndex"] = slot.HotbarIndex,
            ["highlighted"] = slot.IsHighlighted,
        },
        FreshIconButton button => new JsonObject { ["checked"] = button.IsChecked, ["toggle"] = button.IsToggle },
        ToggleButton toggle => new JsonObject { ["checked"] = toggle.IsChecked },
        RangeBase range => range.Maximum > range.Minimum ? (range.Value - range.Minimum) / (range.Maximum - range.Minimum) : 0,
        _ => null,
    };
}
