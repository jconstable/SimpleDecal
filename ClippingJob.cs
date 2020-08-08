using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SimpleDecal
{
    public struct ClippingJob : IJob
    {
        [Unity.Collections.ReadOnly] public NativeArray<Triangle> SourceTriangles;
        [Unity.Collections.ReadOnly] int NumSourceTriangles;
        [WriteOnly] public NativeArray<Triangle> GeneratedTriangles;
        [WriteOnly] public NativeArray<int> NumGeneratedTriangles;
        
        delegate bool TestPoint(float4 v);

        int m_scratchPointsCount;

        public ClippingJob(int numSourceTriangles,
            NativeArray<Triangle> sourceTriangles,
            NativeArray<Triangle> generatedTriangles,
            NativeArray<int> numGeneratedTriangles
            )
        {
            NumSourceTriangles = numSourceTriangles;
            SourceTriangles = sourceTriangles;
            GeneratedTriangles = generatedTriangles;
            NumGeneratedTriangles = numGeneratedTriangles;
            m_scratchPointsCount = 0;
        }
        
        public void Execute()
        {
            int numGeneratedTriangles = 0;
            float4[] scratchPoints = new float4[10];
            Edge[] triangleEdges = new Edge[3];
            Plane[] trianglePlanes = new Plane[1];
            
            for (int tIndex = 0; tIndex < NumSourceTriangles; tIndex++)
            {
                if (numGeneratedTriangles >= DecalProjector.MaxDecalTriangles)
                    break;
                
                Triangle t = SourceTriangles[tIndex];
                m_scratchPointsCount = 0;

                // Some verts may actually be directly inside the projector
                if (UnitCubeContains(t.Vertex0)) scratchPoints[m_scratchPointsCount++] = t.Vertex0;
                if (UnitCubeContains(t.Vertex1)) scratchPoints[m_scratchPointsCount++] = t.Vertex1;
                if (UnitCubeContains(t.Vertex2)) scratchPoints[m_scratchPointsCount++] = t.Vertex2;

                // Early out
                // If all three points of the triangle were in the projector, we don't need to do anything.
                if (m_scratchPointsCount == 3)
                {
                    GeneratedTriangles[numGeneratedTriangles++] = t;
                    continue;
                }

                // Test each triangle edge against the projector planes
                triangleEdges[0] = t.Edge0;
                triangleEdges[1] = t.Edge1;
                triangleEdges[2] = t.Edge2;
                ClipToPlanes(triangleEdges, UnitCube.Planes, scratchPoints, point => UnitCubeContains(point));

                // Test each projector edge against the triangle plane
                trianglePlanes[0] = t.Plane;
                ClipToPlanes(UnitCube.Edges, trianglePlanes, scratchPoints, point => t.Contains(point));

                // No points
                if (m_scratchPointsCount == 0)
                    continue;

                // Sort the points by dot product around the median.
                float4 middle = MiddlePoint(scratchPoints, m_scratchPointsCount);
                float3 orientation = math.normalize(middle - scratchPoints[0]).xyz;
                IComparer<float4> comparer = new TrianglePointComparer(
                    orientation,
                    middle.xyz,
                    t.Normal.xyz);
                Array.Sort(scratchPoints, 0, m_scratchPointsCount, comparer);

                // Create triangles from the points
                for (int p = 0; p < m_scratchPointsCount; p++)
                {
                    GeneratedTriangles[numGeneratedTriangles++] = new Triangle(
                        middle, scratchPoints[p], scratchPoints[(p + 1) % m_scratchPointsCount]
                    ).EnsureNormal(t.Normal);

                    break;
                    
                    if (numGeneratedTriangles >= DecalProjector.MaxDecalTriangles)
                        break;
                }
            }

            NumGeneratedTriangles[0] = numGeneratedTriangles;
        }

        static float Tolerant(float v)
        {
            return v > 0f ? v + DecalProjector.ErrorTolerance : v - DecalProjector.ErrorTolerance;
        }
        
        static bool UnitCubeContains(float4 v)
        {
            if ((v.x >= Tolerant(-0.5f) && v.x <= Tolerant(0.5f)) &&
                (v.y >= Tolerant(-0.5f) && v.y <= Tolerant(0.5f)) &&
                (v.z >= Tolerant(-0.5f) && v.z <= Tolerant(0.5f))
            )
            {
                return true;
            }

            return false;
        }
        
        float4 MiddlePoint(float4[] points, int length)
        {
            float4 sum = float4.zero;
            for(int i = 0; i < length; i++)
            {
                sum = sum + points[i];
            }

            return sum / length;
        }
        
        void ClipToPlanes(Edge[] edges, Plane[] planes, float4[] points, TestPoint testFunction)
        {
            foreach (var edge in edges)
            {
                Ray r = new Ray(edge.Vertex0.xyz, math.normalize(edge.Vertex1 - edge.Vertex0).xyz);

                foreach (Plane p in planes)
                {
                    float dist;
                    p.Raycast(r, out dist);
                    float absDist = math.abs(dist);
                    if (absDist > 0f)
                    {
                        Vector3 unityPt = r.GetPoint(dist);
                        float4 pt = unityPt.ToFloat4();
                        if (edge.Contains(pt))
                        {
                            if (testFunction(pt))
                            {
                                points[m_scratchPointsCount++] = pt;
                            }
                        }
                    }
                }
            }
        }
    }
}