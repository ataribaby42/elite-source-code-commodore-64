using EliteSaveEditor.Core;

var tests = new (string Name, Action Run)[]
{
    ("Original JAMESON checksum", TestOriginalJamesonChecksum),
    ("Original cargo capacity and TAP encoding", TestOriginalCargoCapacity),
    ("Galaxy catalog mission systems", TestGalaxyCatalog),
    ("C64 market prices", TestMarketPrices),
    ("Combat rating boundaries", TestCombatRatings),
    ("Mission presets", TestMissionPresets),
    ("Ship change clears transferable state", TestShipChange),
    ("TAP single-position round trip", TestTapRoundTrip),
    ("TAP backup-copy recovery", TestTapBackupRecovery),
    ("TAP multiple-position selection data", TestMultiplePositions),
    ("Commander lengths, automatic detection and conversion", TestCommanderFormats),
    ("Special Cargo state and boundaries", TestSpecialCargo),
    ("Special Cargo saved galaxy and ship changes", TestSpecialCargoGalaxy),
    ("Mixed Original, legacy Unbound and extended Unbound TAP", TestMixedLengths),
    ("Extended TAP checksum and backup recovery", TestExtendedTapRecovery),
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

static void TestOriginalCargoCapacity()
{
    foreach (var (largeBay, capacity, storedValue) in new[]
    {
        (false, 20, (byte)22),
        (true, 35, (byte)37)
    })
    {
        var commander = CommanderSave.CreateOriginalJameson();
        commander.HasLargeCargoBay = largeBay;
        commander.HasEcm = true;
        Equal(capacity, commander.CargoCapacity(), "Original hold capacity");
        Equal(0, commander.EquipmentWeightTonnes(), "Original equipment weight");

        commander.SetCargo(0, (byte)capacity);
        Equal(0, commander.Validate().Count, "A full original hold must be valid");
        var data = commander.ExportData();
        Equal(storedValue, data[22], "CRGO must retain the on-tape capacity + 2 encoding");
        True(CommanderChecksums.IsValid(data), "Full-hold save checksum validation failed.");

        var tap = TapCodec.Write([new TapCommanderFile(commander.Name, commander.LoadAddress, data)]);
        var loaded = TapCodec.Read(tap).Single();
        SequenceEqual(data, loaded.Data, "Full-hold commander TAP round trip");
        Equal<CommanderFormat?>(CommanderFormat.OriginalElite, CommanderSave.DetectFormat(loaded.Data), "Loaded format");
        var restored = new CommanderSave(loaded.Name, loaded.Data, CommanderFormat.OriginalElite, loaded.LoadAddress);
        Equal(capacity, restored.CargoCapacity(), "Loaded original hold capacity");
        Equal(0, restored.Validate().Count, "Loaded full hold must be valid");

        commander.SetCargo(0, (byte)(capacity + 1));
        True(commander.Validate().Any(error => error.Contains("hold capacity")),
            "An original hold overloaded by one tonne must be rejected.");

        commander.ChangeFormat(CommanderFormat.EliteUnbound);
        Equal(largeBay ? 35 : 25, commander.CargoCapacity(), "Unbound Cobra Mk III capacity must be unchanged");
        Equal(storedValue, commander.ExportData()[22], "Format conversion must preserve CRGO");
        commander.ChangeFormat(CommanderFormat.OriginalElite);
        Equal(capacity, commander.CargoCapacity(), "Capacity after returning to Original Elite");
    }
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

static void TestCommanderFormats()
{
    var original = CommanderSave.CreateOriginalJameson().ExportData();
    var current = CreateUnbound().ExportData();
    Equal(77, original.Length, "Original payload");
    Equal(81, current.Length, "Unbound payload");
    Equal<CommanderFormat?>(CommanderFormat.EliteUnbound, CommanderSave.DetectFormat(current), "Extended detection");
    var legacy = current[..77];
    Equal<CommanderFormat?>(CommanderFormat.EliteUnbound, CommanderSave.DetectFormat(legacy), "Legacy detection");
    var migrated = new CommanderSave("LEGACY", legacy, CommanderFormat.EliteUnbound);
    SequenceEqual(legacy, migrated.ExportData()[..77], "Legacy prefix retained");
    SequenceEqual(new byte[4], migrated.ExportData()[77..], "Empty extension for legacy");

    // Length wins even if an extended save's prefix happens to pass Original
    // checksums, or its registration needs repair.
    var checksumCollision = new byte[81];
    original.CopyTo(checksumCollision, 0);
    Equal<CommanderFormat?>(CommanderFormat.EliteUnbound, CommanderSave.DetectFormat(checksumCollision), "Extended checksum collision");
    Throws<ArgumentException>(() => new CommanderSave("BAD", current, CommanderFormat.OriginalElite));
    foreach (var length in new[] { 0, 76, 78, 79, 80, 82, 83 })
    {
        foreach (var format in Enum.GetValues<CommanderFormat>())
        {
            Throws<ArgumentException>(() => new CommanderSave("BAD", new byte[length], format));
        }
        Throws<ArgumentException>(() => TapCodec.Write([new("BAD", 0, new byte[length])]));
        Equal<CommanderFormat?>(null, CommanderSave.DetectFormat(new byte[length]), "Unknown length");
    }

    var commander = migrated.Clone();
    commander.SetSpecialCargo(GalaxyCatalog.FindByName(0, "Lave")!, 5350);
    commander.ChangeFormat(CommanderFormat.OriginalElite);
    Equal(77, commander.ExportData().Length, "Conversion to Original removes extension");
    True(CommanderChecksums.IsValid(commander.ExportData()), "Converted Original checksums");
    commander.ChangeFormat(CommanderFormat.EliteUnbound);
    Equal(81, commander.ExportData().Length, "Conversion back to Unbound");
    True(!commander.HasSpecialCargo, "Conversion must not resurrect a discarded delivery");
    Equal("JS-042", commander.RegistrationId, "Registration restored when converting");
}

static void TestSpecialCargo()
{
    var commander = CreateUnbound();
    var before = commander.ExportData()[..77];
    var lave = GalaxyCatalog.FindByName(0, "Lave")!;
    foreach (ushort reward in new ushort[] { 1, 5350, ushort.MaxValue })
    {
        commander.SetSpecialCargo(lave, reward);
        Equal(reward, commander.SpecialCargoRewardTenths, "Reward");
        Equal("LAVE", commander.SpecialCargoDestination!.Name, "Destination");
        var data = commander.ExportData();
        SequenceEqual(before, data[..77], "Setting cargo preserves all existing commander fields");
        SequenceEqual(new byte[] { (byte)reward, (byte)(reward >> 8), 20, 173 }, data[77..], "Game extension layout");
        Equal(0, commander.Validate().Count, "Valid delivery");
        var clone = commander.Clone();
        clone.ClearSpecialCargo();
        True(commander.HasSpecialCargo && !clone.HasSpecialCargo, "Clone storage is independent");
        SequenceEqual(data[79..], clone.ExportData()[79..], "Clearing preserves previous offer coordinates as in game");
    }
    var unchanged = commander.ExportData();
    Throws<ArgumentOutOfRangeException>(() => commander.SetSpecialCargo(lave, 0));
    Throws<ArgumentException>(() => commander.SetSpecialCargo(new(0, "INVALID", 0, 0, 0), 100));
    SequenceEqual(unchanged, commander.ExportData(), "Rejected edits are atomic");

    commander.SetRaw(79, 0);
    commander.SetRaw(80, 0);
    True(commander.Validate().Any(error => error.Contains("Special Cargo destination")), "Invalid active target rejected");
    commander.ClearSpecialCargo();
    Equal(0, commander.Validate().Count, "Inactive coordinates need not be a system");
    SequenceEqual(before, commander.ExportData()[..77], "Clear preserves other commander state");
    var original = CommanderSave.CreateOriginalJameson();
    True(!original.HasSpecialCargo, "Original has no cargo extension");
    Throws<InvalidOperationException>(() => original.SetSpecialCargo(lave, 100));
    Throws<InvalidOperationException>(() => original.ClearSpecialCargo());
}

static void TestSpecialCargoGalaxy()
{
    var commander = CreateUnbound();
    foreach (byte galaxy in Enumerable.Range(0, 8).Select(value => (byte)value))
    {
        commander.SetGalaxy(galaxy);
        var target = commander.CurrentGalaxySystems()[255];
        commander.SetSpecialCargo(target, 5350);
        commander.SetGalaxy(galaxy);
        True(commander.HasSpecialCargo, "Selecting the same galaxy preserves cargo");
        Equal(target, commander.SpecialCargoDestination, "Last system in saved galaxy");
        commander.ChangeShip(1);
        True(commander.HasSpecialCargo, "Changing hull preserves separate delivery");
        commander.SetGalaxy((byte)((galaxy + 1) % 8));
        True(!commander.HasSpecialCargo, "Changing galaxy cancels delivery");
    }
    // Use the raw seed rather than assuming the galaxy-number byte is authoritative.
    var seed = GalaxyCatalog.SeedBytes(2);
    for (var word = 0; word < 3; word++)
    {
        commander.SetGalaxySeedWord(word, (ushort)(seed[2 * word] | seed[2 * word + 1] << 8));
    }
    commander.Galaxy = 0;
    var ceerdi = GalaxyCatalog.FindByName(2, "Ceerdi")!;
    commander.SetSpecialCargo(ceerdi, 5350);
    Equal("CEERDI", commander.SpecialCargoDestination!.Name, "Destination uses raw saved seed");
    commander.SetGalaxySeedWord(0, commander.GalaxySeedWord(0));
    True(commander.HasSpecialCargo, "Unchanged seed preserves cargo");
    commander.SetGalaxySeedWord(0, (ushort)(commander.GalaxySeedWord(0) ^ 1));
    True(!commander.HasSpecialCargo, "Changed raw seed cancels cargo");
}

static void TestMixedLengths()
{
    var original = CommanderSave.CreateOriginalJameson();
    var unbound = CreateUnbound();
    unbound.SetRegistrationLetters("ZZ");
    unbound.RegistrationNumber = 255;
    unbound.RegistrationScrambled = true;
    unbound.SetSpecialCargo(GalaxyCatalog.FindByName(0, "Lave")!, 5350);
    var files = new TapCommanderFile[]
    {
        new("SAME", original.LoadAddress, original.ExportData()),
        new("SAME", unbound.LoadAddress, unbound.ExportData()),
        new("SAME", unbound.LoadAddress, unbound.ExportData()[..77])
    };
    var loaded = TapCodec.Read(TapCodec.Write(files));
    Equal(3, loaded.Count, "Same-named files with mixed lengths remain distinct");
    for (var i = 0; i < files.Length; i++)
    {
        SequenceEqual(files[i].Data, loaded[i].Data, "Mixed TAP payload");
        var format = CommanderSave.DetectFormat(loaded[i].Data)!.Value;
        var restored = new CommanderSave(loaded[i].Name, loaded[i].Data, format, loaded[i].LoadAddress);
        Equal(i == 0 ? 77 : 81, restored.ExportData().Length, "Output length by selected type");
        if (i != 0) { Equal("ZZ-255", restored.RegistrationId, "Registration is not checksummed"); }
        Equal(i == 1, restored.HasSpecialCargo, "Only extended file has delivery");
    }
    var fixtureDirectory = Environment.GetEnvironmentVariable("ELITE_TEST_OUTPUT");
    if (!string.IsNullOrEmpty(fixtureDirectory))
    {
        Directory.CreateDirectory(fixtureDirectory);
        TapCodec.Write(Path.Combine(fixtureDirectory, "mixed.tap"), files);
        for (var i = 0; i < files.Length; i++)
        {
            TapCodec.Write(Path.Combine(fixtureDirectory, $"format-{i}.tap"), [files[i]]);
        }
    }
}

static void TestExtendedTapRecovery()
{
    var commander = CreateUnbound();
    commander.SetSpecialCargo(GalaxyCatalog.FindByName(0, "Lave")!, ushort.MaxValue);
    var data = commander.ExportData();
    var tap = TapCodec.Write([new(commander.Name, commander.LoadAddress, data)]);
    var primary = tap.AsSpan().LastIndexOf(CountdownPulses(true));
    var backup = tap.AsSpan().LastIndexOf(CountdownPulses(false));
    True(primary > 0 && backup > primary, "Both data countdowns found");
    // Corrupt the last extension byte, after the nine countdown bytes.
    // Twenty pulses encode each ROM byte; pulse four belongs to its first bit.
    tap[primary + (9 + data.Length - 1) * 20 + 4] = 1;
    SequenceEqual(data, TapCodec.Read(tap).Single().Data, "Backup retains all 81 bytes");
    tap[backup + (9 + data.Length - 1) * 20 + 4] = 1;
    Throws<InvalidDataException>(() => TapCodec.Read(tap));
}

static byte[] CountdownPulses(bool primary)
{
    var pulses = new List<byte>();
    for (var count = 9; count > 0; count--)
    {
        var value = count | (primary ? 0x80 : 0);
        pulses.AddRange(new byte[] { 0x55, 0x41 });
        var parity = 1;
        for (var bit = 0; bit < 9; bit++)
        {
            var set = bit == 8 ? parity : (value >> bit) & 1;
            if (bit < 8) { parity ^= set; }
            pulses.AddRange(set == 0 ? new byte[] { 0x2D, 0x41 } : new byte[] { 0x41, 0x2D });
        }
    }
    return pulses.ToArray();
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
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
