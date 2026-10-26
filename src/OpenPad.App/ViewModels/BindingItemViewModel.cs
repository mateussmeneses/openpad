using OpenPad.App.Common;
using OpenPad.Core.Models;

namespace OpenPad.App.ViewModels;

public class BindingItemViewModel : ViewModelBase
{
    private ActionType _actionType;
    private VirtualKey _key;
    private MouseButton _mouseButton;

    public GamepadButton Button { get; }
    public string ButtonName => Button.ToString();

    public ActionType ActionType
    {
        get => _actionType;
        set
        {
            if (SetField(ref _actionType, value))
            {
                OnPropertyChanged(nameof(IsKeyAction));
                OnPropertyChanged(nameof(IsMouseAction));
                OnPropertyChanged(nameof(DisplayTarget));
            }
        }
    }

    public VirtualKey Key
    {
        get => _key;
        set
        {
            if (SetField(ref _key, value))
            {
                OnPropertyChanged(nameof(DisplayTarget));
            }
        }
    }

    public MouseButton MouseButton
    {
        get => _mouseButton;
        set
        {
            if (SetField(ref _mouseButton, value))
            {
                OnPropertyChanged(nameof(DisplayTarget));
            }
        }
    }

    public bool IsKeyAction => ActionType == ActionType.Key;
    public bool IsMouseAction => ActionType == ActionType.MouseButton;

    public string DisplayTarget => ActionType switch
    {
        ActionType.Key => $"Tecla [{Key}]",
        ActionType.MouseButton => $"Mouse [{MouseButton}]",
        _ => "Nenhuma ação"
    };

    public BindingItemViewModel(GamepadButton button, InputAction action)
    {
        Button = button;
        _actionType = action.Type;
        _key = action.Key ?? VirtualKey.Space;
        _mouseButton = action.MouseButton ?? MouseButton.Left;
    }

    public InputAction ToInputAction() => ActionType switch
    {
        ActionType.Key => InputAction.FromKey(Key),
        ActionType.MouseButton => InputAction.FromMouse(MouseButton),
        _ => InputAction.None()
    };
}
