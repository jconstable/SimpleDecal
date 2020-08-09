using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SimpleDecal
{
    // This class is the job that takes a list of triangles, and clips them against a unit cube
    [BurstCompile]
    public struct ClippingJob : IJob
    {
        // Input and Output
        [ReadOnly] public NativeArray<Triangle> SourceTriangles;
        [ReadOnly] public int NumSourceTriangles;
        [WriteOnly] public NativeArray<Triangle> GeneratedTriangles;
        [WriteOnly] public NativeArray<int> NumGeneratedTriangles; // size of 1, to return value
        
        // Scratch buffers
        public NativeArray<float4> ScratchPoints;
        public NativeArray<Edge> ScratchTriangleEdges;
        public NativeArray<Plane> ScratchTrianglePlanes;
        [ReadOnly] public NativeArray<Edge> UnitCubeEdges;
        [ReadOnly] public NativeArray<Plane> UnitCubePlanes;
        
        delegate bool TestPoint(float4 v);

        public void Execute()
        {
            int generatedTriangleCount = 0;
            int scratchPointsCount;

            Triangle sourceTriangle = default;

            // Comparer for sorting new points around a normal
            TrianglePointComparer comparer = default;

            // For each source triangle, find a new set of points against the unit cube
            for (int tIndex = 0; tIndex < NumSourceTriangles; tIndex++)
            {
                if (generatedTriangleCount >= DecalProjector.MaxDecalTriangles)
                    break;
                
                // We need to build our own version of T, otherwise burst complains
                sourceTriangle.CopyFrom(SourceTriangles[tIndex]);

                // Start over from the beginning of the reused array
                scratchPointsCount = 0;

                // Some verts may actually be directly inside the projector
                if (UnitCubeContains(sourceTriangle.Vertex0)) ScratchPoints[scratchPointsCount++] = sourceTriangle.Vertex0;
                if (UnitCubeContains(sourceTriangle.Vertex1)) ScratchPoints[scratchPointsCount++] = sourceTriangle.Vertex1;
                if (UnitCubeContains(sourceTriangle.Vertex2)) ScratchPoints[scratchPointsCount++] = sourceTriangle.Vertex2;

                // Early out
                // If all three points of the triangle were in the projector, we don't need to do anything.
                if (scratchPointsCount == 3)
                {
                    GeneratedTriangles[generatedTriangleCount++] = sourceTriangle;
                    continue;
                }

                // Test each triangle edge against the projector planes
                ScratchTriangleEdges[0] = sourceTriangle.Edge0;
                ScratchTriangleEdges[1] = sourceTriangle.Edge1;
                ScratchTriangleEdges[2] = sourceTriangle.Edge2;
                ClipToPlanes(ScratchTriangleEdges, UnitCubePlanes, ScratchPoints, point => UnitCubeContains(point), scratchPointsCount, out scratchPointsCount);

                // Test each projector edge against the triangle plane
                ScratchTrianglePlanes[0] = sourceTriangle.Plane;
                ClipToPlanes(UnitCubeEdges, ScratchTrianglePlanes, ScratchPoints, point => sourceTriangle.Contains(point), scratchPointsCount, out scratchPointsCount);

                // No points
                if (scratchPointsCount == 0)
                    continue;

                // Sort the points by dot product around the median.
                float4 middle = MiddlePoint(ScratchPoints, scratchPointsCount);
                float4 orientation = middle - ScratchPoints[0];
                
                // Provide the comparer with some state, and sort the array
                comparer.Update(orientation, middle, sourceTriangle.Normal);
                NativeArraySort(ScratchPoints, 0, scratchPointsCount, comparer);

                Triangle newTriangle = default;
                // Create triangles from the points
                for (int p = 0; p < scratchPointsCount; p++)
                {
                    newTriangle.SetFrom(
                        middle, ScratchPoints[p], ScratchPoints[(p + 1) % scratchPointsCount]
                    );

                    GeneratedTriangles[generatedTriangleCount++] = newTriangle;

                    if (generatedTriangleCount >= DecalProjector.MaxDecalTriangles)
                        break;
                }
            }

            NumGeneratedTriangles[0] = generatedTriangleCount;
        }

        static bool UnitCubeContains(float4 v)
        {
            return math.abs(v.x) <= 0.5f &&
                math.abs(v.y) <= 0.5f &&
                math.abs(v.z) <= 0.5f;
        }
        
        // Find the middle point of a set of points
        float4 MiddlePoint(NativeArray<float4> points, int length)
        {
            float4 sum = float4.zero;
            for(int i = 0; i < length; i++)
            {
                sum = sum + points[i];
            }

            return sum / length;
        }
        
        // Iterate over the edges, comparing each to every plane, and find if the intersection is usable.
        void ClipToPlanes(NativeArray<Edge> edges, NativeArray<Plane> planes, NativeArray<float4> points, TestPoint testFunction, int startingPointsCount, out int m_scratchPointsCount)
        {
            Ray r = default;
            m_scratchPointsCount = startingPointsCount;
            
            foreach (var edge in edges)
            {
                r.SetFrom(edge.Vertex0, edge.Vertex1 - edge.Vertex0);

                foreach (Plane p in planes)
                {
                    float dist;
                    p.Raycast(r, out dist);
                    float absDist = math.abs(dist);
                    if (absDist > 0f)
                    {
                        float4 pt = r.GetPoint(dist);
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

        // Bubble sort, because the arrays are small and NativeArray doesn't have a sort method
        void NativeArraySort<T>(NativeArray<T> a, int start, int length, IComparer<T> comparer) where T : struct
        {
            int upperBound = start + length;
            T t = default(T);
            for (int p = start; p <= upperBound - 2; p++)
            {
                for (int i = start; i <= upperBound - 2; i++)
                {
                    if (comparer.Compare(a[i], a[i + 1]) > 0)
                    {
                        t = a[i + 1];
                        a[i + 1] = a[i];
                        a[i] = t;
                    }
                } 
            }
        }
    }
}