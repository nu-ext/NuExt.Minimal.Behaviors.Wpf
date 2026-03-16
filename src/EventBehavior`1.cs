using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows;

namespace Minimal.Behaviors.Wpf;

/// <summary>
/// Represents an abstract base class for behaviors that are invoked in response to events.
/// </summary>
/// <typeparam name="T">The type of object to which the behavior is attached.</typeparam>
public abstract class EventBehavior<T> : Behavior<T> where T : DependencyObject
{
    private readonly Action<object?, object?> _onEvent;
    private Delegate? _subscribedEventHandler;

    protected EventBehavior()
    {
        _onEvent = OnEvent;
    }

    #region Dependency Properties

    /// <summary>
    /// Identifies the Event dependency property.
    /// </summary>
    public static readonly DependencyProperty EventProperty = DependencyProperty.Register(
        nameof(Event), typeof(RoutedEvent), typeof(EventBehavior<T>),
        new PropertyMetadata(null, (d, e) => ((EventBehavior<T>)d).OnEventChanged((RoutedEvent?)e.OldValue, (RoutedEvent?)e.NewValue)));

    /// <summary>
    /// Identifies the EventName dependency property.
    /// </summary>
    public static readonly DependencyProperty EventNameProperty = DependencyProperty.Register(
        nameof(EventName), typeof(string), typeof(EventBehavior<T>), 
        new PropertyMetadata(nameof(FrameworkElement.Loaded), 
            (d, e) => ((EventBehavior<T>)d).OnEventNameChanged((string?)e.OldValue, (string?)e.NewValue)));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the routed event that activates this behavior.
    /// </summary>
    public RoutedEvent? Event
    {
        get => (RoutedEvent?)GetValue(EventProperty);
        set => SetValue(EventProperty, value);
    }

    /// <summary>
    /// Gets or sets the name of the event that activates this behavior.
    /// </summary>
    public string? EventName
    {
        get => (string?)GetValue(EventNameProperty);
        set => SetValue(EventNameProperty, value);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the event changes.
    /// </summary>
    /// <param name="oldEvent">The old routed event.</param>
    /// <param name="newEvent">The new routed event.</param>
    protected virtual void OnEventChanged(RoutedEvent? oldEvent, RoutedEvent? newEvent)
    {
        if (ReferenceEquals(oldEvent, newEvent))
        {
            return;
        }
        if (newEvent != null)
        {
            EventName = null;
        }
        if (AssociatedObject == null)
        {
            return;
        }
        UnregisterEvent(AssociatedObject, oldEvent);
        RegisterEvent(AssociatedObject, newEvent);
    }

    /// <summary>
    /// Called when the event name changes.
    /// </summary>
    /// <param name="oldEventName">The old event name.</param>
    /// <param name="newEventName">The new event name.</param>
    protected virtual void OnEventNameChanged(string? oldEventName, string? newEventName)
    {
        if (string.Equals(oldEventName, newEventName, StringComparison.Ordinal))
        {
            return;
        }
        if (newEventName != null)
        {
            Event = null;
        }
        if (AssociatedObject == null)
        {
            return;
        }
        UnregisterEvent(AssociatedObject, oldEventName);
        RegisterEvent(AssociatedObject, newEventName);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates an event handler delegate.
    /// </summary>
    /// <param name="eventHandlerType">The type of the event handler.</param>
    /// <param name="parameters">The parameters of the event handler method.</param>
    /// <returns>A delegate representing the event handler.</returns>
    private Delegate CreateEventHandler(Type eventHandlerType, ParameterInfo[] parameters)
    {
        Type handlerType = typeof(EventHandler<,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType);
        var handlerWrapper = Activator.CreateInstance(handlerType, _onEvent);
        Debug.Assert(handlerWrapper != null && handlerWrapper.GetType() == handlerType);
        return Delegate.CreateDelegate(eventHandlerType, handlerWrapper, handlerType.GetMethod(nameof(EventHandler<,>.Handler))!);
    }

    /// <summary>
    /// Determines whether the specified type is a valid event handler.
    /// A valid event handler is a delegate with an 'Invoke' method that returns void 
    /// and has exactly two parameters.
    /// </summary>
    /// <param name="eventHandlerType">The type of the event handler.</param>
    /// <param name="parameters">When this method returns, contains the parameters of the 'Invoke' method if the type is valid.</param>
    /// <returns><see langword="true"/> if the type is a valid event handler; otherwise, <see langword="false"/>.</returns>
    private static bool IsValidEvent(Type eventHandlerType, [NotNullWhen(true)] out ParameterInfo[]? parameters)
    {
        MethodInfo? methodInfo;
        if (!typeof(Delegate).IsAssignableFrom(eventHandlerType) 
            || (methodInfo = eventHandlerType.GetMethod("Invoke")) == null 
            || methodInfo.ReturnType != typeof(void))
        {
            parameters = null;
            return false;
        }
        parameters = methodInfo.GetParameters();
        return parameters.Length == 2;
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        UnregisterEvent(AssociatedObject, Event);
        UnregisterEvent(AssociatedObject, EventName);
        if (Event is not null)
        {
            RegisterEvent(AssociatedObject, Event);
        }
        else
        {
            RegisterEvent(AssociatedObject, EventName);
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        UnregisterEvent(AssociatedObject, Event);
        UnregisterEvent(AssociatedObject, EventName);
        base.OnDetaching();
    }

    /// <summary>
    /// Entry point invoked by the event bridge. Performs common gating and dispatches to <see cref="OnEventCore(object, object)"/>.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    /// <remarks>
    /// The default implementation ignores the event when <see cref="Behavior.IsEnabled"/> is <see langword="false"/>.
    /// Override <see cref="OnEventCore(object, object)"/> to implement behavior logic.
    /// </remarks>
    protected virtual void OnEvent(object? sender, object? eventArgs)
    {
        if (!IsEnabled)
        {
            return;
        }
        OnEventCore(sender, eventArgs);
    }

    /// <summary>
    /// Implements the behavior's event handling logic.
    /// Called only when <see cref="OnEvent(object, object)"/> allows delivery (e.g., when enabled).
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    protected abstract void OnEventCore(object? sender, object? eventArgs);

    /// <summary>
    /// Registers the routed event on the specified object.
    /// </summary>
    /// <param name="obj">The object to register the routed event on.</param>
    /// <param name="event">The routed event to register.</param>
    private void RegisterEvent(T? obj, RoutedEvent? @event)
    {
        if (obj is null || @event is null)
        {
            return;
        }

        Debug.Assert(_subscribedEventHandler == null);
        if (_subscribedEventHandler != null)
        {
            throw new InvalidOperationException($"An event handler is already registered for '{@event}'.");
        }

        var eventHandlerType = @event.HandlerType;
        if (!IsValidEvent(eventHandlerType, out var parameters))
        {
            return;
        }

        _subscribedEventHandler = CreateEventHandler(@event.HandlerType, parameters!);

        // Use handledEventsToo = true to allow behaviors to observe handled routed events
        // (e.g., tunneling/preview stages). This mirrors common interaction frameworks.
        switch (obj)
        {
            case UIElement uiElement:
                uiElement.AddHandler(@event, _subscribedEventHandler, handledEventsToo: true);
                break;
            case ContentElement contentElement:
                contentElement.AddHandler(@event, _subscribedEventHandler, handledEventsToo: true);
                break;
            case UIElement3D uiElement3D:
                uiElement3D.AddHandler(@event, _subscribedEventHandler, handledEventsToo: true);
                break;
            default:
                // No AddHandler available for other IInputElement implementations.
                break;
        }
    }

    /// <summary>
    /// Registers the event on the specified object.
    /// </summary>
    /// <param name="obj">The object to register the event on.</param>
    /// <param name="eventName">The name of the event.</param>
    private void RegisterEvent(T? obj, string? eventName)
    {
        if (obj is null || string.IsNullOrEmpty(eventName))
        {
            return;
        }

        Debug.Assert(_subscribedEventHandler == null);
        if (_subscribedEventHandler != null)
        {
            throw new InvalidOperationException($"An event handler is already registered for '{eventName}'.");
        }

        var eventInfo = obj.GetType().GetEvent(eventName);
        Type? eventHandlerType;
        if (eventInfo == null || (eventHandlerType = eventInfo.EventHandlerType) == null || !IsValidEvent(eventHandlerType, out var parameters))
        {
            return;
        }

        _subscribedEventHandler = CreateEventHandler(eventHandlerType, parameters!);
        eventInfo.AddEventHandler(obj, _subscribedEventHandler);
    }

    /// <summary>
    /// Unregisters the routed event from the specified object.
    /// </summary>
    /// <param name="obj">The object to unregister the routed event from.</param>
    /// <param name="event">The routed event to unregister.</param>
    private void UnregisterEvent(T? obj, RoutedEvent? @event)
    {
        if (obj is null || @event is null || _subscribedEventHandler is null)
        {
            return;
        }

        switch (obj)
        {
            case UIElement uiElement:
                uiElement.RemoveHandler(@event, _subscribedEventHandler);
                break;
            case ContentElement contentElement:
                contentElement.RemoveHandler(@event, _subscribedEventHandler);
                break;
            case UIElement3D uiElement3D:
                uiElement3D.RemoveHandler(@event, _subscribedEventHandler);
                break;
            default:
                // No RemoveHandler available for other IInputElement implementations.
                break;
        }
        _subscribedEventHandler = null;
    }

    /// <summary>
    /// Unregisters the event from the specified object.
    /// </summary>
    /// <param name="obj">The object to unregister the event from.</param>
    /// <param name="eventName">The name of the event.</param>
    private void UnregisterEvent(T? obj, string? eventName)
    {
        if (obj is null || string.IsNullOrEmpty(eventName) || _subscribedEventHandler is null)
        {
            return;
        }

        var eventInfo = obj.GetType().GetEvent(eventName);
        if (eventInfo == null)
        {
            return;
        }
        eventInfo.RemoveEventHandler(obj, _subscribedEventHandler);
        _subscribedEventHandler = null;
    }

    #endregion
}

/// <summary>
/// A generic class used to handle events for EventBehavior.
/// </summary>
/// <typeparam name="TSender">The type of the first parameter of the event handler.</typeparam>
/// <typeparam name="TEventArgs">The type of the second parameter of the event handler.</typeparam>
internal class EventHandler<TSender, TEventArgs>(Action<object?, object?> action)
{
    public void Handler(TSender sender, TEventArgs e)
    {
        action(sender, e);
    }
}