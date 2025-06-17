using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoCompileController : Editor
{
    private const string DirectoryMonitoringKey = "DirectoryMonitoring";
    private const string AutoRefreshKey = "kAutoRefresh";
    private static List<EditorApplication.CallbackFunction> actions = new();
    private static bool s_isEnableAutoCompile = true;
    private static bool s_isCompiling = false;
    private static bool s_isEnableLogs = false;

    static AutoCompileController()
    {
        RestartAutoCompileController();
    }


    [MenuItem("AutoCompileController/Stop", priority = 99)]
    private static void StopAutoCompileController()
    {
        if (actions.Count > 0)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                EditorApplication.update -= actions[i];
            }

            actions.Clear();
        }
    }

    [MenuItem("AutoCompileController/Restart", priority = 98)]
    private static void RestartAutoCompileController()
    {
        StopAutoCompileController();
        actions.Add(DisableAutoCompilation);
        EditorApplication.update += actions[^1];
    }

    [MenuItem("AutoCompileController/Compile Current Changes %", priority = 0)]
    private static void CompileCurrentChanges()
    {
        s_isEnableAutoCompile = true;
        EditorApplication.UnlockReloadAssemblies();
        EditorPrefs.SetBool(DirectoryMonitoringKey, true);
        EditorPrefs.SetBool(AutoRefreshKey, true);
        AssetDatabase.Refresh(); 

        if (s_isEnableLogs)
        {
            Debug.Log("<color=green>Auto compilation unlocked!</color>");
        }
    }

    private static void DisableAutoCompilation()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            if (!s_isCompiling)
            {
                s_isCompiling = true;

                if (s_isEnableLogs)
                {
                    Debug.Log("<color=yellow>Wait recompiling...</color>");
                }
            }
        }
        else
        {
            s_isCompiling = false;

            if (s_isEnableAutoCompile)
            {
                s_isEnableAutoCompile = false;
                EditorApplication.LockReloadAssemblies();
                EditorPrefs.SetBool(DirectoryMonitoringKey, false);
                EditorPrefs.SetBool(AutoRefreshKey, false);
                if (s_isEnableLogs)
                {
                    Debug.Log("<color=orange>Auto compilation disabled after refresh!</color>");
                }
            }
        }
    }
}