using BoboEngine.Input;
using BoboEngine;
using GLFW;

namespace Minecraft.Inputs;

public static class Input
{
    private static readonly Dictionary<Keys, KeyMapping> _keyMappings = new();
    private static readonly Dictionary<MouseButton, MouseMapping> _mouseMappings = new();

    [OnEngineInitialize]
    public static void Init()
    {
        InputSystem.onKeyChanged += KeyChanged;
        BoboEngine.Input.Cursor.onMouseButtonChanged += MouseChanged;
    }

    private static void KeyChanged(BoboEngine.Input.InputState state)
    {
        if (state.state == GLFW.InputState.Repeat) return;

        var mapping = GetKeyMapping(state.key);

        bool value = state.state == GLFW.InputState.Press;

        mapping.OnKeyChanged(value);
    }
    private static void MouseChanged(MouseInputState state)
    {
        if (state.state == GLFW.InputState.Repeat) return;

        var mapping = GetMouseMapping(state.button);

        bool value = state.state == GLFW.InputState.Press;

        mapping.OnKeyChanged(value);
    }

    public static KeyMapping GetKeyMapping(Keys key)
    {
        if (_keyMappings.TryGetValue(key, out var v))
        {
            return v;
        }

        var mapping = new KeyMapping(key);

        _keyMappings.Add(key, mapping);
        return mapping;
    }
    public static MouseMapping GetMouseMapping(MouseButton mouseButton)
    {
        if (_mouseMappings.TryGetValue(mouseButton, out var v))
        {
            return v;
        }

        var mapping = new MouseMapping(mouseButton);

        _mouseMappings.Add(mouseButton, mapping);
        return mapping;
    }
    public class KeyMapping : InputMapping
    {
        public readonly Keys key;

        public KeyMapping(Keys key)
        {
            this.key = key;
        }
    }
    public class MouseMapping : InputMapping
    {
        public readonly MouseButton mouseButton;

        public MouseMapping(MouseButton mouseButton)
        {
            this.mouseButton = mouseButton;
        }
    }

}
public abstract class InputMapping
{
    public bool isDown { get; private set; }

    private int _clickCount = 0;
    public bool ConsumeClick()
    {
        bool result = _clickCount > 0;

        if (result) _clickCount--;

        return result;
    }

    internal void OnKeyChanged(bool value)
    {
        isDown = value;

        if (value) _clickCount++;
    }
}