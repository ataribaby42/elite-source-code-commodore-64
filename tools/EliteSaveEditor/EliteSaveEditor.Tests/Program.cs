using EliteSaveEditor.Core;

var tests = new (string Name, Action Run)[]
{
    ("Original JAMESON checksum", TestOriginalJamesonChecksum),
    ("Galaxy catalog mission systems", TestGalaxyCatalog),
    ("C64 market prices", TestMarketPrices),
    ("Combat rating boundaries", TestCombatRatings),
    ("Mission presets", TestMissionPresets),
    ("Ship change clears transferable state", TestShipChange),
    ("TAP single-position round trip", TestTapRoundTrip),
    ("TAP backup-copy recovery", TestTapBackupRecovery),
    ("TAP multiple-position selection data", TestMultiplePositions),
    ("Attached FLINT TAP", TestAttachedFlintTap),
    ("Attached two-position TAP", TestAttachedMultiPositionTap)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL  {test.Name}");
        Console.WriteLine($"      {exception.Message}");
    }
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? $"All {tests.Length} tests passed." : $"{failures} of {tests.Length} tests failed.");
return failures == 0 ? 0 : 1;

static void TestOriginalJamesonChecksum()
{
    var commander = CommanderSave.CreateOriginalJameson();
    var data = commander.ExportData();
    Equal((byte)0xAA, data[74], "CHK2");
    Equal((byte)0x27, data[75], "CHK3");
    Equal((byte)0x03, data[76], "CHK");
    True(CommanderChecksums.IsValid(data), "Default checksum validation failed.");
    Equal<CommanderFormat?>(CommanderFormat.OriginalElite, CommanderSave.DetectFormat(data), "Format detection");
}

static void TestGalaxyCatalog()
{
    AssertSystem(0, "Xeer", 141, 116);
    AssertSystem(1, "Errius", 184, 149);
    AssertSystem(2, "Ceerdi", 215, 84);

    var lave = GalaxyCatalog.FindByName(0, "Lave");
    True(lave is not null, "Lave was not found in galaxy 1.");
    Equal((byte)5, lave!.Economy, "Lave economy");

    var savedSeedLave = GalaxyCatalog.FindNearest(0x5A4A, 0x0248, 0xB753, 20, 173);
    Equal("LAVE", savedSeedLave.Name, "Saved-seed current system");
    Equal((byte)5, savedSeedLave.Economy, "Saved-seed Lave economy");
}

static void TestMarketPrices()
{
    // Lave has economy 5. These are the exact TT151 results for QQ26 = 0,
    // including the 6502 byte wrap in the Narcotics calculation.
    Equal(36, GameData.MarketPriceTenths(0, 5, 0), "Lave Food price");
    Equal(200, GameData.MarketPriceTenths(2, 5, 0), "Lave Radioactives price");
    Equal(496, GameData.MarketPriceTenths(6, 5, 0), "Lave Narcotics price");
    Equal(368, GameData.MarketPriceTenths(13, 5, 0), "Lave Gold price");

    Equal(40, GameData.MarketPriceTenths(0, 5, 0xFF), "Food fluctuation mask");
}

static void TestCombatRatings()
{
    var cases = new (ushort Points, string Rating)[]
    {
        (0, "Harmless"), (7, "Harmless"),
        (8, "Mostly Harmless"), (15, "Mostly Harmless"),
        (16, "Poor"), (31, "Poor"),
        (32, "Average"), (63, "Average"),
        (64, "Above Average"), (127, "Above Average"),
        (128, "Competent"), (511, "Competent"),
        (512, "Dangerous"), (2559, "Dangerous"),
        (2560, "Deadly"), (6399, "Deadly"),
        (6400, "Elite"), (ushort.MaxValue, "Elite")
    };

    foreach (var (points, rating) in cases)
    {
        Equal(rating, GameData.CombatRating(points), $"Rating at {points} points");
    }
}

