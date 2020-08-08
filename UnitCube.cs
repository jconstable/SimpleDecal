using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SimpleDecal
{
    public static class UnitCube
    {
        //   0---------1
        //   |\    A   |\
        //   | 2---------3
        //   |B|   C   |D|
        //   4-|-------5 |
        //   \ |   E   \ |
        //    6---------7

        static float4 v0 = new float4(-0.5f, 0.5f, -0.5f, 0f);
        static float4 v1 = new float4(0.5f, 0.5f, -0.5f, 0f);
        static float4 v2 = new float4(-0.5f, 0.5f, 0.5f, 0f);
        static float4 v3 = new float4(0.5f, 0.5f, 0.5f, 0f);
        static float4 v4 = new float4(-0.5f, -0.5f, -0.5f, 0f);
        static float4 v5 = new float4(0.5f, -0.5f, -0.5f, 0f);
        static float4 v6 = new float4(-0.5f, -0.5f, 0.5f, 0f);
        static float4 v7 = new float4(0.5f, -0.5f, 0.5f, 0f);

        static Plane p0 = new Plane(v0.xyz, v3.xyz, v1.xyz);
        static Plane p1 = new Plane(v0.xyz, v6.xyz, v2.xyz);
        static Plane p2 = new Plane(v0.xyz, v1.xyz, v5.xyz);
        static Plane p3 = new Plane(v1.xyz, v3.xyz, v7.xyz);
        static Plane p4 = new Plane(v4.xyz, v5.xyz, v7.xyz);
        static Plane p5 = new Plane(v2.xyz, v6.xyz, v3.xyz);

        public static Edge e0 = new Edge(v0, v1);
        public static Edge e1 = new Edge(v1, v3);
        public static Edge e2 = new Edge(v3, v2);
        public static Edge e3 = new Edge(v2, v0);
        public static Edge e4 = new Edge(v4, v5);
        public static Edge e5 = new Edge(v5, v7);
        public static Edge e6 = new Edge(v7, v6);
        public static Edge e7 = new Edge(v6, v4);
        public static Edge e8 = new Edge(v0, v4);
        public static Edge e9 = new Edge(v1, v5);
        public static Edge e10 = new Edge(v3, v7);
        public static Edge e11 = new Edge(v2, v6);

        public static Plane[] Planes = new[]
        {
            p0, p1, p2, p3, p4, p5
        };

        public static Edge[] Edges = new[]
        {
            e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11
        };
    }
}
