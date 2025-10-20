using System;
using System.Collections.Generic;

namespace Aura.Core.Validation;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public List<string> Issues { get; }

    public ValidationException(string message, List<string> issues) : base(message)
    {
        Issues = issues;
    }
}
