using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Extensions
{
    public static class TransformExtensions
    {
        public static Transform FindClosest(this List<Transform> transforms, Vector3 origin)
        {
            if (transforms.Count == 0)
            {
                return null;
            }
            if (transforms.Count == 1)
            {
                return transforms[0];
            }

            Transform closest = transforms[0];

            foreach (Transform t in transforms)
            {
                Vector3 pointOffset = t.position - origin;
                Vector3 closestOffset = closest.position - origin;

                if (pointOffset.sqrMagnitude < closestOffset.sqrMagnitude)
                    closest = t;
            }

            return closest;

        }
    }
}
