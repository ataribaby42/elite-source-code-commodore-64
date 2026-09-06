using System.Buffers.Binary;

namespace EliteSaveEditor.Core;

public sealed class CommanderSave
{
    public const int OriginalDataLength = 77;
    public const int SpecialCargoRewardOffset = OriginalDataLength;
    public const int SpecialCargoXOffset = SpecialCargoRewardOffset + sizeof(ushort);
    public const int SpecialCargoYOffset = SpecialCargoXOffset + 1;
    public const int UnboundDataLength = SpecialCargoYOffset + 1;
    public const ushort DefaultLoadAddress = 0x25D0;

    private byte[] _data;

    public CommanderSave(string name, byte[] data, CommanderFormat format, ushort loadAddress = DefaultLoadAddress)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }

        if (data.Length != OriginalDataLength &&
            !(format == CommanderFormat.EliteUnbound && data.Length == UnboundDataLength))
        {
            throw new ArgumentException(format == CommanderFormat.EliteUnbound
                ? "Unbound commander data must contain 81 bytes, or 77 bytes for an older save."
                : "Original Elite commander data must contain exactly 77 bytes.", nameof(data));
        }

        Name = NormalizeName(name);
        _data = new byte[format == CommanderFormat.EliteUnbound ? UnboundDataLength : OriginalDataLength];
        data.CopyTo(_data, 0); // Older Unbound saves acquire an empty extension.
        Format = format;
        LoadAddress = loadAddress;
    }

    public string Name { get; set; }
    public CommanderFormat Format { get; private set; }
    public ushort LoadAddress { get; set; }
    public ReadOnlySpan<byte> Data => _data;

    public byte MissionStatus { get => _data[0]; set => _data[0] = value; }
    public byte SystemX { get => _data[1]; set => _data[1] = value; }
    public byte SystemY { get => _data[2]; set => _data[2] = value; }
    public uint CashTenths { get => ReadUInt32BigEndian(9); set => WriteUInt32BigEndian(9, value); }
    public byte FuelTenths { get => _data[13]; set => _data[13] = value; }
    public byte CompetitionFlags => _data[14];
    public byte Galaxy { get => _data[15]; set => _data[15] = value; }
    public byte ShipType => Format == CommanderFormat.EliteUnbound ? _data[21] : (byte)0;
    public bool HasLargeCargoBay { get => _data[22] >= 26; set => _data[22] = value ? (byte)37 : (byte)22; }
    public ushort TrumbleCount { get => ReadUInt16LittleEndian(48); set => WriteUInt16LittleEndian(48, value); }
    public byte FractionalKillPoints { get => _data[50]; set => _data[50] = value; }
    public byte Missiles { get => _data[51]; set => _data[51] = value; }
    public byte LegalStatus { get => _data[52]; set => _data[52] = value; }
    public byte MarketPriceRandomizer { get => _data[70]; set => _data[70] = value; }
    public ushort KillPoints { get => ReadUInt16LittleEndian(71); set => WriteUInt16LittleEndian(71, value); }
    public ushort SpecialCargoRewardTenths => Format == CommanderFormat.EliteUnbound
        ? ReadUInt16LittleEndian(SpecialCargoRewardOffset) : (ushort)0;
    public byte SpecialCargoTargetX => Format == CommanderFormat.EliteUnbound ? _data[SpecialCargoXOffset] : (byte)0;
    public byte SpecialCargoTargetY => Format == CommanderFormat.EliteUnbound ? _data[SpecialCargoYOffset] : (byte)0;
    public bool HasSpecialCargo => SpecialCargoRewardTenths != 0;

    public IReadOnlyList<EliteSystem> CurrentGalaxySystems() =>
        GalaxyCatalog.Systems(GalaxySeedWord(0), GalaxySeedWord(1), GalaxySeedWord(2));

    public EliteSystem? SpecialCargoDestination => !HasSpecialCargo ? null :
        CurrentGalaxySystems().FirstOrDefault(system =>
            system.X == SpecialCargoTargetX && system.Y == SpecialCargoTargetY);

    public void SetSpecialCargo(EliteSystem destination, ushort rewardTenths)
    {
        RequireUnboundCargo();
        ArgumentNullException.ThrowIfNull(destination);
        if (rewardTenths == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rewardTenths), "The reward must be from 0.1 to 6553.5 Cr.");
        }
        if (!CurrentGalaxySystems().Contains(destination))
        {
            throw new ArgumentException("The delivery destination must be a system in the saved galaxy.", nameof(destination));
        }

        WriteUInt16LittleEndian(SpecialCargoRewardOffset, rewardTenths);
        _data[SpecialCargoXOffset] = destination.X;
        _data[SpecialCargoYOffset] = destination.Y;
    }

    public void ClearSpecialCargo()
    {
        RequireUnboundCargo();
        // Like delivery/expiry in the game, retain the previous coordinates:
        // they also participate in generation of the next set of offers.
        WriteUInt16LittleEndian(SpecialCargoRewardOffset, 0);
    }

    private void RequireUnboundCargo()
    {
        if (Format != CommanderFormat.EliteUnbound)
        {
            throw new InvalidOperationException("Special Cargo is only available in Elite: Unbound saves.");
        }
    }

    public static CommanderSave CreateOriginalJameson()
    {
        var data = new byte[OriginalDataLength];
        data[0] = 0;
        data[1] = 20;
        data[2] = 173;
        GalaxyCatalog.SeedBytes(0).CopyTo(data, 3);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(9, 4), 1000);
        data[13] = 70;
        data[14] = 0;
        data[15] = 0;
        data[16] = (byte)LaserType.Pulse;
        data[22] = 22;
        data[51] = 3;
        GameData.DefaultMarketAvailability.CopyTo(data, 53);
        data[73] = 128;
        CommanderChecksums.Apply(data);
        return new CommanderSave("JAMESON", data, CommanderFormat.OriginalElite);
    }

    public static CommanderFormat? DetectFormat(byte[] data)
    {
        if (data.Length == UnboundDataLength)
        {
            return CommanderFormat.EliteUnbound;
        }
        if (CommanderChecksums.IsValid(data))
        {
            return CommanderFormat.OriginalElite;
        }

        if (data.Length == OriginalDataLength &&
            data[73] is 0 or 0xFF &&
            data[74] is >= (byte)'A' and <= (byte)'Z' &&
            data[75] is >= (byte)'A' and <= (byte)'Z' &&
            data[76] != 0)
        {
            return CommanderFormat.EliteUnbound;
        }

        return null;
    }

    public CommanderSave Clone() => new(Name, _data, Format, LoadAddress);

    public byte[] ExportData()
    {
        var result = (byte[])_data.Clone();
        if (Format == CommanderFormat.OriginalElite)
        {
            CommanderChecksums.Apply(result);
        }

        return result;
    }

    public byte Raw(int offset) => _data[offset];
    public void SetRaw(int offset, byte value) => _data[offset] = value;

    public ushort GalaxySeedWord(int word) => ReadUInt16LittleEndian(3 + word * 2);
    public void SetGalaxySeedWord(int word, ushort value)
    {
        if (word is < 0 or > 2) { throw new ArgumentOutOfRangeException(nameof(word)); }
        if (GalaxySeedWord(word) != value && Format == CommanderFormat.EliteUnbound)
        {
            ClearSpecialCargo();
        }
        WriteUInt16LittleEndian(3 + word * 2, value);
    }

    public byte Laser(int mount) => _data[16 + mount];
    public void SetLaser(int mount, LaserType value) => _data[16 + mount] = (byte)value;

    public byte Cargo(int commodity) => _data[23 + commodity];
    public void SetCargo(int commodity, byte value) => _data[23 + commodity] = value;

    public byte MarketAvailability(int commodity) => _data[53 + commodity];
    public void SetMarketAvailability(int commodity, byte value) => _data[53 + commodity] = value;

    public bool HasEcm { get => IsSet(40); set => SetFlag(40, value, 0xFF); }
    public bool HasFuelScoops { get => IsSet(41); set => SetFlag(41, value, 0xFF); }
    public bool HasBombSlotEquipment { get => IsSet(42); set => SetFlag(42, value, 0x7F); }
    public EnergyUnitType EnergyUnit { get => (EnergyUnitType)_data[43]; set => _data[43] = (byte)value; }
    public bool HasDockingComputer { get => IsSet(44); set => SetFlag(44, value, 0xFF); }
    public bool HasGalacticHyperdrive { get => IsSet(45); set => SetFlag(45, value, 0xFF); }
    public bool HasEscapeCapsule { get => IsSet(46); set => SetFlag(46, value, 0xFF); }

    public bool RegistrationScrambled { get => _data[73] != 0; set => _data[73] = value ? (byte)0xFF : (byte)0; }
    public string RegistrationLetters => $"{(char)_data[74]}{(char)_data[75]}";
    public byte RegistrationNumber { get => _data[76]; set => _data[76] = value; }
    public byte SaveCount { get => _data[73]; set => _data[73] = value; }

    public string RegistrationId => $"{RegistrationLetters}-{RegistrationNumber:000}";

    public void SetRegistrationLetters(string letters)
    {
        letters = letters.Trim().ToUpperInvariant();
        if (letters.Length != 2 || letters.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Registration must contain exactly two letters A-Z.", nameof(letters));
        }

        _data[74] = (byte)letters[0];
        _data[75] = (byte)letters[1];
    }

    public void SetGalaxy(byte galaxy)
    {
        if (galaxy > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(galaxy));
        }

        var seed = GalaxyCatalog.SeedBytes(galaxy);
        if (Format == CommanderFormat.EliteUnbound && !seed.AsSpan().SequenceEqual(_data.AsSpan(3, seed.Length)))
        {
            ClearSpecialCargo();
        }
        Galaxy = galaxy;
        seed.CopyTo(_data, 3);
    }

    public void SetSystem(EliteSystem system)
    {
        SystemX = system.X;
        SystemY = system.Y;
    }

    public void ChangeFormat(CommanderFormat format)
    {
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }
        if (format == Format)
        {
            return;
        }

        if (format == CommanderFormat.EliteUnbound)
        {
            Array.Resize(ref _data, UnboundDataLength);
            _data[21] = 0;
            _data[73] = 0;
            _data[74] = (byte)'J';
            _data[75] = (byte)'S';
            _data[76] = 42;
        }
        else
        {
            Array.Resize(ref _data, OriginalDataLength); // Original saves have no delivery extension.
            _data[21] = 0;
            _data[73] = 128;
            FuelTenths = Math.Min(FuelTenths, (byte)70);
            Missiles = Math.Min(Missiles, (byte)4);
            CommanderChecksums.Apply(_data);
        }

        Format = format;
    }

    public void ChangeShip(byte shipType)
    {
        if (Format != CommanderFormat.EliteUnbound)
        {
            throw new InvalidOperationException("Player ship selection is only available in Elite: Unbound saves.");
        }

        if (shipType >= GameData.Ships.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(shipType));
        }

        if (_data[21] == shipType)
        {
            return;
        }

        _data[21] = shipType;
        Array.Clear(_data, 16, 4);       // Lasers
        _data[22] = 22;                 // Standard cargo bay
        Array.Clear(_data, 23, 17);      // Cargo
        Array.Clear(_data, 40, 7);       // Equipment
        _data[47] = 0;
        _data[48] = 0;                  // Trumbles are cargo too
        _data[49] = 0;
        _data[51] = 0;                  // Missiles
        FuelTenths = Math.Min(FuelTenths, GameData.Ship(shipType).FuelCapacityTenths);
    }

    public int CargoUsedTonnes()
    {
        var used = 0;
        for (var commodity = 0; commodity <= 12; commodity++)
        {
            used += Cargo(commodity);
        }

        return used + (_data[49]);
    }

    public int EquipmentWeightTonnes()
    {
        if (Format != CommanderFormat.EliteUnbound)
        {
            return 0;
        }

        var weight = Enumerable.Range(0, 4).Count(mount => Laser(mount) != 0);
        weight += HasEscapeCapsule ? 1 : 0;
        weight += HasFuelScoops ? 1 : 0;
        weight += HasEcm ? 1 : 0;
        weight += HasBombSlotEquipment ? 1 : 0;
        weight += EnergyUnit != EnergyUnitType.None ? 1 : 0;
        weight += HasDockingComputer ? 1 : 0;
        weight += HasGalacticHyperdrive ? 1 : 0;
        return weight;
    }

    public int CargoCapacity()
    {
        if (Format == CommanderFormat.OriginalElite)
        {
            // CRGO stores capacity + 2 (22/37); expose the actual tonnes here.
            return HasLargeCargoBay ? 35 : 20;
        }

        var ship = GameData.Ship(ShipType);
        return HasLargeCargoBay ? ship.ExtendedCargoCapacity : ship.StandardCargoCapacity;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 7 || Name.Any(character => character < 32 || character > 126))
        {
            errors.Add("Commander name must contain 1-7 printable characters.");
        }

        if (Galaxy > 7)
        {
            errors.Add("Galaxy must be in the range 1-8.");
        }

        if (!Enum.IsDefined(typeof(LaserType), Laser(0)) ||
            !Enum.IsDefined(typeof(LaserType), Laser(1)) ||
            !Enum.IsDefined(typeof(LaserType), Laser(2)) ||
            !Enum.IsDefined(typeof(LaserType), Laser(3)))
        {
            errors.Add("One or more laser values are not recognized.");
        }

        if (_data[43] > 2)
        {
            errors.Add("Energy Unit must be None, Extra or Naval.");
        }

        if (_data[22] is not (22 or 37))
        {
            errors.Add("Cargo bay value must be 22 (standard) or 37 (Large Cargo Bay).");
        }

        var ship = Format == CommanderFormat.EliteUnbound ? GameData.Ship(ShipType) : GameData.Ships[0];
        if (Format == CommanderFormat.EliteUnbound && _data[21] >= GameData.Ships.Length)
        {
            errors.Add("Elite: Unbound ship type is outside the valid range.");
        }

        if (FuelTenths > ship.FuelCapacityTenths)
        {
            errors.Add($"Fuel exceeds the {ship.Name} capacity of {ship.FuelCapacityTenths / 10m:0.0} LY.");
        }

        if (Missiles > ship.MaximumMissiles)
        {
            errors.Add($"Missile count exceeds the {ship.Name} capacity of {ship.MaximumMissiles}.");
        }

        var used = CargoUsedTonnes() + EquipmentWeightTonnes();
        if (used > CargoCapacity())
        {
            errors.Add($"Cargo and equipment use {used}t, but the hold capacity is {CargoCapacity()}t.");
        }

        if (Format == CommanderFormat.EliteUnbound)
        {
            if (HasSpecialCargo && SpecialCargoDestination is null)
            {
                errors.Add("Special Cargo destination is not a system in the saved galaxy; select a destination or clear the delivery.");
            }
            if (ship.LaserMounts < 4 && (Laser(2) != 0 || Laser(3) != 0) ||
                ship.LaserMounts < 2 && Laser(1) != 0)
            {
                errors.Add($"One or more lasers are fitted to mounts not present on the {ship.Name}.");
            }

            if (_data[73] is not (0 or 0xFF) ||
                _data[74] is < (byte)'A' or > (byte)'Z' ||
                _data[75] is < (byte)'A' or > (byte)'Z' ||
                _data[76] == 0)
            {
                errors.Add("Registration ID must contain two letters and a number from 1 to 255.");
            }
        }

        return errors;
    }

    public static string NormalizeName(string name)
    {
        name = name.Trim().Trim('"').ToUpperInvariant();
        if (name.Length is < 1 or > 7)
        {
            throw new ArgumentException("Commander name must contain 1-7 characters.", nameof(name));
        }

        if (name.Any(character => character < 32 || character > 126))
        {
            throw new ArgumentException("Commander name contains a character unsupported by the C64 tape filename.", nameof(name));
        }

        return name;
    }

    private bool IsSet(int offset) => _data[offset] != 0;
    private void SetFlag(int offset, bool value, byte enabledValue) => _data[offset] = value ? enabledValue : (byte)0;
    private ushort ReadUInt16LittleEndian(int offset) => BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(offset, 2));
    private void WriteUInt16LittleEndian(int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(_data.AsSpan(offset, 2), value);
    private uint ReadUInt32BigEndian(int offset) => BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan(offset, 4));
    private void WriteUInt32BigEndian(int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(offset, 4), value);
}
