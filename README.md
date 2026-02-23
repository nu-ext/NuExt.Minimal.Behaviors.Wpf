# NuExt.Minimal.Behaviors.Wpf

`NuExt.Minimal.Behaviors.Wpf` is a **minimalistic, production‑ready** implementation of WPF **Attached Behaviors** for MVVM. It delivers deterministic, predictable interactivity with ready‑to‑use behaviors (`EventToCommand`, `KeyToCommand`) and template‑driven composition — **dynamic behavior injection via `BehaviorsTemplate`** and **runtime selection via `BehaviorsTemplateSelector`**.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Behaviors.Wpf.svg)](https://www.nuget.org/packages/NuExt.Minimal.Behaviors.Wpf)
[![Build](https://github.com/nu-ext/NuExt.Minimal.Behaviors.Wpf/actions/workflows/ci.yml/badge.svg)](https://github.com/nu-ext/NuExt.Minimal.Behaviors.Wpf/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Behaviors.Wpf?label=license)](https://github.com/nu-ext/NuExt.Minimal.Behaviors.Wpf/blob/main/LICENSE)
[![Downloads](https://img.shields.io/nuget/dt/NuExt.Minimal.Behaviors.Wpf.svg)](https://www.nuget.org/packages/NuExt.Minimal.Behaviors.Wpf)

**Package ecosystem:** The core package ships the foundational attached behavior infrastructure and essential behaviors for MVVM. For extra behaviors/services, see [`NuExt.Minimal.Mvvm.Wpf`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf).

---

## Why Minimal.Behaviors?

- **Simplicity & maintainability.** No trigger/action stacks. One behavior = one responsibility. Readable, testable, predictable.
- **Dynamic composition.** `BehaviorsTemplate` and `BehaviorsTemplateSelector` let you define and reuse behavior sets with minimal XAML.
- **Practical coverage.** Event-to-command binding, keyboard shortcuts, dynamic behavior sets — no hidden indirection.
- **Deterministic semantics.** Clear command targeting; no focus-based ambiguity.
- **Performance-focused.** Hot paths avoid allocations. No unnecessary plumbing.

### Compatibility

- **WPF (.NET 8/9/10 and .NET Framework 4.6.2+)**
- Works with any MVVM framework.
- No dependency on external behavior frameworks.

---

## Core Components

- `Interaction` – attached properties: `Behaviors`, `BehaviorsTemplate`, `BehaviorsTemplateSelector`, `BehaviorsTemplateSelectorParameter`.
- `BehaviorCollection` – observable collection managing behavior lifecycle.
- `EventToCommand`, `KeyToCommand` – ready-to-use behaviors with a predictable contract.

---

## Core Principle

```xml
<!-- Prefer concise attached behaviors over trigger/action composition -->
<nx:Interaction.Behaviors>
  <nx:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" />
  <nx:KeyToCommand Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
  <local:MyService />
</nx:Interaction.Behaviors>
```

---

## Quick Start
1.  **Add the namespace**:
    ```xml
    xmlns:nx="http://schemas.nuext.minimal/xaml"
    ```
2.  **Attach a behavior**:
    ```xml
    <Button Content="Click Me">
      <nx:Interaction.Behaviors>
        <nx:EventToCommand EventName="Click" Command="{Binding MyCommand}" />
      </nx:Interaction.Behaviors>
    </Button>
    ```
3.  **Define the command** in your ViewModel.

---

### Dynamic Behaviors with BehaviorsTemplate
Define once, apply many times. The template supports **two** concise formats:
- **Single behavior** via `ContentControl.Content`
- **Multiple behaviors** via `ItemsControl.Items`

#### Single:
```xml
<Window.Resources>
  <DataTemplate x:Key="SaveBehavior">
    <ContentControl>
      <nx:KeyToCommand Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
    </ContentControl>
  </DataTemplate>
</Window.Resources>

<TextBox nx:Interaction.BehaviorsTemplate="{StaticResource SaveBehavior}" />
```
#### Multiple:
```xml
<Window.Resources>
  <DataTemplate x:Key="EditBehaviors">
    <ItemsControl>
      <nx:KeyToCommand Gesture="F2"     Command="{Binding StartEditCommand}" />
      <nx:KeyToCommand Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
      <nx:KeyToCommand Gesture="Escape" Command="{Binding CancelCommand}" />
    </ItemsControl>
  </DataTemplate>
</Window.Resources>

<ListBox nx:Interaction.BehaviorsTemplate="{StaticResource EditBehaviors}" />
```

---

### Dynamic Selection with BehaviorsTemplateSelector
Switch behavior sets at runtime:
```xml
<Window.Resources>
  <DataTemplate x:Key="ReadOnlyTemplate">
    <ContentControl>
      <nx:KeyToCommand Gesture="F2" Command="{Binding StartEditCommand}" />
    </ContentControl>
  </DataTemplate>

  <DataTemplate x:Key="EditableTemplate">
    <ItemsControl>
      <nx:KeyToCommand Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
      <nx:KeyToCommand Gesture="Escape" Command="{Binding CancelCommand}" />
    </ItemsControl>
  </DataTemplate>

  <local:MyBehaviorSelector x:Key="BehaviorSelector"
                            ReadOnlyTemplate="{StaticResource ReadOnlyTemplate}"
                            EditableTemplate="{StaticResource EditableTemplate}" />
</Window.Resources>

<TextBox nx:Interaction.BehaviorsTemplateSelector="{StaticResource BehaviorSelector}" />
```

```csharp
public sealed class MyBehaviorSelector : DataTemplateSelector
{
    public DataTemplate? ReadOnlyTemplate { get; set; }
    public DataTemplate? EditableTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        => (item is MyDataItem m && m.IsReadOnly) ? ReadOnlyTemplate : EditableTemplate;
}
```
> Tip: If no selector parameter is provided, the framework resolves the element’s data item (DataContext, Content, or Header).
> Bind `BehaviorsTemplateSelectorParameter="{Binding}"` to drive selection from the `DataContext` explicitly when needed.

---

### Practical Scenarios
#### Command on Loaded
```xml
<nx:EventToCommand EventName="Loaded" Command="{Binding InitializeCommand}" />
```
#### Deterministic routed events
```xml
<ListView>
  <nx:Interaction.Behaviors>
    <nx:EventToCommand EventName="SelectionChanged"
                       Command="{Binding SelectionChangedCommand}"
                       PassEventArgsToCommand="True" />
  </nx:Interaction.Behaviors>
</ListView>
```
#### MVVM-friendly per-control shortcuts
```xml
<TextBox>
  <nx:Interaction.Behaviors>
    <nx:KeyToCommand Gesture="Ctrl+Enter" Command="{Binding SubmitCommand}" />
    <nx:KeyToCommand Gesture="Escape"     Command="{Binding CancelCommand}" />
  </nx:Interaction.Behaviors>
</TextBox>
```

#### Selector reevaluation on DataContext changes
```xml
<TextBox nx:Interaction.BehaviorsTemplateSelector="{StaticResource BehaviorSelector}"
         nx:Interaction.BehaviorsTemplateSelectorParameter="{Binding}" />
```

#### Handling already-handled routed events (Preview included)
```xml
<nx:EventToCommand EventName="PreviewMouseDown" ProcessHandledEvent="True"
                   Command="{Binding PreviewMouseDownCommand}" />
```

#### Deterministic command parameter resolution
```xml
<nx:EventToCommand EventName="SelectionChanged"
                   Command="{Binding SelectionChangedCommand}"
                   EventArgsParameterPath="AddedItems[0]" />
```

### Creating a custom service (attachable)

A meaningful non‑visual service can participate in `nx:Interaction.Behaviors` and solve a concrete UX task.  
Example below: **auto‑scroll the host control to the currently selected item** for `ListBox`/`ListView`/`DataGrid`.

#### Example: `AutoScrollSelectedItemService`

```csharp

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Minimal.Behaviors.Wpf;

namespace MyApp.Behaviors
{
    /// <summary>
    /// Automatically scrolls the host selector (ListBox/ListView/DataGrid) to the current SelectedItem.
    /// </summary>
    public sealed class AutoScrollSelectedItemService : Behavior<Selector>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SelectionChanged += OnSelectionChanged;
            AssociatedObject.Dispatcher.InvokeAsync(ScrollToCurrent, DispatcherPriority.Loaded);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
            base.OnDetaching();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            AssociatedObject.Dispatcher.InvokeAsync(ScrollToCurrent, DispatcherPriority.Background);
        }

        private void ScrollToCurrent()
        {
            var item = AssociatedObject.SelectedItem;
            if (item is null)
                return;

            // Prefer control-specific ScrollIntoView when available.
            switch (AssociatedObject)
            {
                case ListBox lb:
                    lb.ScrollIntoView(item);
                    return;
                case ListView lv:
                    lv.ScrollIntoView(item);
                    return;
                case DataGrid dg:
                    dg.ScrollIntoView(item);
                    return;
            }

            // Fallback: resolve container and bring it into view.
            if (AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement fe)
                fe.BringIntoView();
        }
    }
}
```

#### Usage in XAML

```xml
<Window
  x:Class="MyApp.Views.MainView"
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:nx="http://schemas.nuext.minimal/xaml"
  xmlns:local="clr-namespace:MyApp.Behaviors">

  <Grid>
    <ListView ItemsSource="{Binding Items}"
              SelectedItem="{Binding Selected, Mode=TwoWay}">
      <nx:Interaction.Behaviors>
        <local:AutoScrollSelectedItemService />
      </nx:Interaction.Behaviors>
    </ListView>
  </Grid>
</Window>
```

#### Notes & best practices

- **Attach/detach symmetry**: subscribe in `OnAttached`, unsubscribe in `OnDetaching`. No dangling handlers.
- **Keep OnAttached cheap**: defer heavy work via `DispatcherPriority.Background` or offload to a worker.
- **Thread safety**: marshal UI access to `Dispatcher`.
- **Order matters**: items in `nx:Interaction.Behaviors` attach **top‑to‑bottom**; place services before/after others if you need specific ordering.
- **Error handling**: do not swallow exceptions silently; ensure resources are released on failures.
- **Testability**: keep logic behind clear, minimal methods to simplify unit testing.

### Deterministic parameters & selector coordination

**Parameter precedence (short):**

1. **`CommandParameter`** (if set) — wins; everything else is ignored.  
2. **`EventArgsConverter`** (if set):  
   • **value** = (`EventArgsParameterPath` ? `eventArgs[path]` : `eventArgs`)  
   • **parameter** = (`EventArgsConverterParameter` ?? `sender`)  
3. **`EventArgsParameterPath`** (no converter): `eventArgs[path]`  
4. **`SenderParameterPath`** (no converter): `sender[path]`  
5. **Fallback**: `PassEventArgsToCommand ? eventArgs : null`

> When a converter is set (step 2), `SenderParameterPath` is intentionally ignored.  
> Without a converter, if both paths are provided, step 3 (`EventArgsParameterPath`) is considered before step 4 (`SenderParameterPath`).

---

## What Makes Teams Adopt It

- **Minimal and explicit API surface.** No trigger/action stacks; behaviors are explicit and composable.
- **Template-driven composition.** Reuse behavior sets via data templates (single or multiple behaviors) — concise and flexible.
- **Selector-driven switching.** Change behavior sets at runtime with `BehaviorsTemplateSelector` and a single parameter binding.
- **Consistent WPF semantics.** Clear command targeting; no implicit focus-based resolution.
- **MVVM-first design.** Clean event → `ICommand` binding with a well-defined parameter precedence.
- **Predictable behavior lifecycle.** Dynamic composition does not leave dangling references.
- **Source package option.** Drop-in sources for straightforward embedding and debugging.

---

## Migration from Blend/Interactivity

If your project uses **Blend Behaviors**, **System.Windows.Interactivity**, or **Microsoft.Xaml.Behaviors**, migration is straightforward. Minimal.Behaviors preserves the mental model but removes the heavy trigger/action stack.

|Blend / Interactivity|Minimal equivalent|Notes|
|---|---|---|
|Interaction.Behaviors|nx:Interaction.Behaviors|Same structure, predictable lifecycle.|
|EventTrigger + InvokeCommandAction|nx:EventToCommand|Direct event → `ICommand` binding.|
|KeyBinding / InputBinding|nx:KeyToCommand|Keyboard gestures per control, MVVM-friendly.|
|Reusable stacks in resources|BehaviorsTemplate|Define behavior sets once, attach anywhere.|
|Runtime switching via triggers|BehaviorsTemplateSelector|Cleaner, deterministic selection.|

**No hidden dependencies, no extra assemblies, no performance traps.**
Your existing behavior patterns map directly to smaller, clearer equivalents.

---

## Performance Tips

- **Use explicit** `CommandParameter` for high-frequency events; avoid deep parameter extraction on high-frequency routes (e.g., mouse move).
- **Keep converters lightweight** and pass only required data.
- **Reuse behavior templates** instead of repeating inline declarations.
- **Control selector reevaluation**: bind `BehaviorsTemplateSelectorParameter` (typically to `"{Binding}"`) to update only when needed.

---

## Installation

Via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.Minimal.Behaviors.Wpf
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Behaviors.Wpf`.
3. Install.

### Source Code Package (no external binary dependency)

Prefer to vendor the framework and keep your app dependency‑flat?  
Use the **source package** to embed the entire behavior infrastructure directly into your project:

- **No external binary dependency** — sources compile as part of your app.
- **Easier debugging** — step into the framework code without symbol servers.
- **Deterministic builds** — you control updates via package version pinning.
- **Same API** — identical public surface to the binary package.

> This is ideal for teams that prefer **zero external runtime dependencies** and want to keep UI infrastructure fully **in-tree**.

NuGet: [`NuExt.Minimal.Behaviors.Wpf.Sources`](https://www.nuget.org/packages/NuExt.Minimal.Behaviors.Wpf.Sources).

```sh
dotnet add package NuExt.Minimal.Behaviors.Wpf.Sources
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Behaviors.Wpf.Sources`.
3. Install.

## Ecosystem

- [NuExt.Minimal.Mvvm](https://github.com/nu-ext/NuExt.Minimal.Mvvm)
- [NuExt.Minimal.Mvvm.SourceGenerator](https://github.com/nu-ext/NuExt.Minimal.Mvvm.SourceGenerator)
- [NuExt.Minimal.Mvvm.Wpf](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf)
- [NuExt.Minimal.Mvvm.MahApps.Metro](https://github.com/nu-ext/NuExt.Minimal.Mvvm.MahApps.Metro)
- [NuExt.System](https://github.com/nu-ext/NuExt.System)
- [NuExt.System.Data](https://github.com/nu-ext/NuExt.System.Data)
- [NuExt.System.Data.SQLite](https://github.com/nu-ext/NuExt.System.Data.SQLite)

## Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

## License

MIT. See LICENSE.