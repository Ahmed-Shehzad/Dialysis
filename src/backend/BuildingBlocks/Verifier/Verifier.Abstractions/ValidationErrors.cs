using System.Collections;
using System.Collections.ObjectModel;

namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Immutable ordered list of validation failures.
/// </summary>
public sealed class ValidationErrors : IReadOnlyList<ValidationFailure>
{
    private readonly ReadOnlyCollection<ValidationFailure> _failures;

    private ValidationErrors(ReadOnlyCollection<ValidationFailure> failures) => _failures = failures;

    public static ValidationErrors From(IEnumerable<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        var list = failures.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one failure is required.", nameof(failures));

        return new ValidationErrors(new ReadOnlyCollection<ValidationFailure>(list));
    }

    public int Count => _failures.Count;

    public ValidationFailure this[int index] => _failures[index];

    public IEnumerator<ValidationFailure> GetEnumerator() => _failures.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Dictionary<string, string[]> ToDictionary() =>
        _failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

    public override string ToString() =>
        string.Join(Environment.NewLine, _failures.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}
