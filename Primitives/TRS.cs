using Unity.Mathematics;

namespace SimpleDecal
{
    // A small class that handles Translation, Rotation, and Scale for dealing with Triangle type
    public struct TRS
    {
        float4x4 m_worldToLocal;
        float4x4 m_localToWorld;
        float4 m_translation;

        public void Update(float4x4 w2l, float4x4 l2w, float4 offset)
        {
            m_worldToLocal = w2l;
            m_localToWorld = l2w;
            m_translation = offset;
        }

        public float4 LocalToWorld(float4 point)
        {
            return math.mul(m_localToWorld, point) + m_translation;
        }

        public float4 WorldToLocal(float4 point)
        {
            return math.mul(m_worldToLocal, point + -m_translation);
        }
    }
}