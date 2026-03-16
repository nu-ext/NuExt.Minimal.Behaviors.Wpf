using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Minimal.Behaviors.Wpf;

/// <summary>
/// Represents a base class that can be used to attach behaviors to WPF elements.
/// </summary>
public abstract class Behavior : Animatable, INotifyPropertyChanged
{
    private readonly Type _associatedType;
    private DependencyObject? _associatedObject;

    #region Dependency Properties

    /// <summary>
    /// Identifies the IsEnabled dependency property.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
        nameof(IsEnabled), typeof(bool), typeof(Behavior), new PropertyMetadata(defaultValue: true,
            static (d, e) =>
            {
                var b = (Behavior)d;
                var oldValue = (bool)e.OldValue;
                var newValue = (bool)e.NewValue;
                if (oldValue == newValue) return;
                b.OnPropertyChanged(EventArgsCache.IsEnabledPropertyChanged);
                b.OnIsEnabledChanged(oldValue, newValue);
            },

            static (d, baseValue) =>
            {
                var b = (Behavior)d;
                if (!b.IsAttached && (bool)baseValue) return false;
                return baseValue;
            }
            ));

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Behavior"/> class with the specified type.
    /// </summary>
    /// <param name="associatedType">The type of object to which this behavior can be attached.</param>
    /// <exception cref="ArgumentNullException">Thrown when associatedType is null.</exception>
    protected Behavior(Type associatedType)
    {
        _associatedType = associatedType ?? throw new ArgumentNullException(nameof(associatedType));
    }

    #region Properties

    /// <summary>
    /// Gets the object to which this behavior is attached.
    /// </summary>
    protected internal DependencyObject? AssociatedObject
    {
        get
        {
            ReadPreamble();
            return _associatedObject;
        }
        private set
        {
            if (AssociatedObject == value) return;
            WritePreamble();
            _associatedObject = value;
            WritePostscript();
            CoerceValue(IsEnabledProperty);
            OnPropertyChanged(EventArgsCache.AssociatedObjectPropertyChanged);
            OnPropertyChanged(EventArgsCache.IsAttachedPropertyChanged);
        }
    }

    /// <summary>
    /// Gets the type of object to which this behavior can be attached.
    /// </summary>
    protected Type AssociatedType
    {
        get
        {
            ReadPreamble();
            return _associatedType;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this behavior is attached.
    /// </summary>
    public bool IsAttached => AssociatedObject != null;

    /// <summary>
    /// Gets or sets a value indicating whether this behavior is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Attaches the behavior to the specified object.
    /// </summary>
    /// <param name="obj">The object to attach to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the behavior is already attached to an object, or when the type of the specified object does not match the associated type.
    /// </exception>
    public void Attach(DependencyObject obj)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));

        if (obj == AssociatedObject)
        {
            return;
        }
        Debug.Assert(AssociatedObject == null);
        ThrowInvalidOperationExceptionIfAttached();
        ThrowInvalidOperationExceptionIfTypeMismatch(obj.GetType());
        AssociatedObject = obj;
        Debug.Assert(AssociatedObject != null, "AssociatedObject should never be null after successful assignment.");
        try
        {
            OnAttached();
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
            ResetState();
            throw;

        }
    }

    /// <summary>
    /// Detaches the behavior from the associated object.
    /// </summary>
    public void Detach()
    {
        Debug.Assert(AssociatedObject != null);

        if (AssociatedObject == null)
        {
            return;
        }
        try
        {
            OnDetaching();
        }
        finally
        {
            ResetState();
        }
    }

    /// <summary>
    /// Called after the behavior is attached to an object.
    /// Override this method to hook up functionality to the associated object.
    /// </summary>
    protected virtual void OnAttached()
    {
    }

    /// <summary>
    /// Called before the behavior is detached from an object.
    /// Override this method to clean up any functionality hooked up in OnAttached.
    /// </summary>
    protected virtual void OnDetaching()
    {
    }

    /// <summary>
    /// Called when the <see cref="IsEnabled"/> property changes.
    /// </summary>
    protected virtual void OnIsEnabledChanged(bool oldValue, bool newValue)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Behavior"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="Behavior"/> class.</returns>
    protected override Freezable CreateInstanceCore()
    {
        var type = GetType();
        var ctor = type.GetConstructor([]) ?? throw new InvalidOperationException($"{type.Name} must have a public parameterless constructor.");
        return (Freezable)ctor.Invoke(null)!;
    }

    private void ResetState()
    {
        // Clear bindings to avoid System.Windows.Data Error: 2 : Cannot find governing FrameworkElement or FrameworkContentElement for target element.
        try { BindingOperations.ClearAllBindings(this); } catch { /* best effort */ }
        PropertyChanged = null;// Clear all event subscribers to prevent memory leaks
        AssociatedObject = null;
    }

    /// <summary>
    /// Throws an InvalidOperationException if the behavior is already attached to an object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the behavior is already attached to an object.</exception>
    private void ThrowInvalidOperationExceptionIfAttached()
    {
        if (AssociatedObject != null)
        {
            throw new InvalidOperationException($"An instance of a Behavior {GetType().Name} cannot be attached to more than one object at a time.");
        }
    }

    /// <summary>
    /// Throws an InvalidOperationException if the type of the specified object does not match the associated type.
    /// </summary>
    /// <param name="type">The type of the object to check.</param>
    /// <exception cref="InvalidOperationException">Thrown when the type of the specified object does not match the associated type.</exception>
    private void ThrowInvalidOperationExceptionIfTypeMismatch(Type type)
    {
        if (!AssociatedType.IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Cannot attach type {GetType().Name} to type {type.Name}. Instances of type {GetType().Name} can only be attached to objects of type {AssociatedType.Name}.");
        }
    }

    #endregion

    #region INotifyPropertyChanged

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    private event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    /// <param name="e">The event data containing the name of the property that changed.</param>
    protected void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    #endregion
}

internal static class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs AssociatedObjectPropertyChanged = new(nameof(Behavior.AssociatedObject));
    internal static readonly PropertyChangedEventArgs IsAttachedPropertyChanged = new(nameof(Behavior.IsAttached));
    internal static readonly PropertyChangedEventArgs IsEnabledPropertyChanged = new(nameof(Behavior.IsEnabled));
}
