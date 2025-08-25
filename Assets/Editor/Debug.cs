using UnityEditor;
using UnityEngine;
using System.Reflection;

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class UniversalDebugInspector : Editor
{
    public override void OnInspectorGUI()
    {
        if (target == null)
        {
            EditorGUILayout.HelpBox("Target is null.", MessageType.Warning);
            return;
        }

        var t = target.GetType();

        // --- Safe guard for serializedObject ---
        if (serializedObject == null)
        {
            EditorGUILayout.HelpBox("SerializedObject is null.", MessageType.Warning);
            return;
        }

        // --- Serialized properties (Unity knows about these) ---
        try
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("=== Serialized Properties ===", EditorStyles.boldLabel);

            var prop = serializedObject.GetIterator();
            bool expanded = true;
            while (prop.NextVisible(expanded))
            {
                expanded = false;
                EditorGUILayout.PropertyField(prop, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Error drawing serializedObject: {e.Message}", MessageType.Error);
        }

        EditorGUILayout.Space();

        // --- Reflection fields (private + public) ---
        EditorGUILayout.LabelField("=== Reflection Fields ===", EditorStyles.boldLabel);

        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var f in t.GetFields(flags))
        {
            object value = null;
            try
            {
                value = f.GetValue(target);
            }
            catch (System.Exception e)
            {
                value = $"⚠ Error: {e.Message}";
            }

            EditorGUILayout.LabelField(f.Name, value != null ? value.ToString() : "null");
        }
    }

}
