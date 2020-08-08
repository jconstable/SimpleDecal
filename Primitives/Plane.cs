using Unity.Mathematics;
using UnityEditor.Build.Pipeline;

namespace SimpleDecal
{
    public struct Plane
    {
        public float4 Normal;
        public float Distance;
        
        public Plane(float4 a, float4 b, float4 c)
        {
            this = default;
            SetFrom(a,b,c);
        }

        public void SetFrom(float4 a, float4 b, float4 c)
        {
            float3 n3 = math.normalize(math.cross((b - a).xyz, (c - a).xyz));
            Normal = float4.zero;
            Normal.xyz = n3.xyz;
            Distance = -math.dot(Normal, a);
        }

        public bool Raycast(Ray r, out float dist)
        {
            float a = math.dot(r.Direction, Normal);
            float num = -math.dot(r.Origin, Normal) - Distance;
            if (math.abs(a - 0.0f) < DecalProjector.ErrorTolerance)
            {
                dist = 0f;
                return false;
            }
            dist = num / a;
            return (double) dist > 0.0;
        }
    }
}