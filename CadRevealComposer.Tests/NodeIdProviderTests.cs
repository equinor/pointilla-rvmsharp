﻿namespace CadRevealComposer.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class NodeIdProviderTests
    {
        [Test]
        public void FuzzTest_Produces_NoOutsideValidRange()
        {
            // This test tests N random values. It should never fail.
            var nip = new NodeIdProvider();

            const ulong maxSafeInt = TreeIndexGenerator.MaxSafeInteger;

            for(var i=0; i<100; i++)
                Assert.That(nip.GetNodeId(null), Is.LessThanOrEqualTo(maxSafeInt));
        }

    }
}