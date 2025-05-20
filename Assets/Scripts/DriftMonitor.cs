using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class DriftMonitor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform userTransform;
    [SerializeField] private TextMeshProUGUI recenterPromptText;
    [SerializeField] private QrCodeRecenter qrCodeRecenter;

    [Header("Timing (seconds)")]
    [SerializeField] private float gentlePromptTime = 2f;
    [SerializeField] private float strongPromptTime = 8f;

    [Header("NavMesh Sampling")]
    [SerializeField] private float sampleDistance = 0.5f;

    [Header("Blinking")]
    [SerializeField] private float blinkInterval = 0.5f;

    private float offNavMeshTimer = 0f;
    private float blinkTimer = 0f;
    private bool isBlinking = false;
    private string currentPromptMessage = "";
    private bool isStrongPrompt = false;

    private void Start()
    {
        if (recenterPromptText != null)
        {
            recenterPromptText.gameObject.SetActive(false);
            SetAlpha(0f);
        }
    }

    private void Update()
    {
        if (userTransform == null) return;

        // Only monitor drift if stabilization is finished
        if (qrCodeRecenter != null && !qrCodeRecenter.IsStabilizationFinished)
        {
            offNavMeshTimer = 0f;
            // Do not hide the heavy drift prompt here if isStrongPrompt is true
            if (!isStrongPrompt)
            {
                StopBlinking();
                HidePrompt();
            }
            return;
        }

        if (!IsUserOnNavMesh())
        {
            offNavMeshTimer += Time.deltaTime;
            Debug.Log($"Off NavMesh Timer: {offNavMeshTimer}");

            if (offNavMeshTimer > strongPromptTime)
            {
                // Show static strong prompt (no blinking, fully visible)
                isStrongPrompt = true;
                StopBlinking();
                ShowPrompt("Heavy drift detected.\nPlease recenter your device.");
                SetAlpha(1f);
            }
            else if (offNavMeshTimer > gentlePromptTime)
            {
                // Start or continue smooth blinking gentle prompt
                isStrongPrompt = false;
                StartBlinking("Slight drift detected.\nWe suggest recentering your device.");
            }
            else
            {
                isStrongPrompt = false;
                StopBlinking();
                HidePrompt();
            }
        }
        else
        {
            offNavMeshTimer = 0f;
            // Only hide prompt if NOT in strong prompt mode
            if (!isStrongPrompt)
            {
                StopBlinking();
                HidePrompt();
            }
            // else: do nothing, keep the heavy drift prompt visible
        }

        // Handle smooth blinking effect
        if (isBlinking && !isStrongPrompt)
        {
            blinkTimer += Time.deltaTime;
            float phase = Mathf.PingPong(blinkTimer, blinkInterval) / blinkInterval;
            float alpha = Mathf.Lerp(0f, 1f, phase);
            SetAlpha(alpha);
        }
    }

    private bool IsUserOnNavMesh()
    {
        Vector3 position = userTransform.position;
        position.y -= 1; // or set to the expected NavMesh Y level
        NavMeshHit hit;
        return NavMesh.SamplePosition(position, out hit, sampleDistance, NavMesh.AllAreas);
    }

    private void StartBlinking(string message)
    {
        if (!isBlinking || currentPromptMessage != message)
        {
            isBlinking = true;
            blinkTimer = 0f;
            currentPromptMessage = message;
            if (recenterPromptText != null)
            {
                recenterPromptText.text = message;
                recenterPromptText.gameObject.SetActive(true);
            }
        }
    }

    private void StopBlinking()
    {
        if (isBlinking)
        {
            isBlinking = false;
            blinkTimer = 0f;
        }
    }

    private void ShowPrompt(string message)
    {
        if (recenterPromptText != null)
        {
            recenterPromptText.text = message;
            recenterPromptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (recenterPromptText != null)
        {
            recenterPromptText.gameObject.SetActive(false);
            SetAlpha(0f);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (recenterPromptText != null)
        {
            Color c = recenterPromptText.color;
            c.a = alpha;
            recenterPromptText.color = c;
        }
    }

    public void HeavyDriftPromptReset()
    {
        isStrongPrompt = false;
    }
}
