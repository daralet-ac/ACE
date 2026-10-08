using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACE.Server.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

/// <summary>
/// Everything here comes straight off the wire from the client, so it has to cope with anything
/// </summary>
[TestClass]
public class NetworkInputTests
{
    #region ClientPacket

    [TestMethod]
    public void Unpack_AWellFormedPacketGivesItsFragments()
    {
        var packet = new ClientPacket();
        var buffer = BuildPacket(Fragment(1, 1, 0, [1, 2, 3, 4]), Fragment(2, 1, 0, [5, 6, 7, 8, 9]));

        Assert.IsTrue(packet.Unpack(buffer, buffer.Length));

        Assert.AreEqual(2, packet.Fragments.Count);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, packet.Fragments[0].Data);
        CollectionAssert.AreEqual(new byte[] { 5, 6, 7, 8, 9 }, packet.Fragments[1].Data);
    }

    [TestMethod]
    public void Unpack_RefusesAPacketShorterThanItsHeader()
    {
        var buffer = BuildPacket(Fragment(1, 1, 0, [1, 2, 3, 4]));

        Assert.IsFalse(new ClientPacket().Unpack(buffer, 0));
        Assert.IsFalse(new ClientPacket().Unpack(buffer, PacketHeader.HeaderSize - 1));
    }

    [TestMethod]
    public void Unpack_RefusesAPacketClaimingMoreThanWasReceived()
    {
        var buffer = BuildPacket(Fragment(1, 1, 0, [1, 2, 3, 4]));

        Assert.IsFalse(new ClientPacket().Unpack(buffer, buffer.Length - 1));
    }

    [DataTestMethod]
    [DataRow(0, DisplayName = "no room for its own header")]
    [DataRow(PacketFragmentHeader.HeaderSize - 1, DisplayName = "smaller than its own header")]
    [DataRow(PacketFragment.MaxFragementSize + 1, DisplayName = "bigger than a fragment can be")]
    [DataRow(PacketFragmentHeader.HeaderSize + 100, DisplayName = "more data than the packet has")]
    public void Unpack_RefusesAFragmentWhoseSizeIsWrong(int size)
    {
        var fragment = Fragment(1, 1, 0, [1, 2, 3, 4]);
        BitConverter.GetBytes((ushort)size).CopyTo(fragment, 10);

        var buffer = BuildPacket(fragment);

        Assert.IsFalse(new ClientPacket().Unpack(buffer, buffer.Length));
    }

    [TestMethod]
    public void Unpack_RefusesARetransmitRequestForMoreThanItLists()
    {
        // asks for 1000 sequences and lists one
        var optional = new List<byte>();
        optional.AddRange(BitConverter.GetBytes(1000u));
        optional.AddRange(BitConverter.GetBytes(7u));

        var buffer = BuildPacket(PacketHeaderFlags.RequestRetransmit, optional.ToArray());

        Assert.IsFalse(new ClientPacket().Unpack(buffer, buffer.Length));
    }

    [TestMethod]
    public void VerifyCRC_AcceptsTheRightChecksumAndNothingElse()
    {
        var buffer = BuildPacket(Fragment(1, 1, 0, [1, 2, 3, 4]), Fragment(2, 1, 0, [5, 6, 7, 8, 9]));

        // what the client would send: the hash of the header, then of everything after it
        var unchecked_ = new ClientPacket();
        Assert.IsTrue(unchecked_.Unpack(buffer, buffer.Length));
        var checksum =
            unchecked_.Header.CalculateHash32()
            + unchecked_.HeaderOptional.CalculateHash32()
            + (uint)unchecked_.Fragments.Cast<ClientPacketFragment>().Sum(f => (long)f.CalculateHash32());
        BitConverter.GetBytes(checksum).CopyTo(buffer, 8);

        var packet = new ClientPacket();
        Assert.IsTrue(packet.Unpack(buffer, buffer.Length));
        Assert.IsTrue(packet.VerifyCRC(null));

        // one byte of a fragment changed on the way
        buffer[buffer.Length - 1] ^= 0xFF;

        var tampered = new ClientPacket();
        Assert.IsTrue(tampered.Unpack(buffer, buffer.Length));
        Assert.IsFalse(tampered.VerifyCRC(null));
    }

    [TestMethod]
    public void Unpack_GarbageNeverThrowsAndWhatItAcceptsIsConsistent()
    {
        var random = new Random(1234);
        var valid = BuildPacket(Fragment(1, 1, 0, [1, 2, 3, 4]), Fragment(2, 2, 1, [5, 6, 7, 8, 9]));

        for (var i = 0; i < 20000; i++)
        {
            byte[] buffer;

            if (i % 2 == 0)
            {
                // random bytes
                buffer = new byte[random.Next(0, ClientPacket.MaxPacketSize)];
                random.NextBytes(buffer);
            }
            else
            {
                // a good packet with a few bytes changed
                buffer = (byte[])valid.Clone();
                for (var j = random.Next(1, 4); j > 0; j--)
                {
                    buffer[random.Next(buffer.Length)] = (byte)random.Next(256);
                }
            }

            var packet = new ClientPacket();

            if (!packet.Unpack(buffer, buffer.Length))
            {
                continue;
            }

            foreach (var fragment in packet.Fragments)
            {
                Assert.AreEqual(
                    fragment.Header.Size - PacketFragmentHeader.HeaderSize,
                    fragment.Data.Length,
                    $"iteration {i}: a fragment has less data than its header says"
                );
            }
        }
    }

    #endregion

    #region MessageBuffer

    [TestMethod]
    public void MessageBuffer_PutsFragmentsBackInOrder()
    {
        var buffer = new MessageBuffer(5, 3);

        buffer.AddFragment(ClientFragment(5, 3, 2, [7, 8]));
        buffer.AddFragment(ClientFragment(5, 3, 0, [1, 2, 3]));
        Assert.IsFalse(buffer.Complete);
        buffer.AddFragment(ClientFragment(5, 3, 1, [4, 5, 6]));
        Assert.IsTrue(buffer.Complete);

        var message = buffer.TryGetMessage();

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, message.Data.ToArray());
        Assert.AreEqual(0x04030201u, message.Opcode);
    }

    [TestMethod]
    public void MessageBuffer_IgnoresAFragmentItAlreadyHas()
    {
        var buffer = new MessageBuffer(5, 2);

        buffer.AddFragment(ClientFragment(5, 2, 0, [1, 2, 3, 4]));
        buffer.AddFragment(ClientFragment(5, 2, 0, [9, 9, 9, 9]));

        Assert.AreEqual(1, buffer.Count);
        Assert.IsFalse(buffer.Complete);
    }

    [TestMethod]
    public void MessageBuffer_IgnoresAFragmentPastItsCount()
    {
        // two fragments, numbered 0 and 1: a third can't make it complete
        var buffer = new MessageBuffer(5, 2);

        buffer.AddFragment(ClientFragment(5, 2, 0, [1, 2, 3, 4]));
        buffer.AddFragment(ClientFragment(5, 2, 7, [9, 9, 9, 9]));

        Assert.IsFalse(buffer.Complete);

        buffer.AddFragment(ClientFragment(5, 2, 1, [5, 6]));

        Assert.IsTrue(buffer.Complete);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, buffer.TryGetMessage().Data.ToArray());
    }

    [TestMethod]
    public void MessageBuffer_TooShortForAMessageGivesNothing()
    {
        var buffer = new MessageBuffer(5, 2);

        buffer.AddFragment(ClientFragment(5, 2, 0, [1]));
        buffer.AddFragment(ClientFragment(5, 2, 1, [2]));

        Assert.IsNull(buffer.TryGetMessage());
    }

    #endregion

    #region Helpers

    private static byte[] BuildPacket(params byte[][] fragments)
    {
        return BuildPacket(PacketHeaderFlags.BlobFragments, [], fragments);
    }

    private static byte[] BuildPacket(PacketHeaderFlags flags, byte[] optional, params byte[][] fragments)
    {
        var body = optional.Concat(fragments.SelectMany(f => f)).ToArray();

        var header = new PacketHeader
        {
            Sequence = 1,
            Flags = flags,
            Size = (ushort)body.Length,
        };

        var buffer = new byte[PacketHeader.HeaderSize + body.Length];
        header.Pack(buffer);
        body.CopyTo(buffer, PacketHeader.HeaderSize);

        return buffer;
    }

    /// <summary>
    /// A fragment as the client writes it: its header, then its data
    /// </summary>
    private static byte[] Fragment(uint sequence, ushort count, ushort index, byte[] data)
    {
        var header = new PacketFragmentHeader
        {
            Sequence = sequence,
            Id = 1,
            Count = count,
            Size = (ushort)(PacketFragmentHeader.HeaderSize + data.Length),
            Index = index,
        };

        var buffer = new byte[PacketFragmentHeader.HeaderSize + data.Length];
        header.Pack(buffer);
        data.CopyTo(buffer, PacketFragmentHeader.HeaderSize);

        return buffer;
    }

    private static ClientPacketFragment ClientFragment(uint sequence, ushort count, ushort index, byte[] data)
    {
        var fragment = new ClientPacketFragment();

        Assert.IsTrue(fragment.Unpack(new BinaryReader(new MemoryStream(Fragment(sequence, count, index, data)))));

        return fragment;
    }

    #endregion
}
