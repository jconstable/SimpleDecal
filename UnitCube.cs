using Unity.Collections;
using Unity.Mathematics;

namespace SimpleDecal
{
    public struct UnitCube
    {
        //   0---------1
        //   |\    A   |\
        //   | 2---------3
        //   |B|   C   |D|
        //   4-|-------5 |
        //   \ |   E   \ |
        //    6---------7

        public static void GenerateStructures(out NativeArray<Edge> Edges, out NativeArray<Plane> Planes)
        {
            Edges = new NativeArray<Edge>(12, Allocator.TempJob);
            Edges[0] = Edge0;
            Edges[1] = Edge1;
            Edges[2] = Edge2;
            Edges[3] = Edge3;
            Edges[4] = Edge4;
            Edges[5] = Edge5;
            Edges[6] = Edge6;
            Edges[7] = Edge7;
            Edges[8] = Edge8;
            Edges[9] = Edge9;
            Edges[10] = Edge10;
            Edges[11] = Edge11;
            
            Planes = new NativeArray<Plane>(6, Allocator.TempJob);
            Planes[0] = Plane0;
            Planes[1] = Plane1;
            Planes[2] = Plane2;
            Planes[3] = Plane3;
            Planes[4] = Plane4;
            Planes[5] = Plane5;
        }

        static float4 Vertex0 = new float4(-0.5f, 0.5f, -0.5f, 0f);
        static float4 Vertex1 = new float4(0.5f, 0.5f, -0.5f, 0f);
        static float4 Vertex2 = new float4(-0.5f, 0.5f, 0.5f, 0f);
        static float4 Vertex3 = new float4(0.5f, 0.5f, 0.5f, 0f);
        static float4 Vertex4 = new float4(-0.5f, -0.5f, -0.5f, 0f);
        static float4 Vertex5 = new float4(0.5f, -0.5f, -0.5f, 0f);
        static float4 Vertex6 = new float4(-0.5f, -0.5f, 0.5f, 0f);
        static float4 Vertex7 = new float4(0.5f, -0.5f, 0.5f, 0f);

        static Plane Plane0 = new Plane(Vertex0, Vertex3, Vertex1);
        static Plane Plane1 = new Plane(Vertex0, Vertex6, Vertex2);
        static Plane Plane2 = new Plane(Vertex0, Vertex1, Vertex5);
        static Plane Plane3 = new Plane(Vertex1, Vertex3, Vertex7);
        static Plane Plane4 = new Plane(Vertex4, Vertex5, Vertex7);
        static Plane Plane5 = new Plane(Vertex2, Vertex6, Vertex3);

        public static Edge Edge0 = new Edge(Vertex0, Vertex1);
        public static Edge Edge1 = new Edge(Vertex1, Vertex3);
        public static Edge Edge2 = new Edge(Vertex3, Vertex2);
        public static Edge Edge3 = new Edge(Vertex2, Vertex0);
        public static Edge Edge4 = new Edge(Vertex4, Vertex5);
        public static Edge Edge5 = new Edge(Vertex5, Vertex7);
        public static Edge Edge6 = new Edge(Vertex7, Vertex6);
        public static Edge Edge7 = new Edge(Vertex6, Vertex4);
        public static Edge Edge8 = new Edge(Vertex0, Vertex4);
        public static Edge Edge9 = new Edge(Vertex1, Vertex5);
        public static Edge Edge10 = new Edge(Vertex3, Vertex7);
        public static Edge Edge11 = new Edge(Vertex2, Vertex6);
    }
}
