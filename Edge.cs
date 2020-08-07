using UnityEngine;

namespace SimpleDecal
{
    public struct Edge
    {
        public Vector3 Vertex0 { get; }
        public Vector3 Vertex1 { get; }
        public Vector3 mid;
        public float length;

        public Edge(Vector3 a, Vector3 b)
        {
            Vertex0 = a;
            Vertex1 = b;

            Vector3 dir = a - b;
            length = dir.magnitude;
            mid = a + (dir.normalized * (length * 0.5f));
        }

        public Vector3 EdgeVector()
        {
            return Vertex1 - Vertex0;
        }

        public bool Contains(Vector3 point)
        {
            float testLength = (Vertex0 - point).magnitude + (Vertex1 - point).magnitude;
            return Mathf.Abs(length - testLength) < DecalProjector.ErrorTolerance;
        }
    }
}