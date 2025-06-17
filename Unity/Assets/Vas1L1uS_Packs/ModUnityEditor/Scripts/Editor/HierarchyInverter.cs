using UnityEditor;
using UnityEngine;
using System.Linq;

public static class HierarchyInverter
{
    [MenuItem("GameObject/Invert Order", false, 0)]
    private static void InvertOrder()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected!");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects
            .OrderBy(go => go.transform.GetSiblingIndex())
            .ToArray();

        int childCount = selectedObjects[0].transform.parent.childCount;

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            selectedObjects[i].transform.SetSiblingIndex(0);
        }
    }
}