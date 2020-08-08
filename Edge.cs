using Unity.Mathematics;

namespace SimpleDecal
{
    public struct Edge
    {
        public float4 Vertex0 { get; }
        public float4 Vertex1 { get; }
        public float length;

        public Edge(float4 a, float4 b)
        {
            Vertex0 = a;
            Vertex1 = b;

            float4 dir = a - b;
            length = math.length(dir);
        }

        public float4 EdgeVector()
        {
            return Vertex1 - Vertex0;
        }

        public bool Contains(float4 point)
        {
            float testLength = math.length(Vertex0 - point) + math.length(Vertex1 - point);
            return math.abs(length - testLength) < DecalProjector.ErrorTolerance;
        }
    }
}