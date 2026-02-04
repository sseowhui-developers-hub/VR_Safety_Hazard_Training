using UnityEngine;

public class UserManual : MonoBehaviour
{
    [SerializeField] private GameObject manualCanvas;   // Assign ManualCanvas here
    [SerializeField] private bool pauseWhenOpen = true; // Optional: pause game when manual open

    private bool isOpen = false;
    private float prevTimeScale = 1f;

    private void Start()
    {
        if (manualCanvas != null) manualCanvas.SetActive(false);
    }

    private void Update()
    {
        // Uses old Input Manager. If you use the New Input System only, see the alt version below.
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;

        if (manualCanvas != null)
            manualCanvas.SetActive(isOpen);

        if (pauseWhenOpen)
        {
            if (isOpen)
            {
                prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = prevTimeScale;
            }
        }
    }

    public void Show()
    {
        if (!isOpen) Toggle();
    }

    public void Hide()
    {
        if (isOpen) Toggle();
    }
}
