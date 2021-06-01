namespace CadRevealComposer.Utils
{
    using System;
    using System.Numerics;

    public static class AlgebraUtils
    {
        public static (float roll, float pitch, float yaw) DecomposeQuaternion(Quaternion q)
        {
            // where the X-axis points forward, Y-axis to the right and Z-axis downward
            var X = (double)q.X;
            var Y = (double)q.Y;
            var Z = (double)q.Z;
            var W = (double)q.W;
            var roll = Math.Atan2(2.0 * (W * X + Y * Z), 1.0 - 2.0 * (X * X + Y * Y));
            var sinp = 2.0 * (W * Y - Z * X);
            var pitch = Math.Abs(sinp) >= 1.0 ? Math.Sign(sinp) * Math.PI / 2.0 : Math.Asin(sinp);
            
            var yaw = Math.Atan2(2.0 * (W * Z + X * Y), 1.0 - 2.0 * (Y * Y + Z * Z));

            // Avoid Gimbal lock
            // TODO: 0.005 is chosen by a few tries. This need adjustments. Do a test run to find optimal values
            // TODO: write tests
            if (Math.Abs((-X * Z + Y * W) - 0.5) < 0.005)
            {
                yaw = -2.0 * Math.Atan2(X, W);
                roll = 0;
            } else if (Math.Abs((-X * Z + Y * W) + 0.5) < 0.005)
            {
                yaw = 2.0 * Math.Atan2(X, W);
                roll = 0;
            }
            return ((float)roll, (float)pitch, (float)yaw);
        }
    }
}