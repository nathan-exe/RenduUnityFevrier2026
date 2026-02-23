using UnityEngine;

namespace NathanTazi
{
    public class LSystemWireframeRenderer : MonoBehaviour
    {
        [SerializeField]
        private LSystemGenerator generator;
        void Draw()
        {
        
            foreach (Segment segment in generator.Graph.segments)
            {
                //Gizmos.color = Color.Lerp( Color.red , Color.black,segment.age*segment.age*segment.age);
                Gizmos.color = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.Range(.4f,.6f), UnityEngine.Random.Range(.7f,.9f));
                Gizmos.DrawLine(transform.TransformPoint(segment.a) ,transform.TransformPoint(segment.b));
            }

            Gizmos.color =  Color.red;
            foreach (Vector3 leaf in generator.Graph.leaves)
            {
                //Gizmos.DrawSphere(transform.TransformPoint(leaf),0.05f);
            }
        
            Gizmos.color = Color.grey;
            Vector3 min = transform.TransformPoint(generator.BoundingBox.Item1);
            Vector3 max = transform.TransformPoint(generator.BoundingBox.Item2);
            //Gizmos.DrawWireCube((min + max)*.5f,(max - min));
        }

        void OnDrawGizmos()
        {
            Draw();
        }
    }
}
