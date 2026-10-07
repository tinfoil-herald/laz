// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Laz;

/// <summary>
/// The exception that is thrown when an accessible element no longer exists, for example,
/// because its window was closed.
/// </summary>
public class ElementNotAvailableException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the exception.</summary>
    public ElementNotAvailableException()
    {
    }

    /// <summary>Initializes a new instance of the exception with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ElementNotAvailableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the exception with a message and an inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public ElementNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
