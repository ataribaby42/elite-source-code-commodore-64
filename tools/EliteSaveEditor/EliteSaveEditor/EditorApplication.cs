using System.Globalization;
using System.Text;
using EliteSaveEditor.Core;

namespace EliteSaveEditor;

internal sealed class EditorApplication
{
    private const string Title = "Elite C64 Commander Save Editor - Ataribaby 2026";

    private CommanderSave _commander = CommanderSave.CreateOriginalJameson();
    private string? _sourcePath;
    private bool _dirty;

    public int Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = Title;

        if (args.Length > 0)
        {
            LoadTap(Unquote(args[0]));
        }

        var selected = 0;
        while (true)
        {
            var choice = ConsoleUi.Select(
                Title,
                [
                    new("Load TAP..."),
                    new("Save TAP As..."),
                    new("Commander identity and format"),
                    new("Position, credits and fuel"),
                    new("Missions and presets"),
                    new("Ship and cargo"),
                    new("Equipment and weapons"),
                    new("Combat and legal status"),
                    new("Local market"),
                    new("Advanced values"),
                    new("Reset to original JAMESON"),
                    new("Exit")
                ],
                Summary(),
                selected: selected,
                allowCancel: false);

            selected = choice ?? selected;

            switch (choice)
            {
                case 0: LoadTapInteractive(); break;
                case 1: SaveTap(); break;
                case 2: IdentityMenu(); break;
                case 3: PositionMenu(); break;
                case 4: MissionMenu(); break;
                case 5: CargoMenu(); break;
                case 6: EquipmentMenu(); break;
                case 7: CombatMenu(); break;
                case 8: MarketMenu(); break;
                case 9: AdvancedMenu(); break;
                case 10: ResetCommander(); break;
                case 11:
                    if (!_dirty || ConsoleUi.Confirm("Exit", "Discard unsaved changes and exit?"))
                    {
                        return 0;
                    }
                    break;
            }
        }
    }

    private string Summary()
    {
        var system = GalaxyCatalog.FindNearest(_commander.Galaxy, _commander.SystemX, _commander.SystemY);
        var exact = system.X == _commander.SystemX && system.Y == _commander.SystemY ? "" : " (nearest)";
        var format = _commander.Format == CommanderFormat.EliteUnbound ? "Elite: Unbound" : "Original Elite";
        var ship = _commander.Format == CommanderFormat.EliteUnbound
            ? GameData.Ship(_commander.ShipType).Name
            : "Cobra Mk III";
        var dirty = _dirty ? "  [modified]" : "";

        return $"Commander {_commander.Name}  |  {format}  |  {ship}{dirty}\n" +
               $"Galaxy {_commander.Galaxy + 1}, {system.Name}{exact} " +
               $"({_commander.SystemX},{_commander.SystemY})  |  {FormatOneDecimal(_commander.CashTenths / 10m)} Cr";
    }

    private void LoadTapInteractive()
    {
        var initial = _sourcePath ?? string.Empty;
        var path = ConsoleUi.ReadText("Load TAP", "Enter a C64 TAP file path:", initial);
        if (path is not null)
        {
            LoadTap(Unquote(path));
        }
    }

    private void LoadTap(string path)
    {
        try
        {
            var positions = TapCodec.Read(path);
            var selected = 0;
            if (positions.Count > 1)
            {
                var choice = ConsoleUi.Select(
                    "Select Commander Position",
                    positions.Select((position, index) =>
                    {
                        var format = CommanderSave.DetectFormat(position.Data) switch
                        {
                            CommanderFormat.OriginalElite => "Original Elite",
                            CommanderFormat.EliteUnbound => "Elite: Unbound",
                            _ => "Unknown format"
                        };
                        return new MenuItem($"{index + 1,2}. {position.Name,-7}  {format}");
                    }).ToArray(),
                    $"{positions.Count} commander positions found in {Path.GetFileName(path)}.");

                if (choice is null)
                {
                    return;
                }

                selected = choice.Value;
            }

            var entry = positions[selected];
            var detected = CommanderSave.DetectFormat(entry.Data);
            if (detected is null)
            {
                var formatChoice = ConsoleUi.Select(
                    "Unknown Commander Format",
                    [new("Treat as Original Elite"), new("Treat as Elite: Unbound")],
                    "The internal Original Elite checksums and the Elite: Unbound registration fields are both invalid.\n" +
                    "Choose how the 77-byte commander block should be interpreted.");
                if (formatChoice is null)
                {
                    return;
                }

                detected = formatChoice == 0 ? CommanderFormat.OriginalElite : CommanderFormat.EliteUnbound;
            }

            _commander = new CommanderSave(entry.Name, entry.Data, detected.Value, entry.LoadAddress);
            _sourcePath = Path.GetFullPath(path);
            _dirty = false;

            var problems = _commander.Validate();
            var message = new List<string>
            {
                $"Loaded commander {_commander.Name} from {Path.GetFileName(path)}.",
                $"Format: {FormatName(_commander.Format)}"
            };
            if (problems.Count > 0)
            {
                message.Add(string.Empty);
                message.Add("The position contains values that must be corrected before saving:");
                message.AddRange(problems.Select(problem => $"- {problem}"));
            }

            ConsoleUi.Message("TAP Loaded", message.ToArray());
        }
        catch (Exception exception)
        {
            ConsoleUi.Message("Load Failed", exception.Message);
        }
    }

    private void SaveTap()
    {
        var problems = _commander.Validate();
        if (problems.Count > 0)
        {
            var lines = new[] { "Correct these values before saving:", "" }
                .Concat(problems.Select(problem => $"- {problem}"))
                .ToArray();
            ConsoleUi.Message("Cannot Save", lines);
            return;
        }

        var defaultDirectory = _sourcePath is null
            ? Environment.CurrentDirectory
            : Path.GetDirectoryName(_sourcePath) ?? Environment.CurrentDirectory;
        var defaultPath = Path.Combine(defaultDirectory, $"{_commander.Name}.tap");
        if (_sourcePath is not null && string.Equals(defaultPath, _sourcePath, StringComparison.OrdinalIgnoreCase))
        {
            defaultPath = Path.Combine(defaultDirectory, $"{_commander.Name}-edited.tap");
        }

        var entered = ConsoleUi.ReadText(
            "Save TAP As",
            "Enter a path for the new TAP file. The tape filename inside it is the commander name.",
            defaultPath);
        if (entered is null)
        {
            return;
        }

        var path = Path.GetFullPath(Unquote(entered));
        if (!string.Equals(Path.GetExtension(path), ".tap", StringComparison.OrdinalIgnoreCase))
        {
            path += ".tap";
        }

        if (File.Exists(path) && !ConsoleUi.Confirm("Overwrite TAP", $"{path}\n\nThe file already exists. Overwrite it?"))
        {
            return;
        }

        try
        {
            var parent = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            {
                throw new DirectoryNotFoundException("The destination directory does not exist.");
            }

            var data = _commander.ExportData();
            TapCodec.Write(path, [new TapCommanderFile(_commander.Name, _commander.LoadAddress, data)]);
            _sourcePath = path;
            _dirty = false;

            var checksumText = _commander.Format == CommanderFormat.OriginalElite
                ? $"Original checksums: {data[74]:X2} {data[75]:X2} {data[76]:X2}"
                : $"Registration ID: {_commander.RegistrationId}";
            ConsoleUi.Message(
                "TAP Saved",
                $"Saved commander {_commander.Name} to:",
                path,
                "",
                checksumText,
                "Both KERNAL tape copies and all tape XOR checksums were generated.");
        }
        catch (Exception exception)
        {
            ConsoleUi.Message("Save Failed", exception.Message);
        }
    }

    private void IdentityMenu()
    {
        var selected = 0;
        while (true)
        {
            var items = new List<MenuItem>
            {
                new($"Commander name: {_commander.Name}"),
                new($"Save format: {FormatName(_commander.Format)}"),
                new($"Competition/version flags: 0x{_commander.CompetitionFlags:X2} (read-only)")
            };

            if (_commander.Format == CommanderFormat.EliteUnbound)
            {
                items.Add(new($"Registration ID: {_commander.RegistrationId}"));
                items.Add(new($"Scrambled ID: {OnOff(_commander.RegistrationScrambled)}"));
            }
            else
            {
                var data = _commander.ExportData();
                items.Add(new($"Save count: {_commander.SaveCount}"));
                items.Add(new($"Checksums: {data[74]:X2} {data[75]:X2} {data[76]:X2} (automatic)"));
            }

            var choice = ConsoleUi.Select("Commander Identity and Format", items, Summary(), selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            if (choice == 0)
            {
                var name = ConsoleUi.ReadText("Commander Name", "Enter 1-7 characters. This becomes the C64 tape filename:", _commander.Name, 7);
                if (name is not null)
                {
                    TryChange(() => _commander.Name = CommanderSave.NormalizeName(name));
                }
            }
            else if (choice == 1)
            {
                ChangeFormat();
            }
            else if (choice == 2)
            {
                ConsoleUi.Message(
                    "Competition/Version Flags",
                    $"Stored value: 0x{_commander.CompetitionFlags:X2}",
                    "",
                    "This field is deliberately read-only. The editor preserves it exactly.");
            }
            else if (_commander.Format == CommanderFormat.EliteUnbound)
            {
                if (choice == 3)
                {
                    EditRegistration();
                }
                else if (choice == 4)
                {
                    _commander.RegistrationScrambled = !_commander.RegistrationScrambled;
                    _dirty = true;
                }
            }
            else
            {
                if (choice == 3)
                {
                    var value = ReadByte("Save Count", "Enter the original Elite save count:", _commander.SaveCount);
                    if (value is not null)
                    {
                        _commander.SaveCount = value.Value;
                        _dirty = true;
                    }
                }
                else if (choice == 4)
                {
                    var data = _commander.ExportData();
                    ConsoleUi.Message(
                        "Original Elite Checksums",
                        $"CHK2: 0x{data[74]:X2}",
                        $"CHK3: 0x{data[75]:X2}",
                        $"CHK:  0x{data[76]:X2}",
                        "",
                        "These values are read-only and are recalculated whenever the TAP is saved.");
                }
            }
        }
    }

    private void ChangeFormat()
    {
        var other = _commander.Format == CommanderFormat.OriginalElite
            ? CommanderFormat.EliteUnbound
            : CommanderFormat.OriginalElite;
        var note = other == CommanderFormat.EliteUnbound
            ? "The original checksum bytes will become registration JS-042, and the player ship will be Cobra Mk III."
            : "The registration bytes will become the original save count and recalculated commander checksums.";

        if (ConsoleUi.Confirm("Convert Save Format", $"Convert this position to {FormatName(other)}?\n\n{note}"))
        {
            _commander.ChangeFormat(other);
            _dirty = true;
        }
    }

    private void EditRegistration()
    {
        var letters = ConsoleUi.ReadText(
            "Registration ID",
            "Enter exactly two letters A-Z:",
            _commander.RegistrationLetters,
            2);
        if (letters is null)
        {
            return;
        }

        try
        {
            var normalized = letters.Trim().ToUpperInvariant();
            if (normalized.Length != 2 || normalized.Any(character => character is < 'A' or > 'Z'))
            {
                throw new ArgumentException("Registration must contain exactly two letters A-Z.");
            }

            var number = ReadByte("Registration ID", "Enter a registration number from 1 to 255:", _commander.RegistrationNumber, 1, 255);
            if (number is null)
            {
                return;
            }

            _commander.SetRegistrationLetters(normalized);
            _commander.RegistrationNumber = number.Value;
            _dirty = true;
        }
        catch (Exception exception)
        {
            ConsoleUi.Message("Invalid Registration", exception.Message);
        }
    }

    private void PositionMenu()
    {
        var selected = 0;
        while (true)
        {
            var nearest = GalaxyCatalog.FindNearest(_commander.Galaxy, _commander.SystemX, _commander.SystemY);
            var maxFuel = CurrentShip().FuelCapacityTenths;
            var choice = ConsoleUi.Select(
                "Position, Credits and Fuel",
                [
                    new($"Credits: {FormatOneDecimal(_commander.CashTenths / 10m)} Cr"),
                    new($"Galaxy: {_commander.Galaxy + 1}"),
                    new($"Current system: {nearest.Name} ({_commander.SystemX},{_commander.SystemY})"),
                    new($"System X coordinate: {_commander.SystemX}"),
                    new($"System Y coordinate: {_commander.SystemY}"),
                    new($"Fuel: {FormatOneDecimal(_commander.FuelTenths / 10m)} / {FormatOneDecimal(maxFuel / 10m)} LY")
                ],
                Summary(),
                selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            switch (choice)
            {
                case 0:
                    var cash = ReadCredits();
                    if (cash is not null) { _commander.CashTenths = cash.Value; _dirty = true; }
                    break;
                case 1:
                    var galaxy = SelectValue("Galaxy", Enumerable.Range(1, 8).Select(value => $"Galaxy {value}").ToArray(), _commander.Galaxy);
                    if (galaxy is not null) { _commander.SetGalaxy((byte)galaxy.Value); _dirty = true; }
                    break;
                case 2:
                    SelectSystem();
                    break;
                case 3:
                    var x = ReadByte("System X Coordinate", "Enter X from 0 to 255:", _commander.SystemX);
                    if (x is not null) { _commander.SystemX = x.Value; _dirty = true; }
                    break;
                case 4:
                    var y = ReadByte("System Y Coordinate", "Enter Y from 0 to 255:", _commander.SystemY);
                    if (y is not null) { _commander.SystemY = y.Value; _dirty = true; }
                    break;
                case 5:
                    var fuel = ReadByte("Fuel", $"Enter fuel in 0.1 LY units (0-{maxFuel}):", _commander.FuelTenths, 0, maxFuel);
                    if (fuel is not null) { _commander.FuelTenths = fuel.Value; _dirty = true; }
                    break;
            }
        }
    }

    private void SelectSystem()
    {
        var systems = GalaxyCatalog.Systems(_commander.Galaxy)
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Number)
            .ToArray();
        var current = GalaxyCatalog.FindNearest(_commander.Galaxy, _commander.SystemX, _commander.SystemY);
        var selected = Array.FindIndex(systems, system => system.Number == current.Number);
        var choice = ConsoleUi.Select(
            $"Select System - Galaxy {_commander.Galaxy + 1}",
            systems.Select(system => new MenuItem($"{system.Name,-10}  ({system.X,3},{system.Y,3})  #{system.Number}")).ToArray(),
            "Page Up/Down moves through the 256 systems.",
            Math.Max(selected, 0));
        if (choice is not null)
        {
            _commander.SetSystem(systems[choice.Value]);
            _dirty = true;
        }
    }

    private void MissionMenu()
    {
        var selected = 0;
        while (true)
        {
            var constrictor = _commander.MissionStatus & 0x03;
            var plans = (_commander.MissionStatus >> 2) & 0x03;
            var choice = ConsoleUi.Select(
                "Missions and Presets",
                [
                    new($"Constrictor status: {ConstrictorState(constrictor)}"),
                    new($"Thargoid Plans status: {PlansState(plans)}"),
                    new($"Trumble offer answered: {YesNo((_commander.MissionStatus & 0x10) != 0)}"),
                    new($"Trumble count: {_commander.TrumbleCount}"),
                    new("Preset: Mission 1 - Constrictor, Galaxy 1 start (Xeer)"),
                    new("Preset: Mission 1 - Constrictor, Galaxy 2 start (Errius)"),
                    new("Preset: Mission 2 - Thargoid Plans (Ceerdi)"),
                    new("Preset: Mission 3 - Trumbles offer")
                ],
                $"Raw mission byte TP: 0x{_commander.MissionStatus:X2}\n" +
                "Mission presets prepare their trigger for the next docking event.",
                selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            switch (choice)
            {
                case 0: EditConstrictorState(); break;
                case 1: EditPlansState(); break;
                case 2:
                    _commander.MissionStatus ^= 0x10;
                    _dirty = true;
                    break;
                case 3:
                    var trumbles = ReadUInt16("Trumble Count", "Enter a count from 0 to 65535:", _commander.TrumbleCount);
                    if (trumbles is not null)
                    {
                        TryCapacityChange(() => _commander.TrumbleCount = trumbles.Value);
                    }
                    break;
                case 4:
                    MissionPresets.ConstrictorGalaxyOne(_commander);
                    _dirty = true;
                    ConsoleUi.Message("Preset Applied", "Mission 1 will start on the next docking in Galaxy 1 at Xeer.", "Mission 2 progress was reset; kill points are at least 256.");
                    break;
                case 5:
                    MissionPresets.ConstrictorGalaxyTwo(_commander);
                    _dirty = true;
                    ConsoleUi.Message("Preset Applied", "Mission 1 will start on the next docking in Galaxy 2 at Errius.", "Mission 2 progress was reset; kill points are at least 256.");
                    break;
                case 6:
                    MissionPresets.ThargoidPlans(_commander);
                    _dirty = true;
                    ConsoleUi.Message("Preset Applied", "Mission 1 is complete. Mission 2 will start on the next docking in Galaxy 3 at Ceerdi.", "Kill points are at least 1280.");
                    break;
                case 7:
                    MissionPresets.Trumbles(_commander);
                    _dirty = true;
                    ConsoleUi.Message("Preset Applied", "Both story missions are marked complete and the Trumble offer is unanswered.", "The exact CASH+2 trigger byte is valid, so the offer will appear on the next docking.");
                    break;
            }
        }
    }

    private void EditConstrictorState()
    {
        byte[] values = [0, 1, 3, 2];
        string[] labels = ["Not started", "In progress", "Constrictor destroyed; debrief pending", "Complete"];
        var current = Array.IndexOf(values, (byte)(_commander.MissionStatus & 3));
        var selected = SelectValue("Constrictor Mission Status", labels, Math.Max(current, 0));
        if (selected is not null)
        {
            _commander.MissionStatus = (byte)((_commander.MissionStatus & 0xFC) | values[selected.Value]);
            _dirty = true;
        }
    }

    private void EditPlansState()
    {
        string[] labels = ["Not started", "Started; plans not collected", "Plans collected; carrying to Birera", "Complete"];
        var current = (_commander.MissionStatus >> 2) & 3;
        var selected = SelectValue("Thargoid Plans Mission Status", labels, current);
        if (selected is not null)
        {
            _commander.MissionStatus = (byte)((_commander.MissionStatus & 0xF3) | selected.Value << 2);
            _dirty = true;
        }
    }

    private void CargoMenu()
    {
        var selected = 0;
        while (true)
        {
            var items = new List<MenuItem>();
            if (_commander.Format == CommanderFormat.EliteUnbound)
            {
                items.Add(new($"Current ship: {CurrentShip().Name}"));
            }

            var cargoUsage = _commander.Format == CommanderFormat.EliteUnbound
                ? $"Cargo usage: {_commander.CargoUsedTonnes()}t cargo + {_commander.EquipmentWeightTonnes()}t equipment / {_commander.CargoCapacity()}t"
                : $"Cargo usage: {_commander.CargoUsedTonnes()}t / {_commander.CargoCapacity()}t";
            items.Add(new(cargoUsage));
            for (var commodity = 0; commodity < GameData.Commodities.Length; commodity++)
            {
                items.Add(new($"{GameData.Commodities[commodity]}: {_commander.Cargo(commodity)} {GameData.CommodityUnits[commodity]}"));
            }

            var choice = ConsoleUi.Select("Ship and Cargo", items, Summary(), selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            var offset = _commander.Format == CommanderFormat.EliteUnbound ? 2 : 1;
            if (_commander.Format == CommanderFormat.EliteUnbound && choice == 0)
            {
                ChangeShip();
            }
            else if (choice == offset - 1)
            {
                ShowCargoCapacity();
            }
            else
            {
                var commodity = choice.Value - offset;
                var value = ReadByte(
                    GameData.Commodities[commodity],
                    $"Enter quantity in {GameData.CommodityUnits[commodity]} (0-255):",
                    _commander.Cargo(commodity));
                if (value is not null)
                {
                    TryCapacityChange(() => _commander.SetCargo(commodity, value.Value));
                }
            }
        }
    }

    private void ShowCargoCapacity()
    {
        var lines = new List<string>
        {
            $"Cargo: {_commander.CargoUsedTonnes()}t"
        };

        if (_commander.Format == CommanderFormat.EliteUnbound)
        {
            lines.Add($"Installed equipment: {_commander.EquipmentWeightTonnes()}t");
        }

        lines.Add($"Capacity: {_commander.CargoCapacity()}t");
        lines.Add("");
        lines.Add(_commander.Format == CommanderFormat.EliteUnbound
            ? "Gold, platinum and gem-stones use smaller units. The current Unbound capacity routine also excludes Alien Items from its tonne total."
            : "Gold, platinum and gem-stones use smaller units.");

        ConsoleUi.Message("Cargo Capacity", lines.ToArray());
    }

    private void ChangeShip()
    {
        var selected = ConsoleUi.Select(
            "Select Elite: Unbound Ship",
            GameData.Ships.Select(ship => new MenuItem(
                $"{ship.Name,-14}  cargo {ship.StandardCargoCapacity}t  fuel {FormatOneDecimal(ship.FuelCapacityTenths / 10m)} LY  missiles {ship.MaximumMissiles}"))
                .ToArray(),
            "Changing ship clears all cargo, Trumbles, fitted equipment, lasers and missiles.",
            _commander.ShipType);
        if (selected is null || selected == _commander.ShipType)
        {
            return;
        }

        _commander.ChangeShip((byte)selected.Value);
        _dirty = true;
        ConsoleUi.Message(
            "Ship Changed",
            $"Current ship: {CurrentShip().Name}",
            "Cargo, Trumbles, equipment, lasers and missiles were cleared.",
            "Fuel was retained and clamped to the new ship's tank capacity.");
    }

    private void EquipmentMenu()
    {
        var selected = 0;
        while (true)
        {
            var bombName = _commander.Format == CommanderFormat.EliteUnbound ? "I.F.F. Unit" : "Energy Bomb";
            var holdSummary = _commander.Format == CommanderFormat.EliteUnbound
                ? $"Equipment weight: {_commander.EquipmentWeightTonnes()}t  |  Total hold use: " +
                  $"{_commander.CargoUsedTonnes() + _commander.EquipmentWeightTonnes()}t / {_commander.CargoCapacity()}t"
                : $"Cargo hold use: {_commander.CargoUsedTonnes()}t / {_commander.CargoCapacity()}t";
            var choice = ConsoleUi.Select(
                "Equipment and Weapons",
                [
                    new($"Large Cargo Bay: {OnOff(_commander.HasLargeCargoBay)}"),
                    new($"Front Laser: {GameData.FormatLaser(_commander.Laser(0))}"),
                    new($"Rear Laser: {GameData.FormatLaser(_commander.Laser(1))}"),
                    new($"Left Laser: {GameData.FormatLaser(_commander.Laser(2))}"),
                    new($"Right Laser: {GameData.FormatLaser(_commander.Laser(3))}"),
                    new($"Missiles: {_commander.Missiles} / {CurrentShip().MaximumMissiles}"),
                    new($"E.C.M. System: {OnOff(_commander.HasEcm)}"),
                    new($"Fuel Scoops: {OnOff(_commander.HasFuelScoops)}"),
                    new($"{bombName}: {OnOff(_commander.HasBombSlotEquipment)}"),
                    new($"Energy Unit: {EnergyUnitName(_commander.EnergyUnit)}"),
                    new($"Docking Computer: {OnOff(_commander.HasDockingComputer)}"),
                    new($"Galactic Hyperdrive: {OnOff(_commander.HasGalacticHyperdrive)}"),
                    new($"Escape Capsule: {OnOff(_commander.HasEscapeCapsule)}")
                ],
                holdSummary,
                selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            switch (choice)
            {
                case 0: TryCapacityChange(() => _commander.HasLargeCargoBay = !_commander.HasLargeCargoBay); break;
                case >= 1 and <= 4: EditLaser(choice.Value - 1); break;
                case 5:
                    var missiles = ReadByte("Missiles", $"Enter missile count (0-{CurrentShip().MaximumMissiles}):", _commander.Missiles, 0, CurrentShip().MaximumMissiles);
                    if (missiles is not null) { _commander.Missiles = missiles.Value; _dirty = true; }
                    break;
                case 6: TryCapacityChange(() => _commander.HasEcm = !_commander.HasEcm); break;
                case 7: TryCapacityChange(() => _commander.HasFuelScoops = !_commander.HasFuelScoops); break;
                case 8: TryCapacityChange(() => _commander.HasBombSlotEquipment = !_commander.HasBombSlotEquipment); break;
                case 9: EditEnergyUnit(); break;
                case 10: TryCapacityChange(() => _commander.HasDockingComputer = !_commander.HasDockingComputer); break;
                case 11: TryCapacityChange(() => _commander.HasGalacticHyperdrive = !_commander.HasGalacticHyperdrive); break;
                case 12: TryCapacityChange(() => _commander.HasEscapeCapsule = !_commander.HasEscapeCapsule); break;
            }
        }
    }

    private void EditLaser(int mount)
    {
        var mountName = new[] { "Front", "Rear", "Left", "Right" }[mount];
        var types = Enum.GetValues<LaserType>();
        var current = Array.IndexOf(types, (LaserType)_commander.Laser(mount));
        var selected = ConsoleUi.Select(
            $"{mountName} Laser",
            types.Select(type => new MenuItem(GameData.FormatLaser((byte)type))).ToArray(),
            $"The {CurrentShip().Name} has {CurrentShip().LaserMounts} laser mount(s).",
            Math.Max(current, 0));
        if (selected is null)
        {
            return;
        }

        var type = types[selected.Value];
        var supported = mount == 0 || mount == 1 && CurrentShip().LaserMounts >= 2 || mount >= 2 && CurrentShip().LaserMounts >= 4;
        if (type != LaserType.None && !supported)
        {
            ConsoleUi.Message("Unsupported Laser Mount", $"The {CurrentShip().Name} does not have a {mountName.ToLowerInvariant()} laser mount.");
            return;
        }

        TryCapacityChange(() => _commander.SetLaser(mount, type));
    }

    private void EditEnergyUnit()
    {
        var values = Enum.GetValues<EnergyUnitType>();
        var current = Array.IndexOf(values, _commander.EnergyUnit);
        var selected = ConsoleUi.Select(
            "Energy Unit",
            values.Select(value => new MenuItem(EnergyUnitName(value))).ToArray(),
            selected: Math.Max(current, 0));
        if (selected is not null)
        {
            TryCapacityChange(() => _commander.EnergyUnit = values[selected.Value]);
        }
    }

    private void CombatMenu()
    {
        var selected = 0;
        while (true)
        {
            var choice = ConsoleUi.Select(
                "Combat and Legal Status",
                [
                    new($"Integer kill points: {_commander.KillPoints}"),
                    new($"Fractional kill points: {_commander.FractionalKillPoints} / 256"),
                    new($"Legal status: {_commander.LegalStatus} ({LegalName(_commander.LegalStatus)})")
                ],
                $"Combat rating: {GameData.CombatRating(_commander.KillPoints)}\n\n" +
                "Mission thresholds use integer kill points: 256 for Constrictor and 1280 for Thargoid Plans.",
                selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            if (choice == 0)
            {
                var kills = ReadUInt16("Integer Kill Points", "Enter a value from 0 to 65535:", _commander.KillPoints);
                if (kills is not null) { _commander.KillPoints = kills.Value; _dirty = true; }
            }
            else if (choice == 1)
            {
                var fraction = ReadByte("Fractional Kill Points", "Enter a value from 0 to 255:", _commander.FractionalKillPoints);
                if (fraction is not null) { _commander.FractionalKillPoints = fraction.Value; _dirty = true; }
            }
            else
            {
                var legal = ReadByte("Legal Status", "Enter 0 for Clean, 1-49 for Offender, or 50-255 for Fugitive:", _commander.LegalStatus);
                if (legal is not null) { _commander.LegalStatus = legal.Value; _dirty = true; }
            }
        }
    }

    private void MarketMenu()
    {
        var selected = 0;
        while (true)
        {
            var system = GalaxyCatalog.FindNearest(
                _commander.GalaxySeedWord(0),
                _commander.GalaxySeedWord(1),
                _commander.GalaxySeedWord(2),
                _commander.SystemX,
                _commander.SystemY);
            var items = new List<MenuItem>
            {
                new($"Market price randomizer: {_commander.MarketPriceRandomizer}", BlankLineAfter: true)
            };
            items.AddRange(GameData.Commodities
                .Select((commodity, index) =>
                {
                    var unit = GameData.CommodityUnits[index];
                    var price = GameData.MarketPriceTenths(index, system.Economy, _commander.MarketPriceRandomizer);
                    return new MenuItem(
                        $"{commodity}: {_commander.MarketAvailability(index)} {unit} available  |  " +
                        $"{FormatOneDecimal(price / 10m)} Cr/{unit}");
                }));

            var choice = ConsoleUi.Select(
                "Local Market",
                items,
                $"Prices use {system.Name}'s economy and the saved price-randomization byte, matching the C64 market routine.",
                selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            if (choice == 0)
            {
                var value = ReadByte("Market Price Randomizer", "Enter a value from 0 to 255:", _commander.MarketPriceRandomizer);
                if (value is not null) { _commander.MarketPriceRandomizer = value.Value; _dirty = true; }
            }
            else
            {
                var commodity = choice.Value - 1;
                var value = ReadByte(
                    GameData.Commodities[commodity],
                    "Enter market availability from 0 to 255:",
                    _commander.MarketAvailability(commodity));
                if (value is not null)
                {
                    _commander.SetMarketAvailability(commodity, value.Value);
                    _dirty = true;
                }
            }
        }
    }

    private void AdvancedMenu()
    {
        var selected = 0;
        while (true)
        {
            var data = _commander.ExportData();
            var tapeXor = data.Aggregate((byte)0, (current, value) => (byte)(current ^ value));
            var items = new List<MenuItem>
            {
                new($"Galaxy seed s0: 0x{_commander.GalaxySeedWord(0):X4}"),
                new($"Galaxy seed s1: 0x{_commander.GalaxySeedWord(1):X4}"),
                new($"Galaxy seed s2: 0x{_commander.GalaxySeedWord(2):X4}"),
                new($"Unused commander byte #20: {_commander.Raw(20)}"),
                new($"Unused commander byte #47: {_commander.Raw(47)}"),
                new($"TAP load address: 0x{_commander.LoadAddress:X4}"),
                new($"Competition/version flags #14: 0x{_commander.CompetitionFlags:X2} (read-only)"),
                new($"Tape data XOR checksum: 0x{tapeXor:X2} (automatic)"),
                new("View raw 77-byte commander block")
            };
            if (_commander.Format == CommanderFormat.OriginalElite)
            {
                items.Insert(8, new($"Original checksums #74-#76: {data[74]:X2} {data[75]:X2} {data[76]:X2} (automatic)"));
            }

            var choice = ConsoleUi.Select(
                "Advanced Values",
                items,
                "Decimal numbers and values prefixed with 0x are accepted.",
                selected);
            if (choice is null)
            {
                return;
            }

            selected = choice.Value;

            if (choice <= 2)
            {
                var word = ReadUInt16(
                    $"Galaxy Seed s{choice}",
                    "Enter a 16-bit value in decimal or with a 0x prefix:",
                    _commander.GalaxySeedWord(choice.Value),
                    hexadecimalInitial: true);
                if (word is not null) { _commander.SetGalaxySeedWord(choice.Value, word.Value); _dirty = true; }
            }
            else if (choice is 3 or 4)
            {
                var offset = choice == 3 ? 20 : 47;
                var value = ReadByte($"Commander Byte #{offset}", "Enter a value from 0 to 255:", _commander.Raw(offset));
                if (value is not null) { _commander.SetRaw(offset, value.Value); _dirty = true; }
            }
            else if (choice == 5)
            {
                var address = ReadUInt16("TAP Load Address", "Enter a 16-bit address in decimal or with a 0x prefix:", _commander.LoadAddress, hexadecimalInitial: true);
                if (address is not null) { _commander.LoadAddress = address.Value; _dirty = true; }
            }
            else if (choice == items.Count - 1)
            {
                ShowRawData(data);
            }
            else
            {
                ConsoleUi.Message(
                    "Read-Only Value",
                    items[choice.Value].Label,
                    "",
                    "This value is preserved or recalculated automatically and cannot be edited.");
            }
        }
    }

    private void ShowRawData(byte[] data)
    {
        var lines = new List<string>();
        for (var offset = 0; offset < data.Length; offset += 16)
        {
            var count = Math.Min(16, data.Length - offset);
            lines.Add($"{offset:X2}: {Convert.ToHexString(data, offset, count).Replace("-", " ")}");
        }

        ConsoleUi.Message("Raw Commander Block", lines.ToArray());
    }

    private void ResetCommander()
    {
        if (_dirty && !ConsoleUi.Confirm("Reset Commander", "Discard all current changes and restore the original Elite JAMESON starting position?"))
        {
            return;
        }

        _commander = CommanderSave.CreateOriginalJameson();
        _sourcePath = null;
        _dirty = false;
        ConsoleUi.Message("Commander Reset", "Original Elite JAMESON restored:", "Cobra Mk III, Lave, 100.0 Cr, 7.0 LY fuel and three missiles.");
    }

    private void TryCapacityChange(Action change)
    {
        var before = _commander.Clone();
        change();

        var used = _commander.CargoUsedTonnes() + _commander.EquipmentWeightTonnes();
        if (used > _commander.CargoCapacity())
        {
            _commander = before;
            ConsoleUi.Message(
                "Cargo Capacity Exceeded",
                $"The change would use {used}t, but the ship has only {_commander.CargoCapacity()}t of capacity.");
            return;
        }

        _dirty = true;
    }

    private void TryChange(Action change)
    {
        try
        {
            change();
            _dirty = true;
        }
        catch (Exception exception)
        {
            ConsoleUi.Message("Invalid Value", exception.Message);
        }
    }

    private uint? ReadCredits()
    {
        while (true)
        {
            var input = ConsoleUi.ReadText(
                "Credits",
                $"Enter credits from 0.0 to {FormatOneDecimal(uint.MaxValue / 10m)}, with at most one decimal place:",
                (_commander.CashTenths / 10m).ToString("0.0", CultureInfo.InvariantCulture),
                20);
            if (input is null)
            {
                return null;
            }

            if (decimal.TryParse(input.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var credits) &&
                credits >= 0 && credits * 10 <= uint.MaxValue && decimal.Truncate(credits * 10) == credits * 10)
            {
                return (uint)(credits * 10);
            }

            ConsoleUi.Message("Invalid Credits", "Use a value in range with no more than one decimal place, for example 100.0.");
        }
    }

    private static byte? ReadByte(string title, string prompt, byte current, byte minimum = 0, byte maximum = byte.MaxValue)
    {
        while (true)
        {
            var input = ConsoleUi.ReadText(title, prompt, current.ToString(CultureInfo.InvariantCulture), 8);
            if (input is null)
            {
                return null;
            }

            if (TryParseUnsigned(input, out var value) && value >= minimum && value <= maximum)
            {
                return (byte)value;
            }

            ConsoleUi.Message("Invalid Number", $"Enter a value from {minimum} to {maximum}.");
        }
    }

    private static ushort? ReadUInt16(string title, string prompt, ushort current, bool hexadecimalInitial = false)
    {
        while (true)
        {
            var initial = hexadecimalInitial ? $"0x{current:X4}" : current.ToString(CultureInfo.InvariantCulture);
            var input = ConsoleUi.ReadText(title, prompt, initial, 10);
            if (input is null)
            {
                return null;
            }

            if (TryParseUnsigned(input, out var value) && value <= ushort.MaxValue)
            {
                return (ushort)value;
            }

            ConsoleUi.Message("Invalid Number", "Enter a value from 0 to 65535.");
        }
    }

    private static bool TryParseUnsigned(string input, out uint value)
    {
        input = input.Trim();
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(input[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        return uint.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static int? SelectValue(string title, IReadOnlyList<string> labels, int selected)
    {
        return ConsoleUi.Select(title, labels.Select(label => new MenuItem(label)).ToArray(), selected: selected);
    }

    private ShipDefinition CurrentShip() =>
        _commander.Format == CommanderFormat.EliteUnbound ? GameData.Ship(_commander.ShipType) : GameData.Ships[0];

    private static string FormatName(CommanderFormat format) =>
        format == CommanderFormat.EliteUnbound ? "Elite: Unbound" : "Original Elite";

    private static string ConstrictorState(int value) => value switch
    {
        0 => "Not started",
        1 => "In progress",
        3 => "Constrictor destroyed; debrief pending",
        2 => "Complete",
        _ => "Unknown"
    };

    private static string PlansState(int value) => value switch
    {
        0 => "Not started",
        1 => "Started; plans not collected",
        2 => "Plans collected; carrying to Birera",
        3 => "Complete",
        _ => "Unknown"
    };

    private static string EnergyUnitName(EnergyUnitType value) => value switch
    {
        EnergyUnitType.None => "None",
        EnergyUnitType.ExtraEnergyUnit => "Extra Energy Unit",
        EnergyUnitType.NavalEnergyUnit => "Naval Energy Unit",
        _ => $"Unknown ({(byte)value})"
    };

    private static string LegalName(byte value) => value switch
    {
        0 => "Clean",
        < 50 => "Offender",
        _ => "Fugitive"
    };

    private static string OnOff(bool value) => value ? "On" : "Off";
    private static string YesNo(bool value) => value ? "Yes" : "No";
    private static string FormatOneDecimal(decimal value) => value.ToString("N1", CultureInfo.InvariantCulture);
    private static string Unquote(string value) => value.Trim().Trim('"');
}
