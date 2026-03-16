using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Minimal.Behaviors.Wpf;

internal class PathExpressionConverter : IValueConverter
{
    object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is not string path)
            return null;
        return Convert(value, path);
    }

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"ConvertBack is not supported by {GetType().Name}.");
    }

    public object? Convert(object? source, string path)
    {
        return TryGetValueByPath(source, path!, out var value) ? value : null;
    }

    public static readonly PathExpressionConverter Instance = new();

    internal readonly record struct Token(string Name, int? Index)
    {
        public bool HasIndex => Index.HasValue;
    }

    private static readonly char[] s_splitChars = ['.'];
    private static readonly ConcurrentDictionary<string, Token[]> s_pathCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Attempts to extract a value from <paramref name="source"/> using a simple dotted path with integer indexers.
    /// Examples: "OriginalSource", "OriginalSource.Items[0]".
    /// </summary>
    internal static bool TryGetValueByPath(object? source, string path, out object? value)
    {
        value = null;
        if (source is null || string.IsNullOrWhiteSpace(path)) return false;

        var tokens = GetTokens(path);
        object? current = source;

        for (var i = 0; i < tokens.Length && current != null; i++)
        {
            var token = tokens[i];
            if (!string.IsNullOrEmpty(token.Name))
            {
                var type = current.GetType();
                var prop = type.GetProperty(token.Name, BindingFlags.Instance | BindingFlags.Public);
                if (prop is { CanRead: true })
                {
                    current = prop.GetValue(current, null);
                }
                else
                {
                    return false; // property not found / not readable -> path resolution failed
                }
            }

            // Optional indexer hop
            if (token.HasIndex && current != null)
            {
                var idx = token.Index!.Value;
                switch (current)
                {
                    case Array arr:
                        if ((uint)idx < (uint)arr.Length)
                            current = arr.GetValue(idx);
                        else
                            return false;// out of range -> structural failure
                        break;
                    case IList list:
                        if ((uint)idx < (uint)list.Count)
                            current = list[idx];
                        else
                            return false;// out of range -> structural failure
                        break;
                    default:
                        return false;  // unsupported indexer target -> structural failure
                }
            }
        }

        value = current;
        return true;
    }

    private static Token[] GetTokens(string path) => s_pathCache.GetOrAdd(path, ParsePath);

    private static Token[] ParsePath(string propertyPath)
    {
        // Very small parser: split by '.' and detect optional trailing [int]
        var propertyPathParts = propertyPath.Split(s_splitChars, StringSplitOptions.RemoveEmptyEntries
#if NET5_0_OR_GREATER
            | StringSplitOptions.TrimEntries
#endif
            );
        var tokens = new Token[propertyPathParts.Length];

        for (int i = 0; i < propertyPathParts.Length; i++)
        {
#if NET5_0_OR_GREATER
            var propertyPathPart = propertyPathParts[i];
#else
            var propertyPathPart = propertyPathParts[i].Trim();
#endif
            string name = propertyPathPart;
            int? index = null;

            var lb = propertyPathPart.IndexOf('[');//"OriginalSource.Items[0]"
            if (lb >= 0 && propertyPathPart
#if NETFRAMEWORK
                    .EndsWith("]", StringComparison.Ordinal)
#else
                    .EndsWith(']')
#endif
               )
            {
#if NETFRAMEWORK
                var content = propertyPathPart.Substring(lb + 1, propertyPathPart.Length - lb - 2).Trim();
#else
                var content = propertyPathPart.AsSpan(lb + 1, propertyPathPart.Length - lb - 2).Trim();
#endif
                if (int.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) && idx >= 0)
                {
#if NETFRAMEWORK
                    name = propertyPathPart.Substring(0, lb);
#else
                    name = propertyPathPart[..lb];
#endif
                    index = idx;
                }
            }

            tokens[i] = new Token(name, index);
        }

        return tokens;
    }
}
