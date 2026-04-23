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
            foreach (PlantGraph.Leaf leaf in generator.Graph.leaves)
            {
                //Gizmos.DrawSphere(transform.TransformPoint(leaf),0.05f);
            }
        
            Gizmos.color = Color.grey;
            Gizmos.DrawWireCube( 
                transform.TransformPoint(generator.BoundingBoxLs.center),
                transform.TransformVector(generator.BoundingBoxLs.size));
        }

        void OnDrawGizmos()
        {
            Draw();
        }
    }
}
