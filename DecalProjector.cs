using System;
using System.Collections.Generic;
using Bolt;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
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
        Bounds m_bounds;
        TRS m_TRS = new TRS();
        MeshRenderer m_renderer;

        static Vector3[] m_scratchVertices;
        static int m_scratchVerticesCount;
        static int[] m_scratchIndices;
        static int m_scratchIndicesCount;
        static Vector3[] m_scratchNormals;
        static int m_scratchNormalsCount;
        static Vector2[] m_scratchUVs;
        static int m_scratchUVCount;

        JobHandle m_clippingJobHandle;
        bool m_outstandingClippingJob;
        ClippingJob m_clippingJob;

        bool m_wantsToScheduleNewJob;

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
                
            m_renderer = GetComponent<MeshRenderer>();
            m_meshFilter = GetComponent<MeshFilter>();
            if (m_bakeOnStart)
            {
                Bake();
            }
        }

        void GenerateScratchBuffers()
        {
            if (_lastMaxTriangles != MaxDecalTriangles ||
                m_scratchVertices == null ||
                m_scratchNormals == null ||
                m_scratchIndices == null ||
                m_scratchUVs == null
                )
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

        void OnDestroy()
        {
        }

        void UpdateTRS()
        {
            m_TRS.Update(transform.worldToLocalMatrix, transform.localToWorldMatrix, transform.position.ToFloat4());
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
                    m_wantsToScheduleNewJob = true;
                    _lastRotation = transform.rotation;
                    _lastPosition = transform.position;
                    _lastScale = transform.lossyScale;
                    _lastDisplacement = m_displacement;
                    _lastMaterial = m_decalMaterial;
                    _lastLayerMask = m_layerMask;
                }
            }

            if (m_wantsToScheduleNewJob && !m_outstandingClippingJob)
            {
                Bake();
                m_wantsToScheduleNewJob = false;
            }
        }

        
        void OnDrawGizmos()
        {
            UpdateTRS();

            if (Selection.activeGameObject != gameObject)
                return;
            
            foreach( var e in UnitCube.Edges)
                GizmoLine(e);
            
            Gizmos.color = Color.green;
            GizmoLine(UnitCube.e0);
            GizmoLine(UnitCube.e1);
            GizmoLine(UnitCube.e2);
            GizmoLine(UnitCube.e3);
            
            float4 extrapolate0 = GizmoLineExtrapolate(UnitCube.e0.Vertex0);
            float4 extrapolate1 = GizmoLineExtrapolate(UnitCube.e1.Vertex0);
            float4 extrapolate2 = GizmoLineExtrapolate(UnitCube.e2.Vertex0);
            float4 extrapolate3 = GizmoLineExtrapolate(UnitCube.e3.Vertex0);
            
            GizmoLine(extrapolate0, extrapolate1);
            GizmoLine(extrapolate1, extrapolate2);
            GizmoLine(extrapolate2, extrapolate3);
            GizmoLine(extrapolate3, extrapolate0);
        }

        float4 GizmoLineExtrapolate(float4 v)
        {
            float4 extrapolatedV = v + (math.normalize(v) * 0.2f);
            GizmoLine(v, extrapolatedV);
            return extrapolatedV;
        }

        void GizmoLine(Edge e)
        {
            GizmoLine(e.Vertex0, e.Vertex1);
        }

        void GizmoLine(float4 a, float4 b)
        {
            Gizmos.DrawLine(
                m_TRS.LocalToWorld(a).xyz, m_TRS.LocalToWorld(b).xyz
            );
        }
