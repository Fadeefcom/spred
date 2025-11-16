using AggregatorService.Abstractions;

namespace AggregatorService.Components;

/// <inheritdoc cref="IParserAccessGate"/>
public class ParserAccessGate : IParserAccessGate
{
    private int _invalidAttempts;
    private const int Limit = 5;

    public bool IsBlocked() => _invalidAttempts >= Limit;

    public void RegisterFailure() => _invalidAttempts++;

    public void Reset() => _invalidAttempts = 0;
}