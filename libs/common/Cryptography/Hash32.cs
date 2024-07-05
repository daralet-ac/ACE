using System;

namespace ACE.Common.Cryptography;

public static class Hash32
{
    public static uint Calculate(byte[] data, int length)
    {
        var checksum = (uint)length << 16;

        for (var i = 0; i < length && i + 4 <= length; i += 4)
        {
            checksum += BitConverter.ToUInt32(data, i);
        }

        var shift = 3;
        var j = (length / 4) * 4;

        while (j < length)
        {
            checksum += (uint)(data[j++] << (8 * shift--));
        }

        return checksum;
    }

    public static uint Calculate(byte[] data, int offset, int length)
    {
        var checksum = (uint)length << 16;

        for (var i = 0; i < length && i + 4 <= length; i += 4)
        {
            checksum += BitConverter.ToUInt32(data, offset + i);
        }

        var shift = 3;
        var j = (length / 4) * 4;

        while (j < length)
        {
            checksum += (uint)(data[offset + j++] << (8 * shift--));
        }

        return checksum;
    }
}
