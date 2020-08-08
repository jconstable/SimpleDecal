using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SimpleDecal
{
    public static class DecalGizmo
    {
        public static void DrawGizmos(TRS trs)
        {
            NativeArray<Edge> unitCubeEdges;
            NativeArray<Plane> unitCubePlanes;
            UnitCube.GenerateStructures(out unitCubeEdges, out unitCubePlanes);
            
            foreach( var e in unitCubeEdges)
                GizmoLine(e, trs);
            
            Gizmos.color = Color.green;
            GizmoLine(UnitCube.Edge0, trs);
            GizmoLine(UnitCube.Edge1, trs);
            GizmoLine(UnitCube.Edge2, trs);
            GizmoLine(UnitCube.Edge3, trs);
            
            float4 extrapolate0 = GizmoLineExtrapolate(UnitCube.Edge0.Vertex0, trs);
            float4 extrapolate1 = GizmoLineExtrapolate(UnitCube.Edge1.Vertex0, trs);
            float4 extrapolate2 = GizmoLineExtrapolate(UnitCube.Edge2.Vertex0, trs);
            float4 extrapolate3 = GizmoLineExtrapolate(UnitCube.Edge3.Vertex0, trs);
            
            GizmoLine(extrapolate0, extrapolate1, trs);
            GizmoLine(extrapolate1, extrapolate2, trs);
            GizmoLine(extrapolate2, extrapolate3, trs);
            GizmoLine(extrapolate3, extrapolate0, trs);

            unitCubeEdges.Dispose();
            unitCubePlanes.Dispose();
        }

        static float4 GizmoLineExtrapolate(float4 v, TRS trs)
        {
            float4 extrapolatedV = v + (math.normalize(v) * 0.2f);
            GizmoLine(v, extrapolatedV, trs);
            return extrapolatedV;
        }

        static void GizmoLine(Edge e, TRS trs)
        {
            GizmoLine(e.Vertex0, e.Vertex1, trs);
        }

        static void GizmoLine(float4 a, float4 b, TRS trs)
        {
            Gizmos.DrawLine(
                trs.LocalToWorld(a).xyz, trs.LocalToWorld(b).xyz
            );
        }
    }
}