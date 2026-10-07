// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Laz;

/// <summary>
/// A bounded breadth-first search over a tree of disposable nodes.
/// </summary>
/// <remarks>
/// The search takes ownership of the root and of every node returned by <c>fetchChildren</c>.
/// Nodes that are returned as matches are handed over to the caller; all other nodes are disposed.
/// </remarks>
internal static class TreeSearch
{
    internal static List<T> Find<T>(
        T root,
        Func<T, IReadOnlyList<T>> fetchChildren,
        Func<T, bool> predicate,
        FindOptions options,
        int maxResults)
        where T : class, IDisposable
    {
        var results = new List<T>();
        var queue = new Queue<(T Node, int Depth)>();
        queue.Enqueue((root, 0));
        var visited = 0;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            while (queue.Count > 0)
            {
                var (node, depth) = queue.Dequeue();
                var keep = false;
                try
                {
                    if (depth > 0)
                    {
                        visited++;
                        if (predicate(node))
                        {
                            results.Add(node);
                            keep = true;
                            if (results.Count >= maxResults)
                                break;
                        }
                    }

                    if (depth < options.MaxDepth &&
                        visited < options.MaxElements &&
                        stopwatch.Elapsed < options.Timeout)
                    {
                        IReadOnlyList<T> children;
                        try
                        {
                            children = fetchChildren(node);
                        }
                        catch (ElementNotAvailableException)
                        {
                            // The subtree disappeared while searching, e.g. a window was closed.
                            children = Array.Empty<T>();
                        }

                        foreach (var child in children)
                            queue.Enqueue((child, depth + 1));
                    }
                }
                finally
                {
                    if (!keep)
                        node.Dispose();
                }

                if (visited >= options.MaxElements || stopwatch.Elapsed >= options.Timeout)
                    break;
            }
        }
        catch
        {
            foreach (var result in results)
                result.Dispose();
            throw;
        }
        finally
        {
            while (queue.Count > 0)
                queue.Dequeue().Node.Dispose();
        }

        return results;
    }
}
