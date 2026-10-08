using System;
using ACE.Common.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests.Libraries;

/// <summary>
/// Every packet's checksum is built from these
/// </summary>
[TestClass]
public class Hash32Tests
{
    [TestMethod]
    public void TheSpanAndArrayVersionsAgreeForEveryLength()
    {
        var random = new Random(42);
        var data = new byte[80];
        random.NextBytes(data);

        for (var offset = 0; offset < 8; offset++)
        {
            for (var length = 0; length <= data.Length - offset; length++)
            {
                Assert.AreEqual(
                    Hash32.Calculate(data.AsSpan(offset), length),
                    Hash32.Calculate(data, offset, length),
                    $"offset {offset}, length {length}"
                );
            }
        }
    }

    [TestMethod]
    public void KnownValues()
    {
        // the length in the high word, plus each whole little-endian uint
        Assert.AreEqual(0u, Hash32.Calculate(Array.Empty<byte>(), 0, 0));
        Assert.AreEqual((4u << 16) + 0x04030201u, Hash32.Calculate(new byte[] { 1, 2, 3, 4 }, 0, 4));

        // and the bytes left over from the top byte down
        Assert.AreEqual((1u << 16) + (0xAAu << 24), Hash32.Calculate(new byte[] { 0xAA }, 0, 1));
        Assert.AreEqual(
            (6u << 16) + 0x04030201u + (0xBBu << 24) + (0xCCu << 16),
            Hash32.Calculate(new byte[] { 1, 2, 3, 4, 0xBB, 0xCC }, 0, 6)
        );
    }
}
