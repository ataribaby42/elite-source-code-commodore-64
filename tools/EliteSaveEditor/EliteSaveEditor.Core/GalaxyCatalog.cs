namespace EliteSaveEditor.Core;

public sealed record EliteSystem(byte Number, string Name, byte X, byte Y, byte Economy);

public static class GalaxyCatalog
{
    private const string NamePairs =
        "ALLEXEGEZACEBISOUSESARMAINDIREA.ERATENBERALAVETIEDORQUANTEISRION";

    private static readonly ushort[] GalaxyOneSeed = [0x5A4A, 0x0248, 0xB753];
    private static readonly IReadOnlyList<EliteSystem>[] Galaxies = BuildGalaxies();

    public static IReadOnlyList<EliteSystem> Systems(byte galaxy) =>
        Galaxies[Math.Min(galaxy, (byte)7)];

    public static EliteSystem? FindByName(byte galaxy, string name) =>
        Systems(galaxy).FirstOrDefault(system =>
            string.Equals(system.Name, name, StringComparison.OrdinalIgnoreCase));

    public static EliteSystem FindNearest(byte galaxy, byte x, byte y)
        => FindNearest(Systems(galaxy), x, y);

    public static EliteSystem FindNearest(ushort seed0, ushort seed1, ushort seed2, byte x, byte y)
        => FindNearest(BuildSystems([seed0, seed1, seed2]), x, y);

    private static EliteSystem FindNearest(IReadOnlyList<EliteSystem> systems, byte x, byte y)
    {
        EliteSystem? best = null;
        var bestDistance = int.MaxValue;

        foreach (var system in systems)
        {
            // This is the same comparison metric used by Elite's TT111 routine.
            var distance = Math.Abs(system.X - x) / 2 + Math.Abs(system.Y - y) / 2;
            if (distance >= bestDistance)
            {
                continue;
            }

            best = system;
            bestDistance = distance;
        }

        return best!;
    }

    public static byte[] SeedBytes(byte galaxy)
    {
        var seed = (ushort[])GalaxyOneSeed.Clone();
        for (var i = 0; i < galaxy; i++)
        {
            for (var word = 0; word < seed.Length; word++)
            {
                var low = RotateByteLeft((byte)seed[word]);
                var high = RotateByteLeft((byte)(seed[word] >> 8));
                seed[word] = (ushort)(low | (high << 8));
            }
        }

        return
        [
            (byte)seed[0], (byte)(seed[0] >> 8),
            (byte)seed[1], (byte)(seed[1] >> 8),
            (byte)seed[2], (byte)(seed[2] >> 8)
        ];
    }

    private static IReadOnlyList<EliteSystem>[] BuildGalaxies()
    {
        var result = new IReadOnlyList<EliteSystem>[8];
        for (byte galaxy = 0; galaxy < 8; galaxy++)
        {
            var bytes = SeedBytes(galaxy);
            var seed = new ushort[]
            {
                (ushort)(bytes[0] | (bytes[1] << 8)),
                (ushort)(bytes[2] | (bytes[3] << 8)),
                (ushort)(bytes[4] | (bytes[5] << 8))
            };

            result[galaxy] = BuildSystems(seed);
        }

        return result;
    }

    private static IReadOnlyList<EliteSystem> BuildSystems(ushort[] initialSeed)
    {
        var seed = (ushort[])initialSeed.Clone();
        var systems = new List<EliteSystem>(256);
        for (var number = 0; number < 256; number++)
        {
            var government = (byte)(((byte)seed[1] >> 3) & 7);
            var economy = (byte)((seed[0] >> 8) & 7);
            if (government <= 1)
            {
                economy |= 2;
            }

            systems.Add(new EliteSystem(
                (byte)number,
                GenerateName(seed),
                (byte)(seed[1] >> 8),
                (byte)(seed[0] >> 8),
                economy));

            for (var twist = 0; twist < 4; twist++)
            {
                Twist(seed);
            }
        }

        return systems;
    }

    private static string GenerateName(ushort[] source)
    {
        var seed = (ushort[])source.Clone();
        var pairCount = (seed[0] & 0x40) != 0 ? 4 : 3;
        var name = new System.Text.StringBuilder(8);

        for (var i = 0; i < pairCount; i++)
        {
            var pair = (seed[2] >> 8) & 0x1F;
            if (pair != 0)
            {
                name.Append(NamePairs[pair * 2]);
                var second = NamePairs[pair * 2 + 1];
                if (second != '.')
                {
                    name.Append(second);
                }
            }

            Twist(seed);
        }

        return name.ToString();
    }

    private static void Twist(ushort[] seed)
    {
        var next = (ushort)(seed[0] + seed[1] + seed[2]);
        seed[0] = seed[1];
        seed[1] = seed[2];
        seed[2] = next;
    }

    private static byte RotateByteLeft(byte value) =>
        (byte)((value << 1) | (value >> 7));
}
