using System.Buffers.Binary;
using System.Text;

namespace EliteSaveEditor.Core;

public sealed record TapCommanderFile(string Name, ushort LoadAddress, byte[] Data);

public static class TapCodec
{
    private static readonly byte[] Magic = "C64-TAPE-RAW"u8.ToArray();
    private static readonly byte[] PrimaryCountdown = [0x89, 0x88, 0x87, 0x86, 0x85, 0x84, 0x83, 0x82, 0x81];
    private static readonly byte[] BackupCountdown = [9, 8, 7, 6, 5, 4, 3, 2, 1];

    private const byte RomShort = 0x2D;
    private const byte RomMedium = 0x41;
    private const byte RomLong = 0x55;

    public static IReadOnlyList<TapCommanderFile> Read(string path) => Read(File.ReadAllBytes(path));

    public static IReadOnlyList<TapCommanderFile> Read(ReadOnlySpan<byte> tap)
    {
        if (tap.Length < 20 || !tap[..12].SequenceEqual(Magic))
        {
            throw new InvalidDataException("The file is not a C64 TAP image.");
        }

        var version = tap[12];
        if (version > 2)
        {
            throw new InvalidDataException($"Unsupported TAP version {version}.");
        }

        var declaredLength = BinaryPrimitives.ReadUInt32LittleEndian(tap.Slice(16, 4));
        if (declaredLength > tap.Length - 20)
        {
            throw new InvalidDataException("The TAP payload is truncated.");
        }

        var pulses = DecodePulses(tap.Slice(20, checked((int)declaredLength)), version);
        var candidates = FindCountdowns(pulses);
        var headers = new List<HeaderRecord>();

        foreach (var candidate in candidates)
        {
            if (!TryReadRecord(pulses, candidate.Position, 192, out var payload, out var endPosition))
            {
                continue;
            }

            var start = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(1, 2));
            var end = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(3, 2));
            if (payload[0] is not (1 or 3) || end - start != CommanderSave.DataLength)
            {
                continue;
            }

