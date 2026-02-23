using UnityEngine;

namespace _Scripts.Extensions
{
    public static class RectExtensions
    {
        public static Vector2 PickRandomPointInside(this Rect rect)
        {
            return new Vector2(
                Random.Range(rect.xMin, rect.xMax),
                Random.Range(rect.yMin, rect.yMax));
        }
        
        public static Vector2 PickRandomPointInside(this RectInt rect)
        {
            return new Vector2(
                Random.Range((float)rect.xMin, rect.xMax),
                Random.Range((float)rect.yMin, rect.yMax));
        }
    }
}