﻿using System;

namespace Engine.Utils.MathUtils
{
    public struct Quaternion
    {
        // TODO: This entire class needs like, a ton of work.

        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static Quaternion identity = new Quaternion(0, 0, 0, 1);

        public static Quaternion FromEulerAngles(Vector3f eulerAngles)
        {
            var degToRad = 0.0174533f;
            var halfCosX = (float)Math.Cos(eulerAngles.X * degToRad) / 2.0f;
            var halfCosY = (float)Math.Cos(eulerAngles.Y * degToRad) / 2.0f;
            var halfCosZ = (float)Math.Cos(eulerAngles.Z * degToRad) / 2.0f;
            var halfSinX = (float)Math.Cos(eulerAngles.X * degToRad) / 2.0f;
            var halfSinY = (float)Math.Cos(eulerAngles.Y * degToRad) / 2.0f;
            var halfSinZ = (float)Math.Cos(eulerAngles.Z * degToRad) / 2.0f;

            return new Quaternion(halfCosX * halfCosY * halfCosZ + halfSinX * halfSinY * halfSinZ,
                halfSinX * halfCosY * halfCosZ - halfCosX * halfSinY * halfSinZ,
                halfCosX * halfSinY * halfCosZ + halfSinX * halfCosY * halfSinZ,
                halfCosX * halfCosY * halfSinZ - halfSinX * halfSinY * halfCosZ);
        }

        public Vector3d ToEulerAngles()
        {
            return new Vector3d(
                Math.Atan2(2 * ((w * x) + (y * z)), 1 - 2 * ((x * x) + (y * y))),
                Math.Asin(2 * ((w * y) - (z * x))),
                Math.Atan2(2 * ((w * z) + (x * y)), 1 - 2 * ((y * y) + (z * z)))
            );
        }
    }
}
