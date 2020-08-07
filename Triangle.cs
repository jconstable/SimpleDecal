using UnityEngine;

namespace SimpleDecal
{
    struct Triangle
    {
        public Edge Edge0 { get; private set; }
        public Edge Edge1 { get; private set; }
        public Edge Edge2 { get; private set; }
        public Vector3 normal { get; private set; }
        public Plane plane { get; private set; }
        public Vector3 Vertex0 { get; private set; }
        public Vector3 Vertex1 { get; private set; }
        public Vector3 Vertex2 { get; private set; }

        public Triangle(Vector3 aIn, Vector3 bIn, Vector3 cIn)
        {
            Edge0 = new Edge(aIn, bIn);
            Edge1 = new Edge(bIn, cIn);
            Edge2 = new Edge(cIn, aIn);

            Vertex0 = aIn;
            Vertex1 = bIn;
            Vertex2 = cIn;

            plane = new Plane(aIn, bIn, cIn);
            normal = plane.normal;
        }

        public Triangle World2Local(Transform t)
        {
            return new Triangle(
                Vertex0.WorldToLocal(t),
                Vertex1.WorldToLocal(t),
                Vertex2.WorldToLocal(t)
            );
        }

        public Triangle Local2World(Transform t)
        {
            return new Triangle(
                Vertex0.LocalToWorld(t),
                Vertex1.LocalToWorld(t),
                Vertex2.LocalToWorld(t)
            );
        }

        public Triangle Offset(float distance)
        {
            var dir = normal * distance;
            return new Triangle(
                Vertex0 + dir,
                Vertex1 + dir,
                Vertex2 + dir);
        }

        public float Area()
        {
            Vector3 ab = Edge0.EdgeVector();
            Vector3 ac = Edge1.EdgeVector();
            float abLength = ab.magnitude;
            float acLength = ac.magnitude;
            float theta = Mathf.Acos(Vector3.Dot(ab, ac) / (abLength * acLength));

            return 0.5f * abLength * acLength * Mathf.Sin(theta);
        }

        public bool Contains(Vector3 position)
        {
            float area = Area();

            float a1 = new Triangle(Vertex0, Vertex1, position).Area();
            float a2 = new Triangle(Vertex1, Vertex2, position).Area();
            float a3 = new Triangle(Vertex2, Vertex0, position).Area();

            // Position is inside triangle of the sum of the areas of the three new triangle made using position
            // equals the area of the whole triangle
            return Mathf.Abs(area - (a1 + a2 + a3)) < DecalProjector.ErrorTolerance;
        }
    }
}