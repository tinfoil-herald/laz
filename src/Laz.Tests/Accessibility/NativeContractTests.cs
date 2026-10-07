// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Laz.Native;
using Xunit;

namespace Laz.Tests;

/// <summary>
/// Checks that the managed mirror of <c>laz_a11y.h</c> matches the header.
/// </summary>
public class NativeContractTests
{
    private static string ReadHeader([CallerFilePath] string thisFile = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", ".."));
        return File.ReadAllText(Path.Combine(root, "native", "include", "laz_a11y.h"));
    }

    [Fact]
    public void StateFlagsMatchHeader()
    {
        var matches = Regex.Matches(ReadHeader(), @"#define LAZ_A11Y_STATE_(\w+) \(1u << (\d+)\)");

        Assert.NotEmpty(matches);
        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value.Replace("_", "");
            var managed = Enum.GetValues<AccessibleStates>()
                .Single(s => string.Equals(s.ToString(), name, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(1u << int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), (uint)managed);
        }
        // Every managed flag except None has a native counterpart.
        Assert.Equal(Enum.GetValues<AccessibleStates>().Length - 1, matches.Count);
    }

    [Fact]
    public void ResultCodesMatchHeader()
    {
        var header = ReadHeader();

        int Code(string name) => int.Parse(Regex.Match(header, $@"LAZ_A11Y_{name} = (-?\d+)").Groups[1].Value, CultureInfo.InvariantCulture);

        Assert.Equal(A11yNative.Ok, Code("OK"));
        Assert.Equal(A11yNative.InvalidArg, Code("E_INVALID_ARG"));
        Assert.Equal(A11yNative.NotAvailable, Code("E_NOT_AVAILABLE"));
        Assert.Equal(A11yNative.AccessDenied, Code("E_ACCESS_DENIED"));
        Assert.Equal(A11yNative.ElementGone, Code("E_ELEMENT_GONE"));
        Assert.Equal(A11yNative.TimedOut, Code("E_TIMEOUT"));
        Assert.Equal(A11yNative.NotFound, Code("E_NOT_FOUND"));
        Assert.Equal(A11yNative.OutOfMemory, Code("E_OUT_OF_MEMORY"));
        Assert.Equal(A11yNative.InternalError, Code("E_INTERNAL"));
    }

    [Fact]
    public void InfoStructHasNativeLayout()
    {
        // 2 ints, 7 pointers, 8 ints/uints.
        var expected = 2 * 4 + 7 * IntPtr.Size + 8 * 4;

        Assert.Equal(expected, Marshal.SizeOf<A11yNative.LazA11yInfo>());
        Assert.Equal(8, (int)Marshal.OffsetOf<A11yNative.LazA11yInfo>(nameof(A11yNative.LazA11yInfo.RawRole)));
    }

    [Fact]
    public void ErrorCodesMapToExceptions()
    {
        Assert.Throws<PlatformNotSupportedException>(() => A11yNative.Check(A11yNative.NotAvailable, "op"));
        Assert.Throws<UnauthorizedAccessException>(() => A11yNative.Check(A11yNative.AccessDenied, "op"));
        Assert.Throws<ElementNotAvailableException>(() => A11yNative.Check(A11yNative.ElementGone, "op"));
        Assert.Throws<TimeoutException>(() => A11yNative.Check(A11yNative.TimedOut, "op"));
        Assert.Throws<ArgumentException>(() => A11yNative.Check(A11yNative.InvalidArg, "op"));
        Assert.Throws<InvalidOperationException>(() => A11yNative.Check(A11yNative.InternalError, "op"));
        A11yNative.Check(A11yNative.Ok, "op");
    }

    [Fact]
    public void ConvertsNativeInfo()
    {
        var name = Marshal.StringToCoTaskMemUTF8("Grüße, 世界");
        var role = Marshal.StringToCoTaskMemUTF8("button");
        try
        {
            var native = new A11yNative.LazA11yInfo
            {
                RawRoleId = 50000,
                RawRole = role,
                Name = name,
                X = 10,
                Y = 20,
                Width = 30,
                Height = 40,
                BoundsKind = 1,
                States = 1u | (1u << 4),
                ProcessId = 42,
            };

            var info = ElementInfo.FromNative(native, A11yPlatform.Windows);

            Assert.Equal(AccessibleRole.Button, info.Role);
            Assert.Equal("button", info.RawRole);
            Assert.Equal("Grüße, 世界", info.Name);
            Assert.Null(info.Value);
            Assert.Null(info.RawSubrole);
            Assert.Equal(new Rectangle(10, 20, 30, 40), info.Bounds);
            Assert.Equal(BoundsKind.Screen, info.BoundsKind);
            Assert.Equal(AccessibleStates.Enabled | AccessibleStates.Checked, info.States);
            Assert.Equal(42, info.ProcessId);
        }
        finally
        {
            Marshal.FreeCoTaskMem(name);
            Marshal.FreeCoTaskMem(role);
        }
    }

    [Fact]
    public void UnknownBoundsKindBecomesNone()
    {
        var info = ElementInfo.FromNative(new A11yNative.LazA11yInfo { BoundsKind = 99 }, A11yPlatform.Linux);

        Assert.Equal(BoundsKind.None, info.BoundsKind);
    }
}
