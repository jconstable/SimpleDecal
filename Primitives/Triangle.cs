using Unity.Mathematics;

namespace SimpleDecal
{
    // Our representation of a Triangle, with some convenience functions
    public struct Triangle
    {
        public Edge Edge0;
        public Edge Edge1;
        public Edge Edge2;
        public float4 Normal;
        public Plane Plane;
        public float4 Vertex0;
        public float4 Vertex1;
        public float4 Vertex2;

        bool m_hasCalculatedArea;
        float m_area;

        public static readonly Triangle zero = new Triangle()
        {
            Vertex0 = 0f,
            Vertex1 = 0f,
            Vertex2 = 0f,
            Normal = 0f,
            Plane = Plane.zero,
            Edge0 = Edge.zero,
            Edge1 = Edge.zero,
            Edge2 = Edge.zero
        };

        public void SetFrom(float4 aIn, float4 bIn, float4 cIn)
        {
            Edge0 = Edge.zero;
            Edge1 = Edge.zero;
            Edge2 = Edge.zero;
            
            Edge0.SetFrom(aIn, bIn);
            Edge1.SetFrom(bIn, cIn);
            Edge2.SetFrom(cIn, aIn);

            Vertex0 = aIn;
            Vertex1 = bIn;
            Vertex2 = cIn;

            Plane = Plane.zero;
            Plane.SetFrom(aIn, bIn, cIn);
            Normal = Plane.Normal;
            m_hasCalculatedArea = false;
            m_area = 0f;
        }

        public void CopyFrom(Triangle t)
        {
            Edge0 = t.Edge0;
            Edge1 = t.Edge1;
            Edge2 = t.Edge2;
            
            Vertex0 = t.Vertex0;
            Vertex1 = t.Vertex1;
            Vertex2 = t.Vertex2;

            Plane = t.Plane;
            Normal = t.Normal;
            m_hasCalculatedArea = false;
            m_area = 0f;
        }

        public Triangle LocalToWorld(TRS trs)
        {
            Triangle t = Triangle.zero;
            t.SetFrom(
                trs.LocalToWorld(Vertex0),
                trs.LocalToWorld(Vertex1),
                trs.LocalToWorld(Vertex2)
            );
            return t;
        }
        
        public Triangle WorldToLocal(TRS trs)
        {
            Triangle t = Triangle.zero;
            t.SetFrom(
                trs.WorldToLocal(Vertex0),
                trs.WorldToLocal(Vertex1),
                trs.WorldToLocal(Vertex2)
            );
            return t;
        }

        public Triangle Offset(float distance)
        {
            float4 offset = Normal * distance;
            Triangle t = Triangle.zero;
            t.SetFrom(
                Vertex0 + offset,
                Vertex1 + offset,
                Vertex2 + offset
            );
            return t;
        }

        public float Area()
        {
            if (m_hasCalculatedArea)
                return m_area;
            m_area = Area(Vertex0, Vertex1, Vertex2);
            m_hasCalculatedArea = true;
            return m_area;
        }
        
        public float Area(float4 a, float4 b, float4 c)
        {
            float4 ab = (a - b);
            float4 ac = (a - c);
            float abLength = math.length(ab);
            float acLength = math.length(ac);
            float theta = math.acos(math.dot(ab, ac) / (abLength * acLength));

            return 0.5f * abLength * acLength * math.sin(theta);
        }

        public bool Contains(float4 position)
        {
            float area = Area();

            float a1 = Area(Vertex0, Vertex1, position);
            float a2 = Area(Vertex1, Vertex2, position);
            float a3 = Area(Vertex2, Vertex0, position);

            // Position is inside triangle of the sum of the areas of the three new triangle made using position
            // equals the area of the whole triangle
            return math.abs(area - (a1 + a2 + a3)) < DecalProjector.ErrorTolerance;
        }
    }
}