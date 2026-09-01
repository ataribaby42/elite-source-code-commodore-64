namespace EliteSaveEditor.Core;

public readonly record struct OriginalChecksums(byte Chk2, byte Chk3, byte Chk);

public static class CommanderChecksums
{
    // NT% is the highest commander offset ($4C = 76), so NT%-3 is 73.
    // The loops therefore cover data bytes #0 through #73; #74-#76 hold the
    // checksum values themselves.
    private const int LastProtectedIndex = 73;

    public static OriginalChecksums Calculate(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != CommanderSave.DataLength)
        {
            throw new ArgumentException("Commander data must contain exactly 77 bytes.", nameof(data));
        }

        var checksum = CalculatePrimary(data);
        var checksum3 = CalculateThird(data);
        return new OriginalChecksums((byte)(checksum ^ 0xA9), checksum3, checksum);
    }

    public static void Apply(byte[] data)
    {
        var checksums = Calculate(data);
        data[74] = checksums.Chk2;
        data[75] = checksums.Chk3;
        data[76] = checksums.Chk;
    }

    public static bool IsValid(byte[] data)
    {
        if (data.Length != CommanderSave.DataLength)
        {
            return false;
        }

        var primary = CalculatePrimary(data);
        return data[76] == primary &&
               data[74] == (byte)(primary ^ 0xA9) &&
               data[75] == CalculateThird(data);
    }

    private static byte CalculatePrimary(byte[] data)
    {
        var accumulator = LastProtectedIndex;
        var carry = 0;

        for (var x = LastProtectedIndex; x > 0; x--)
        {
            var sum = accumulator + data[x - 1] + carry;
            accumulator = (byte)sum;
            carry = sum > byte.MaxValue ? 1 : 0;
            accumulator ^= data[x];
        }

        return (byte)accumulator;
    }

    private static byte CalculateThird(byte[] data)
    {
        var accumulator = LastProtectedIndex;
        var carry = 0;

        for (var x = LastProtectedIndex; x > 0; x--)
        {
            accumulator ^= x;
            var newCarry = accumulator & 1;
            accumulator = (accumulator >> 1) | (carry << 7);
            carry = newCarry;

            var sum = accumulator + data[x - 1] + carry;
            accumulator = (byte)sum;
            carry = sum > byte.MaxValue ? 1 : 0;
            accumulator ^= data[x];
        }

        return (byte)accumulator;
    }
}
