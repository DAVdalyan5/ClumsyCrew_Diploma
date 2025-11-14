using R3;
using System;

namespace HeistNSeek.Helpers
{
    /// <summary>
    /// Reactive variable extension methods using R3.
    /// Allows automatic synchronization when primitive values change.
    /// </summary>
    public static class VariableLister
    {
        /// <summary>
        /// Syncs a source ReactiveProperty to a target ReactiveProperty.
        /// Whenever source changes, target updates automatically.
        /// Syncs between two EXISTING properties (one-to-one):
        /// </summary>        
        /// <remarks>
        /// Example: health.LinkTo(displayHealth);
        /// </remarks>
        public static IDisposable SyncVariables<T>(this ReactiveProperty<T> source, ReactiveProperty<T> target)
        {
            return source.Subscribe(value => target.Value = value);
        }

        /// <summary>
        /// Creates a ReactiveProperty and links it to another ReactiveProperty.
        /// Shorthand for AsReactive + SyncVariables.
        /// </summary>
        /// <remarks>
        /// Example: 100.LinkToTarget(targetHealth);
        /// </remarks>
        public static ReactiveProperty<T> SyncVariables<T>(this T value, ReactiveProperty<T> target)
        {
            var reactive = new ReactiveProperty<T>(value);
            reactive.SyncVariables(target);
            return reactive;
        }

        /// <summary>
        /// Creates a ReactiveProperty and immediately subscribes to changes.
        /// Shorthand for AsReactive + ExecuteOnChange.
        /// </summary>
        /// <remarks>
        /// Example: true.OnChanged(isAlive => Debug.Log($"Alive: {isAlive}"));
        /// </remarks>
        public static ReactiveProperty<T> ExecuteOnChange<T>(this T value, Action<T> onValueChanged)
        {
            var reactive = new ReactiveProperty<T>(value);
            reactive.ExecuteOnChange(onValueChanged);
            return reactive;
        }

        /// <summary>
        /// Executes an action whenever the ReactiveProperty value changes.
        /// Just runs the code when value changs.
        /// Useful for triggering side effects (UI updates, logging, etc).
        /// </summary>
        /// <remarks>
        /// Example: score.Subscribe(value => Debug.Log($"Score: {value}"));
        /// </remarks>
        public static IDisposable ExecuteOnChange<T>(this ReactiveProperty<T> source, Action<T> onValueChanged)
        {
            return source.Subscribe(onValueChanged);
        }

        /// <summary>
        /// Transforms a ReactiveProperty value and creates a NEW ReactiveProperty with the result.
        /// Useful for derived properties (e.g., converting int health to float health percent).
        /// </summary>
        /// <remarks>
        /// Example: var healthPercent = health.MapTo(h => (float)h / maxHealth);
        /// </remarks>
        public static ReactiveProperty<TOut> MapTo<TIn, TOut>(
            this ReactiveProperty<TIn> source,
            Func<TIn, TOut> transformer,
            TOut initialValue = default)
        {
            var result = new ReactiveProperty<TOut>(initialValue);
            source.Subscribe(value => result.Value = transformer(value));
            return result;
        }

        // ============================================
        // Convenience methods for primitives
        // ============================================

        /// <summary>
        /// Converts a primitive value to a ReactiveProperty.
        /// Allows using VariableLister methods on simple types.
        /// </summary>
        /// <remarks>
        /// Example: var isAlive = true.AsReactive();
        ///          isAlive.ExecuteOnChange(v => Debug.Log(v));
        /// </remarks>
        public static ReactiveProperty<T> AsReactive<T>(this T value)
        {
            return new ReactiveProperty<T>(value);
        }
    }
}
