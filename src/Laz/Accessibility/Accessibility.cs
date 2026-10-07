// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Laz.Native;

namespace Laz;

/// <summary>
/// Read-only access to the accessibility tree: windows, controls, their names, values, states, and bounds.
/// </summary>
///
/// <remarks>
/// <para>
/// Every element returned by this class is owned by the caller and must be disposed.
/// </para>
///
/// <para>
/// On Windows, Laz uses UI Automation. Bounds are in the coordinate space of the DPI awareness of the calling
/// thread, the same space that <see cref="Mouse"/> uses.
/// </para>
///
/// <para>
/// On macOS, Laz uses the AXUIElement API, which requires the Accessibility permission, the same permission that
/// <see cref="Mouse"/> and <see cref="Keyboard"/> need. Without it, the methods throw
/// <see cref="UnauthorizedAccessException"/>. Bounds are in logical points with the origin at the top-left corner
/// of the main display.
/// </para>
///
/// <para>
/// On Linux, Laz uses AT-SPI2 over D-Bus, which requires a running accessibility bus. In X11 sessions, bounds are
/// in root window coordinates. In Wayland sessions, applications do not know their position on the screen, so
/// bounds are relative to the window (see <see cref="BoundsKind.WindowRelative"/>) and
/// <see cref="ElementFromPoint"/> is not supported.
/// </para>
///
/// <para>
/// Some toolkits build their accessibility tree only when they detect an assistive technology.
/// Chromium and Electron need the <c>--force-renderer-accessibility</c> switch, Qt on Linux needs
/// <c>QT_LINUX_ACCESSIBILITY_ALWAYS_ON=1</c>, and Java needs the Java Access Bridge on Windows.
/// Without them, such applications show up as a window with no children.
/// </para>
///
/// <para>
/// Do not call these methods from the UI thread of a window that you inspect. The accessibility service sends
/// requests back to that thread, so the call blocks until it times out.
/// </para>
/// </remarks>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API reserved for potential future stateful extensions.")]
public class Accessibility
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    private TimeSpan _timeout = TimeSpan.FromSeconds(5);

    internal Accessibility()
    {
    }

    /// <summary>
    /// The timeout of a single request to the accessibility service. Defaults to 5 seconds.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not positive or exceeds <see cref="int.MaxValue"/> milliseconds.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    public TimeSpan Timeout
    {
        get => _timeout;
        set
        {
            if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Timeout must be positive.");
            A11yNative.Check(A11yNative.SetTimeout((int)value.TotalMilliseconds), "Set timeout");
            _timeout = value;
        }
    }

    /// <summary>
    /// Checks whether the accessibility tree can be read.
    /// </summary>
    /// <param name="prompt">
    /// On macOS, when <c>true</c> and the process lacks the Accessibility permission, shows the system prompt that
    /// asks the user to grant it. Ignored on other platforms.
    /// </param>
    /// <returns><c>true</c> if the other methods of this class can be used.</returns>
    public bool IsAvailable(bool prompt = false)
    {
        return A11yNative.IsAvailable(prompt) == A11yNative.Ok;
    }

    /// <summary>
    /// Returns the root of the accessibility tree.
    /// </summary>
    /// <remarks>
    /// On Windows, the children of the root are the top-level windows. On macOS and Linux, they are applications,
    /// and windows are the children of applications.
    /// </remarks>
    /// <returns>The root element. The caller must dispose it.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public AccessibleElement GetDesktop()
    {
        var result = A11yNative.GetRoot(out var handle);
        return AccessibleElement.FromResult(result, handle, "Get desktop");
    }

    /// <summary>
    /// Returns the top-level windows and dialogs of all applications.
    /// </summary>
    /// <returns>The windows. The caller must dispose each of them.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public IReadOnlyList<AccessibleElement> GetWindows()
    {
        var windows = new List<AccessibleElement>();
        using var desktop = GetDesktop();
        try
        {
            foreach (var child in desktop.FetchChildren())
            {
                using (child)
                {
                    if (IsWindow(child))
                    {
                        windows.Add(child.Clone());
                    }
                    else if (child.Role == AccessibleRole.Application)
                    {
                        IReadOnlyList<AccessibleElement> appChildren;
                        try
                        {
                            appChildren = child.FetchChildren();
                        }
                        catch (ElementNotAvailableException)
                        {
                            continue;
                        }

                        foreach (var appChild in appChildren)
                        {
                            if (IsWindow(appChild))
                                windows.Add(appChild);
                            else
                                appChild.Dispose();
                        }
                    }
                }
            }
        }
        catch
        {
            foreach (var window in windows)
                window.Dispose();
            throw;
        }
        return windows;
    }

    /// <summary>
    /// Returns the element that has the keyboard focus.
    /// </summary>
    /// <returns>The focused element, or <c>null</c> if no element has the focus. The caller must dispose it.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public AccessibleElement? GetFocusedElement()
    {
        var result = A11yNative.GetFocused(out var handle);
        return AccessibleElement.FromResultOrNull(result, handle, "Get focused element");
    }

    /// <summary>
    /// Returns the deepest element at the given point on the screen.
    /// </summary>
    /// <param name="point">The point, in the coordinate space of <see cref="Mouse"/>.</param>
    /// <returns>The element, or <c>null</c> if there is none. The caller must dispose it.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown if the accessibility service is not available, including in Wayland sessions on Linux.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public AccessibleElement? ElementFromPoint(Point point)
    {
        var result = A11yNative.ElementFromPoint(point.X, point.Y, out var handle);
        return AccessibleElement.FromResultOrNull(result, handle, "Get element from point");
    }

    /// <summary>
    /// Finds the first descendant with the given role and, optionally, name.
    /// </summary>
    /// <param name="role">The role to look for.</param>
    /// <param name="name">The exact name to look for, or <c>null</c> to match any name.</param>
    /// <param name="root">The element to search under. Defaults to the desktop. The root itself is not matched.</param>
    /// <param name="options">Limits for the search. Defaults to <see cref="FindOptions.Default"/>.</param>
    /// <returns>
    /// The first match in breadth-first order, or <c>null</c> if there is none within the limits.
    /// The caller must dispose it.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public AccessibleElement? Find(AccessibleRole role, string? name = null, AccessibleElement? root = null, FindOptions? options = null)
    {
        return Find(e => e.Role == role && (name == null || e.Name == name), root, options);
    }

    /// <summary>
    /// Finds the first descendant that matches a predicate.
    /// </summary>
    /// <param name="predicate">The condition to match.</param>
    /// <param name="root">The element to search under. Defaults to the desktop. The root itself is not matched.</param>
    /// <param name="options">Limits for the search. Defaults to <see cref="FindOptions.Default"/>.</param>
    /// <returns>
    /// The first match in breadth-first order, or <c>null</c> if there is none within the limits.
    /// The caller must dispose it.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public AccessibleElement? Find(Func<AccessibleElement, bool> predicate, AccessibleElement? root = null, FindOptions? options = null)
    {
        var results = Search(predicate, root, options, maxResults: 1);
        return results.Count > 0 ? results[0] : null;
    }

    /// <summary>
    /// Finds all descendants that match a predicate.
    /// </summary>
    /// <param name="predicate">The condition to match.</param>
    /// <param name="root">The element to search under. Defaults to the desktop. The root itself is not matched.</param>
    /// <param name="options">Limits for the search. Defaults to <see cref="FindOptions.Default"/>.</param>
    /// <returns>The matches in breadth-first order. The caller must dispose each of them.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public IReadOnlyList<AccessibleElement> FindAll(Func<AccessibleElement, bool> predicate, AccessibleElement? root = null, FindOptions? options = null)
    {
        return Search(predicate, root, options, maxResults: int.MaxValue);
    }

    /// <summary>
    /// Waits until a descendant that matches a predicate appears.
    /// </summary>
    /// <param name="predicate">The condition to match.</param>
    /// <param name="timeout">How long to wait.</param>
    /// <param name="root">The element to search under. Defaults to the desktop. The root itself is not matched.</param>
    /// <param name="options">Limits for each search attempt. Defaults to <see cref="FindOptions.Default"/>.</param>
    /// <returns>The first match. The caller must dispose it.</returns>
    /// <exception cref="TimeoutException">Thrown if no match appears in time.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the accessibility service is not available.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown on macOS if the Accessibility permission is not granted.</exception>
    public AccessibleElement WaitFor(Func<AccessibleElement, bool> predicate, TimeSpan timeout, AccessibleElement? root = null, FindOptions? options = null)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            var match = Find(predicate, root, options);
            if (match != null)
                return match;
            if (stopwatch.Elapsed >= timeout)
                throw new TimeoutException($"No matching element appeared within {timeout}.");
            Thread.Sleep(PollInterval);
        }
    }

    private List<AccessibleElement> Search(Func<AccessibleElement, bool> predicate, AccessibleElement? root, FindOptions? options, int maxResults)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        // The search disposes every node it visits, so it gets its own handle to the root.
        var start = root != null ? root.Clone() : GetDesktop();
        return TreeSearch.Find(start, e => e.FetchChildren(), predicate, options ?? FindOptions.Default, maxResults);
    }

    private static bool IsWindow(AccessibleElement element)
    {
        return element.Role is AccessibleRole.Window or AccessibleRole.Dialog;
    }
}
