using System.Windows;
using System.Windows.Input;

namespace Minimal.Behaviors.Wpf;

/// <summary>
/// Executes a command in response to a specific key gesture.
/// Extends <see cref="EventToCommandBehavior{T}"/> with key gesture matching capabilities.
/// </summary>
/// <remarks>
/// This behavior is associated with the <see cref="UIElement.KeyUp"/> event by default.
/// Use the <see cref="Gesture"/> property to specify the key gesture that triggers the command.
/// </remarks>
public sealed class KeyToCommand : EventToCommandBehavior<UIElement>
{
    static KeyToCommand()
    {
        // Overrides the default event name to be "KeyUp".
        EventNameProperty.OverrideMetadata(typeof(KeyToCommand), new PropertyMetadata(nameof(UIElement.KeyUp)));
    }

    #region Dependency Properties

    /// <summary>
    /// Identifies the <see cref="Gesture"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GestureProperty = DependencyProperty.Register(
        nameof(Gesture), typeof(KeyGesture), typeof(KeyToCommand));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the key gesture that triggers the command.
    /// </summary>
    /// <value>
    /// A <see cref="KeyGesture"/> that must be matched to execute the command; 
    /// otherwise, <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// If <see cref="Gesture"/> is <see langword="null"/>, the command will not execute
    /// regardless of the keyboard input.
    /// </remarks>
    public KeyGesture? Gesture
    {
        get => (KeyGesture?)GetValue(GestureProperty);
        set => SetValue(GestureProperty, value);
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    /// <remarks>
    /// Checks if the specified key gesture matches the input event arguments.
    /// </remarks>
    protected override bool CanExecuteCore(object? sender, object? eventArgs)
    {
        if (Gesture is null || eventArgs is not InputEventArgs inputEventArgs)
        {
            return false;
        }
        if (!base.CanExecuteCore(sender, eventArgs))
        {
            return false;
        }
        return Gesture.Matches(AssociatedObject, inputEventArgs);
    }

    #endregion
}
