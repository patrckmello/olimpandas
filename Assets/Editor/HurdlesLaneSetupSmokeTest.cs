using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class HurdlesLaneSetupSmokeTest
{
    public static void Run()
    {
        Type setupType = Type.GetType("HurdlesLaneSetup, Assembly-CSharp");
        if (setupType == null)
            throw new Exception("HurdlesLaneSetup component is missing.");

        GameObject root = new GameObject("Lane setup test");
        try
        {
            Collider2D p1 = NewCollider("P1", root.transform);
            Collider2D p2 = NewCollider("P2", root.transform);
            Collider2D upper = NewCollider("Upper track", root.transform);
            Collider2D lower = NewCollider("Lower track", root.transform);
            MonoBehaviour setup = (MonoBehaviour)root.AddComponent(setupType);
            SerializedObject data = new SerializedObject(setup);
            data.FindProperty("player1").objectReferenceValue = p1;
            data.FindProperty("player2").objectReferenceValue = p2;
            data.FindProperty("upperLane").arraySize = 1;
            data.FindProperty("upperLane").GetArrayElementAtIndex(0).objectReferenceValue = upper;
            data.FindProperty("lowerLane").arraySize = 1;
            data.FindProperty("lowerLane").GetArrayElementAtIndex(0).objectReferenceValue = lower;
            data.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo configure = setupType.GetMethod("Configure");
            if (configure == null)
                throw new Exception("Lane setup has no Configure method.");
            configure.Invoke(setup, null);

            if (!Physics2D.GetIgnoreCollision(p1, p2) ||
                !Physics2D.GetIgnoreCollision(p1, lower) ||
                !Physics2D.GetIgnoreCollision(p2, upper))
                throw new Exception("Cross-lane collisions were not disabled.");
            if (Physics2D.GetIgnoreCollision(p1, upper) ||
                Physics2D.GetIgnoreCollision(p2, lower))
                throw new Exception("A player cannot interact with its own lane.");
            Debug.Log("HurdlesLaneSetupSmokeTest passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Collider2D NewCollider(string name, Transform parent)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.AddComponent<BoxCollider2D>();
    }
}
