// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit;

namespace Laz.Tests;

public class TreeSearchTests
{
    /// <summary>
    /// A node of a fake tree. Each fetch creates new node instances, like the native API does.
    /// </summary>
    private sealed class Node : IDisposable
    {
        private readonly Tracker _tracker;

        public Node(string name, Tracker tracker, params Func<Node>[] children)
        {
            Name = name;
            _tracker = tracker;
            Children = children;
            tracker.Created.Add(this);
        }

        public string Name { get; }
        public Func<Node>[] Children { get; }
        public bool Disposed { get; private set; }

        public List<Node> Fetch() => Children.Select(c => c()).ToList();

        public void Dispose()
        {
            Assert.False(Disposed, $"{Name} disposed twice");
            Disposed = true;
        }
    }

    private sealed class Tracker
    {
        public List<Node> Created { get; } = new();
    }

    //        root
    //       /    \
    //      a      b
    //     / \      \
    //    a1  a2     b1
    //               |
    //              b1x
    private static Node BuildTree(Tracker t)
    {
        return new Node("root", t,
            () => new Node("a", t,
                () => new Node("a1", t),
                () => new Node("a2", t)),
            () => new Node("b", t,
                () => new Node("b1", t,
                    () => new Node("b1x", t))));
    }

    private static FindOptions Unbounded => new() { MaxDepth = 100, MaxElements = 1000, Timeout = TimeSpan.FromMinutes(1) };

    [Fact]
    public void FindsInBreadthFirstOrder()
    {
        var tracker = new Tracker();

        var results = TreeSearch.Find(BuildTree(tracker), n => n.Fetch(), _ => true, Unbounded, int.MaxValue);

        Assert.Equal(["a", "b", "a1", "a2", "b1", "b1x"], results.Select(n => n.Name));
    }

    [Fact]
    public void DoesNotMatchRoot()
    {
        var tracker = new Tracker();

        var results = TreeSearch.Find(BuildTree(tracker), n => n.Fetch(), n => n.Name == "root", Unbounded, int.MaxValue);

        Assert.Empty(results);
    }

    [Fact]
    public void ReturnsMatchesUndisposedAndDisposesEverythingElse()
    {
        var tracker = new Tracker();

        var results = TreeSearch.Find(BuildTree(tracker), n => n.Fetch(), n => n.Name == "a2", Unbounded, 1);

        var match = Assert.Single(results);
        Assert.Equal("a2", match.Name);
        Assert.False(match.Disposed);
        Assert.All(tracker.Created.Where(n => n != match), n => Assert.True(n.Disposed, $"{n.Name} leaked"));
    }

    [Fact]
    public void StopsAtMaxResults()
    {
        var tracker = new Tracker();

        var results = TreeSearch.Find(BuildTree(tracker), n => n.Fetch(), _ => true, Unbounded, 2);

        Assert.Equal(["a", "b"], results.Select(n => n.Name));
        Assert.All(tracker.Created.Except(results), n => Assert.True(n.Disposed, $"{n.Name} leaked"));
    }

    [Fact]
    public void RespectsMaxDepth()
    {
        var tracker = new Tracker();
        var options = new FindOptions { MaxDepth = 1, MaxElements = 1000, Timeout = TimeSpan.FromMinutes(1) };

        var results = TreeSearch.Find(BuildTree(tracker), n => n.Fetch(), _ => true, options, int.MaxValue);

        Assert.Equal(["a", "b"], results.Select(n => n.Name));
    }

    [Fact]
    public void RespectsMaxElements()
    {
        var tracker = new Tracker();
        var options = new FindOptions { MaxDepth = 100, MaxElements = 3, Timeout = TimeSpan.FromMinutes(1) };

        var results = TreeSearch.Find(BuildTree(tracker), n => n.Fetch(), _ => true, options, int.MaxValue);

        Assert.Equal(3, results.Count);
        Assert.All(tracker.Created.Except(results), n => Assert.True(n.Disposed, $"{n.Name} leaked"));
    }

    [Fact]
    public void StopsWhenTimeIsUp()
    {
        var tracker = new Tracker();
        var options = new FindOptions { MaxDepth = 100, MaxElements = 1000, Timeout = TimeSpan.Zero };

        var results = TreeSearch.Find(BuildTree(tracker), n => n.Fetch(), _ => true, options, int.MaxValue);

        Assert.Empty(results);
        Assert.All(tracker.Created, n => Assert.True(n.Disposed));
    }

    [Fact]
    public void SkipsSubtreesThatDisappear()
    {
        var tracker = new Tracker();

        var results = TreeSearch.Find(
            BuildTree(tracker),
            n => n.Name == "a" ? throw new ElementNotAvailableException() : n.Fetch(),
            _ => true,
            Unbounded,
            int.MaxValue);

        Assert.Equal(["a", "b", "b1", "b1x"], results.Select(n => n.Name));
    }

    [Fact]
    public void PropagatesPredicateExceptionsAndDisposesEverything()
    {
        var tracker = new Tracker();

        Assert.Throws<InvalidOperationException>(() => TreeSearch.Find(
            BuildTree(tracker),
            n => n.Fetch(),
            n => n.Name == "a1" ? throw new InvalidOperationException() : true,
            Unbounded,
            int.MaxValue));

        Assert.All(tracker.Created, n => Assert.True(n.Disposed, $"{n.Name} leaked"));
    }
}