            headers.Add(new HeaderRecord(candidate.Position, endPosition, candidate.Primary, payload));
        }

        var groups = GroupHeaders(headers);
        var files = new List<TapCommanderFile>();
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            var group = groups[groupIndex];
            var boundary = groupIndex + 1 < groups.Count ? groups[groupIndex + 1].FirstPosition : pulses.Count;
            var dataCandidates = candidates
                .Where(candidate => candidate.Position > group.LastPosition && candidate.Position < boundary)
                .OrderByDescending(candidate => candidate.Primary)
                .ThenBy(candidate => candidate.Position);

            byte[]? commanderData = null;
            foreach (var candidate in dataCandidates)
            {
                if (TryReadRecord(pulses, candidate.Position, CommanderSave.DataLength, out var payload, out _))
                {
                    commanderData = payload;
                    break;
                }
            }

            if (commanderData is null)
            {
                continue;
            }

            var header = group.Header;
            var name = DecodeFilename(header.AsSpan(5, 16));
            var loadAddress = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(1, 2));
            files.Add(new TapCommanderFile(name, loadAddress, commanderData));
        }

        if (files.Count == 0)
        {
            throw new InvalidDataException("No valid 77-byte Elite commander position was found in the TAP image.");
        }

        return files;
    }

    public static void Write(string path, IEnumerable<TapCommanderFile> files) =>
        File.WriteAllBytes(path, Write(files));

    public static byte[] Write(IEnumerable<TapCommanderFile> files)
    {
        var pulses = new List<byte>();
        var count = 0;
        foreach (var file in files)
        {
            if (file.Data.Length != CommanderSave.DataLength)
            {
                throw new ArgumentException("Each commander position must contain exactly 77 bytes.", nameof(files));
            }

            AppendStandardFile(pulses, file);
            count++;
        }

        if (count == 0)
        {
            throw new ArgumentException("At least one commander position is required.", nameof(files));
        }

        var result = new byte[20 + pulses.Count];
        Magic.CopyTo(result, 0);
        result[12] = 1;
        result[13] = 0; // C64
        result[14] = 0; // PAL/NTSC neutral
        result[15] = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16, 4), (uint)pulses.Count);
        pulses.CopyTo(result, 20);
        return result;
    }

    private static void AppendStandardFile(List<byte> output, TapCommanderFile file)
    {
        var header = Enumerable.Repeat((byte)0x20, 192).ToArray();
        header[0] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(1, 2), file.LoadAddress);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(3, 2), (ushort)(file.LoadAddress + file.Data.Length));
        EncodeFilename(file.Name).CopyTo(header, 5);

        Repeat(output, RomShort, 27135);
        AppendRecord(output, header, true, true);
        Repeat(output, RomShort, 79);
        AppendRecord(output, header, false, false);
        Repeat(output, RomShort, 5671);
        AppendRecord(output, file.Data, true, true);
        Repeat(output, RomShort, 79);
        AppendRecord(output, file.Data, false, true);
    }

    private static void AppendRecord(List<byte> output, byte[] payload, bool primary, bool endMarker)
    {
        var countdown = primary ? PrimaryCountdown : BackupCountdown;
        foreach (var value in countdown)
        {
            AppendRomByte(output, value);
        }

        foreach (var value in payload)
        {
            AppendRomByte(output, value);
        }

        AppendRomByte(output, Xor(payload));
        if (endMarker)
        {
            output.Add(RomLong);
            output.Add(RomShort);
        }
    }

    private static void AppendRomByte(List<byte> output, byte value)
    {
        output.Add(RomLong);
        output.Add(RomMedium);
        var parity = 1;
        for (var bit = 0; bit < 8; bit++)
        {
            var set = (value >> bit) & 1;
            AppendRomBit(output, set);
            parity ^= set;
        }

        AppendRomBit(output, parity);
    }

    private static void AppendRomBit(List<byte> output, int bit)
    {
        if (bit == 0)
        {
            output.Add(RomShort);
            output.Add(RomMedium);
        }
        else
        {
            output.Add(RomMedium);
            output.Add(RomShort);
        }
    }

    private static List<int> DecodePulses(ReadOnlySpan<byte> payload, byte version)
    {
        var pulses = new List<int>(payload.Length);
        for (var offset = 0; offset < payload.Length; offset++)
        {
            var value = payload[offset];
            if (value != 0)
            {
                pulses.Add(value * 8);
                continue;
            }

            if (version == 0)
            {
                pulses.Add(2048);
                continue;
            }

            if (offset + 3 >= payload.Length)
            {
                throw new InvalidDataException("The TAP image ends inside an extended pulse.");
            }

            pulses.Add(payload[offset + 1] | payload[offset + 2] << 8 | payload[offset + 3] << 16);
            offset += 3;
        }

        return pulses;
    }

    private static List<CountdownRecord> FindCountdowns(IReadOnlyList<int> pulses)
    {
        var records = new List<CountdownRecord>();
        for (var position = 0; position + 180 < pulses.Count; position++)
        {
            if (Classify(pulses[position]) != PulseKind.Long || Classify(pulses[position + 1]) != PulseKind.Medium)
            {
                continue;
            }

            if (!TryDecodeBytes(pulses, position, 9, out var countdown, out _))
            {
                continue;
            }

            if (countdown.SequenceEqual(PrimaryCountdown))
            {
                records.Add(new CountdownRecord(position, true));
            }
            else if (countdown.SequenceEqual(BackupCountdown))
            {
                records.Add(new CountdownRecord(position, false));
            }
        }

        return records;
    }

    private static bool TryReadRecord(
        IReadOnlyList<int> pulses,
        int position,
        int payloadLength,
        out byte[] payload,
        out int endPosition)
    {
        payload = [];
        endPosition = position;
        if (!TryDecodeBytes(pulses, position, 9 + payloadLength + 1, out var record, out endPosition))
        {
            return false;
        }

        payload = record.AsSpan(9, payloadLength).ToArray();
        return Xor(payload) == record[^1];
    }

    private static bool TryDecodeBytes(
        IReadOnlyList<int> pulses,
        int position,
        int count,
        out byte[] bytes,
        out int endPosition)
    {
        bytes = new byte[count];
        endPosition = position;
        for (var index = 0; index < count; index++)
        {
            if (!TryDecodeByte(pulses, endPosition, out bytes[index], out endPosition))
            {
                bytes = [];
                return false;
            }
        }

        return true;
    }

    private static bool TryDecodeByte(
        IReadOnlyList<int> pulses,
        int position,
        out byte value,
        out int endPosition)
    {
        value = 0;
        endPosition = position;
        if (position + 20 > pulses.Count ||
            Classify(pulses[position]) != PulseKind.Long ||
            Classify(pulses[position + 1]) != PulseKind.Medium)
        {
            return false;
        }

        var parity = 1;
        var pulse = position + 2;
        for (var bit = 0; bit < 8; bit++)
        {
            if (!TryDecodeBit(pulses, pulse, out var set))
            {
                return false;
            }

            value |= (byte)(set << bit);
            parity ^= set;
            pulse += 2;
        }

        if (!TryDecodeBit(pulses, pulse, out var parityOnTape) || parityOnTape != parity)
        {
            return false;
        }

        endPosition = pulse + 2;
        return true;
    }

    private static bool TryDecodeBit(IReadOnlyList<int> pulses, int position, out int bit)
    {
        bit = 0;
        if (position + 2 > pulses.Count)
        {
            return false;
        }

        var first = Classify(pulses[position]);
        var second = Classify(pulses[position + 1]);
        if (first == PulseKind.Short && second == PulseKind.Medium)
        {
            return true;
        }

        if (first == PulseKind.Medium && second == PulseKind.Short)
        {
            bit = 1;
            return true;
        }

        return false;
    }

    private static PulseKind Classify(int cycles)
    {
        var units = cycles / 8;
        return units switch
        {
            >= 0x24 and <= 0x36 => PulseKind.Short,
            >= 0x37 and <= 0x49 => PulseKind.Medium,
            >= 0x4A and <= 0x64 => PulseKind.Long,
            _ => PulseKind.Unknown
        };
    }

    private static List<HeaderGroup> GroupHeaders(List<HeaderRecord> headers)
    {
        var groups = new List<HeaderGroup>();
        var ordered = headers.OrderBy(header => header.Position).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            var first = ordered[index];
            var lastPosition = first.Position;
            if (index + 1 < ordered.Count &&
                ordered[index + 1].Position - first.Position < 10000 &&
                ordered[index + 1].Payload.SequenceEqual(first.Payload))
            {
                lastPosition = ordered[++index].Position;
            }

            groups.Add(new HeaderGroup(first.Position, lastPosition, first.Payload));
        }

        return groups;
    }

    private static byte[] EncodeFilename(string name)
    {
        var normalized = CommanderSave.NormalizeName(name);
        var result = Enumerable.Repeat((byte)0x20, 16).ToArray();
        Encoding.ASCII.GetBytes(normalized).CopyTo(result, 0);
        return result;
    }

    private static string DecodeFilename(ReadOnlySpan<byte> filename)
    {
        var length = filename.Length;
        while (length > 0 && filename[length - 1] is 0x20 or 0xA0)
        {
            length--;
        }

        var builder = new StringBuilder(length);
        foreach (var value in filename[..length])
        {
            builder.Append(value is >= 32 and <= 126 ? (char)value : '?');
        }

        return CommanderSave.NormalizeName(builder.ToString()[..Math.Min(builder.Length, 7)]);
    }

    private static byte Xor(IEnumerable<byte> values)
    {
        byte result = 0;
        foreach (var value in values)
        {
            result ^= value;
        }

        return result;
    }

    private static void Repeat(List<byte> output, byte value, int count)
    {
        if (output.Capacity < output.Count + count)
        {
            output.Capacity = output.Count + count;
        }

        for (var i = 0; i < count; i++)
        {
            output.Add(value);
        }
    }

    private enum PulseKind { Unknown, Short, Medium, Long }
    private sealed record CountdownRecord(int Position, bool Primary);
    private sealed record HeaderRecord(int Position, int EndPosition, bool Primary, byte[] Payload);
    private sealed record HeaderGroup(int FirstPosition, int LastPosition, byte[] Header);
}
