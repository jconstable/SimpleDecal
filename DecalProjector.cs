using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using EventProvider = System.Diagnostics.Eventing.EventProvider;

namespace SimpleDecal
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class DecalProjector : MonoBehaviour
    {
        public static readonly float ErrorTolerance = 0.005f;
        public static readonly int MaxDecalTriangles = 100;
        
        [SerializeField]
        public float m_displacement = 0.0001f;
        [SerializeField]
        public LayerMask m_layerMask = int.MaxValue; // Everything

        Mesh m_mesh;
        Bounds m_bounds;
        TRS m_TRS;
        MeshFilter m_meshFilter;

        // The results of the clipping job will be written to these buffers. The mesh will then be generated using them.
        static Vector3[] m_scratchVertices;
        static int m_scratchVerticesCount;
        static int[] m_scratchIndices;
        static int m_scratchIndicesCount;
        static Vector3[] m_scratchNormals;
        static int m_scratchNormalsCount;
        static Vector2[] m_scratchUVs;
        static int m_scratchUVCount;

        // Job info
        JobHandle m_clippingJobHandle;
        bool m_outstandingClippingJob;
        ClippingJob m_clippingJob;
        bool m_wantsToScheduleNewJob;



        // Start is called before the first frame update
        void Start()
        {
            m_meshFilter = GetComponent<MeshFilter>();
        }
        
        void OnDestroy()
        {
            DestroyMesh();
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

        // Editor-only logic for detecting if the user is moving the projector around in the scene. Currently,
        // automatic baking is only supported outside of playmode, in the Editor, for potential performance reasons.
        // These restrictions can be lifted if you want.
#if UNITY_EDITOR
        Quaternion _lastRotation;
        Vector3 _lastPosition;
        Vector3 _lastScale;
        float _lastDisplacement;
        Material _lastMaterial;
        int _lastLayerMask;
        int _lastMaxTriangles;

        // Update is called once per frame
        void Update()
        {
            if (!Application.isPlaying)
            {
                if (IsDirty())
                {
                    m_wantsToScheduleNewJob = true;
                    
                    _lastRotation = transform.rotation;
                    _lastPosition = transform.position;
                    _lastScale = transform.lossyScale;
                    _lastDisplacement = m_displacement;
                    _lastLayerMask = m_layerMask;
                }
            }

            if (m_wantsToScheduleNewJob && !m_outstandingClippingJob)
            {
                Bake();
                m_wantsToScheduleNewJob = false;
            }
        }

        // Has the GameObject or projector been configured differently or moved?
        bool IsDirty()
        {
            return !_lastRotation.Approximately(transform.rotation) ||
                !_lastPosition.Approximately(transform.position) ||
                !_lastScale.Approximately(transform.lossyScale) ||
                !Mathf.Approximately(_lastDisplacement, m_displacement) ||
                _lastLayerMask != m_layerMask;
        }

        void OnDrawGizmos()
        {
            UpdateSelfTRS();

            if (UnityEditor.Selection.activeGameObject != gameObject)
                return;

            DecalGizmo.DrawGizmos(m_TRS);
        }
#endif

        void UpdateSelfTRS()
        {
            m_TRS.Update(transform.worldToLocalMatrix, transform.localToWorldMatrix, transform.position.ToFloat4());
        }
        
        public void Bake()
        {
            m_bounds = new Bounds(transform.position, transform.lossyScale);
            m_outstandingClippingJob = true;
            
            UpdateSelfTRS();
            GenerateScratchBuffers();
            CreateMeshJob();
            
            m_clippingJobHandle = m_clippingJob.Schedule();
            
            // Wait for the job to complete in LateUpdate
        }

        // Create the job that will perform the clipping
        void CreateMeshJob()
        {
            NativeArray<Triangle> sourceTriangleArray = new NativeArray<Triangle>(MaxDecalTriangles, Allocator.TempJob);
            int numSourceTriangles = GatherTriangles(sourceTriangleArray);
            
            NativeArray<int> numGeneratedTriangles = new NativeArray<int>(1, Allocator.TempJob);
            NativeArray<Triangle> generatedTriangleArray = new NativeArray<Triangle>(MaxDecalTriangles, Allocator.TempJob);

            m_clippingJob = new ClippingJob();
            m_clippingJob.NumSourceTriangles = numSourceTriangles;
            m_clippingJob.SourceTriangles = sourceTriangleArray;
            m_clippingJob.GeneratedTriangles = generatedTriangleArray;
            m_clippingJob.NumGeneratedTriangles = numGeneratedTriangles;
            
            m_clippingJob.ScratchPoints = new NativeArray<float4>(10, Allocator.TempJob);
            m_clippingJob.ScratchTriangleEdges = new NativeArray<Edge>(3, Allocator.TempJob);
            m_clippingJob.ScratchTrianglePlanes = new NativeArray<Plane>(1, Allocator.TempJob);
            UnitCube.GenerateStructures(out m_clippingJob.UnitCubeEdges, out m_clippingJob.UnitCubePlanes);
        }

        void CleanUpMeshJob()
        {
            m_clippingJob.SourceTriangles.Dispose();
            m_clippingJob.GeneratedTriangles.Dispose();
            m_clippingJob.NumGeneratedTriangles.Dispose();
            
            m_clippingJob.ScratchPoints.Dispose();
            m_clippingJob.ScratchTriangleEdges.Dispose();
            m_clippingJob.ScratchTrianglePlanes.Dispose();
            m_clippingJob.UnitCubeEdges.Dispose();
            m_clippingJob.UnitCubePlanes.Dispose();
        }
        
        // Process a clipping job that has completed
        void HandleJob()
        {
            int numGeneratedTriangles = m_clippingJob.NumGeneratedTriangles[0];

            if (numGeneratedTriangles >= MaxDecalTriangles)
            {
                Debug.LogError($"Decal triangles exceeds max triangles {MaxDecalTriangles}.");
            }

            BuildMesh(m_clippingJob.GeneratedTriangles, numGeneratedTriangles);

            CleanUpMeshJob();
        }

        // Scan for MeshRenderers that overlap the projector, and collect all of their triangles to be clipped
        // into the projector
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

                // Iterate over the submeshes
                for (int submeshIndex = 0; submeshIndex < m.subMeshCount; submeshIndex++)
                {
                    // Iterate over every group of 3 indices that form triangles
                    int[] meshIndices = m.GetIndices(submeshIndex);
                    for (int meshIndex = 0; meshIndex < meshIndices.Length; meshIndex += 3)
                    {
                        // TODO, make triangle Transform modify the triangle instead of making a new one
                        Triangle tInMeshLocal = new Triangle(
                            meshVertices[meshIndices[meshIndex]].ToFloat4(),
                            meshVertices[meshIndices[meshIndex + 1]].ToFloat4(),
                            meshVertices[meshIndices[meshIndex + 2]].ToFloat4());
                        Triangle tInWorld = tInMeshLocal.LocalToWorld(meshTRS);
                        
                        // If the bounds of the individual triangle don't intersect with the unit cube bounds, we can
                        // ignore it
                        Bounds triangleBounds = BoundsFromTriangle(tInWorld);
                        if (!triangleBounds.Intersects(m_bounds))
                            continue;
       
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

        Bounds BoundsFromTriangle(Triangle t)
        {
            float4 center = (t.Vertex0 + t.Vertex1 + t.Vertex2) / 3f;

            float4 d0 = (center - t.Vertex0) * 2f;
            float4 d1 = (center - t.Vertex1) * 2f;
            float4 d2 = (center - t.Vertex2) * 2f;

            Bounds b = new Bounds(center.xyz,
                new Vector3(
                    Mathf.Max(math.abs(d0.x), math.abs(d1.x), math.abs(d2.x)),
                    Mathf.Max(math.abs(d0.y), math.abs(d1.y), math.abs(d2.y)),
                    Mathf.Max(math.abs(d0.z), math.abs(d1.z), math.abs(d2.z))
                ));
                    

            return b;
        }


        void LateUpdate()
        {
            // Complete and handle results of clipping job, if there was one
            if (m_outstandingClippingJob)
            {
                m_clippingJobHandle.Complete(); // Sync on the job

                HandleJob();
                
                m_outstandingClippingJob = false;
            }
        }

        // Construct the mesh from the job data
        void BuildMesh(NativeArray<Triangle> triangleBuffer, int numTriangles)
        {
            DestroyMesh();
            
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

            if (m_meshFilter != null)
            {
                m_meshFilter.sharedMesh = m_mesh;
            }
        }
        
        void DestroyMesh()
        {
            if (m_mesh != null)
#if UNITY_EDITOR
                DestroyImmediate(m_mesh);
#else
            Destroy(_mesh);
#endif
        }
        
        // Fill out the scratch buffers with a triangle's data
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
    }
}