static void TestMissionPresets()
{
    var commander = CreateUnbound();
    commander.MissionStatus = 0x1E;
    commander.KillPoints = 0;

    MissionPresets.ConstrictorGalaxyOne(commander);
    Equal((byte)0, (byte)(commander.MissionStatus & 0x0F), "Constrictor preset mission state");
    Equal((byte)0, commander.Galaxy, "Constrictor galaxy");
    Equal((byte)141, commander.SystemX, "Xeer X");
    Equal((byte)116, commander.SystemY, "Xeer Y");
    Equal((ushort)0x0100, commander.KillPoints, "Constrictor trigger tally");

    MissionPresets.ThargoidPlans(commander);
    Equal((byte)2, (byte)(commander.MissionStatus & 0x0F), "Plans preset mission state");
    Equal((byte)2, commander.Galaxy, "Plans galaxy");
    Equal((byte)215, commander.SystemX, "Ceerdi X");
    Equal((byte)84, commander.SystemY, "Ceerdi Y");
    Equal((ushort)0x0500, commander.KillPoints, "Plans trigger tally");

    commander.CashTenths = 10_000_000;
    commander.MissionStatus |= 0x10;
    MissionPresets.Trumbles(commander);
    Equal((byte)0, (byte)(commander.MissionStatus & 0x10), "Trumble offer flag");
    Equal((byte)0x0E, (byte)(commander.MissionStatus & 0x0F), "Completed story missions for Trumble trigger");
    True((byte)(commander.CashTenths >> 8) >= 0xC4, "Trumble cash byte is below the source-code threshold.");
}

static void TestShipChange()
{
    var commander = CreateUnbound();
    commander.SetCargo(0, 1);
    commander.HasEcm = true;
    commander.HasFuelScoops = true;
    commander.HasLargeCargoBay = true;
    commander.SetLaser(0, LaserType.Military);
    commander.Missiles = 3;
    commander.TrumbleCount = 512;

    commander.ChangeShip(10);
    Equal((byte)10, commander.ShipType, "Ship type");
    Equal((byte)0, commander.Cargo(0), "Cargo was not cleared");
    Equal((byte)0, commander.Laser(0), "Laser was not cleared");
    Equal((byte)0, commander.Missiles, "Missiles were not cleared");
    Equal((ushort)0, commander.TrumbleCount, "Trumbles were not cleared");
    True(!commander.HasEcm && !commander.HasFuelScoops && !commander.HasLargeCargoBay,
        "Equipment was not cleared.");
}

static void TestTapRoundTrip()
{
    var commander = CommanderSave.CreateOriginalJameson();
    var tap = TapCodec.Write([new TapCommanderFile(commander.Name, commander.LoadAddress, commander.ExportData())]);
    Equal(44_550, tap.Length, "Generated TAP length");
    SequenceEqual("C64-TAPE-RAW"u8.ToArray(), tap[..12], "TAP signature");
    var loaded = TapCodec.Read(tap);
    Equal(1, loaded.Count, "Position count");
    Equal("JAMESON", loaded[0].Name, "Tape filename");
    Equal(commander.LoadAddress, loaded[0].LoadAddress, "Load address");
    SequenceEqual(commander.ExportData(), loaded[0].Data, "Commander data");
}

static void TestTapBackupRecovery()
{
    var commander = CommanderSave.CreateOriginalJameson();
    var tap = TapCodec.Write([new TapCommanderFile(commander.Name, commander.LoadAddress, commander.ExportData())]);

    // Payload positions from the standard writer layout: leader, two header
    // records, data leader, then the primary data record. Damage one data-bit
    // pulse after its countdown; the intact backup copy must still be loaded.
    const int primaryDataStartInPayload = 40_967;
    tap[20 + primaryDataStartInPayload + 9 * 20 + 4] = 1;

    var loaded = TapCodec.Read(tap);
    Equal(1, loaded.Count, "Recovered position count");
    SequenceEqual(commander.ExportData(), loaded[0].Data, "Recovered backup data");
}

