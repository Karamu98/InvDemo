using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class ForceSceneOnPlay : EditorWindow
{
    static bool m_shouldForce = true;

    static ForceSceneOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayPressed;
    }

    public static void OnPlayPressed(PlayModeStateChange playModeStateChange)
    {
        if(playModeStateChange == PlayModeStateChange.EnteredPlayMode && m_shouldForce)
        {
            SceneManager.LoadScene(Constants.Scenes.Boot);
        }
    }
}
