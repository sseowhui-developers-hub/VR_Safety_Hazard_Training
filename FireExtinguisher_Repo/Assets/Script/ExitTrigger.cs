using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    [SerializeField] private GameObject ExitCylinder;
    [SerializeField] private AudioSource backgroundAudioSource;
    [SerializeField] private AudioClip newBackgroundClip;
    [SerializeField] private AudioSource alarmSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.PlayerExited();

            // Change background music
            if (backgroundAudioSource != null && newBackgroundClip != null)
            {
                backgroundAudioSource.clip = newBackgroundClip;
                backgroundAudioSource.Play();
            }

            alarmSound.Stop();

            Destroy(ExitCylinder);
        }
    }
}