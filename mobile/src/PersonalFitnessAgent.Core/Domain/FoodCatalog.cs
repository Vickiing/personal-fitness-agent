namespace PersonalFitnessAgent.Core.Domain;

public sealed record FoodCatalogItem(string Code, string Name, decimal EnergyKcalPer100Grams, string Source, int SourceVersion);

/// <summary>Small offline subset of measured Brazilian food composition values; quantities remain user-entered.</summary>
public static class FoodCatalog
{
    public static IReadOnlyList<FoodCatalogItem> Items { get; } =
    [
        new("TACO-0003", "Arroz tipo 1 cozido", 128m, "TACO", 4),
        new("TACO-0561", "Feijão carioca cozido", 76m, "TACO", 4),
        new("TACO-0567", "Feijão preto cozido", 77m, "TACO", 4),
        new("TACO-0182", "Banana prata crua", 98m, "TACO", 4),
        new("TACO-0488", "Ovo de galinha cozido", 146m, "TACO", 4),
        new("TACO-0408", "Peito de frango sem pele cozido", 163m, "TACO", 4)
    ];

    public static Quantity? EstimateEnergy(FoodCatalogItem item, decimal? grams)
    {
        if (grams is null) return null;
        if (grams < 0) throw new ArgumentOutOfRangeException(nameof(grams));
        return new Quantity(item.EnergyKcalPer100Grams * grams.Value / 100m, "kcal", QuantitySource.Estimated);
    }
}
