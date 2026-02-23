
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3[] AllFlatDirections = { Vector3.forward, Vector3.right, -Vector3.forward, -Vector3.right };

#region rounding

        public static Vector2 SnapToGrid(this Vector2 u,float size)
        {
            return (u/size).Round()*size;
        }

        public static Vector2 Round(this Vector2 u)
        {
            return new Vector2(Mathf.Round(u.x), Mathf.Round(u.y));
        }
        
        public static Vector3 Round(this Vector3 u)
        {
            return new Vector3(Mathf.Round(u.x), Mathf.Round(u.y), Mathf.Round(u.z));
        }
        
        public static Vector2Int RoundToInt(this Vector2 u)
        {
            return new Vector2Int(Mathf.RoundToInt(u.x), Mathf.RoundToInt(u.y));
        }
        
        public static Vector3Int RoundToInt(this Vector3 u)
        {
            return new Vector3Int(Mathf.RoundToInt(u.x), Mathf.RoundToInt(u.y), Mathf.RoundToInt(u.z));
        }
        
        public static Vector2 Floor(this Vector2 u)
        {
            return new Vector2(Mathf.Floor(u.x), Mathf.Floor(u.y));
        }
        
        public static Vector3 Floor(this Vector3 u)
        {
            return new Vector3(Mathf.Floor(u.x), Mathf.Floor(u.y), Mathf.Floor(u.z));
        }
        
        public static Vector2Int FloorToInt(this Vector2 u)
        {
            return new Vector2Int(Mathf.FloorToInt(u.x), Mathf.FloorToInt(u.y));
        }
        
        public static Vector3Int FloorToInt(this Vector3 u)
        {
            return new Vector3Int(Mathf.FloorToInt(u.x), Mathf.FloorToInt(u.y), Mathf.FloorToInt(u.z));
        }
        
        public static Vector2 Ceil(this Vector2 u)
        {
            return new Vector2(Mathf.Ceil(u.x), Mathf.Ceil(u.y));
        }
        
        public static Vector3 Ceil(this Vector3 u)
        {
            return new Vector3(Mathf.Ceil(u.x), Mathf.Ceil(u.y), Mathf.Ceil(u.z));
        }
        
        public static Vector2Int CeilToInt(this Vector2 u)
        {
            return new Vector2Int(Mathf.CeilToInt(u.x), Mathf.CeilToInt(u.y));
        }
        
        public static Vector3Int CeilToInt(this Vector3 u)
        {
            return new Vector3Int(Mathf.CeilToInt(u.x), Mathf.CeilToInt(u.y), Mathf.CeilToInt(u.z));
        }
        
#endregion
        
        //clamping
        public static bool ClampRangeInBounds(ref this Vector2 u, Vector2 min, Vector2 max)
        {
            bool clamped = false;
            
            Vector2 v = new Vector2(
                Mathf.Clamp(u.x, min.x, max.x),
                Mathf.Clamp(u.y, min.y, max.y));

            if (u != v)
            {
                clamped = true;
                u = v;
            }

            return clamped;
        }
        
        //swizzle
        public static Vector2 XY(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }
        
        public static Vector3 Flattened(this Vector3 v)
        {
            v.y = 0;
            return v;
        }
    
        //movement
        public static Vector3 MoveToward(this Vector3 from,Vector3 target,float maxDelta)
        {
            return from + Vector3.ClampMagnitude(target - from, maxDelta);
        }
    
        public static Vector2 MoveToward(this Vector2 from,Vector2 target,float maxDelta)
        {
            return from + Vector2.ClampMagnitude(target - from, maxDelta);
        }
        
        //distance
        
        /// <summary>
        /// retourne la distance au carré vers le point donné
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float SqrDistanceTo(this Vector2 u, Vector2 v)
        {
            return (u - v).sqrMagnitude;
        }
        
        public static float SqrDistanceTo(this Vector3 u, Vector3 v)
        {
            return (u - v).sqrMagnitude;
        }
        
        //lists
        public static Vector3 FindClosest(this List<Vector3> points, Vector3 origin)
        {
            if (points.Count == 0)
            {
                return Vector3.zero;
            }
            if (points.Count == 1)
            {
                return points[0];
            }

            Vector3 closest = points[0];

            foreach (Vector3 point in points)
            {
                Vector3 pointOffset = point - origin;
                Vector3 closestOffset = closest - origin;

                if (pointOffset.sqrMagnitude < closestOffset.sqrMagnitude)
                    closest = point;
            }

            return closest;
        }
    }
}