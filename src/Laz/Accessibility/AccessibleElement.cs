// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Laz.Native;

namespace Laz;

/// <summary>
/// An element of the accessibility tree: a window, a control, or a piece of text.
/// </summary>
///
/// <remarks>
/// <para>
/// Properties are a snapshot taken when the element was obtained. Call <see cref="Refresh"/> to read them again.
/// </para>
///
/// <para>
/// The element holds a native resource. Dispose it when it is no longer needed. Disposing an element also disposes
/// the <see cref="Children"/> and <see cref="Parent"/> elements it has loaded.
/// </para>
///
/// <para>
/// An element is safe to use from multiple threads.
/// </para>
/// </remarks>
public sealed class AccessibleElement : IDisposable
{
    private readonly A11yElementHandle _handle;
    private readonly object _lock = new();
    private IReadOnlyList<AccessibleElement>? _children;
    private AccessibleElement? _parent;
    private bool _parentLoaded;
    private bool _disposed;
    private ElementInfo _info;

    private AccessibleElement(A11yElementHandle handle, ElementInfo info)
    {
        _handle = handle;
        _info = info;
    }

    /// <summary>The normalized role of the element.</summary>
    public AccessibleRole Role => _info.Role;

    /// <summary>
    /// The platform role: the localized control type on Windows, <c>AXRole</c> on macOS,
    /// or the AT-SPI role name on Linux.
    /// </summary>
    public string? RawRole => _info.RawRole;

    /// <summary>The <c>AXSubrole</c> on macOS; <c>null</c> on other platforms.</summary>
    public string? RawSubrole => _info.RawSubrole;

    /// <summary>
    /// The numeric platform role: the UI Automation control type ID on Windows, the <c>AtspiRole</c> value on Linux,
    /// and -1 on macOS.
    /// </summary>
    public int RawRoleId => _info.RawRoleId;

    /// <summary>The accessible name, such as the label of a button or the title of a window.</summary>
    public string? Name => _info.Name;

    /// <summary>The value, such as the text of a text field or the position of a slider.</summary>
    public string? Value => _info.Value;

    /// <summary>The description or help text.</summary>
    public string? Description => _info.Description;

    /// <summary>
    /// The identifier assigned by the developer: <c>AutomationId</c> on Windows, <c>AXIdentifier</c> on macOS,
    /// or the accessible ID on Linux.
    /// </summary>
    public string? AutomationId => _info.AutomationId;

    /// <summary>The class name of the underlying control on Windows; <c>null</c> on other platforms.</summary>
    public string? ClassName => _info.ClassName;

    /// <summary>
    /// The bounding rectangle. See <see cref="BoundsKind"/> for the coordinate space.
    /// </summary>
    ///
    /// <remarks>
    /// When <see cref="BoundsKind"/> is <see cref="Laz.BoundsKind.Screen"/>, the bounds use the coordinate space of
    /// <see cref="Mouse"/>, so <c>Mouse.JumpTo(element.Bounds.Center)</c> points at the element.
    /// On Windows, this space depends on the DPI awareness of the calling thread.
    /// </remarks>
    public Rectangle Bounds => _info.Bounds;

    /// <summary>What <see cref="Bounds"/> is relative to.</summary>
    public BoundsKind BoundsKind => _info.BoundsKind;

    /// <summary>The states of the element.</summary>
    public AccessibleStates States => _info.States;

    /// <summary>Whether the element accepts input.</summary>
    public bool IsEnabled => (_info.States & AccessibleStates.Enabled) != 0;

    /// <summary>Whether the element has the keyboard focus.</summary>
    public bool IsFocused => (_info.States & AccessibleStates.Focused) != 0;

    /// <summary>Whether the element is outside of the visible area.</summary>
    public bool IsOffscreen => (_info.States & AccessibleStates.Offscreen) != 0;

    /// <summary>The ID of the process that owns the element, or 0 if unknown.</summary>
    public int ProcessId => _info.ProcessId;

