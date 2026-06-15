// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using Xunit;
using Xunit.v3;

namespace Laz.Tests.UI.Infrastructure;

/// <summary>
/// Skips the test on the specified platforms. Use with [Fact] or [Theory].
/// </summary>
/// <example>
/// [Fact, SkipOn(Platform.LinuxX11, "Numpad not supported in Xvfb")]
/// [Theory, SkipOn(Platform.MacOS, "macOS uses Command instead of Control")]
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipOnAttribute(Platform platforms, string reason) : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (platforms.IncludesCurrent())
        {
            Assert.Skip(reason);
        }
    }
}
