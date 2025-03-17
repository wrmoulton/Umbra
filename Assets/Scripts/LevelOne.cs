using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelOne : MonoBehaviour
{
    [SerializeField] private string sceneName;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Triggering Scene!");
        SceneManager.LoadScene(sceneName);

    }
}
