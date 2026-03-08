namespace ForaChallenge.Domain.ValueObjects;

public readonly record struct Cik
{
    private const int RequiredLength = 10;
    private static readonly string DefaultValue = new('0', RequiredLength);

    private readonly string? _value;

    public string Value => _value ?? DefaultValue;

    private Cik(string value) => _value = value;

    public static Cik From(string? cik)
    {
        if (string.IsNullOrWhiteSpace(cik))
            return new Cik(new('0', RequiredLength));

        string digits = new([.. cik.Where(char.IsDigit)]);
        if (digits.Length == 0)
            return new Cik(new('0', RequiredLength));

        string normalized = digits.Length <= RequiredLength
            ? digits.PadLeft(RequiredLength, '0')
            : digits[^RequiredLength..];
        return new Cik(normalized);
    }

    public static Cik From(int cik)
    {
        if (cik < 0) cik = 0;
        return From(cik.ToString());
    }

    public override string ToString() => Value;
}
