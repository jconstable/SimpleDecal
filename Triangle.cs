using Unity.Mathematics;

namespace SimpleDecal
{
    public struct Triangle
    {
        public Edge Edge0;
        public Edge Edge1;
        public Edge Edge2;
        public float4 Normal;
        public UnityEngine.Plane Plane;
        public float4 Vertex0;
        public float4 Vertex1;
        public float4 Vertex2;

        bool m_hasCalculatedArea;
        float m_area;

        public Triangle(float4 aIn, float4 bIn, float4 cIn)
        {
            Edge0 = new Edge(aIn, bIn);
            Edge1 = new Edge(bIn, cIn);
            Edge2 = new Edge(cIn, aIn);

            Vertex0 = aIn;
            Vertex1 = bIn;
            Vertex2 = cIn;

            Plane = new UnityEngine.Plane(aIn.xyz, bIn.xyz, cIn.xyz);
            Normal = math.normalize(Plane.normal.ToFloat4());
            m_hasCalculatedArea = false;
            m_area = 0f;
        }

        public Triangle LocalToWorld(TRS trs)
        {
            return new Triangle(
                trs.LocalToWorld(Vertex0),
                trs.LocalToWorld(Vertex1),
                trs.LocalToWorld(Vertex2)
            );
        }
        
        public Triangle WorldToLocal(TRS trs)
        {
            return new Triangle(
                trs.WorldToLocal(Vertex0),
                trs.WorldToLocal(Vertex1),
                trs.WorldToLocal(Vertex2)
            );
        }

        public Triangle Offset(float distance)
        {
            float4 offset = Normal * distance;
            return new Triangle(
                Vertex0 + offset,
                Vertex1 + offset,
                Vertex2 + offset
            );
        }

        public Triangle EnsureNormal(float4 normal)
        {
            if (math.dot(normal, Normal) > 0)
            {
                return this;
            }
            return new Triangle(
                Vertex0,
                Vertex2,
                Vertex1
                );
        }

        public float Area()
        {
            if (m_hasCalculatedArea)
                return m_area;
            
            float4 ab = Edge0.EdgeVector();
            float4 ac = Edge1.EdgeVector();
            float abLength = math.length(ab);
            float acLength = math.length(ac);
            float theta = VectorExtensions.Angle(ab.xyz, ac.xyz);

            m_area = 0.5f * abLength * acLength * math.sin(theta);
            m_hasCalculatedArea = true;
            return m_area;
        }

        public bool Contains(float4 position)
        {
            float area = Area();

            float a1 = new Triangle(Vertex0, Vertex1, position).Area();
            float a2 = new Triangle(Vertex1, Vertex2, position).Area();
            float a3 = new Triangle(Vertex2, Vertex0, position).Area();

            // Position is inside triangle of the sum of the areas of the three new triangle made using position
            // equals the area of the whole triangle
            return math.abs(area - (a1 + a2 + a3)) < DecalProjector.ErrorTolerance;
        }
    }
}