static void TestMultiplePositions()
{
    var jameson = CommanderSave.CreateOriginalJameson();
    var flint = CreateUnbound();
    flint.Name = "FLINT";
    flint.ChangeShip(10);
    flint.SetRegistrationLetters("LU");
    flint.RegistrationNumber = 162;

    var tap = TapCodec.Write(
    [
        new TapCommanderFile(jameson.Name, jameson.LoadAddress, jameson.ExportData()),
        new TapCommanderFile(flint.Name, flint.LoadAddress, flint.ExportData())
    ]);

    var loaded = TapCodec.Read(tap);
    Equal(2, loaded.Count, "Position count");
    Equal("JAMESON", loaded[0].Name, "First tape filename");
    Equal("FLINT", loaded[1].Name, "Second tape filename");
    Equal((byte)10, loaded[1].Data[21], "Second position ship");
}

static void TestAttachedFlintTap()
{
    var configured = Environment.GetEnvironmentVariable("ELITE_TEST_TAP");
    var path = !string.IsNullOrWhiteSpace(configured)
        ? configured
        : @"G:\Emulace\C64\Elite-Unbound-C64\v0.52\alternate-starts-FLINT.tap";

    if (!File.Exists(path))
    {
        Console.WriteLine("SKIP  Attached FLINT TAP is not available on this machine.");
        return;
    }

    var loaded = TapCodec.Read(path);
    Equal(1, loaded.Count, "FLINT position count");
    Equal("FLINT", loaded[0].Name, "FLINT tape filename");
    Equal(77, loaded[0].Data.Length, "FLINT commander length");
    Equal((byte)10, loaded[0].Data[21], "FLINT ship type");
    Equal<CommanderFormat?>(CommanderFormat.EliteUnbound, CommanderSave.DetectFormat(loaded[0].Data), "FLINT format");
}

static void TestAttachedMultiPositionTap()
{
    var configured = Environment.GetEnvironmentVariable("ELITE_TEST_MULTI_TAP");
    var path = !string.IsNullOrWhiteSpace(configured)
        ? configured
        : @"G:\Emulace\C64\Elite-Play\test-scramble.tap";

    if (!File.Exists(path))
    {
        Console.WriteLine("SKIP  Attached two-position TAP is not available on this machine.");
        return;
    }

    var loaded = TapCodec.Read(path);
    Equal(2, loaded.Count, "Attached multi-TAP position count");

    var commanders = loaded.Select(file =>
    {
        var format = CommanderSave.DetectFormat(file.Data);
        Equal<CommanderFormat?>(CommanderFormat.EliteUnbound, format, $"{file.Name} format");
        return new CommanderSave(file.Name, file.Data, format!.Value, file.LoadAddress);
    }).ToArray();

    Equal("TOM", commanders[0].Name, "First tape filename");
    Equal((byte)3, commanders[0].SystemX, "First position X");
    Equal((byte)181, commanders[0].SystemY, "First position Y");
    Equal(35_831u, commanders[0].CashTenths, "First position credits");

    Equal("TOM", commanders[1].Name, "Second tape filename");
    Equal((byte)13, commanders[1].SystemX, "Second position X");
    Equal((byte)186, commanders[1].SystemY, "Second position Y");
    Equal(34_180u, commanders[1].CashTenths, "Second position credits");
}

static CommanderSave CreateUnbound()
{
    var commander = CommanderSave.CreateOriginalJameson();
    commander.ChangeFormat(CommanderFormat.EliteUnbound);
    return commander;
}

static void AssertSystem(byte galaxy, string name, byte x, byte y)
{
    var system = GalaxyCatalog.FindByName(galaxy, name);
    True(system is not null, $"{name} was not found in galaxy {galaxy + 1}.");
    Equal(x, system!.X, $"{name} X");
    Equal(y, system.Y, $"{name} Y");
}

static void Equal<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
    }
}

static void True(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void SequenceEqual(byte[] expected, byte[] actual, string label)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException($"{label} differs.");
    }
}
