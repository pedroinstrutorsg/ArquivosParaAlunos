using UnityEngine;
using UnityEngine.SceneManagement;

public class Trofeu : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("Vitoria");
        }
    }
}