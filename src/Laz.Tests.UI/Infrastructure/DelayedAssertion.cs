// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz.Tests.UI.Infrastructure;

/// <summary>
/// Provides utilities for assertions that need to wait for asynchronous UI side effects.
/// </summary>
/// <remarks>
/// <para>
/// In UI testing, operations are inherently asynchronous - a mouse click, keyboard input,
/// or property change may not immediately reflect in the UI state. This class provides
/// a polling mechanism that repeatedly checks an assertion until it passes or times out.
/// </para>
/// <para>
/// Using a fixed <c>Task.Delay</c> is problematic because:
/// </para>
/// <list type="bullet">
///   <item>Too short: The test may fail intermittently when the system is slow.</item>
///   <item>Too long: Tests become unnecessarily slow when the system is fast.</item>
/// </list>
/// <para>
/// <c>DelayedAssertion</c> solves this by polling: it checks the condition frequently
/// and succeeds as soon as the assertion passes, with a reasonable timeout as a safety net.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Instead of:
/// await Task.Delay(500);
/// Assert.Equal("Hello", textBox.Text);
///
/// // Use:
/// await DelayedAssertion.Eventually(
///     () => Assert.Equal("Hello", textBox.Text),
///     "Text box should display 'Hello' after typing");
/// </code>
/// </example>
public static class DelayedAssertion
{
    /// <summary>
    /// Default maximum time to wait for an assertion to pass.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Default interval between assertion checks.
    /// </summary>
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Repeatedly evaluates an assertion action until it passes or the timeout expires.
    /// </summary>
    /// <param name="expectation">
    ///     A human-readable description of what the assertions are checking.
    /// </param>
    /// <param name="assertion">
    ///     An action containing one or more xUnit assertions.
    ///     The assertion is considered passed if it completes without throwing.
    /// </param>
    /// <param name="timeout">
    ///     Optional. Maximum time to wait. Defaults to <see cref="DefaultTimeout"/> (5 seconds).
    /// </param>
    /// <param name="pollInterval">
    ///     Optional. How often to check. Defaults to <see cref="DefaultPollInterval"/> (500ms).
    /// </param>
    /// <returns>A task that completes when assertions pass or fails on timeout.</returns>
    /// <remarks>
    /// This overload is useful when you need to make multiple assertions or use
    /// assertion methods like <c>Assert.Equal</c> that provide better error messages.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Wait for specific color values
    /// await DelayedAssertion.Eventually(
    ///     () =>
    ///     {
    ///         var color = screen.GetColorAt(point);
    ///         Assert.True(color.R > 200, $"Red should be high, got {color.R}");
    ///         Assert.True(color.G &lt; 50, $"Green should be low, got {color.G}");
    ///     },
    ///     "Pixel should turn red after button click");
    /// </code>
    /// </example>
    public static async Task Eventually(string expectation,
        Action assertion,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        timeout ??= DefaultTimeout;
        pollInterval ??= DefaultPollInterval;

        var deadline = DateTime.UtcNow + timeout.Value;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                assertion();
                return; // All assertions passed
            }
            catch (Xunit.Sdk.XunitException)
            {
            }

            await Task.Delay(pollInterval.Value);
        }

        try
        {
            assertion();
        }
        catch (Exception ex)
        {
            throw new TimeoutException(
                $"Timed out waiting for: {expectation}\n\nLast assertion failure:\n{ex.Message}",
                ex);
        }
    }
}
