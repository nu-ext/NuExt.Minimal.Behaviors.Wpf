using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Minimal.Behaviors.Wpf;

/// <summary>
/// Represents a collection of <see cref="Behavior"/> objects that can be attached to a WPF element.
/// </summary>
public sealed class BehaviorCollection : FreezableCollection<Behavior>
{
    private readonly List<Behavior> _snapshot = [];
    private DependencyObject? _associatedObject;

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorCollection"/> class.
    /// </summary>
    internal BehaviorCollection()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            ((INotifyCollectionChanged)this).CollectionChanged += OnCollectionChanged;
        }
    }

    #region Properties

    /// <summary>
    /// Gets the object to which this behavior collection is attached.
    /// </summary>
    public DependencyObject? AssociatedObject
    {
        get
        {
            ReadPreamble();
            return _associatedObject;
        }
        private set
        {
            WritePreamble();
            _associatedObject = value;
            WritePostscript();
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles changes in the collection to attach or detach behaviors as necessary.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="NotifyCollectionChangedEventArgs"/> that contains the event data.</param>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Reset:
                // Detach all behaviors in reverse order
                for (int i = _snapshot.Count - 1; i >= 0; i--)
                {
                    var item = _snapshot[i];
                    ItemRemoved(item);
                }
                _snapshot.Clear();
                // IMPORTANT:
                // Even though FreezableCollection uses Reset exclusively for Clear(),
                // this code intentionally handles Reset as a general "rebuild state"
                // event. This is by design, forward‑compatible, and MUST remain as-is.
                Debug.Assert(Count == 0, "Reset should only occur on Clear, collection should be empty");
                for (int i = 0; i < Count; i++)
                {
                    Behavior item = this[i];
                    AddItem(item, i);
                }
                return;
            case NotifyCollectionChangedAction.Move:
                // FreezableCollection does not generate Move notifications.
                Debug.Fail("Move operation is not expected from FreezableCollection.");
                return;
        }

        // Handle Add, Remove, and Replace operations
        // FreezableCollection generates single-item notifications even for batch operations
        if (e.OldItems != null)
        {
            Debug.Assert(e.OldItems.Count == 1);
            // Detach behaviors in reverse order
            for (int i = e.OldItems.Count - 1; i >= 0; i--)
            {
                RemoveItem((Behavior)e.OldItems[i]!);
            }
        }
        if (e.NewItems != null)
        {
            Debug.Assert(e.NewItems.Count == 1);
            for (int i = 0; i < e.NewItems.Count; i++)
            {
                int index = e.NewStartingIndex + i;
                AddItem((Behavior)e.NewItems[i]!, index);
            }
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Attaches the behavior collection to the specified <see cref="DependencyObject"/>.
    /// </summary>
    /// <param name="obj">The object to attach to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the behavior is already attached to an object.
    /// </exception>
    public void Attach(DependencyObject obj)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        if (obj == AssociatedObject)
        {
            return;
        }
        ThrowInvalidOperationExceptionIfAttached();
        AssociatedObject = obj;
        foreach (Behavior item in this)
        {
            item.Attach(AssociatedObject);
        }
    }

    /// <summary>
    /// Detaches the behavior collection from the associated <see cref="DependencyObject"/>.
    /// </summary>
    public void Detach()
    {
        // Detach in reverse order for dependency safety
        for (int i = Count - 1; i >= 0; i--)
        {
            var item = this[i];
            item.Detach();
        }
        AssociatedObject = null;
    }

    /// <summary>
    /// Adds a behavior to the snapshot and attaches it if the collection is already attached.
    /// </summary>
    /// <param name="item">The behavior to add.</param>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    private void AddItem(Behavior item, int index)
    {
        if (_snapshot.Contains(item))
        {
            return;
        }
        ItemAdded(item);
        if (index >= 0 && index <= _snapshot.Count)
        {
            _snapshot.Insert(index, item);
        }
        else
        {
            _snapshot.Add(item);
        }
    }

    /// <summary>
    /// Removes the behavior from the snapshot and detaches it.
    /// </summary>
    /// <param name="item">The behavior to remove.</param>
    private void RemoveItem(Behavior item)
    {
        ItemRemoved(item);
        _snapshot.Remove(item);
    }

    /// <summary>
    /// Attaches the behavior to the associated object if one exists.
    /// </summary>
    /// <param name="item">The behavior to attach.</param>
    private void ItemAdded(Behavior item)
    {
        if (AssociatedObject != null)
        {
            item.Attach(AssociatedObject);
        }
    }

    /// <summary>
    /// Detaches the behavior from its associated object.
    /// </summary>
    /// <param name="item">The behavior to detach.</param>
    private static void ItemRemoved(Behavior item)
    {
        if (item.AssociatedObject != null)
        {
            item.Detach();
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="BehaviorCollection"/> class.
    /// </summary>
    /// <returns>A new instance of <see cref="BehaviorCollection"/>.</returns>
    protected override Freezable CreateInstanceCore()
    {
        return new BehaviorCollection();
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the collection is already attached to an object.
    /// </summary>
    private void ThrowInvalidOperationExceptionIfAttached()
    {
        if (AssociatedObject != null)
        {
            throw new InvalidOperationException("Cannot set the same BehaviorCollection on multiple objects.");
        }
    }

    #endregion
}