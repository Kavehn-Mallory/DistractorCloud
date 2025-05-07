using Unity.Mathematics;

namespace DistractorClouds.Noise
{
    public static class RandomExtensions
    {
        public static float2 GetPointOnUnitCircleCircumference(this ref Random random)
        {
            //this is a bit weird but nextFloat seems to define the max element as exclusive, but I guess that would work in our favour here? 
            float randomAngle = random.NextFloat() * math.PI2;
            return math.normalize(new float2(math.sin(randomAngle), math.cos(randomAngle)));
        }
        
        public static float2 NextRandomPointInsideUnitCircle(this ref Unity.Mathematics.Random random)
        {
            return math.sqrt(random.NextFloat()) * random.NextSinCos();
        }


        public static float2 NextSinCos(this ref Unity.Mathematics.Random random)
        {
            math.sincos(random.NextFloat(0, math.TAU), out float sin, out float cos);
            return new float2(sin, cos);
        }

        public static float2 NextRandomPointInside2RadiusCircle(this ref Random random, float radius)
        {
            var angle = random.NextFloat() * math.PI2;

            // Random radius between r and 2r

            var r = random.NextFloat(radius, 2 * radius);

            // Convert polar coordinates to cartesian and viola,

            // a new point is generated around the source point (x, y)

            return new float2(r * math.cos(angle), r * math.sin(angle));

        }

        public static void RestrictToGridDimensions(this ref float2 value, float width, float height)
        {
            value = math.max(0, math.min(value, new float2(width, height)));
        }
        
        
    }
}