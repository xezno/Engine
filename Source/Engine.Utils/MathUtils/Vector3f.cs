﻿using System;

namespace Engine.Utils.MathUtils
{
    public struct Vector3f : IVector3<float, Vector3f>
    {
        /// <summary>
        /// The point at which the vector resides on the X axis
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// The point at which the vector resides on the Y axis
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// The point at which the vector resides on the Z axis
        /// </summary>
        public float Z { get; set; }

        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public float SqrMagnitude => X * X + Y * Y + Z * Z;

        public Vector3f Normalized
        {
            get
            {
                if (Math.Abs(Magnitude) < 0.0001f)
                    return new Vector3f(0, 0, 0);
                return this / Magnitude;
            }
        }

        public void Normalize() => this = Normalized;

        /// <summary>
        /// Construct a <see cref="Vector3f"/> with three initial values.
        /// </summary>
        /// <param name="x">The initial x coordinate</param>
        /// <param name="y">The initial y coordinate</param>
        /// <param name="z">The initial z coordinate</param>
        public Vector3f(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static Vector3f operator *(Vector3f a, Vector3f b) => new Vector3f(a.X * b.X,
                                                                                a.Y * b.Y,
                                                                                a.Z * b.Z);
        public static Vector3f operator -(Vector3f a, Vector3f b) => new Vector3f(a.X - b.X,
                                                                                a.Y - b.Y,
                                                                                a.Z - b.Z);
        public static Vector3f operator /(Vector3f a, Vector3f b) => new Vector3f(a.X / b.X,
                                                                                a.Y / b.Y,
                                                                                a.Z / b.Z);

        public Vector3d ToVector3d()
        {
            return new Vector3d(X, Y, Z);
        }

        public static Vector3f operator +(Vector3f a, Vector3f b) => new Vector3f(a.X + b.X,
                                                                                a.Y + b.Y,
                                                                                a.Z + b.Z);

        public static Vector3f operator *(Vector3f a, float b) => new Vector3f(a.X * b,
                                                                            a.Y * b,
                                                                            a.Z * b);
        public static Vector3f operator -(Vector3f a, float b) => new Vector3f(a.X - b,
                                                                            a.Y - b,
                                                                            a.Z - b);
        public static Vector3f operator /(Vector3f a, float b) => new Vector3f(a.X / b,
                                                                            a.Y / b,
                                                                            a.Z / b);

        public static Vector3f operator +(Vector3f a, float b) => new Vector3f(a.X + b,
                                                                            a.Y + b,
                                                                            a.Z + b);

        public static Vector3f operator %(Vector3f a, float b) => new Vector3f(a.X % b,
                                                                            a.Y % b,
                                                                            a.Z % b);

        public static Vector3f up = new Vector3f(0, 1, 0);
        public static Vector3f right = new Vector3f(1, 0, 0);
        public static Vector3f forward = new Vector3f(0, 0, 1);

        /// <summary>
        /// Get all values within the <see cref="Vector3f"/> as a string.
        /// </summary>
        /// <returns>All coordinates (<see cref="X"/>, <see cref="Y"/> and <see cref="Z"/>) concatenated as a string.</returns>
        public override string ToString()
        {
            return $"{X}, {Y}, {Z}";
        }

        public static Vector3f ConvertFromNumerics(System.Numerics.Vector3 numericsVector3)
        {
            return new Vector3f(numericsVector3.X, numericsVector3.Y, numericsVector3.Z);
        }

        public System.Numerics.Vector3 ConvertToNumerics()
        {
            return new System.Numerics.Vector3(X, Y, Z);
        }

        public static bool operator ==(Vector3f a, Vector3f b)
        {
            return (a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z);
        }

        public static bool operator !=(Vector3f a, Vector3f b)
        {
            return !(a == b);
        }
    }
}
