using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsEnabler : MonoBehaviour
{
    public GameObject settings;
    public GameObject reminderPanel;
    public RectTransform confirmExitPanel;
    public QrCodeRecenter scanQR;
    public StatusController statusController;
    public GameObject bottomBar;
    [SerializeField]
    private PullUpUI pullupUI;
    

    public Button backButton;

    public float animationTime = 0.3f; // Animation speed
    public float xOffset = -100f; 
    public float spacing = 80f;

    private void Start()
    {
        bottomBar.SetActive(false);
        Debug.Log("Selected option: " + DataScene.SelectedOption);

        switch (DataScene.SelectedOption)
        {
            case "Navigation":
                scanQR.ToggleScanning();
                statusController.ShowStatusUI();
                break;
            case "Tour":
                scanQR.ToggleScanning();
                statusController.ShowStatusUI();
                pullupUI.ClosePanel();
                break;
            default:
                Debug.LogWarning("No option selected!");
                scanQR.ToggleScanning();
                break;
        }
        
        ResetPositions();
        backButton.onClick.AddListener(confirmExit);
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            confirmExit();
        }
    }

    public void ScanFirst()
    {
        reminderPanel.SetActive(false);
        
    }

    public void openSettings()
    {
        settings.SetActive(true);
    }

    public void closeSettings()
    {
        settings.SetActive(false);
        bottomBar.SetActive(true);
    }

    private void confirmExit(){
        Vector2 targetPos = backButton.GetComponent<RectTransform>().anchoredPosition 
                                + new Vector2(xOffset,-spacing); // Expands downward & shifts left

            confirmExitPanel.DOAnchorPos(targetPos, animationTime)
                        .SetEase(Ease.OutBack);

            confirmExitPanel.DOScale(1, animationTime).SetEase(Ease.OutBack);
            confirmExitPanel.GetComponent<CanvasGroup>().DOFade(1, animationTime);
    }

    public void confirmExitNo(){
        Vector2 targetPos = backButton.GetComponent<RectTransform>().anchoredPosition + new Vector2(-30f, 0); // Shifted left
            
            confirmExitPanel.DOAnchorPos(targetPos, animationTime)
                .SetEase(Ease.InBack);

            confirmExitPanel.DOScale(0, animationTime).SetEase(Ease.InBack);

            CanvasGroup canvasGroup = confirmExitPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0, animationTime); // Fade out
            }
    }

    private void ResetPositions(){
        confirmExitPanel.anchoredPosition = backButton.GetComponent<RectTransform>().anchoredPosition;
        confirmExitPanel.localScale = Vector3.zero;
        confirmExitPanel.GetComponent<CanvasGroup>().alpha = 0;
    }
}
