using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonGroupHandler : MonoBehaviour
{
    public GameObject canvas;
    public TargetHandler targetHandler;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BackButton(){
        canvas.SetActive(false);
    }

    public void buttonClick(string categoryName){
        Debug.Log(categoryName);
        canvas.SetActive(true);
        targetHandler.ShowPins(categoryName);
    }


}
