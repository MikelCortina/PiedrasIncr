using PiedrasIncr.Audio;
using UnityEngine;

public class PruebaAudio : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
            AudioManager.Play("Patada");
    }
}