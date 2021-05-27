﻿namespace RvmSharp.Tests.Tesselator
{
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Numerics;
    using Tessellation;

    [TestFixture]
    public class TessellatorBridgeTests
    {
        private static readonly RvmBoundingBox ArbitraryBoundingBox =
            new RvmBoundingBox(Min: Vector3.Zero, Max: Vector3.One);

        [TestFixture]
        public class TessellateBoxTests
        {
            [Test]
            public void TessellateBox_WithUnitBox_ReturnsExpected1x1Mesh()
            {
                var unitBox = new RvmBox(1, Matrix4x4.Identity,
                    new RvmBoundingBox(Min: new Vector3(-0.5f, -0.5f, -0.5f), Max: new Vector3(0.5f, 0.5f, 0.5f)), 1, 1,
                    1);

                var randomToleranceValue = 0.1f;
                var box = TessellatorBridge.Tessellate(unitBox, 1, randomToleranceValue);
                Assert.That(box, Is.Not.Null);
                Assert.That(box.Vertices, Has.Exactly(24).Items);
            }
        }

        [TestFixture]
        public class TessellatePyramidTests
        {
            [Test]
            public void TessellatePyramid_WithUnitPyramid_MatchesReferenceMethod()
            {
                var unitPyramid = new RvmPyramid(1, Matrix4x4.Identity, ArbitraryBoundingBox, 0, 0, 0, 1, 0, 0, 1);

                var randomToleranceValue = 0.1f;
                var pyramid = TessellatorBridge.Tessellate(unitPyramid, 1, randomToleranceValue);
                Assert.That(pyramid, Is.Not.Null);
            }
        }

        [TestFixture]
        public class TessellateCylinderTests
        {
            [Test]
            public void TessellatePyramid_WithUnitPyramid_MatchesReferenceMethod()
            {
                var unitCylinder = new RvmCylinder(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 1);

                var randomToleranceValue = 0.1f;
                var cylinder = TessellatorBridge.Tessellate(unitCylinder, 1, randomToleranceValue);

                Assert.That(cylinder, Is.Not.Null);
                Assert.That(cylinder.Triangles, Has.Exactly(156).Items);
            }
        }

        [TestFixture]
        public class TessellateLineTests
        {
            [Test]
            public void TessellateLine_IsNotPossible_ReturnsNull()
            {
                // If somehow Line is tessellated, improve this test.
                var rvmLine = new RvmLine(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 3);

                var randomToleranceValue = 0.1f;
                var line = TessellatorBridge.Tessellate(rvmLine, 1, randomToleranceValue);

                Assert.That(line, Is.Null);
            }
        }
    }
}