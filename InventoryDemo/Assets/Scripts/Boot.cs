using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Boot : MonoBehaviour
{
    private void Update()
    {
        // Load here to ensure all Awake/Start calls have happened
        if(!hasStarted)
        {
            hasStarted = true;
            SceneManager.LoadScene(Constants.Scenes.Main);
        }
    }

    bool hasStarted = false;
}
