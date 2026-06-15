// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;
using Laz.Tests.UI.Infrastructure;
using Xunit.Runner.InProc.SystemConsole;

namespace Laz.Tests.UI;

/// <summary>
/// Custom test runner that initializes Avalonia on the main thread.
/// Required for macOS where GUI operations must run on the first thread.
///
/// Usage: dotnet run --project src/Laz.Tests.UI [-- [options] [xunit-args]]
/// Options:
///   --filter <expression>   Run only tests matching the xUnit v3 filter query.
///                           The query must start with '/'. Use '=' for exact match, '~' for contains.
///                           Filterable properties: FullyQualifiedName, ClassName, MethodName.
///                           Traits: /trait:Category=Smoke
///
/// Examples:
///   dotnet run --project src/Laz.Tests.UI
///   dotnet run --project src/Laz.Tests.UI -- --filter "/FullyQualifiedName~MouseTests"
///   dotnet run --project src/Laz.Tests.UI -- --filter "/ClassName=Laz.Tests.UI.KeyboardTests"
///   dotnet run --project src/Laz.Tests.UI -- --filter "/FullyQualifiedName=Laz.Tests.UI.MouseTests.ReturnsCorrectPosition"
///   dotnet run --project src/Laz.Tests.UI -- -methodDisplay method
///   dotnet run --project src/Laz.Tests.UI -- -method "Laz.Tests.UI.MouseTests.ReturnsCorrectPosition"
/// </summary>
public static class Program
{
    private static int _testResult;

    public static int Main(string[] args)
    {
        var xunitArgs = PreprocessArgs(args);

        AvaloniaTestFixture.EnsureInitialized();

        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                _testResult = await Task.Run(() => RunTests(xunitArgs));
            }
            finally
            {
                AvaloniaTestFixture.StopMainLoop();
            }
        });

        AvaloniaTestFixture.RunMainLoop();

        AvaloniaTestFixture.Shutdown();

        return _testResult;
    }

    /// <summary>
    /// Converts <c>--filter &lt;expression&gt;</c> to xUnit's <c>-filter &lt;expression&gt;</c>
    /// and passes all other arguments through unchanged.
    /// </summary>
    private static string[] PreprocessArgs(string[] args)
    {
        var result = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--filter" && i + 1 < args.Length)
            {
                result.Add("-filter");
                result.Add(args[++i]);
            }
            else
            {
                result.Add(args[i]);
            }
        }
        return result.ToArray();
    }

    private static int RunTests(string[] args)
    {
        return ConsoleRunner.Run(args).GetAwaiter().GetResult();
    }
}
