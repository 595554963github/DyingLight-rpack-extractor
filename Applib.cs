namespace Applib
{
    public class Vector3D
    {
        public float X;
        public float Y;
        public float Z;

        public static readonly Vector3D UnitX = new Vector3D(1f, 0f, 0f);
        public static readonly Vector3D UnitY = new Vector3D(0f, 1f, 0f);
        public static readonly Vector3D UnitZ = new Vector3D(0f, 0f, 1f);
        public static readonly Vector3D Zero = new Vector3D(0f, 0f, 0f);
        public static readonly Vector3D One = new Vector3D(1f, 1f, 1f);

        public Vector3D()
        {
            X = 0f;
            Y = 0f;
            Z = 0f;
        }

        public Vector3D(float xx, float yy, float zz)
        {
            X = xx;
            Y = yy;
            Z = zz;
        }

        public Vector3D(Vector3D Vec)
        {
            X = Vec.X;
            Y = Vec.Y;
            Z = Vec.Z;
        }

        public void SetVector(float xx, float yy, float zz)
        {
            X = xx;
            Y = yy;
            Z = zz;
        }

        public float DotProduct(Vector3D Vec)
        {
            return X * Vec.X + Y * Vec.Y + Z * Vec.Z;
        }

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public float AngleTo(Vector3D Vec)
        {
            float num = DotProduct(Vec);
            float num2 = Length() * Vec.Length();
            if (num2 == 0f)
            {
                return 0f;
            }
            return (float)Math.Acos(num / num2);
        }

        public Vector3D UnitVector()
        {
            Vector3D vector3D = new Vector3D();
            float num = Length();
            if (num == 0f)
            {
                vector3D.SetVector(0f, 0f, 0f);
                return vector3D;
            }
            vector3D.X = X / num;
            vector3D.Y = Y / num;
            vector3D.Z = Z / num;
            return vector3D;
        }

        public bool IsCodirectionalTo(Vector3D Vec)
        {
            Vector3D vector3D = UnitVector();
            Vector3D vector3D2 = Vec.UnitVector();
            return vector3D.X == vector3D2.X && vector3D.Y == vector3D2.Y && vector3D.Z == vector3D2.Z;
        }

        public bool IsEqualTo(Vector3D? Vec)
        {
            if (Vec is null) return false;
            return X == Vec.X && Y == Vec.Y && Z == Vec.Z;
        }

        public bool IsParallelTo(Vector3D Vec)
        {
            Vector3D vector3D = UnitVector();
            Vector3D vector3D2 = Vec.UnitVector();
            return (vector3D.X == vector3D2.X && vector3D.Y == vector3D2.Y && vector3D.Z == vector3D2.Z) |
                   (vector3D.X == -vector3D2.X && vector3D.Y == -vector3D2.Y && vector3D.Z == vector3D2.Z);
        }

        public bool IsPerpendicularTo(Vector3D Vec)
        {
            double num = AngleTo(Vec);
            return num == 1.5707963267948966;
        }

        public bool IsXAxis()
        {
            return X != 0f && Y == 0f && Z == 0f;
        }

        public bool IsYAxis()
        {
            return X == 0f && Y != 0f && Z == 0f;
        }

        public bool IsZAxis()
        {
            return X == 0f && Y == 0f && Z != 0f;
        }

        public void Negate()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        public Vector3D Add(Vector3D Vec)
        {
            return new Vector3D(X + Vec.X, Y + Vec.Y, Z + Vec.Z);
        }

        public Vector3D Subtract(Vector3D Vec)
        {
            return new Vector3D(X - Vec.X, Y - Vec.Y, Z - Vec.Z);
        }

        public static bool operator ==(Vector3D? a, Vector3D? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vector3D? a, Vector3D? b)
        {
            return !(a == b);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3D operator -(Vector3D left, Vector3D right)
        {
            return new Vector3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3D Multiply(Vector3D vector, float scale)
        {
            return new Vector3D(vector.X * scale, vector.Y * scale, vector.Z * scale);
        }

        public static Vector3D Multiply(Vector3D vector, int scale)
        {
            return new Vector3D(vector.X * scale, vector.Y * scale, vector.Z * scale);
        }

        public static Vector3D Multiply(Vector3D vector, Vector3D scale)
        {
            return new Vector3D(vector.X * scale.X, vector.Y * scale.Y, vector.Z * scale.Z);
        }

        public static Vector3D Multiply(Vector3D vec, Quaternion3D q)
        {
            float num = 2f * (q.i * vec.X + q.j * vec.Y + q.k * vec.Z);
            float num2 = 2f * q.real;
            float num3 = num2 * q.real - 1f;
            float num4 = num3 * vec.X + num * q.i + num2 * (q.k * vec.Z - q.k * vec.Y);
            float num5 = num3 * vec.Y + num * q.j + num2 * (q.k * vec.X - q.i * vec.Z);
            float num6 = num3 * vec.Z + num * q.k + num2 * (q.i * vec.Y - q.j * vec.X);
            return new Vector3D(num4, num5, num6);
        }

        public static Vector3D operator *(Vector3D left, float right)
        {
            return Multiply(left, right);
        }

        public static Vector3D operator *(Vector3D left, int right)
        {
            return Multiply(left, right);
        }

        public static Vector3D operator *(float left, Vector3D right)
        {
            return Multiply(right, left);
        }

        public static Vector3D operator *(Vector3D left, Vector3D right)
        {
            return Multiply(left, right);
        }

        public static Vector3D operator *(Vector3D left, Quaternion3D right)
        {
            return Multiply(left, right);
        }

        public static Vector3D operator /(Vector3D vec, float scale)
        {
            float num = 1f / scale;
            return new Vector3D(vec.X * num, vec.Y * num, vec.Z * num);
        }

        public static Vector3D Cross(Vector3D left, Vector3D right)
        {
            return new Vector3D(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X
            );
        }

        public static float Dot(Vector3D left, Vector3D right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public Vector3D Normalized()
        {
            Vector3D result = new Vector3D(this);
            result.Normalize();
            return result;
        }

        public void Normalize()
        {
            float num = 1f / Length();
            X *= num;
            Y *= num;
            Z *= num;
        }

        public static Vector3D Normalize(Vector3D vec)
        {
            Vector3D result = new Vector3D(vec);
            result.Normalize();
            return result;
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    default: throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
                }
            }
            set
            {
                switch (index)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    default: throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
                }
            }
        }

        public float LengthSquared
        {
            get { return X * X + Y * Y + Z * Z; }
        }

        public string WriteString()
        {
            return string.Format("{0} {1} {2}", X, Y, Z);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            Vector3D? vector3D = obj as Vector3D;
            return vector3D != null && IsEqualTo(vector3D);
        }

        public override int GetHashCode()
        {
            int num = 17;
            num = num * 23 + X.GetHashCode();
            num = num * 23 + Y.GetHashCode();
            return num * 23 + Z.GetHashCode();
        }
    }

    public class Quaternion3D
    {
        public float real;
        public float i;
        public float j;
        public float k;

        public Vector3D xyz
        {
            get { return new Vector3D(i, j, k); }
            set
            {
                i = value.X;
                j = value.Y;
                k = value.Z;
            }
        }

        public Quaternion3D()
        {
            real = 0f;
            i = 0f;
            j = 0f;
            k = 0f;
        }

        public Quaternion3D(float _real, float _i, float _j, float _k)
        {
            real = _real;
            i = _i;
            j = _j;
            k = _k;
        }

        public Quaternion3D(Vector3D vecXYZ, float _real)
        {
            real = _real;
            i = vecXYZ.X;
            j = vecXYZ.Y;
            k = vecXYZ.Z;
        }

        public Quaternion3D(Quaternion3D q)
        {
            real = q.real;
            i = q.i;
            j = q.j;
            k = q.k;
        }

        public Vector3D ToVec()
        {
            return new Vector3D(xyz);
        }

        public float Length
        {
            get { return Convert.ToSingle(Math.Sqrt(real * real + xyz.LengthSquared)); }
        }

        public void Normalize()
        {
            float num = 1f / Length;
            xyz *= num;
            real *= num;
        }

        public static Quaternion3D Invert(Quaternion3D q)
        {
            float lengthSquared = q.LengthSquared;
            if (lengthSquared != 0f)
            {
                float num = 1f / lengthSquared;
                return new Quaternion3D(q.xyz * -num, q.real * num);
            }
            return q;
        }

        public float LengthSquared
        {
            get { return real * real + xyz.LengthSquared; }
        }

        public static Quaternion3D Multiply(Quaternion3D left, Quaternion3D right)
        {
            return new Quaternion3D(
                right.real * left.xyz + left.real * right.xyz + Vector3D.Cross(left.xyz, right.xyz),
                left.real * right.real - Vector3D.Dot(left.xyz, right.xyz)
            );
        }

        public static Quaternion3D operator *(Quaternion3D left, Quaternion3D right)
        {
            return Multiply(left, right);
        }
    }

    public static class C3D
    {
        private const double FLT_EPSILON = 1E-05;

        public static float FlipFloat(float inputF)
        {
            return -inputF;
        }

        public static double deg2rad(double deg)
        {
            return deg * 0.017453292519943295;
        }

        public static double rad2deg(double rad)
        {
            return rad * 57.29577951308232;
        }

        public static float NanSafe(float val)
        {
            if (float.IsNaN(val)) return 0f;
            if (val == 0f) return 0f;
            return val;
        }

        public static Vector3D Quat2Euler_UBISOFT(Quaternion3D quat)
        {
            Vector3D vector3D = new Vector3D();
            double num = 1.5707963267948966;
            float num2 = quat.i * quat.j + quat.k * quat.real;

            if (num2 > 0.499f)
            {
                vector3D.X = 2f * (float)Math.Atan2(quat.i, quat.real);
                vector3D.Y = (float)num;
                vector3D.Z = 0f;
            }
            else if (num2 < -0.499f)
            {
                vector3D.X = -2f * (float)Math.Atan2(quat.i, quat.real);
                vector3D.Y = (float)(-num);
                vector3D.Z = 0f;
            }
            else
            {
                float num3 = quat.i * quat.i;
                float num4 = quat.j * quat.j;
                float num5 = quat.k * quat.k;
                vector3D.X = (float)Math.Atan2(2f * quat.j * quat.real - 2f * quat.i * quat.k, 1f - 2f * num4 - 2f * num5);
                vector3D.Y = (float)Math.Asin(2f * num2);
                vector3D.Z = (float)Math.Atan2(2f * quat.i * quat.real - 2f * quat.j * quat.k, 1f - 2f * num3 - 2f * num5);
            }

            if (float.IsNaN(vector3D.X)) vector3D.X = 0f;
            if (float.IsNaN(vector3D.Y)) vector3D.Y = 0f;
            if (float.IsNaN(vector3D.Z)) vector3D.Z = 0f;

            return vector3D;
        }

        public static Vector3D QuaternionToEuler(Quaternion3D quat)
        {
            Vector3D vector3D = new Vector3D();
            float num = quat.real * quat.real;
            float num2 = quat.i * quat.i;
            float num3 = quat.j * quat.j;
            float num4 = quat.k * quat.k;

            vector3D.Z = (float)rad2deg(Math.Atan2(2.0 * (quat.j * quat.k + quat.i * quat.real), -num2 - num3 + num4 + num));
            vector3D.X = (float)rad2deg(Math.Asin(-2.0 * (quat.i * quat.k - quat.j * quat.real)));
            vector3D.Y = (float)rad2deg(Math.Atan2(2.0 * (quat.i * quat.j + quat.k * quat.real), num2 - num3 - num4 + num));

            if (float.IsNaN(vector3D.X)) vector3D.X = 0f;
            if (float.IsNaN(vector3D.Y)) vector3D.Y = 0f;
            if (float.IsNaN(vector3D.Z)) vector3D.Z = 0f;

            return vector3D;
        }

        public static Vector3D QuaternionToEulerRAD(Quaternion3D quat)
        {
            Vector3D vector3D = new Vector3D();
            float num = quat.real * quat.real;
            float num2 = quat.i * quat.i;
            float num3 = quat.j * quat.j;
            float num4 = quat.k * quat.k;

            vector3D.Z = (float)Math.Atan2(2.0 * (quat.j * quat.k + quat.i * quat.real), -num2 - num3 + num4 + num);
            vector3D.X = (float)Math.Asin(-2.0 * (quat.i * quat.k - quat.j * quat.real));
            vector3D.Y = (float)Math.Atan2(2.0 * (quat.i * quat.j + quat.k * quat.real), num2 - num3 - num4 + num);

            if (float.IsNaN(vector3D.X)) vector3D.X = 0f;
            if (float.IsNaN(vector3D.Y)) vector3D.Y = 0f;
            if (float.IsNaN(vector3D.Z)) vector3D.Z = 0f;

            return vector3D;
        }

        public static Vector3D QuaternionToEulerRAD2(Quaternion3D quat)
        {
            Vector3D vector3D = new Vector3D();
            float real = quat.real;
            float i = quat.i;
            float j = quat.j;
            float k = quat.k;
            float num = i * i;
            float num2 = j * j;
            float num3 = k * k;

            vector3D.Z = (float)Math.Atan2(2.0 * (real * i + j * k), 1f - 2f * (num + num2));
            vector3D.X = (float)Math.Asin(2.0 * (real * j - k * i));
            vector3D.Y = (float)Math.Atan2(2.0 * (real * k + i * j), 1f - 2f * (num2 + num3));

            return vector3D;
        }

        public static Quaternion3D EulerAnglesToQuaternion(float yaw, float pitch, float roll)
        {
            double num = NormalizeAngle(yaw);
            double num2 = NormalizeAngle(pitch);
            double num3 = NormalizeAngle(roll);

            double num4 = Math.Cos(num);
            double num5 = Math.Cos(num2);
            double num6 = Math.Cos(num3);
            double num7 = Math.Sin(num);
            double num8 = Math.Sin(num2);
            double num9 = Math.Sin(num3);

            return new Quaternion3D
            {
                real = (float)(num4 * num5 * num6 - num7 * num8 * num9),
                i = (float)(num7 * num8 * num6 + num4 * num5 * num9),
                j = (float)(num7 * num5 * num6 + num4 * num8 * num9),
                k = (float)(num4 * num8 * num6 - num7 * num5 * num9)
            };
        }

        public static Quaternion3D DEG_EulerAnglesToQuaternion(float yaw, float pitch, float roll)
        {
            double num = deg2rad(yaw);
            double num2 = deg2rad(pitch);
            double num3 = deg2rad(roll);

            double num4 = Math.Cos(num);
            double num5 = Math.Cos(num2);
            double num6 = Math.Cos(num3);
            double num7 = Math.Sin(num);
            double num8 = Math.Sin(num2);
            double num9 = Math.Sin(num3);

            return new Quaternion3D
            {
                real = (float)(num4 * num5 * num6 - num7 * num8 * num9),
                i = (float)(num7 * num8 * num6 + num4 * num5 * num9),
                j = (float)(num7 * num5 * num6 + num4 * num8 * num9),
                k = (float)(num4 * num8 * num6 - num7 * num5 * num9)
            };
        }

        public static Quaternion3D Euler2Quat(Vector3D orientation)
        {
            Quaternion3D quaternion3D = new Quaternion3D();
            float num = 0f;
            float num2 = 0f;
            float num3 = 0f;
            float num4 = 0f;
            float num5 = 0f;
            float num6 = 0f;

            MathUtil.SinCos(ref num, ref num4, orientation.X * 0.5f);
            MathUtil.SinCos(ref num2, ref num5, orientation.Y * 0.5f);
            MathUtil.SinCos(ref num3, ref num6, orientation.Z * 0.5f);

            quaternion3D.real = num6 * num4 * num5 + num3 * num * num2;
            quaternion3D.i = -num6 * num * num5 - num3 * num4 * num2;
            quaternion3D.j = num6 * num * num2 - num3 * num5 * num4;
            quaternion3D.k = num3 * num * num5 - num6 * num4 * num2;

            return quaternion3D;
        }

        public static float NormalizeAngle(float input)
        {
            return (float)(input * 3.141592653589793 / 360.0);
        }

        public static Vector3D ToEulerAngles(Quaternion3D q)
        {
            return Eul_FromQuat(q, 0, 1, 2, 0, EulerParity.Even, EulerRepeat.No, EulerFrame.S);
        }

        private static Vector3D Eul_FromQuat(Quaternion3D q, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame)
        {
            double[,] array = new double[4, 4];
            double num = q.i * q.i + q.j * q.j + q.k * q.k + q.real * q.real;
            double num2 = num > 0.0 ? 2.0 / num : 0.0;

            double num3 = q.i * num2;
            double num4 = q.j * num2;
            double num5 = q.k * num2;
            double num6 = q.real * num3;
            double num7 = q.real * num4;
            double num8 = q.real * num5;
            double num9 = q.i * num3;
            double num10 = q.i * num4;
            double num11 = q.i * num5;
            double num12 = q.j * num4;
            double num13 = q.j * num5;
            double num14 = q.k * num5;

            array[0, 0] = 1.0 - (num12 + num14);
            array[0, 1] = num10 - num8;
            array[0, 2] = num11 + num7;
            array[1, 0] = num10 + num8;
            array[1, 1] = 1.0 - (num9 + num14);
            array[1, 2] = num13 - num6;
            array[2, 0] = num11 - num7;
            array[2, 1] = num13 + num6;
            array[2, 2] = 1.0 - (num9 + num12);
            array[3, 3] = 1.0;

            return Eul_FromHMatrix(array, i, j, k, h, parity, repeat, frame);
        }

        private static Vector3D Eul_FromHMatrix(double[,] M, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame)
        {
            Vector3D vector3D = new Vector3D();

            if (repeat == EulerRepeat.Yes)
            {
                double num = Math.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
                if (num > 0.00016)
                {
                    vector3D.X = (float)Math.Atan2(M[i, j], M[i, k]);
                    vector3D.Y = (float)Math.Atan2(num, M[i, i]);
                    vector3D.Z = (float)Math.Atan2(M[j, i], -M[k, i]);
                }
                else
                {
                    vector3D.X = (float)Math.Atan2(-M[j, k], M[j, j]);
                    vector3D.Y = (float)Math.Atan2(num, M[i, i]);
                    vector3D.Z = 0f;
                }
            }
            else
            {
                double num2 = Math.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
                if (num2 > 0.00016)
                {
                    vector3D.X = (float)Math.Atan2(M[k, j], M[k, k]);
                    vector3D.Y = (float)Math.Atan2(-M[k, i], num2);
                    vector3D.Z = (float)Math.Atan2(M[j, i], M[i, i]);
                }
                else
                {
                    vector3D.X = (float)Math.Atan2(-M[j, k], M[j, j]);
                    vector3D.Y = (float)Math.Atan2(-M[k, i], num2);
                    vector3D.Z = 0f;
                }
            }

            if (parity == EulerParity.Odd)
            {
                vector3D.X = -vector3D.X;
                vector3D.Y = -vector3D.Y;
                vector3D.Z = -vector3D.Z;
            }

            if (frame == EulerFrame.R)
            {
                float temp = vector3D.X;
                vector3D.X = vector3D.Z;
                vector3D.Z = temp;
            }

            return vector3D;
        }

        public enum EulerParity { Even, Odd }
        public enum EulerRepeat { No, Yes }
        public enum EulerFrame { S, R }

        public class MathUtil
        {
            public static float kPi = 3.1415927f;
            public static float k2Pi = kPi * 2f;
            public static float kPiOver2 = kPi / 2f;
            public static float k1OverPi = 1f / kPi;
            public static float k1Over2Pi = 1f / k2Pi;
            public static float kPiOver180 = kPi / 180f;
            public static float k180OverPi = 180f / kPi;
            public static Vector3D kZeroVector = new Vector3D(0f, 0f, 0f);

            public static void SinCos(ref float returnSin, ref float returnCos, float theta)
            {
                returnSin = (float)Math.Sin(deg2rad(theta));
                returnCos = (float)Math.Cos(deg2rad(theta));
            }
        }
    }
}