using System;
using System.IO;
using ACE.Common.Cryptography;

namespace ACE.Server.Network;

public class ClientPacketFragment : PacketFragment
{
    public bool Unpack(BinaryReader payload)
    {
        Header.Unpack(payload);

        if (Header.Size - PacketFragmentHeader.HeaderSize < 0)
        {
            return false;
        }

        if (Header.Size > 464)
        {
            return false;
        }

        var dataSize = Header.Size - PacketFragmentHeader.HeaderSize;

        // a fragment can't hold more than is left of the packet
        if (dataSize > payload.BaseStream.Length - payload.BaseStream.Position)
        {
            return false;
        }

        Data = payload.ReadBytes(dataSize);
        return true;
    }

    public uint CalculateHash32()
    {
        Span<byte> buffer = stackalloc byte[PacketFragmentHeader.HeaderSize];

        Header.Pack(buffer);

        var fragmentChecksum = Hash32.Calculate(buffer, buffer.Length) + Hash32.Calculate(Data, Data.Length);

        return fragmentChecksum;
    }
}
