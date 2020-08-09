using Unity.Mathematics;

namespace SimpleDecal
{
    // Small class that represent a ray
    public struct Ray
    {
        public float4 Origin;
        public float4 Direction;

        public Ray(float4 origin, float4 direction)
        {
            this = default;
            SetFrom(origin, direction);
        }
        
        public void SetFrom(float4 origin, float4 direction)
        {
            Origin = origin;
            Direction = math.normalize(direction);
        }

        public float4 GetPoint(float dist)
        {
            return Origin + (Direction * dist);
        }
    }
}