﻿using System.Collections.Generic;
using System.Numerics;

namespace rvmsharp.Rvm
{
    public abstract class RvmPrimitive
    {
        public readonly uint Version;
        public readonly RvmPrimitiveKind Kind;
        public readonly Matrix4x4 Matrix;
        public readonly RvmBoundingBox BoundingBoxLocal;

        public RvmPrimitive(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal)
        {
            Version = version;
            Kind = kind;
            Matrix = matrix;
            BoundingBoxLocal = bBoxLocal;
        }
    }
}