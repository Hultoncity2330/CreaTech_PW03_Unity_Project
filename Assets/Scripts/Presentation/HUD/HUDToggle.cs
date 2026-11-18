using UnityEngine;
using UnityEngine.InputSystem;

public class HUDToggle : MonoBehaviour
{
    [SerializeField] private GameObject sheepPanel;
    [SerializeField] private GameObject wolfPanel;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            bool visible = !sheepPanel.activeSelf;
            sheepPanel.SetActive(visible);
            wolfPanel.SetActive(visible);
        }
    }
}
