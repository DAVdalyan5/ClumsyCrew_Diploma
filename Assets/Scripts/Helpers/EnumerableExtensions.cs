using R3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Runtime.Helpers
{
    public static class EnumerableExtensions
    {
        public static void AddIfNotContains<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        public static void AddIfNotContains<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.TryAdd(key, value))
            {
                return;
            }

            if (EqualityComparer<TValue>.Default.Equals(dict[key], default(TValue)))
            {
                dict[key] = value;
            }
        }

        /// <summary>
        /// returns if collection had the specified item or not
        /// </summary>
        public static bool TryAddIfNotContains<Tkey, TValue>(this IDictionary<Tkey, TValue> dict, Tkey key, TValue value)
        {
            if (dict.TryAdd(key, value))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// returns if collection had the specified item or not
        /// </summary>
        public static bool TryAddIfNotContains<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
                return false;
            }
            return true;
        }

        public static void RemoveEntryIfContains<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.ContainsKey(key))
            {
                dict.Remove(key);
            }
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }

        /// <summary>
        /// Returns a random element from the list and outputs the other elements in a separate list.
        /// </summary>
        public static T TakeRandomElement<T>(this IList<T> source, out IList<T> otherValues)
        {
            if (source == null || source.Count == 0)
            {
                otherValues = new List<T>();
                return default;
            }

            var randomIndex = new Random().Next(0, source.Count);
            var randomElement = source[randomIndex];

            otherValues = source.Where((x, i) => i != randomIndex).ToList();

            return randomElement;
        }

        /// <summary>
        /// Returns a random element from the list
        /// </summary>
        public static T TakeRandomElement<T>(this IList<T> source)
        {
            if (source == null || source.Count == 0)
            {
                return default;
            }

            var randomIndex = new Random().Next(0, source.Count);
            return source[randomIndex];
        }

        /// <summary>
        /// Performs the specified action on each element of the collection
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) return;
            foreach (var item in source)
            {
                action?.Invoke(item);
            }
        }

        public static void AddMany(this CompositeDisposable disposables, IEnumerable<IDisposable> toAdd)
        {
            toAdd.ForEach(d => disposables.Add(d));
        }
    }
}
