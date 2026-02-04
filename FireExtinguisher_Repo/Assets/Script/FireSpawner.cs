using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject fireObject;
        [Tooltip("Delay (seconds) from scene start when this fire becomes active.")]
        public float delayUntilActive = 0f;
        [Tooltip("If true, the fire starts active immediately (delay ignored).")]
        public bool startActive = false;
    }

    [Tooltip("Only these Fire GameObjects will be counted by GameManager.")]
    public SpawnEntry[] entries = new SpawnEntry[0];

    void Awake()
    {
        // Register expected fires as early as possible
        TryRegisterExpectedFires();
    }

    IEnumerator Start()
    {
        // Safety: try again in Start in case GameManager wasn't ready in Awake.
        TryRegisterExpectedFires();

        // Configure activation: set inactive for delayed ones, activate immediate ones.
        foreach (var e in entries)
        {
            if (e == null || e.fireObject == null) continue;

            if (e.startActive)
            {
                e.fireObject.SetActive(true);
            }
            else
            {
                e.fireObject.SetActive(false);
                StartCoroutine(ActivateAfterDelay(e.fireObject, e.delayUntilActive));
            }
        }

        yield break;
    }

    private void TryRegisterExpectedFires()
    {
        if (GameManager.Instance == null) return;

        foreach (var e in entries)
        {
            if (e == null || e.fireObject == null) continue;
            var f = e.fireObject.GetComponent<Fire>();
            if (f == null)
            {
                Debug.LogWarning($"FireSpawner: '{e.fireObject.name}' has no Fire component.");
                continue;
            }
            GameManager.Instance.RegisterExpectedFire(f);
        }
    }

    private IEnumerator ActivateAfterDelay(GameObject go, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (go != null) go.SetActive(true);
    }
}
