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

    public static void RunSlowOnPlayerImpact()
    {
        GameObject hurdle = new GameObject("Hurdle slow test");
        GameObject player = new GameObject("Player slow test");
        GameObject slowVfxPrefab = new GameObject("Slow VFX test prefab");
        try
        {
            hurdle.AddComponent<BoxCollider2D>();
            Hurdle component = hurdle.AddComponent<Hurdle>();
            player.AddComponent<BoxCollider2D>();
            PlayerController controller = player.AddComponent<PlayerController>();
            PlayerStatusEffects effects = player.AddComponent<PlayerStatusEffects>();
            Transform anchor = new GameObject("Status anchor").transform;
            anchor.SetParent(player.transform);

            SerializedObject effectData = new SerializedObject(effects);
            effectData.FindProperty("statusVfxAnchor").objectReferenceValue = anchor;
            effectData.FindProperty("slowStatusPrefab").objectReferenceValue = slowVfxPrefab;
            effectData.ApplyModifiedPropertiesWithoutUndo();
            typeof(PlayerStatusEffects).GetField("controller",
                BindingFlags.NonPublic | BindingFlags.Instance).SetValue(effects, controller);

            MethodInfo impact = typeof(Hurdle).GetMethod("HitPlayer");
            if (impact == null)
                throw new Exception("Hurdle has no player impact handler.");
            impact.Invoke(component, new object[] { controller });

            if (!component.IsFallen || anchor.childCount != 1)
                throw new Exception("Hurdle impact did not apply the existing Slow effect.");
            impact.Invoke(component, new object[] { controller });
            if (anchor.childCount != 1)
                throw new Exception("A fallen hurdle applied Slow again.");
            Debug.Log("HurdleSmokeTest slow impact passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hurdle);
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(slowVfxPrefab);
        }
    }
}
