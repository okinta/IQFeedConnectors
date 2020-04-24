using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Extends classes.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Triggers the action in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">The type of argument to send with the Action.</typeparam>
        /// <param name="action">The action to trigger.</param>
        /// <param name="arg">The argument to send with the Action.</param>
        public static void SafeTrigger<T>(this Action<T> action, T arg)
        {
            action?.Invoke(arg);
        }

        /// <summary>
        /// Runs an async action on all items in a collection. Returns when all actions
        /// have completed.
        /// </summary>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <param name="collection">The collection to run actions on.</param>
        /// <param name="action">The action to run.</param>
        public static async Task AsyncForEach<T>(
            this IEnumerable<T> collection, Func<T, Task> action)
        {
            var tasks = new List<Task>();
            foreach (var item in collection)
            {
                tasks.Add(action(item));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Splits a list into multiple parts.
        /// </summary>
        /// <typeparam name="T">The type of list to split.</typeparam>
        /// <param name="list">The list to split.</param>
        /// <param name="parts">The number of parts to split the list into.</param>
        /// <returns>A list containing the original list split into parts.</returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(
            this IEnumerable<T> list, int parts)
        {
            int i = 0;
            return
                from item in list
                group item by i++ % parts into part
                select part.AsEnumerable();
        }

        /// <summary>
        /// Retrieves the last element in the list.
        /// </summary>
        /// <typeparam name="T">The type of element to retrieve.</typeparam>
        /// <param name="collection">The list to retrieve the last item from.</param>
        /// <returns>The last item in the list.</returns>
        public static T GetLast<T>(this IReadOnlyList<T> collection)
        {
            return collection[collection.Count - 1];
        }
    }
}
