using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SimpleDecal
{
    [ExecuteInEditMode]
    public class DecalProjector : MonoBehaviour
    {
        public static readonly float ErrorTolerance = 0.005f;
        public static readonly int MaxDecalTriangles = 100;
        
        [SerializeField]
        public float m_displacement = 0.0001f;
        [SerializeField]
        public bool m_bakeOnStart;
        [SerializeField]
        public MeshFilter m_meshFilter;
        [SerializeField]
        public Material m_decalMaterial;
        [SerializeField]
        public LayerMask m_layerMask = int.MaxValue; // Everything

        Mesh m_mesh;
        List<Vector3> m_points = new List<Vector3>();
        Bounds m_bounds;

        Vector3[] m_scratchVertices;
        int m_scratchVerticesCount;
        int[] m_scratchIndices;
        int m_scratchIndicesCount;
        Vector3[] m_scratchNormals;
        int m_scratchNormalsCount;
        Vector2[] m_scratchUVs;
        int m_scratchUVCount;
        List<Triangle> m_scratchTris = new List<Triangle>();

#if UNITY_EDITOR
        Quaternion _lastRotation;
        Vector3 _lastPosition;
        Vector3 _lastScale;
        float _lastDisplacement;
        Material _lastMaterial;
        int _lastLayerMask;
        int _lastMaxTriangles;
#endif

        public void SetMaterial(Material m)
        {
            m_decalMaterial = m;
        }

        // Start is called before the first frame update
        void Start()
        {
            GenerateScratchBuffers();
                
            m_meshFilter = GetComponent<MeshFilter>();
            if (m_bakeOnStart)
            {
                Bake();
            }
        }

        void GenerateScratchBuffers()
        {
            if (_lastMaxTriangles != MaxDecalTriangles)
            {
                m_scratchVertices = new Vector3[MaxDecalTriangles * 3];
                m_scratchNormals = new Vector3[MaxDecalTriangles * 3];
                m_scratchIndices = new int[MaxDecalTriangles * 3];
                m_scratchUVs = new Vector2[MaxDecalTriangles * 3];
                for (int i = 0; i < MaxDecalTriangles * 3; i++)
                {
                    m_scratchIndices[i] = i;
                }

                _lastMaxTriangles = MaxDecalTriangles;
            }
        }

#if UNITY_EDITOR
        // Update is called once per frame
        void Update()
        {
            if (!Application.isPlaying)
            {
                if (!_lastRotation.Approximately(transform.rotation) ||
                    !_lastPosition.Approximately(transform.position) ||
                    !_lastScale.Approximately(transform.lossyScale) ||
                    !Mathf.Approximately(_lastDisplacement, m_displacement) ||
                    _lastMaterial != m_decalMaterial ||
                    _lastLayerMask != m_layerMask)
                {
                    Bake();
                    _lastRotation = transform.rotation;
                    _lastPosition = transform.position;
                    _lastScale = transform.lossyScale;
                    _lastDisplacement = m_displacement;
                    _lastMaterial = m_decalMaterial;
                    _lastLayerMask = m_layerMask;
                }
            }
        }
#endif
        
        void OnDrawGizmos()
        {
            foreach( var e in UnitCube.Edges)
                GizmoLine(e);
            
            Gizmos.color = Color.green;
            GizmoLine(UnitCube.e0);
            GizmoLine(UnitCube.e1);
            GizmoLine(UnitCube.e2);
            GizmoLine(UnitCube.e3);
            
            Vector3 extrapolate0 = GizmoLineExtrapolate(UnitCube.e0.Vertex0);
            Vector3 extrapolate1 = GizmoLineExtrapolate(UnitCube.e1.Vertex0);
            Vector3 extrapolate2 = GizmoLineExtrapolate(UnitCube.e2.Vertex0);
            Vector3 extrapolate3 = GizmoLineExtrapolate(UnitCube.e3.Vertex0);
            
            GizmoLine(extrapolate0, extrapolate1);
            GizmoLine(extrapolate1, extrapolate2);
            GizmoLine(extrapolate2, extrapolate3);
            GizmoLine(extrapolate3, extrapolate0);
        }

        Vector3 GizmoLineExtrapolate(Vector3 v)
        {
            Vector3 extrapolatedV = v + (v.normalized * 0.2f);
            GizmoLine(v, extrapolatedV);
            return extrapolatedV;
        }

        void GizmoLine(Edge e)
        {
            GizmoLine(e.Vertex0, e.Vertex1);
        }

        void GizmoLine(Vector3 a, Vector3 b)
        {
            Gizmos.DrawLine(
                a.LocalToWorld(transform),
                b.LocalToWorld(transform)
            );
        }

        public static float Tolerant(float v)
        {
            return v > 0f ? v + ErrorTolerance : v - ErrorTolerance;
        }

        bool Contains(Vector3 v)
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

        public void Bake()
        {
            GenerateScratchBuffers();
            
            m_bounds = new Bounds(transform.position, transform.lossyScale);
            GenerateMesh();

            if (m_meshFilter != null)
            {
                m_meshFilter.sharedMesh = m_mesh;
            }
        }

        void CopyInto(Triangle t)
        {
            m_scratchIndicesCount++; // Already set
            m_scratchVertices[m_scratchVerticesCount++] = t.Vertex0;
            m_scratchNormals[m_scratchNormalsCount++] = t.normal;
            m_scratchUVs[m_scratchUVCount].x = t.Vertex0.x + 0.5f;
            m_scratchUVs[m_scratchUVCount++].y = t.Vertex0.z + 0.5f;
            
            m_scratchIndicesCount++; // Already set
            m_scratchVertices[m_scratchVerticesCount++] = t.Vertex1;
            m_scratchNormals[m_scratchNormalsCount++] = t.normal;
            m_scratchUVs[m_scratchUVCount].x = t.Vertex1.x + 0.5f;
            m_scratchUVs[m_scratchUVCount++].y = t.Vertex1.z + 0.5f;
            
            m_scratchIndicesCount++; // Already set
            m_scratchVertices[m_scratchVerticesCount++] = t.Vertex2;
            m_scratchNormals[m_scratchNormalsCount++] = t.normal;
            m_scratchUVs[m_scratchUVCount].x = t.Vertex2.x + 0.5f;
            m_scratchUVs[m_scratchUVCount++].y = t.Vertex2.z + 0.5f;
        }
        
        void GenerateMesh()
        {
            if (m_mesh != null)
#if UNITY_EDITOR
                DestroyImmediate(m_mesh);
#else
            Destroy(_mesh);
#endif
            m_scratchVerticesCount = 0;
            m_scratchIndicesCount = 0;
            m_scratchNormalsCount = 0;
            m_scratchUVCount = 0;

            GatherTringles();

            m_mesh = new Mesh();
            m_mesh.SetVertices(m_scratchVertices, 0, m_scratchVerticesCount);
            m_mesh.SetIndices(m_scratchIndices, 0, m_scratchIndicesCount, MeshTopology.Triangles, 0);
            m_mesh.SetNormals(m_scratchNormals, 0, m_scratchNormalsCount);
            m_mesh.SetUVs(0, m_scratchUVs, 0, m_scratchUVCount);
            m_mesh.UploadMeshData(true);
            MeshRenderer rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = m_decalMaterial;
            }
        }

        int GatherTringles()
        {
            int num = 0;
            foreach (var meshFilter in FindObjectsOfType<MeshFilter>())
            {
                // Filter out object by layer mask
                int mask = 1 << meshFilter.gameObject.layer;
                if ((mask & m_layerMask) != mask)
                    continue;
                
                // Filter out objects that are themselves decal projectors
                if (meshFilter.GetComponent<DecalProjector>() != null)
                    continue;

                // Filter out objects by render bounds
                Renderer r = meshFilter.GetComponent<Renderer>();
                if (!r.bounds.Intersects(m_bounds))
                {
                    continue;
                }

                // Filter out objects with no mesh
                Mesh m = meshFilter.sharedMesh;
                if (m == null)
                    continue;

                Vector3[] meshVertices = m.vertices;
                for (int submeshIndex = 0; submeshIndex < m.subMeshCount; submeshIndex++)
                {
                    int[] meshIndices = m.GetIndices(submeshIndex);
                    for (int meshIndex = 0; meshIndex < meshIndices.Length; meshIndex += 3)
                    {
                        Transform meshTransform = meshFilter.transform;
                        Triangle tInMeshLocal = new Triangle(
                            meshVertices[meshIndices[meshIndex]],
                            meshVertices[meshIndices[meshIndex + 1]],
                            meshVertices[meshIndices[meshIndex + 2]]);
                        Triangle tInWorld = tInMeshLocal.Local2World(meshTransform);
                        Triangle tInProjectorLocal = tInWorld.World2Local(transform);

                        CollectClippedTriangles(tInProjectorLocal);
                        foreach(var newTriangle in m_scratchTris)
                        {
                            if (num >= MaxDecalTriangles)
                            {
                                Debug.LogError($"Decal triangles exceeds max trianges {MaxDecalTriangles}.");
                                return num;
                            }
                            CopyInto(newTriangle.Offset(m_displacement));
                            num++;
                        }
                    }
                }
            }

            return num;
        }

        
        void CollectClippedTriangles(Triangle t)
        {
            m_scratchTris.Clear();;
            m_points.Clear();
            
            // Some verts may actually be directly inside the projector
            if (Contains(t.Vertex0)) m_points.Add(t.Vertex0);
            if (Contains(t.Vertex1)) m_points.Add(t.Vertex1);
            if (Contains(t.Vertex2)) m_points.Add(t.Vertex2);

            // Early out
            // If all three points of the triangle were in the projector, we don't need to do anything.
            if (m_points.Count == 3)
            {
                m_scratchTris.Add(t);
                return;
            }
            
            // Test each projector plane against the edges.
            ClipToPlanes(new Edge[] {t.Edge0, t.Edge1, t.Edge2 }, UnitCube.Planes, m_points, point => Contains(point));
            
            // Test each projector edge against the triangle plane
            ClipToPlanes(UnitCube.Edges, new Plane[] { t.plane }, m_points, point => t.Contains(point));

            // No points
            if (m_points.Count == 0)
                return;
            
            // Sort the points by dot product around the median.
            Vector3 middle = MiddlePoint(m_points);
            Vector3 orientation = middle - m_points[0];
            m_points.Sort((a,b) => SignedAngle(orientation, middle - a, t.normal).CompareTo(SignedAngle(orientation, middle - b, t.normal)));
            
            // Create triangles from the points
            for (int i = 0; i <= m_points.Count - 1; i++)
            {
                m_scratchTris.Add(new Triangle(
                    middle, m_points[i], m_points[(i+1) % m_points.Count]
                    ));
            }
        }

        float SignedAngle(Vector3 a, Vector3 b, Vector3 normal)
        {
            float angle = Vector3.Angle(a, b);
            Vector3 cross = Vector3.Cross(a, b);
            if (Vector3.Dot(normal, cross) < 0f)
            {
                return -angle;
            }

            return angle;
        }

        Vector3 MiddlePoint(List<Vector3> list)
        {
            Vector3 sum = Vector3.zero;
            foreach (var v in list)
            {
                sum += v;
            }

            return sum / list.Count;
        }

        delegate bool TestPoint(Vector3 v);

        void ClipToPlanes(Edge[] edges, Plane[] planes, List<Vector3> points, TestPoint testFunction)
        {
            foreach (var edge in edges)
            {
                Ray r = new Ray(edge.Vertex0, (edge.Vertex1 - edge.Vertex0).normalized);

                foreach (Plane p in planes)
                {
                    float dist;
                    p.Raycast(r, out dist);
                    float absDist = Mathf.Abs(dist);
                    if (absDist > 0f)
                    {
                        Vector3 pt = r.GetPoint(dist);
                        if (edge.Contains(pt))
                        {
                            if (testFunction(pt))
                            {
                                points.Add(pt);
                            }
                        }
                    }
                }
            }
        }
    }
}
