namespace EliteSaveEditor.Core;

public enum CommanderFormat
{
    OriginalElite,
    EliteUnbound
}

public enum LaserType : byte
{
    None = 0x00,
    Pulse = 0x0F,
    Beam = 0x8F,
    Mining = 0x32,
    Military = 0x97
}

public enum EnergyUnitType : byte
{
    None = 0,
    ExtraEnergyUnit = 1,
    NavalEnergyUnit = 2
}

public sealed record ShipDefinition(
    byte Id,
    string Name,
    byte MaximumMissiles,
    byte FuelCapacityTenths,
    byte LaserMounts,
    byte StandardCargoCapacity,
    byte ExtendedCargoCapacity);

public sealed record MarketItemDefinition(byte BasePrice, sbyte EconomicFactor, byte FluctuationMask);

public static class GameData
{
    public static readonly string[] Commodities =
    [
        "Food", "Textiles", "Radioactives", "Slaves", "Liquor/Wines",
        "Luxuries", "Narcotics", "Computers", "Machinery", "Alloys",
        "Firearms", "Furs", "Minerals", "Gold", "Platinum", "Gem-Stones",
        "Alien Items"
    ];

    public static readonly string[] CommodityUnits =
    [
        "t", "t", "t", "t", "t", "t", "t", "t", "t", "t", "t",
        "t", "t", "kg", "kg", "g", "t"
    ];

    public static readonly ShipDefinition[] Ships =
    [
        new(0,  "Cobra Mk III",  4,  70, 4,  28,  30),
        new(1,  "Adder",         1,  60, 2,   8,   9),
        new(2,  "Gecko",         2,  70, 4,   9,  10),
        new(3,  "Moray",         2,  80, 4,  11,  12),
        new(4,  "Cobra Mk I",    3,  60, 4,  14,  15),
        new(5,  "Fer-de-Lance",  3,  85, 4,   9,  10),
        new(6,  "Python",        4,  80, 4, 106, 116),
        new(7,  "Boa",           6,  90, 2, 132, 145),
        new(8,  "Anaconda",     16, 100, 4, 215, 236),
        new(9,  "Asp Mk II",     1, 125, 1,   6,   7),
        new(10, "Sidewinder",    1,  50, 1,   4,   5),
        new(11, "Krait",         0,  60, 1,  10,  11),
        new(12, "Mamba",         2,  60, 1,  10,  11)
    ];

    public static readonly byte[] DefaultMarketAvailability =
        [16, 15, 17, 0, 3, 28, 14, 0, 0, 10, 0, 17, 58, 7, 9, 8, 0];

    // The base price, economic factor and fluctuation mask are the values in
    // the game's four-byte QQ23 market table. Base quantity is already stored
    // separately in each commander position as current market availability.
    public static readonly MarketItemDefinition[] MarketItems =
    [
        new(19,  -2, 0x01), // Food
        new(20,  -1, 0x03), // Textiles
        new(65,  -3, 0x07), // Radioactives
        new(40,  -5, 0x1F), // Slaves
        new(83,  -5, 0x0F), // Liquor/Wines
        new(196,  8, 0x03), // Luxuries
        new(235, 29, 0x78), // Narcotics
        new(154, 14, 0x03), // Computers
        new(117,  6, 0x07), // Machinery
        new(78,   1, 0x1F), // Alloys
        new(124, 13, 0x07), // Firearms
        new(176, -9, 0x3F), // Furs
        new(32,  -1, 0x03), // Minerals
        new(97,  -1, 0x07), // Gold
        new(171, -2, 0x1F), // Platinum
        new(45,  -1, 0x0F), // Gem-Stones
        new(53,  15, 0x07)  // Alien Items
    ];

    public static ShipDefinition Ship(byte id) =>
        id < Ships.Length ? Ships[id] : Ships[0];

    public static int MarketPriceTenths(int commodity, byte economy, byte randomizer)
    {
        if ((uint)commodity >= MarketItems.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(commodity));
        }

        if (economy > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(economy));
        }

        var item = MarketItems[commodity];
        var priceQuarterCredits = unchecked((byte)(item.BasePrice + (randomizer & item.FluctuationMask)));
        var economyAdjustment = economy * Math.Abs(item.EconomicFactor);
        priceQuarterCredits = item.EconomicFactor < 0
            ? unchecked((byte)(priceQuarterCredits - economyAdjustment))
            : unchecked((byte)(priceQuarterCredits + economyAdjustment));

        // TT151 multiplies this byte by four to obtain tenths of a credit.
        return priceQuarterCredits * 4;
    }

    public static string CombatRating(ushort killPoints) => killPoints switch
    {
        < 8 => "Harmless",
        < 16 => "Mostly Harmless",
        < 32 => "Poor",
        < 64 => "Average",
        < 128 => "Above Average",
        < 512 => "Competent",
        < 2560 => "Dangerous",
        < 6400 => "Deadly",
        _ => "Elite"
    };

    public static string FormatLaser(byte value) => value switch
    {
        (byte)LaserType.None => "None",
        (byte)LaserType.Pulse => "Pulse Laser",
        (byte)LaserType.Beam => "Beam Laser",
        (byte)LaserType.Mining => "Mining Laser",
        (byte)LaserType.Military => "Military Laser",
        _ => $"Unknown (0x{value:X2})"
    };
}
