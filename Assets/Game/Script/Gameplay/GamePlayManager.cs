
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{
    [SerializeField]
    private InputManager _input;

    private void Start()
    {
        _input.OnBackToMainMenu += BackToMenu;
    }

    private void OnDestroy()
    {
        _input.OnBackToMainMenu -= BackToMenu;
    }
    private void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}