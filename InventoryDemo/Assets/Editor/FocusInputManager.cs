using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class FocusInputManager : MonoBehaviour
{
	[MenuItem("Custom/Focus menu manager")]
	public static void Focus()
    {
        UnityEngine.Object test = AssetDatabase.LoadMainAssetAtPath(Constants.Editor.InputManagerPath);
        if (test == null)
        {
            Debug.LogError("Couldn't find input manager asset");
        }

        Selection.activeObject = test;
    }
}
