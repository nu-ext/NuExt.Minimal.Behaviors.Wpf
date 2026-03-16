using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Minimal.Behaviors.Wpf;

/// <summary>
/// Provides a base behavior that converts an event into an <see cref="ICommand"/> execution.
/// It supports flexible command-parameter resolution, routed command targets, and data-binding friendly deferral.
/// </summary>
/// <typeparam name="T">The associated object type (must derive from <see cref="DependencyObject"/>).</typeparam>
/// <remarks>
/// <para>
/// Parameter resolution precedence (deterministic):
/// <br/>1) <see cref="CommandParameter"/> — if set, it wins and everything else is ignored.
/// <br/>2) <see cref="EventArgsConverter"/> — if set:
///   <list type="bullet">
///     <item><description><b>value</b> = (<see cref="EventArgsParameterPath"/> ? eventArgs[path] : eventArgs)</description></item>
///     <item><description><b>parameter</b> = (<see cref="EventArgsConverterParameter"/> ?? sender)</description></item>
///   </list>
/// <br/>3) <see cref="EventArgsParameterPath"/> (no converter): eventArgs[path]
/// <br/>4) <see cref="SenderParameterPath"/> (no converter): sender[path]
/// <br/>5) <see cref="PassEventArgsToCommand"/> ? eventArgs : null
/// </para>
/// <para>
/// Notes:
/// <list type="bullet">
///   <item><description>For routed events, <c>sender</c> is typically the element the behavior is attached to; the deep origin is <c>eventArgs.OriginalSource</c>.</description></item>
///   <item><description>If a binding for <see cref="Command"/> exists but hasn't evaluated yet, the behavior defers execution to <see cref="DispatcherPriority.DataBind"/>.</description></item>
/// </list>
/// </para>
/// </remarks>
public abstract class EventToCommandBehavior<T> : EventBehavior<T> where T : DependencyObject
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the <see cref="Command"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="CommandParameter"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="EventArgsConverter"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EventArgsConverterProperty = DependencyProperty.Register(
        nameof(EventArgsConverter), typeof(IValueConverter), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="EventArgsConverterParameter"/> dependency property.
    /// This value is passed as the converter parameter when <see cref="EventArgsConverter"/> is used.
    /// </summary>
    public static readonly DependencyProperty EventArgsConverterParameterProperty = DependencyProperty.Register(
        nameof(EventArgsConverterParameter), typeof(object), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="EventArgsParameterPath"/> dependency property.
    /// When set, the behavior extracts a value from event args using a simple dotted path with optional integer indexers,
    /// for example: "OriginalSource" or "OriginalSource.Items[0]".
    /// </summary>
    public static readonly DependencyProperty EventArgsParameterPathProperty = DependencyProperty.Register(
        nameof(EventArgsParameterPath), typeof(string), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="SenderParameterPath"/> dependency property.
    /// When set, the behavior extracts a value from sender using a simple dotted path with optional integer indexers,
    /// for example: "DataContext" or "Items[0]".
    /// </summary>
    public static readonly DependencyProperty SenderParameterPathProperty = DependencyProperty.Register(
        nameof(SenderParameterPath), typeof(string), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="PassEventArgsToCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PassEventArgsToCommandProperty = DependencyProperty.Register(
        nameof(PassEventArgsToCommand), typeof(bool), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: false));

    /// <summary>
    /// Identifies the <see cref="CommandTarget"/> dependency property.
    /// Used for <see cref="RoutedCommand"/> invocation to determine target element.
    /// </summary>
    public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
        nameof(CommandTarget), typeof(IInputElement), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="ProcessHandledEvent"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ProcessHandledEventProperty = DependencyProperty.Register(
        nameof(ProcessHandledEvent), typeof(bool), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: false));

    /// <summary>
    /// Identifies the <see cref="MarkEventHandled"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkEventHandledProperty = DependencyProperty.Register(
        nameof(MarkEventHandled), typeof(bool), typeof(EventToCommandBehavior<T>), new PropertyMetadata(defaultValue: false));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the command to execute when the event is raised.
    /// </summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the parameter to pass to the command when it is executed.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>
    /// Gets or sets the converter used to transform event arguments before passing them to the command.
    /// This converter is used only when <see cref="CommandParameter"/> is not set.
    /// If the converter returns <see langword="null"/>, the command receives <see langword="null"/>.
    /// </summary>
    public IValueConverter? EventArgsConverter
    {
        get => (IValueConverter?)GetValue(EventArgsConverterProperty);
        set => SetValue(EventArgsConverterProperty, value);
    }

    /// <summary>
    /// Gets or sets the converter parameter passed to <see cref="EventArgsConverter"/>.
    /// </summary>
    public object? EventArgsConverterParameter
    {
        get => GetValue(EventArgsConverterParameterProperty);
        set => SetValue(EventArgsConverterParameterProperty, value);
    }

    /// <summary>
    /// Gets or sets the dotted path used to extract a value from event args.
    /// Supports simple segments and integer indexers like "OriginalSource.Items[0]".
    /// </summary>
    public string? EventArgsParameterPath
    {
        get => (string?)GetValue(EventArgsParameterPathProperty);
        set => SetValue(EventArgsParameterPathProperty, value);
    }

    /// <summary>
    /// Gets or sets a dotted path to extract a value from the event sender.
    /// Supports simple segments ("DataContext") and integer indexers like "OriginalSource.Items[0]".
    /// </summary>
    public string? SenderParameterPath
    {
        get => (string?)GetValue(SenderParameterPathProperty);
        set => SetValue(SenderParameterPathProperty, value);
    }


    /// <summary>
    /// Gets or sets a value indicating whether to pass the event arguments directly to the command
    /// when <see cref="CommandParameter"/> is not set.
    /// </summary>
    public bool PassEventArgsToCommand
    {
        get => (bool)GetValue(PassEventArgsToCommandProperty);
        set => SetValue(PassEventArgsToCommandProperty, value);
    }

    /// <summary>
    /// When false (default), events already marked as handled are ignored.
    /// </summary>
    public bool ProcessHandledEvent
    {
        get => (bool)GetValue(ProcessHandledEventProperty);
        set => SetValue(ProcessHandledEventProperty, value);
    }

    /// <summary>
    /// Marks the routed event as handled <b>after</b> a successful command execution.
    /// </summary>
    /// <remarks>
    /// The event is not marked as handled during deferral. If the command does not execute,
    /// the event remains unhandled.
    /// </remarks>
    public bool MarkEventHandled
    {
        get => (bool)GetValue(MarkEventHandledProperty);
        set => SetValue(MarkEventHandledProperty, value);
    }

    /// <summary>
    /// Target for <see cref="RoutedCommand"/> execution. If null, the behavior uses
    /// <c>sender</c> (when it implements <see cref="IInputElement"/>).
    /// </summary>
    public IInputElement? CommandTarget
    {
        get => (IInputElement?)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Lightweight pre-check before command resolution and execution.
    /// Default implementation returns <see langword="true"/>.
    /// Override to impose additional constraints based on <paramref name="sender"/>/<paramref name="eventArgs"/>.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The event data.</param>
    /// <returns><see langword="true"/> if the command can be executed; otherwise, <see langword="false"/>.</returns>
    protected virtual bool CanExecuteCore(object? sender, object? eventArgs)
    {
        return true;
    }

    /// <summary>
    /// Executes the command if possible.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The event data.</param>
    protected virtual void ExecuteCommand(object? sender, object? eventArgs)
    {
        if (!IsAttached || !ReferenceEquals(sender, AssociatedObject))
        {
            return;
        }

        if (!CanExecuteCore(sender, eventArgs))
        {
            return;
        }

        var command = Command;
        if (command == null)
        {
            return;
        }

        var parameter = ResolveCommandParameter(sender, eventArgs);
        bool executed = false;

        switch (command)
        {
            case RoutedCommand routed:
                {
                    var target = ResolveCommandTarget(sender);
                    if (target is not null && routed.CanExecute(parameter, target))
                    {
                        routed.Execute(parameter, target);
                        executed = true;
                    }
                    break;
                }
            default:
                if (command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                    executed = true;
                }
                break;
        }

        if (executed && eventArgs is RoutedEventArgs args && MarkEventHandled)
        {
            args.Handled = true;
        }
    }

    /// <summary>
    /// Defers execution to <see cref="DispatcherPriority.DataBind"/> when <see cref="Command"/> is null but data-bound.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnEventCore(object? sender, object? eventArgs)
    {
        if (eventArgs is RoutedEventArgs { Handled: true } && !ProcessHandledEvent)
        {
            return;
        }

        if (Command is not null)
        {
            ExecuteCommand(sender, eventArgs);
            return;
        }

        if (!BindingOperations.IsDataBound(this, CommandProperty))
        {
            return;
        }

        Dispatcher.InvokeAsync(() => { if (IsEnabled) ExecuteCommand(sender, eventArgs); }, DispatcherPriority.DataBind);
    }

    /// <summary>
    /// Resolves the command parameter in the following precedence:
    /// <list type="number">
    /// <item><description><see cref="CommandParameter"/></description></item>
    /// <item><description><para><see cref="EventArgsConverter"/>:</para>
    ///     <para><b>value</b> = (<see cref="EventArgsParameterPath"/> ? eventArgs[path] : eventArgs),</para>
    ///     <para><b>parameter</b> = (<see cref="EventArgsConverterParameter"/> ?? sender)</para></description></item>
    /// <item><description><see cref="EventArgsParameterPath"/> (no converter)</description></item>
    /// <item><description><see cref="SenderParameterPath"/> (no converter)</description></item>
    /// <item><description><see cref="PassEventArgsToCommand"/> ? eventArgs : null</description></item>
    /// </list>
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The event data.</param>
    /// <returns>The command parameter.</returns>
    /// <remarks>
    /// The converter call and path resolution are performed without catching exceptions.
    /// Invalid paths or out-of-range indexers yield <see langword="null"/> without throwing.
    /// </remarks>
    protected virtual object? ResolveCommandParameter(object? sender, object? eventArgs)
    {
        // 1) CommandParameter
        if (CommandParameter is { } explicitParameter)
        {
            return explicitParameter;
        }

        object? eventArgsSource = null;
        var eventArgsPath = EventArgsParameterPath;
        bool useEventArgsParameterPath = false;
        if (!string.IsNullOrWhiteSpace(eventArgsPath))
        {
            useEventArgsParameterPath = true;
            eventArgsSource = PathExpressionConverter.Instance.Convert(eventArgs, eventArgsPath!);
        }

        // 2) EventArgsConverter: value = (EventArgsParameterPath ? eventArgs[path] : eventArgs),
        //    parameter = (EventArgsConverterParameter ?? sender)
        if (EventArgsConverter is { } converter)
        {
            return converter.Convert(useEventArgsParameterPath ? eventArgsSource : eventArgs, typeof(object), EventArgsConverterParameter ?? sender, CultureInfo.CurrentCulture);
        }

        // 3) EventArgsParameterPath (no converter)
        if (useEventArgsParameterPath)
        {
            return eventArgsSource;
        }

        // 4) SenderParameterPath (no converter)
        var senderPath = SenderParameterPath;
        if (!string.IsNullOrWhiteSpace(senderPath))
        {
            return PathExpressionConverter.Instance.Convert(sender, senderPath!);
        }

        // 5) PassEventArgsToCommand ? eventArgs : null
        return PassEventArgsToCommand ? eventArgs : null;
    }

    /// <summary>
    /// Resolves the target element for <see cref="RoutedCommand"/> execution.
    /// Default implementation returns <see cref="CommandTarget"/> if set; otherwise, <paramref name="sender"/> when it implements <see cref="IInputElement"/>.
    /// </summary>
    protected virtual IInputElement? ResolveCommandTarget(object? sender)
    {
        return CommandTarget ?? sender as IInputElement;
    }

    #endregion
}
