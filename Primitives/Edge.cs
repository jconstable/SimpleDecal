using Unity.Mathematics;

namespace SimpleDecal
{
    // Small class representing an Edge
    public struct Edge
    {
        public static readonly Edge zero = new Edge(0f, 0f);
        
        public float4 Vertex0;
        public float4 Vertex1;
        public float length;

        public Edge(float4 a, float4 b)
        {
            this = default;
            SetFrom(a,b);
        }

        public void SetFrom(float4 a, float4 b)
        {
            Vertex0 = a;
            Vertex1 = b;

            float4 dir = a - b;
            length = math.length(dir);
        }

        public bool Contains(float4 point)
        {
            float testLength = math.length(Vertex0 - point) + math.length(Vertex1 - point);
            return math.abs(length - testLength) < DecalProjector.ErrorTolerance;
        }
    }
}