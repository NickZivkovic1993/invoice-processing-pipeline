namespace LedgerFlow.Core.Domain;

/// <summary>An amount in a specific ISO-4217 currency. Arithmetic across currencies is rejected.</summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money value, decimal factor) =>
        new(value.Amount * factor, value.Currency);

    /// <summary>Absolute difference between two amounts of the same currency.</summary>
    public static Money AbsoluteDifference(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(Math.Abs(left.Amount - right.Amount), left.Currency);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in {left.Currency} and {right.Currency}.");
        }
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
