using System.Windows;

namespace Minimal.Behaviors.Wpf;

/// <summary>
/// Represents a type-safe behavior that can attach to objects of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The associated object type.</typeparam>
public abstract class Behavior<T> : Behavior where T : DependencyObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Behavior{T}"/> class.
    /// </summary>
    protected Behavior() : base(typeof(T))
    {
    }

    #region Properties

    /// <summary>
    /// Gets the associated object typed as <typeparamref name="T"/>.
    /// Returns <see langword="null"/> if the behavior is not attached.
    /// </summary>
    public new T? AssociatedObject => (T?)base.AssociatedObject;

    #endregion
}