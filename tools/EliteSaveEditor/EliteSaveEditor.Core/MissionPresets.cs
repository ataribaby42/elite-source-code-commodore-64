namespace EliteSaveEditor.Core;

public static class MissionPresets
{
    public static void ConstrictorGalaxyOne(CommanderSave commander) =>
        PrepareConstrictor(commander, 0, "Xeer");

    public static void ConstrictorGalaxyTwo(CommanderSave commander) =>
        PrepareConstrictor(commander, 1, "Errius");

    public static void ThargoidPlans(CommanderSave commander)
    {
        commander.MissionStatus = (byte)((commander.MissionStatus & 0xF0) | 0x02);
        commander.KillPoints = Math.Max(commander.KillPoints, (ushort)0x0500);
        commander.SetGalaxy(2);
        commander.SetSystem(RequiredSystem(2, "Ceerdi"));
    }

    public static void Trumbles(CommanderSave commander)
    {
        // Story mission events are checked before the Trumble offer. Mark both
        // story missions complete and clear the independent offer-answer bit so
        // the offer is the next docking event.
        commander.MissionStatus = (byte)((commander.MissionStatus & 0xE0) | 0x0E);
        commander.TrumbleCount = 0;

        // The original code checks only CASH+2, not the full 32-bit balance.
        var cash = commander.CashTenths;
        var testedByte = (byte)(cash >> 8);
        if (testedByte < 0xC4)
        {
            commander.CashTenths = (cash & 0xFFFF00FFu) | 0x0000C400u;
        }
    }

    private static void PrepareConstrictor(CommanderSave commander, byte galaxy, string systemName)
    {
        commander.MissionStatus &= 0xF0;
        commander.KillPoints = Math.Max(commander.KillPoints, (ushort)0x0100);
        commander.SetGalaxy(galaxy);
        commander.SetSystem(RequiredSystem(galaxy, systemName));
    }

    private static EliteSystem RequiredSystem(byte galaxy, string name) =>
        GalaxyCatalog.FindByName(galaxy, name) ??
        throw new InvalidOperationException($"System {name} was not generated in galaxy {galaxy + 1}.");
}
