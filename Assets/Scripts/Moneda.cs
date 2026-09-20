using UnityEngine;

public class Moneda : MonoBehaviour
{
    [Min(1)]
    public int valor = 1;
}