namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record ClosedTorusSegment(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
             Vector3 Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
             float Radius,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]

            float TubeRadius,
            [property: I3df(I3dfAttribute.AttributeType.Angle)]

            float RotationAngle,
            [property: I3df(I3dfAttribute.AttributeType.Angle)]
             float ArcAngle
        )
        : APrimitive(CommonPrimitiveProperties);
}