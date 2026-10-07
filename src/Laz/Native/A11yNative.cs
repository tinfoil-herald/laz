// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Laz.Native;

/// <summary>
/// P/Invoke declarations for the accessibility API in <c>laz_a11y.h</c>.
/// The ABI is identical on all platforms, so it lives outside of <see cref="NativeLazbot"/>.
/// </summary>
internal static class A11yNative
{
    private const string LibraryName = "laz_native";

    internal const int Ok = 0;
    internal const int InvalidArg = -1;
    internal const int NotAvailable = -2;
    internal const int AccessDenied = -3;
    internal const int ElementGone = -4;
    internal const int TimedOut = -5;
    internal const int NotFound = -6;
    internal const int OutOfMemory = -7;
    internal const int InternalError = -8;

    /// <summary>
    /// Mirrors <c>LazA11yInfo</c>. String fields point to UTF-8 memory owned by the element handle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LazA11yInfo
    {
        public int StructSize;
        public int RawRoleId;
        public IntPtr RawRole;
        public IntPtr RawSubrole;
        public IntPtr Name;
        public IntPtr Value;
        public IntPtr Description;
        public IntPtr AutomationId;
        public IntPtr ClassName;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int BoundsKind;
        public uint States;
        public int ProcessId;
        public int ChildCountHint;
    }

    /// <summary>
    /// Throws the exception that corresponds to a native result code.
    /// </summary>
    internal static void Check(int result, string operation)
    {
        if (result == Ok)
            return;

        throw result switch
        {
            InvalidArg => new ArgumentException($"{operation}: invalid argument."),
            NotAvailable => new PlatformNotSupportedException(
                $"{operation}: the accessibility service is not available on this system."),
            AccessDenied => new UnauthorizedAccessException(
                $"{operation}: the process is not trusted for accessibility. " +
                "Grant the Accessibility permission in System Settings."),
            ElementGone => new ElementNotAvailableException(
                $"{operation}: the element is no longer available."),
            TimedOut => new TimeoutException($"{operation}: the accessibility request timed out."),
            OutOfMemory => new InvalidOperationException($"{operation}: the native library ran out of memory."),
            _ => new InvalidOperationException($"{operation} failed with code {result}."),
        };
    }

    #region P/Invoke Declarations

    [DllImport(LibraryName, EntryPoint = "lazA11yIsAvailable")]
    internal static extern int IsAvailable([MarshalAs(UnmanagedType.I1)] bool prompt);

    [DllImport(LibraryName, EntryPoint = "lazA11ySetTimeout")]
    internal static extern int SetTimeout(int milliseconds);

    [DllImport(LibraryName, EntryPoint = "lazA11yGetRoot")]
    internal static extern int GetRoot(out A11yElementHandle element);

    [DllImport(LibraryName, EntryPoint = "lazA11yGetFocused")]
    internal static extern int GetFocused(out A11yElementHandle element);

    [DllImport(LibraryName, EntryPoint = "lazA11yElementFromPoint")]
    internal static extern int ElementFromPoint(int x, int y, out A11yElementHandle element);

    [DllImport(LibraryName, EntryPoint = "lazA11yGetParent")]
    internal static extern int GetParent(A11yElementHandle element, out A11yElementHandle parent);

    [DllImport(LibraryName, EntryPoint = "lazA11yGetChildren")]
    internal static extern int GetChildren(A11yElementHandle element, out IntPtr array, out int count);

    [DllImport(LibraryName, EntryPoint = "lazA11yFreeElementArray")]
    internal static extern void FreeElementArray(IntPtr array);

    [DllImport(LibraryName, EntryPoint = "lazA11yGetInfo")]
    internal static extern int GetInfo(A11yElementHandle element, ref LazA11yInfo info);

    [DllImport(LibraryName, EntryPoint = "lazA11yRefresh")]
    internal static extern int Refresh(A11yElementHandle element);

    [DllImport(LibraryName, EntryPoint = "lazA11yIsSameElement")]
    internal static extern int IsSameElement(
        A11yElementHandle a,
        A11yElementHandle b,
        [MarshalAs(UnmanagedType.I1)] out bool same);

    [DllImport(LibraryName, EntryPoint = "lazA11yCloneHandle")]
    internal static extern int CloneHandle(A11yElementHandle element, out A11yElementHandle clone);

    [DllImport(LibraryName, EntryPoint = "lazA11yRelease")]
    internal static extern void Release(IntPtr element);

    #endregion
}

/// <summary>
/// Owns a native <c>LazA11yElement*</c> and releases it when disposed or finalized.
/// The native release function is thread-agnostic, so finalization is safe.
/// </summary>
internal sealed class A11yElementHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public A11yElementHandle()
        : base(ownsHandle: true)
    {
    }

    internal A11yElementHandle(IntPtr handle)
        : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        A11yNative.Release(handle);
        return true;
    }
}
