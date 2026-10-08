using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class HurdleSmokeTest
{
    public static void Run()
    {
        Type hurdleType = Type.GetType("Hurdle, Assembly-CSharp");
        if (hurdleType == null)
            throw new Exception("Hurdle component is missing.");

        GameObject hurdle = new GameObject("Hurdle test");
        try
        {
            BoxCollider2D collider = hurdle.AddComponent<BoxCollider2D>();
            MonoBehaviour component = (MonoBehaviour)hurdle.AddComponent(hurdleType);
            MethodInfo hit = hurdleType.GetMethod("Hit");
            PropertyInfo isFallen = hurdleType.GetProperty("IsFallen");
            if (hit == null || isFallen == null)
                throw new Exception("Hurdle impact API is incomplete.");

            if ((bool)isFallen.GetValue(component))
                throw new Exception("A new hurdle starts fallen.");

            hit.Invoke(component, null);
            if (!(bool)isFallen.GetValue(component) || collider.enabled)
                throw new Exception("Impact did not drop the hurdle and clear its collider.");

            hit.Invoke(component, null);
            if (!(bool)isFallen.GetValue(component) || collider.enabled)
                throw new Exception("Repeated impact changed the fallen state.");

            Debug.Log("HurdleSmokeTest passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hurdle);
        }
    }
}
