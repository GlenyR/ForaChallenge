namespace ForaChallenge.Domain.ValueObjects;

public readonly record struct Money(decimal Amount)
{
    private const int DecimalPlaces = 2;

    public decimal Value => Math.Round(Amount, DecimalPlaces);

    public static Money Zero => new(0);

    public static Money From(decimal amount) => new(amount);

    public override string ToString() => Value.ToString("F2");
}
