using Ebony.Core.Extraction;
using Ebony.Core.Library;

namespace Ebony.Infrastructure.Inspection;

public class Diagnostic(Severity level, string message, string? details)
{
    public Severity Level { get; } = level;
    public string Message { get; } = message;
    public string? Details { get; } = details;
}

public sealed record Inspected<T>(T Info, IReadOnlyList<Diagnostic> Diagnostics) where T :Info;