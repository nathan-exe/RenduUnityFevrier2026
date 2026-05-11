using UnityEngine;

namespace NathanTazi
{
    public class LSystemWireframeRenderer : MonoBehaviour
    {
        [SerializeField]
        private float thickness = .2f;
        [SerializeField]
        private LSystemGenerator generator;
        void Draw()
        {
            Random.InitState(generator.lsystem.seed);
            foreach (Segment segment in generator.Graph.segments)
            {
                //Gizmos.color = Color.Lerp( Color.red , Color.black,segment.age*segment.age*segment.age);
                Gizmos.color = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.Range(.4f,.6f), UnityEngine.Random.Range(.7f,.9f));

                Vector3 ABws = transform.TransformPoint(segment.b) - transform.TransformPoint(segment.a);
                Gizmos.matrix = Matrix4x4.LookAt(transform.TransformPoint(segment.a), transform.TransformPoint(segment.b), Vector3.up);
                Gizmos.DrawCube(Vector3.forward*.5f*ABws.magnitude,new Vector3(thickness,thickness,ABws.magnitude));
                
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawLine(transform.TransformPoint(segment.a) ,transform.TransformPoint(segment.b));
            }

            Gizmos.matrix = Matrix4x4.identity;
            
            Gizmos.color =  Color.red;
            foreach (PlantGraph.Leaf leaf in generator.Graph.leaves)
            {
                //Gizmos.DrawSphere(transform.TransformPoint(leaf),0.05f);
            }

            return;
            Gizmos.color = Color.grey;
            Gizmos.DrawWireCube( 
                transform.TransformPoint(generator.BoundingBoxLs.center),
                transform.TransformVector(generator.BoundingBoxLs.size));
        }

        void OnDrawGizmos()
        {
            if(enabled)
                Draw();
        }
    }
}
