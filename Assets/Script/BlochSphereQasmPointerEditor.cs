#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlochSphereQasmPointer))]
public class BlochSphereQasmPointerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var comp = (BlochSphereQasmPointer)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Run", GUILayout.Height(28)))
        {
            comp.Run();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Point +Z", GUILayout.Height(28)))
        {
            comp.TestNorth();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Point +X", GUILayout.Height(28)))
        {
            comp.TestEquatorX();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Paste QASM in 'QASM Source', choose 'showQubit', then click Run.\n" +
            "Pointer is created as a child named __BlochPointer.",
            MessageType.Info);
    }

    // Optional: menu item (Tools ▸ Quantum ▸ Run QASM on Selected)
    [MenuItem("Tools/Quantum/Run QASM on Selected", false, 0)]
    static void RunOnSelected()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var p = obj.GetComponent<BlochSphereQasmPointer>();
            if (p) { p.Run(); }
        }
    }
}
#endif
