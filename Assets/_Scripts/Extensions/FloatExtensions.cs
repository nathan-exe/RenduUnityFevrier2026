namespace _scripts.Extensions
{
    public static class FloatExtensions
    {
        /// <summary>
        /// prend une moyenne et la met à jour en ajoutant un nouvel élément dedans.
        /// </summary>
        /// <param name="averageOfValues"></param>
        /// <param name="valueCount"></param>
        /// <param name="newValueToAccumulate"></param>
        public static void AccumulateAverage(ref this float averageOfValues, ref int valueCount, float newValueToAccumulate)
        {
            float currentSum = averageOfValues * valueCount;
            float newSum = currentSum + newValueToAccumulate;
        
            valueCount++;
            float newAverage = newSum / valueCount;
            averageOfValues = newAverage;
        }

        public static float RemapRange(ref this float v, float fromA, float fromB, float toA, float toB)
        {
            return (v - fromA) / (fromB - fromA) * (toB - toA) + toA;
        }
        
        public static float RemapRange(ref this float v, float toA, float toB)
        {
            return v * (toB - toA) + toA;
        }

    }
}
