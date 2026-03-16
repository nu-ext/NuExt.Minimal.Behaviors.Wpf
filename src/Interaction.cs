using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Minimal.Behaviors.Wpf;

/// <summary>
/// Provides attached properties and methods for managing <see cref="BehaviorCollection"/> on WPF elements.
/// </summary>
public static class Interaction
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Behaviors attached dependency property.
    /// </summary>
    private static readonly DependencyProperty BehaviorsInternalProperty = DependencyProperty.RegisterAttached(
        "InteractionBehaviors", typeof(BehaviorCollection), typeof(Interaction),
        new FrameworkPropertyMetadata(defaultValue: null, OnBehaviorsChanged));

    /// <summary>
    /// Identifies the BehaviorsTemplate attached dependency property.
    /// </summary>
    public static readonly DependencyProperty BehaviorsTemplateProperty = DependencyProperty.RegisterAttached(
        "BehaviorsTemplate", typeof(DataTemplate), typeof(Interaction),
        new FrameworkPropertyMetadata(defaultValue: null, OnBehaviorsTemplateChanged));

    /// <summary>
    /// Identifies the BehaviorsTemplateSelector attached dependency property.
    /// </summary>
    public static readonly DependencyProperty BehaviorsTemplateSelectorProperty = DependencyProperty.RegisterAttached(
        "BehaviorsTemplateSelector", typeof(DataTemplateSelector), typeof(Interaction),
        new FrameworkPropertyMetadata(defaultValue: null, OnBehaviorsTemplateSelectorChanged));

    /// <summary>
    /// Identifies the BehaviorsTemplateSelectorParameter attached dependency property.
    /// </summary>
    public static readonly DependencyProperty BehaviorsTemplateSelectorParameterProperty = DependencyProperty.RegisterAttached(
        "BehaviorsTemplateSelectorParameter", typeof(object), typeof(Interaction),
        new FrameworkPropertyMetadata(defaultValue: null, OnBehaviorsTemplateSelectorParameterChanged));

    /// <summary>
    /// Identifies the BehaviorsTemplateSnapshot attached dependency property.
    /// </summary>
    private static readonly DependencyProperty BehaviorsSnapshotProperty = DependencyProperty.RegisterAttached(
        "BehaviorsSnapshot", typeof(List<Behavior>), typeof(Interaction), new PropertyMetadata(defaultValue: null));

    #endregion

    #region Event Handlers

    /// <summary>
    /// Invoked when the value of the Behaviors attached property changes.
    /// </summary>
    private static void OnBehaviorsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var oldBehaviors = (BehaviorCollection?)e.OldValue;
        var newBehaviors = (BehaviorCollection?)e.NewValue;

        if (ReferenceEquals(oldBehaviors, newBehaviors))
        {
            return;
        }

        oldBehaviors?.Detach();
        newBehaviors?.Attach(obj);
    }

    /// <summary>
    /// Invoked when the value of the BehaviorsTemplate attached property changes.
    /// </summary>
    private static void OnBehaviorsTemplateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var oldDataTemplate = (DataTemplate?)e.OldValue;
        var newDataTemplate = (DataTemplate?)e.NewValue;

        if (ReferenceEquals(oldDataTemplate, newDataTemplate))
        {
            return;
        }

        var behaviors = GetBehaviors(obj);
        ClearCurrentTemplateBehaviors(obj, behaviors);

        if (newDataTemplate == null)
        {
            return;
        }

        UpdateBehaviorsFromTemplate(obj, newDataTemplate, behaviors);
    }

    /// <summary>
    /// Invoked when the value of the BehaviorsTemplateSelector attached property changes.
    /// </summary>
    private static void OnBehaviorsTemplateSelectorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var oldDataTemplateSelector = (DataTemplateSelector?)e.OldValue;
        var newDataTemplateSelector = (DataTemplateSelector?)e.NewValue;

        if (ReferenceEquals(oldDataTemplateSelector, newDataTemplateSelector))
        {
            return;
        }

        var behaviorsTemplate = GetBehaviorsTemplate(obj);
        if (behaviorsTemplate != null)
        {
            return;
        }

        var behaviors = GetBehaviors(obj);
        ClearCurrentTemplateBehaviors(obj, behaviors);

        if (newDataTemplateSelector == null)
        {
            return;
        }

        var parameter = GetBehaviorsTemplateSelectorParameter(obj);

        var item = parameter ?? GetDataContext(obj);
        var template = newDataTemplateSelector.SelectTemplate(item, obj);

        if (template == null)
        {
            return;
        }

        UpdateBehaviorsFromTemplate(obj, template, behaviors);
    }

    /// <summary>
    /// Invoked when the value of the BehaviorsTemplateSelectorParameter attached property changes.
    /// </summary>
    private static void OnBehaviorsTemplateSelectorParameterChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var oldParameter = e.OldValue;
        var newParameter = e.NewValue;

        if (Equals(oldParameter, newParameter))
        {
            return;
        }

        var behaviorsTemplate = GetBehaviorsTemplate(obj);
        if (behaviorsTemplate != null)
        {
            return;
        }

        var behaviorsTemplateSelector = GetBehaviorsTemplateSelector(obj);
        if (behaviorsTemplateSelector == null)
        {
            return;
        }

        var behaviors = GetBehaviors(obj);
        ClearCurrentTemplateBehaviors(obj, behaviors);

        var item = newParameter ?? GetDataContext(obj);
        var template = behaviorsTemplateSelector.SelectTemplate(item, obj);

        if (template == null)
        {
            return;
        }

        UpdateBehaviorsFromTemplate(obj, template, behaviors);
    }

    #endregion

    #region Dependency Methods

    /// <summary>
    /// Gets the <see cref="BehaviorCollection"/> associated with the specified <see cref="DependencyObject"/>.
    /// This attached property is backed by a private shadow DP and is XAML-visible by name.        
    /// </summary>
    /// <param name="obj">The object from which to get the behaviors.</param>
    /// <returns>The <see cref="BehaviorCollection"/> associated with the specified object.</returns>
    public static BehaviorCollection GetBehaviors(DependencyObject obj)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        var behaviors = (BehaviorCollection?)obj.GetValue(BehaviorsInternalProperty);
        if (behaviors == null)
        {
            behaviors = [];
            obj.SetValue(BehaviorsInternalProperty, behaviors);
        }
        return behaviors;
    }

    /// <summary>
    /// Gets the BehaviorsTemplate attached property value.
    /// </summary>
    public static DataTemplate? GetBehaviorsTemplate(DependencyObject obj)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        return (DataTemplate?)obj.GetValue(BehaviorsTemplateProperty);
    }

    /// <summary>
    /// Sets the BehaviorsTemplate attached property value.
    /// </summary>
    public static void SetBehaviorsTemplate(DependencyObject obj, DataTemplate? template)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        obj.SetValue(BehaviorsTemplateProperty, template);
    }

    /// <summary>
    /// Gets the BehaviorsTemplateSelector attached property value.
    /// </summary>
    public static DataTemplateSelector? GetBehaviorsTemplateSelector(DependencyObject obj)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        return (DataTemplateSelector?)obj.GetValue(BehaviorsTemplateSelectorProperty);
    }

    /// <summary>
    /// Sets the BehaviorsTemplateSelector attached property value.
    /// </summary>
    public static void SetBehaviorsTemplateSelector(DependencyObject obj, DataTemplateSelector? selector)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        obj.SetValue(BehaviorsTemplateSelectorProperty, selector);
    }

    /// <summary>
    /// Gets the BehaviorsTemplateSelectorParameter attached property value.
    /// </summary>
    public static object? GetBehaviorsTemplateSelectorParameter(DependencyObject obj)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        return obj.GetValue(BehaviorsTemplateSelectorParameterProperty);
    }

    /// <summary>
    /// Sets the BehaviorsTemplateSelectorParameter attached property value.
    /// </summary>
    public static void SetBehaviorsTemplateSelectorParameter(DependencyObject obj, object? parameter)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        obj.SetValue(BehaviorsTemplateSelectorParameterProperty, parameter);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Clears all behaviors attached to the specified object. 
    /// Detaches behaviors created from a template and removes the behavior 
    /// collection from the object, allowing all behaviors to be garbage‑collected.
    /// </summary>
    /// <param name="obj">The object whose behaviors should be cleared.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    public static void ClearBehaviors(DependencyObject obj)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        var behaviors = (BehaviorCollection?)obj.GetValue(BehaviorsInternalProperty);
        if (behaviors is null) return;
        ClearCurrentTemplateBehaviors(obj, behaviors);
        obj.ClearValue(BehaviorsInternalProperty);
    }

    private static void ClearCurrentTemplateBehaviors(DependencyObject obj, BehaviorCollection behaviors)
    {
        if (obj.GetValue(BehaviorsSnapshotProperty) is List<Behavior> oldItems)
        {
            // IMPORTANT:
            // We must explicitly Detach() BEFORE removing from the collection.
            // Removing first causes WPF to fire CollectionChanged and detach later,
            // which may trigger "System.Windows.Data Error: 2 : Cannot find governing
            // FrameworkElement or FrameworkContentElement for target element" while bindings
            // still point to the soon-to-be-orphaned target. Pre-detaching avoids this.
            // Detach() is idempotent; the subsequent collection-driven detach is a no-op.
            for (int i = oldItems.Count - 1; i >= 0; i--)
            {
                var behavior = oldItems[i];
                behavior.Detach();
                behaviors.Remove(behavior);
            }
        }
        obj.SetValue(BehaviorsSnapshotProperty, null);
    }

    private static void UpdateBehaviorsFromTemplate(DependencyObject obj, DataTemplate template, BehaviorCollection behaviors)
    {
        _ = template ?? throw new ArgumentNullException(nameof(template));

        if (!template.IsSealed)
        {
            template.Seal();
        }

        var newItems = LoadBehaviorsFromTemplate(template);

        Debug.Assert(obj.GetValue(BehaviorsSnapshotProperty) == null);
        if (newItems.Count > 0)
        {
            obj.SetValue(BehaviorsSnapshotProperty, newItems);
            foreach (var behavior in newItems)
            {
                behaviors.Add(behavior);
            }
        }
    }

    private static List<Behavior> LoadBehaviorsFromTemplate(DataTemplate template)
    {
        var content = template.LoadContent();
        List<Behavior> behaviors;

        switch (content)
        {
            case ContentControl contentControl when contentControl.Content is Behavior behavior:
                behaviors = [behavior];
                contentControl.Content = null;
                break;

            case ItemsControl itemsControl:
                behaviors = new List<Behavior>(itemsControl.Items.Count);
                foreach (var item in itemsControl.Items)
                {
                    if (item is Behavior behavior)
                    {
                        behaviors.Add(behavior);
                    }
                }
                itemsControl.ItemsSource = null;
                itemsControl.Items.Clear();
                break;

            default:
                throw new InvalidOperationException("Use ContentControl or ItemsControl in the template to specify Behaviors.");
        }

        return behaviors;
    }

    /// <summary>
    /// Recursively gets the data item (DataContext, Content, or Header) for the specified dependency object.
    /// </summary>
    /// <param name="obj">The object to start navigation from.</param>
    /// <returns>The found data context, or null.</returns>
    /// <remarks>
    /// Recursion is safe as WPF trees are acyclic by design. Returns null if no data item is found.
    /// </remarks>
    public static object? GetDataContext(DependencyObject obj) =>
        (obj ?? throw new ArgumentNullException(nameof(obj))) switch
        {
            ContentPresenter cp => cp.Content is DependencyObject d
                ? (GetDataContext(d) ?? cp.DataContext)
                : (cp.Content ?? cp.DataContext),
            HeaderedContentControl hc => hc.Content is DependencyObject d
                ? (GetDataContext(d) ?? hc.DataContext)
                : (hc.Content ?? (hc.Header is DependencyObject hd
                    ? (GetDataContext(hd) ?? hc.DataContext)
                    : (hc.Header ?? hc.DataContext))),
            ContentControl cc => cc.Content is DependencyObject d
                ? (GetDataContext(d) ?? cc.DataContext)
                : (cc.Content ?? cc.DataContext),
            FrameworkElement fe => fe.DataContext,
            FrameworkContentElement fce => fce.DataContext,
            _ => null
        };

    #endregion
}