#endif

        public void Bake()
        {
            m_bounds = new Bounds(transform.position, transform.lossyScale);
            
            UpdateTRS();
            GenerateScratchBuffers();
            CreateMeshJob();
        }

        void AppendTriangleToScratchBuffers(Triangle t)
        {
            m_scratchIndicesCount++; // Already set
            m_scratchVertices[m_scratchVerticesCount++] = t.Vertex0.xyz;
            m_scratchNormals[m_scratchNormalsCount++] = t.Normal.xyz;
            m_scratchUVs[m_scratchUVCount].x = t.Vertex0.x + 0.5f;
            m_scratchUVs[m_scratchUVCount++].y = t.Vertex0.z + 0.5f;
            
            m_scratchIndicesCount++; // Already set
            m_scratchVertices[m_scratchVerticesCount++] = t.Vertex1.xyz;
            m_scratchNormals[m_scratchNormalsCount++] = t.Normal.xyz;
            m_scratchUVs[m_scratchUVCount].x = t.Vertex1.x + 0.5f;
            m_scratchUVs[m_scratchUVCount++].y = t.Vertex1.z + 0.5f;
            
            m_scratchIndicesCount++; // Already set
            m_scratchVertices[m_scratchVerticesCount++] = t.Vertex2.xyz;
            m_scratchNormals[m_scratchNormalsCount++] = t.Normal.xyz;
            m_scratchUVs[m_scratchUVCount].x = t.Vertex2.x + 0.5f;
            m_scratchUVs[m_scratchUVCount++].y = t.Vertex2.z + 0.5f;
        }
        
        void CreateMeshJob()
        {
            NativeArray<Triangle> sourceTriangleArray = new NativeArray<Triangle>(MaxDecalTriangles, Allocator.TempJob);
            int numSourceTriangles = GatherTriangles(sourceTriangleArray);
            
            NativeArray<int> numGeneratedTriangles = new NativeArray<int>(1, Allocator.TempJob);
            NativeArray<Triangle> generatedTriangleArray = new NativeArray<Triangle>(MaxDecalTriangles, Allocator.TempJob);
            
            m_clippingJob = new ClippingJob(numSourceTriangles, sourceTriangleArray, generatedTriangleArray, numGeneratedTriangles);

            //m_clippingJobHandle = m_clippingJob.Schedule();
            //m_outstandingClippingJob = true;
            
            m_clippingJob.Execute();
            HandleJob();
        }

        int GatherTriangles(NativeArray<Triangle> sourceTriangleArray)
        {
            TRS meshTRS = new TRS();
            int sourceTriangles = 0;

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
                
                Transform meshTransform = meshFilter.transform;
                meshTRS.Update(meshTransform.worldToLocalMatrix, meshTransform.localToWorldMatrix, meshTransform.position.ToFloat4());

                Vector3[] meshVertices = m.vertices;

                for (int submeshIndex = 0; submeshIndex < m.subMeshCount; submeshIndex++)
                {
                    int[] meshIndices = m.GetIndices(submeshIndex);
                    for (int meshIndex = 0; meshIndex < meshIndices.Length; meshIndex += 3)
                    {
                        // TODO, make triangle Transform modify the triangle instead of making a new one
                        Triangle tInMeshLocal = new Triangle(
                            meshVertices[meshIndices[meshIndex]].ToFloat4(),
                            meshVertices[meshIndices[meshIndex + 1]].ToFloat4(),
                            meshVertices[meshIndices[meshIndex + 2]].ToFloat4());
                        Triangle tInWorld = tInMeshLocal.LocalToWorld(meshTRS);
                        Triangle tInProjectorLocal = tInWorld.WorldToLocal(m_TRS);

                        sourceTriangleArray[sourceTriangles++] = tInProjectorLocal;
                        
                        if (sourceTriangles >= MaxDecalTriangles)
                        {
                            Debug.LogError($"Decal triangles exceeds max trianges {MaxDecalTriangles}.");
                            return sourceTriangles;
                        }
                    }
                }
            }

            return sourceTriangles;
        }

        void LateUpdate()
        {
            // Complete and handle results of clipping job, if there was one
            if (m_outstandingClippingJob)
            {
                m_clippingJobHandle.Complete();

                HandleJob();
                
                m_outstandingClippingJob = false;
            }
        }

        void HandleJob()
        {
            int numGeneratedTriangles = m_clippingJob.NumGeneratedTriangles[0];

            if (numGeneratedTriangles >= MaxDecalTriangles)
            {
                Debug.LogError($"Decal triangles exceeds max triangles {MaxDecalTriangles}.");
            }
                
            Debug.Log($"{numGeneratedTriangles} triangles");

            BuildMesh(m_clippingJob.GeneratedTriangles, numGeneratedTriangles);

            m_clippingJob.SourceTriangles.Dispose();
            m_clippingJob.GeneratedTriangles.Dispose();
            m_clippingJob.NumGeneratedTriangles.Dispose();
        }

        void BuildMesh(NativeArray<Triangle> triangleBuffer, int numTriangles)
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
            
            for( int i = 0; i < numTriangles; i++)
            {
                Triangle t = triangleBuffer[i];
                AppendTriangleToScratchBuffers(t.Offset(m_displacement));
            }
            
            m_mesh = new Mesh();
            m_mesh.SetVertices(m_scratchVertices, 0, m_scratchVerticesCount);
            m_mesh.SetIndices(m_scratchIndices, 0, m_scratchIndicesCount, MeshTopology.Triangles, 0);
            m_mesh.SetNormals(m_scratchNormals, 0, m_scratchNormalsCount);
            m_mesh.SetUVs(0, m_scratchUVs, 0, m_scratchUVCount);
            m_mesh.UploadMeshData(true);
                
            if (m_renderer != null)
            {
                m_renderer.sharedMaterial = m_decalMaterial;
            }
                
            if (m_meshFilter != null)
            {
                m_meshFilter.sharedMesh = m_mesh;
            }
        }
    }
}
