using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CargadorMisil : MonoBehaviour
{
    [Header("Configuración de Arrastre Final")]
    [Tooltip("El punto exacto hacia donde será arrastrado el misil")]
    public Transform puntoDestinoMisil;
    [Tooltip("Velocidad a la que el misil es arrastrado hacia el destino")]
    public float velocidadArrastre = 2f;

    [Tooltip("Tiempo (en segundos) que espera DESPUÉS de cerrarse las puertas antes de disparar")]
    public float retrasoLanzamiento = 1.5f;

    [Header("Conexiones (Obligatorias)")]
    [Tooltip("Arrastra aquí el script de la Palanca. Si no están abiertas las puertas, no tragará el misil")]
    public PalancaTirador palancaAsociada;
    [Tooltip("Arrastra aquí el Gestor Lunar para disparar el misil original de tu script al terminar")]
    public GestorFasesLunares gestorLunar;

    private bool procesandoMisil = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            AgarreLanzamiento agarre = other.GetComponentInChildren<AgarreLanzamiento>();
            if (agarre != null) agarre.EntrarZonaCarga(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            AgarreLanzamiento agarre = other.GetComponentInChildren<AgarreLanzamiento>();
            if (agarre != null) agarre.SalirZonaCarga();
        }
    }

    public bool PuedeRecibirMisil()
    {
        if (procesandoMisil) return false;

        if (palancaAsociada != null && !palancaAsociada.EstanPuertasAbiertas)
        {
            return false;
        }

        return true;
    }

    public void RecibirMisil(Transform misilSuelto)
    {
        if (procesandoMisil) return;
        procesandoMisil = true;

        StartCoroutine(RutinaTragarMisil(misilSuelto));
    }

    private IEnumerator RutinaTragarMisil(Transform misil)
    {
        Vector3 escalaInicial = misil.localScale;
        Vector3 destinoPos = puntoDestinoMisil != null ? puntoDestinoMisil.position : misil.position;

        float t = 0f;
        while (t < 1f)
        {
            if (misil == null) yield break;

            t += Time.deltaTime * velocidadArrastre;
            misil.position = Vector3.Lerp(misil.position, destinoPos, t);
            misil.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t);

            yield return null;
        }

        if (misil != null) Destroy(misil.gameObject);

        // 1. Damos la orden de cerrar las puertas
        if (palancaAsociada != null)
        {
            palancaAsociada.CerrarPuertasMecanismo();

            // 2. PAUSAMOS la corrutina y esperamos a que las puertas estén completamente cerradas
            yield return new WaitUntil(() => palancaAsociada.EstanPuertasCerradas);
        }

        // 3. NUEVO: Esperamos el tiempo extra que hayas configurado en el inspector
        if (retrasoLanzamiento > 0f)
        {
            yield return new WaitForSeconds(retrasoLanzamiento);
        }

        // 4. Cuando terminan de cerrarse y ha pasado el tiempo de gracia, disparamos el misil lunar
        if (gestorLunar != null)
        {
            gestorLunar.DispararMisilDesdeCargador();
        }

        procesandoMisil = false;
    }
}