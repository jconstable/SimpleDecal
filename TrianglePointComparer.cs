using System.Collections.Generic;
using Unity.Mathematics;

namespace SimpleDecal
{
    struct TrianglePointComparer : IComparer<float4>
    {
        float3 m_orientation;
        float3 m_middle;
        float3 m_normal;

        public TrianglePointComparer(float3 orientation, float3 middle, float3 normal)
        {
            m_orientation = orientation;
            m_middle = middle;
            m_normal = normal;
        }

        public int Compare(float4 a, float4 b)
        {
            return SignedAngle(m_orientation, m_middle - a.xyz, m_normal).CompareTo(SignedAngle(m_orientation, m_middle - b.xyz, m_normal));
        }

        public static float SignedAngle(float3 a, float3 b, float3 normal)
        {
            float angle = VectorExtensions.Angle(a, b);
            float3 cross = math.cross(a, b);
            if (math.dot(normal, cross) < 0f)
            {
                return -angle;
            }

            return angle;
        }
    }
}
