using System.Collections.Generic;

namespace Aura.Core.Validation;

/// <summary>
/// Result of a validation operation
/// </summary>
public record ValidationResult(bool IsValid, List<string> Issues);
