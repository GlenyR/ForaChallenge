namespace ForaChallenge.Domain.ValueObjects;

public readonly record struct FundableAmount(Money Standard, Money Special)
{
    public static FundableAmount Zero => new(Money.Zero, Money.Zero);

    public static FundableAmount From(decimal standard, decimal special) =>
        new(Money.From(Math.Max(0, standard)), Money.From(Math.Max(0, special)));
}
