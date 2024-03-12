using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class UnityUtility : MonoBehaviour
{
    public static void MoveToScene(int sceneID) {
        SceneManager.LoadScene(sceneID);
    }

    public static void QuitGame() {
        Application.Quit();
    }
    
    public void ResetGameState()
    {
        GameSceneStaticData.readdata = false;
    }
}
