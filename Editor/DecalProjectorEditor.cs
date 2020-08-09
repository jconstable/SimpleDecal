using System.Collections;
using System.Collections.Generic;
using SimpleDecal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleDecal
{
    [InitializeOnLoad]
    public class DecalProjectorEditor
    {
        [MenuItem("GameObject/3D Object/Decal Projector", false, 0)]
        public static void AddProjector()
        {
            GameObject newGO = new GameObject("Decal Projector");
            if (Selection.activeGameObject != null)
            {
                newGO.transform.parent = Selection.activeGameObject.transform;
                newGO.transform.localPosition = Vector3.zero;
                newGO.transform.localRotation = Quaternion.identity;
            }

            newGO.AddComponent<MeshFilter>();
            MeshRenderer rend = newGO.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = ShadowCastingMode.Off;
            
            DecalProjector proj = newGO.AddComponent<DecalProjector>();
            
            // Build Material for current pipeline
            var pipeline = GraphicsSettings.renderPipelineAsset;
            
            Material m;
            Texture2D defaultTex = Resources.Load<Texture2D>("DefaultDecal");
            if (pipeline == null)
            {
                // Built-in renderer
                Shader s = Shader.Find("Mobile/BumpedDiffuse");
                m = new Material(s);
                m.SetTexture("_MainTex", defaultTex);
                m.SetColor("_Color", Color.white);
            }
            else
            {
                Shader s = Shader.Find("HDRP/Decal");
                if (s == null)
                {
                    s = Shader.Find("");
                }
                else
                {
                    Debug.LogWarning("HDRP has its own decal system, which you should use instead of SimpleDecal.");
                }

                if (s == null)
                {
                    Debug.LogWarning("Unable to find default shader for either URP or HDRP. Using pipeline default.");
                    s = pipeline.defaultShader;
                }
                
                m = new Material(s);
         
                m.SetTexture("_BaseMap", defaultTex); // URP
                m.SetTexture("_BaseColorMap", defaultTex); // HDRP
                m.SetColor("_BaseColor", Color.white);
                m.SetColor("_Color", Color.white);
            }
            rend.material = m;
            proj.Bake();

            Selection.activeGameObject = newGO;
        }
    }
}