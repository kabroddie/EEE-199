using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Tutorial : MonoBehaviour
{

    public TextMeshProUGUI desc;
    public TextMeshProUGUI title;
    public TextMeshProUGUI buttonText;
    public GameObject buttonBack;
    public TextMeshProUGUI buttonBackText;
    public GameObject canvas;
    public GameObject canvasPrefab;

    [SerializeField] private Image[] tourImages;

    List<string> titles = new List<string> { "QR Codes", "Menu", "Directory", "Free Navigation", "AR Information Overlays", "Tour Mode" };
    List<string> descriptions = new List<string>{
        "Before using, scan a QR code for localization. When you experience drifting, please scan any QR Code (highlighted yellow in your map) to recenter.",

        "There are two buttons in the menu, You will find the recenter most useful in here, others are just for your quality of life.Recenter if your application is drifting.",

        "To open this, slide up from the bottom. In this menu, all of the things you need for navigation is here.",

        "Free navigation allows you to choose freely where to go. To use it, click on the search button and choose a destination from the list.",

        "Some objects like classroom plaques can generate an AR overlay that provides you with more information about the room near it. In tour mode, an indicator \"AR information available\" will pop-up whenever such overlays are available for the POI you just found. Can you find them all?",

        "Tour mode will tour you to predefined locations. To use it, click on the \"Start Tour\" from the directory and choose a preset from the list. To exit tour mode, click on the \"Exit Tour\" button. To Resume tour mode, click on the \"Resume Tour\" button."
        };

    int counter = 0;
    bool finish = false;
    // Start is called before the first frame update

    public void StartTutorial()
    {
        canvas.SetActive(true);
        counter = 0;
        tourImages[counter].gameObject.SetActive(true);
        if (counter == 0)
        {
            desc.text = descriptions[counter];
            title.text = titles[counter];
            buttonBackText.text = "Skip";

        }
    }
    void Start()
    {
        
        if (counter == 0)
        {
            desc.text = descriptions[counter];
            title.text = titles[counter];
            buttonBackText.text = "Skip";

        }
    }
    void Update()
    {
        if (counter == titles.Count - 1)
        {
            buttonText.text = "Finish";
            finish = true;
        }
        else
        {
            buttonText.text = "Next";
            buttonBackText.text = "Back";
            finish = false;
        }
        if (counter == 0)
        {
            buttonBackText.text = "Skip";
        }
        else
        {
            buttonBack.SetActive(true);
        }
    }

    public void buttonPress(bool next)
    {
        if (finish & next)
        {
            canvas.SetActive(false);
            tourImages[counter].gameObject.SetActive(false);
            
        }
        if (counter == 0 & !next)
        {
            canvas.SetActive(false);
        }

        int previous = counter;

        if (next)
        {

            if (counter < titles.Count - 1 & counter >= 0)
            {
                counter++;
            }
        }
        else
        {
            if (counter > 0)
            {
                counter--;
            }
        }

        if (previous != counter)
        {
            // Fade out previous image
            tourImages[previous].DOFade(0f, 0.3f).OnComplete(() =>
            {
                tourImages[previous].gameObject.SetActive(false);
            });

            // Fade in current image
            tourImages[counter].gameObject.SetActive(true);
            tourImages[counter].color = new Color(1f, 1f, 1f, 0f); // reset alpha
            tourImages[counter].DOFade(1f, 0.5f);
        }

        desc.text = descriptions[counter];
        title.text = titles[counter];
    }
    

}
