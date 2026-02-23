using System;
using UnityEditor;
using UnityEngine;

namespace NathanTazi
{
    
    /// <summary>
    /// contient un objet Lsystem pour générer la structure d'un arbre procédural.
    /// </summary>
    public class LSystemGenerator : MonoBehaviour 
    {
        [SerializeField] public Lsystem3D lsystem;
        public PlantGraph Graph;
        public Tuple<Vector3, Vector3> BoundingBox;
    
        [Header("Generation")] 
        [SerializeField] protected string _axiom;//l'étape 0 de la simulation.
        [SerializeField] protected int iterations = 5;
        [SerializeField] protected float bbMargin = .1f;
    
        [ContextMenu("Refresh graph")]
        public void RefreshGraph()
        {
            print("Refreshing graph");
            lsystem.Symbols = _axiom;
            lsystem._rules.Refresh();
            lsystem.Simulate(iterations);
            Graph = lsystem.GetGraph();
            BoundingBox = Graph.GetBoundingBox(bbMargin);
        
            gameObject.SendMessageUpwards("OnLSystemRegenerated",SendMessageOptions.DontRequireReceiver);
        }
    
        #if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
        }

        static void OnEditorSceneManagerSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            //Refresh every generator in the scene   
            LSystemGenerator[] generators = scene.GetRootGameObjects()[0].GetComponentsInChildren<LSystemGenerator>();
            foreach (LSystemGenerator generator in generators)
            {
                generator.RefreshGraph();
            }
        }
        #endif
        
        protected virtual void OnValidate() => RefreshGraph();
        protected virtual void Awake() => RefreshGraph();
    }


#if UNITY_EDITOR
[CustomEditor(typeof(LSystemGenerator))]
public class LsystemGeneratorEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        if (target is LSystemGenerator)
        {
            LSystemGenerator t = (LSystemGenerator)target;
            
            GUIStyle style = GUI.skin.box;
            style.richText = true;
            style.alignment = TextAnchor.UpperLeft;
            GUILayout.Box(
                "<color=white>\nUn Lsystem permettant de générer des arbres en 3D avec les symboles suivant :\n\n" +
                "  f -> avancer\n" +
                "  x -> avancer et placer une feuille\n" +
                "  -+ -> tourner (pitch)\n" +
                "  <> -> tourner (yaw)\n" +
                "  .: -> tourner (roll)\n" +
                "  [] -> empiler, dépiler l'état de la tortue\n" +
                "  () -> déclarer le début ou la fin d'une nouvelle itération\n" +
                "  / -> enlever un symbole\n" +
                "  1,2,3,4... -> pour diviser le rayon de la branche\n"
                ,style);
            
            base.OnInspectorGUI();
            GUILayout.Space(5);
            if (GUILayout.Button("ReSeed"))
            {
                t.lsystem.seed = UnityEngine.Random.Range(0, int.MaxValue);
                t.RefreshGraph();
            }
            if (GUILayout.Button("Refresh graph"))
            {
                t.RefreshGraph();
            }
            GUILayout.Space(10);
            GUILayout.Label("Segment count : " + t.Graph.segments.Count);
            GUILayout.Label("Leaf count : " + t.Graph.leaves.Count);
        }
        
    }
}
#endif

}