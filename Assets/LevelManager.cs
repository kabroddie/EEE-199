using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] Animator transitionAnim;
    public string sceneName;
    
    public void changeScene()
    {
        StartCoroutine(loadTheScene());
    }
    
    IEnumerator loadTheScene()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(sceneName);
        transitionAnim.SetTrigger("Start");

    }

    public void OnStartNavigationClicked()
    {
        DataScene.SelectedOption = "Navigation";
        changeScene();
    }

    public void OnStartTourClicked()
    {
        DataScene.SelectedOption = "Tour";
        changeScene();
    }

    public void OnTutorialClicked()
    {
        DataScene.SelectedOption = "Tutorial";
        changeScene();
    }

}
