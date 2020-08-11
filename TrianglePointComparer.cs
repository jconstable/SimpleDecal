using System.Collections.Generic;
using Unity.Mathematics;

namespace SimpleDecal
{
    // IComparer that can sort points in rotational position around a normal
    struct TrianglePointComparer : IComparer<float4>
    {
        public static readonly TrianglePointComparer zero = new TrianglePointComparer
        {
            m_orientation = 0f,
            m_middle = 0f,
            m_normal = 0f
        };
        
        float4 m_orientation;
        float4 m_middle;
        float4 m_normal;
        
        public void Update(float4 orientation, float4 middle, float4 normal)
        {
            m_orientation = orientation;
            m_middle = middle;
            m_normal = normal;
        }

        public int Compare(float4 a, float4 b)
        {
            return SignedAngle(m_orientation, m_middle - a, m_normal).CompareTo(SignedAngle(m_orientation, m_middle - b, m_normal));
        }

        public static float SignedAngle(float4 a, float4 b, float4 normal)
        {
            float angle = VectorExtensions.Angle(a, b);
            float3 cross = math.cross(a.xyz, b.xyz);
            if (math.dot(normal.xyz, cross) < 0f)
            {
                return -angle;
            }

            return angle;
        }
    }
}
