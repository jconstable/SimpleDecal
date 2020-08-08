using Unity.Mathematics;
using UnityEngine;

namespace SimpleDecal
{
    public static class VectorExtensions
    {
        public static bool Approximately(this Quaternion a, Quaternion b)
        {
            return Mathf.Approximately(a.w, b.w) &&
                Mathf.Approximately(a.x, b.x) &&
                Mathf.Approximately(a.y, b.y) &&
                Mathf.Approximately(a.z, b.z);

        }

        public static bool Approximately(this Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                Mathf.Approximately(a.y, b.y) &&
                Mathf.Approximately(a.z, b.z);
        }

        public static float4 ToFloat4(this Vector3 v)
        {
            float4 f = float4.zero;
            f.xyz = v;
            return f;
        }

        public static float Angle(float4 a, float4 b)
        {
            float num = math.sqrt(math.lengthsq(a) * math.lengthsq(b));
            if (num < 1.00000000362749E-15)
                return 0.0f;
            return math.acos(math.clamp(math.dot(a, b) / num, -1f, 1f)) * 57.29578f;
        }
    }
}