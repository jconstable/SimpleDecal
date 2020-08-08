using System.Collections;
using System.Collections.Generic;
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
            float4 f;
            f.x = v.x;
            f.y = v.y;
            f.z = v.z;
            f.w = 0f;
            return f;
        }

        public static float Angle(float3 a, float3 b)
        {
            return math.acos(math.dot(a, b) / (math.length(a) * math.length(b)));
        }
    }
}