    /// <summary>
    /// The children of the element. Loaded on first access and owned by this element.
    /// </summary>
    /// <exception cref="ElementNotAvailableException">Thrown if the element no longer exists.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the element is disposed.</exception>
    public IReadOnlyList<AccessibleElement> Children
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _children ??= FetchChildren();
            }
        }
    }

    /// <summary>
    /// The parent of the element, or <c>null</c> for the root. Loaded on first access and owned by this element.
    /// </summary>
    /// <exception cref="ElementNotAvailableException">Thrown if the element no longer exists.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the element is disposed.</exception>
    public AccessibleElement? Parent
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                if (!_parentLoaded)
                {
                    var result = A11yNative.GetParent(_handle, out var parentHandle);
                    _parent = FromResultOrNull(result, parentHandle, "Get parent");
                    _parentLoaded = true;
                }
                return _parent;
            }
        }
    }

    /// <summary>
    /// Reads the properties of the element again. Children and parent loaded earlier are disposed and loaded
    /// again on next access.
    /// </summary>
    /// <exception cref="ElementNotAvailableException">Thrown if the element no longer exists.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the element is disposed.</exception>
    public void Refresh()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            A11yNative.Check(A11yNative.Refresh(_handle), "Refresh");
            _info = ReadInfo(_handle);
            DisposeRelatives();
        }
    }

    /// <summary>
    /// Checks whether this and another object refer to the same element of the user interface.
    /// </summary>
    /// <param name="other">The element to compare with.</param>
    /// <returns><c>true</c> if both refer to the same element.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if either element is disposed.</exception>
    public bool IsSameAs(AccessibleElement other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
        if (ReferenceEquals(this, other))
            return true;

        lock (_lock)
        {
            ThrowIfDisposed();
            other.ThrowIfDisposed();
            A11yNative.Check(A11yNative.IsSameElement(_handle, other._handle, out var same), "Compare elements");
            return same;
        }
    }

    /// <summary>
    /// Returns a short description of the element, such as <c>Button "OK" [10, 20, 80x24]</c>.
    /// </summary>
    public override string ToString()
    {
        var bounds = BoundsKind == BoundsKind.None ? "no bounds" : $"{Bounds.X}, {Bounds.Y}, {Bounds.Width}x{Bounds.Height}";
        return Name == null ? $"{Role} [{bounds}]" : $"{Role} \"{Name}\" [{bounds}]";
    }

    /// <summary>
    /// Releases the native resource and the children and parent loaded by this element.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;
            _disposed = true;
            DisposeRelatives();
            _handle.Dispose();
        }
    }

    /// <summary>
    /// Creates an element that owns an independent handle to the same user interface element.
    /// </summary>
    internal AccessibleElement Clone()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            var result = A11yNative.CloneHandle(_handle, out var clone);
            return FromResult(result, clone, "Clone element");
        }
    }

    /// <summary>
    /// Loads the children as new elements owned by the caller. Does not touch <see cref="Children"/>.
    /// </summary>
    internal IReadOnlyList<AccessibleElement> FetchChildren()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            var result = A11yNative.GetChildren(_handle, out var array, out var count);
            A11yNative.Check(result, "Get children");
            if (array == IntPtr.Zero || count == 0)
                return Array.Empty<AccessibleElement>();

            // Take ownership of every handle first, so none leaks if wrapping fails.
            var handles = new A11yElementHandle[count];
            for (var i = 0; i < count; i++)
                handles[i] = new A11yElementHandle(Marshal.ReadIntPtr(array, i * IntPtr.Size));
            A11yNative.FreeElementArray(array);

            var children = new AccessibleElement[count];
            try
            {
                for (var i = 0; i < count; i++)
                    children[i] = Wrap(handles[i]);
            }
            catch
            {
                foreach (var handle in handles)
                    handle.Dispose();
                throw;
            }
            return children;
        }
    }

    /// <summary>
    /// Wraps a handle returned by a native call, or returns <c>null</c> when the call reported
    /// <see cref="A11yNative.NotFound"/>. Throws for other errors.
    /// </summary>
    internal static AccessibleElement? FromResultOrNull(int result, A11yElementHandle handle, string operation)
    {
        if (result == A11yNative.NotFound)
        {
            handle.Dispose();
            return null;
        }
        return FromResult(result, handle, operation);
    }

    internal static AccessibleElement FromResult(int result, A11yElementHandle handle, string operation)
    {
        if (result != A11yNative.Ok || handle.IsInvalid)
        {
            handle.Dispose();
            A11yNative.Check(result == A11yNative.Ok ? A11yNative.InternalError : result, operation);
        }
        return Wrap(handle);
    }

    private static AccessibleElement Wrap(A11yElementHandle handle)
    {
        return new AccessibleElement(handle, ReadInfo(handle));
    }

    private static ElementInfo ReadInfo(A11yElementHandle handle)
    {
        var native = new A11yNative.LazA11yInfo { StructSize = Marshal.SizeOf<A11yNative.LazA11yInfo>() };
        A11yNative.Check(A11yNative.GetInfo(handle, ref native), "Get element info");
        return ElementInfo.FromNative(native, RoleMap.CurrentPlatform);
    }

    private void DisposeRelatives()
    {
        if (_children != null)
        {
            foreach (var child in _children)
                child.Dispose();
            _children = null;
        }
        _parent?.Dispose();
        _parent = null;
        _parentLoaded = false;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AccessibleElement));
    }
}

/// <summary>
/// A managed copy of <see cref="A11yNative.LazA11yInfo"/>.
/// </summary>
internal readonly record struct ElementInfo(
    AccessibleRole Role,
    string? RawRole,
    string? RawSubrole,
    int RawRoleId,
    string? Name,
    string? Value,
    string? Description,
    string? AutomationId,
    string? ClassName,
    Rectangle Bounds,
    BoundsKind BoundsKind,
    AccessibleStates States,
    int ProcessId)
{
    internal static ElementInfo FromNative(in A11yNative.LazA11yInfo info, A11yPlatform platform)
    {
        var rawRole = Marshal.PtrToStringUTF8(info.RawRole);
        var rawSubrole = Marshal.PtrToStringUTF8(info.RawSubrole);
        var className = Marshal.PtrToStringUTF8(info.ClassName);
        var states = (AccessibleStates)info.States;
        var boundsKind = Enum.IsDefined(typeof(BoundsKind), info.BoundsKind) ? (BoundsKind)info.BoundsKind : BoundsKind.None;

        return new ElementInfo(
            RoleMap.Map(platform, info.RawRoleId, rawRole, rawSubrole, className, states),
            rawRole,
            rawSubrole,
            info.RawRoleId,
            Marshal.PtrToStringUTF8(info.Name),
            Marshal.PtrToStringUTF8(info.Value),
            Marshal.PtrToStringUTF8(info.Description),
            Marshal.PtrToStringUTF8(info.AutomationId),
            className,
            new Rectangle(info.X, info.Y, info.Width, info.Height),
            boundsKind,
            states,
            info.ProcessId);
    }
}
