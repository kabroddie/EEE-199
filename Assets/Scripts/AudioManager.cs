using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip arrivalClip;
    [SerializeField] private AudioClip transitionClip;
    [SerializeField] private AudioClip recenterClip;
    [SerializeField] private AudioClip touringClip;

    [SerializeField] private AudioClip arrivalVoiceOverClip;
    [SerializeField] private AudioClip transitionUpVoiceOverClip;
    [SerializeField] private AudioClip transitionDownVoiceOverClip;
    [SerializeField] private AudioClip recenterVoiceOverClip;
    [SerializeField] private AudioClip touringVoiceOverClip;
    [SerializeField] private AudioClip headingToStartVoiceOverClip;

    public IEnumerator PlayArrival()
    {

        Handheld.Vibrate();

        audioSource.PlayOneShot(arrivalClip);
        yield return new WaitForSeconds(arrivalClip.length);

        audioSource.PlayOneShot(arrivalVoiceOverClip);
        
    }

    public IEnumerator PlayTransitionUp()
    {
        Handheld.Vibrate();

        audioSource.PlayOneShot(transitionClip);
        yield return new WaitForSeconds(transitionClip.length);

        audioSource.PlayOneShot(transitionUpVoiceOverClip);
    }

    public IEnumerator PlayTransitionDown()
    {
        Handheld.Vibrate();

        audioSource.PlayOneShot(transitionClip);
        yield return new WaitForSeconds(transitionClip.length);

        audioSource.PlayOneShot(transitionDownVoiceOverClip);
    }

    public IEnumerator PlayRecenter()
    {
        Handheld.Vibrate();

        audioSource.PlayOneShot(recenterClip);
        yield return new WaitForSeconds(recenterClip.length);

        audioSource.PlayOneShot(recenterVoiceOverClip);
    }

    public IEnumerator PlayTouring()
    {
        Handheld.Vibrate();

        audioSource.PlayOneShot(touringClip);
        yield return new WaitForSeconds(touringClip.length);

        audioSource.PlayOneShot(touringVoiceOverClip);
    }

    public IEnumerator PlayHeadingToStart()
    {
        Handheld.Vibrate();
        
        audioSource.PlayOneShot(touringClip);
        yield return new WaitForSeconds(touringClip.length);

        audioSource.PlayOneShot(headingToStartVoiceOverClip);
    }
}
