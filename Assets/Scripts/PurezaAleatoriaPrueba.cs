using System.Collections;
using UnityEngine;

[RequireComponent(typeof(DeformacionPiedra))]
public class PurezaAleatoriaPrueba : MonoBehaviour
{
    [Header("MODO PRUEBA")]
    public bool activarPurezaAleatoria = true;


    [Header("Purezas posibles")]
    [Tooltip("Cada piedra elegirá aleatoriamente uno de estos porcentajes.")]
    public float[] purezasPosibles =
    {
        10f,
        30f,
        50f,
        70f,
        90f
    };


    [Header("Ajuste")]
    [Tooltip("Cantidad de pulido aplicada en cada paso.")]
    public float pasoPulido = 0.005f;

    [Tooltip("Máximo de pasos de seguridad.")]
    public int maxPasos = 1000;


    [Header("Debug")]
    public bool mostrarDebug = true;


    private DeformacionPiedra deformacion;


    private IEnumerator Start()
    {
        if (!activarPurezaAleatoria)
            yield break;


        deformacion =
            GetComponent<DeformacionPiedra>();


        if (deformacion == null)
            yield break;


        // Esperamos un frame para asegurarnos de que
        // DeformacionPiedra ha inicializado su malla.
        yield return null;


        if (purezasPosibles == null ||
            purezasPosibles.Length == 0)
        {
            yield break;
        }


        // =================================================
        // ELEGIR PUREZA
        // =================================================

        int indice =
            Random.Range(
                0,
                purezasPosibles.Length
            );


        float purezaObjetivo =
            Mathf.Clamp(
                purezasPosibles[indice],
                0f,
                100f
            );


        // =================================================
        // PULIR HASTA LLEGAR AL OBJETIVO
        // =================================================

        int pasos = 0;


        while (pasos < maxPasos)
        {
            float purezaActual =
                deformacion
                    .ObtenerPorcentajeDesgasteHaciaEsfera();


            if (purezaActual >= purezaObjetivo)
                break;


            deformacion.PulirUniformemente(
                pasoPulido
            );


            pasos++;


            // Dejamos respirar al frame cada cierto número
            // de iteraciones para no meter todo de golpe.
            if (pasos % 20 == 0)
            {
                yield return null;
            }
        }


        // =================================================
        // DEBUG
        // =================================================

        float purezaFinal =
            deformacion
                .ObtenerPorcentajeDesgasteHaciaEsfera();


        // Le ponemos la pureza en el nombre
        // para verla en Hierarchy durante Play.
        gameObject.name =
            gameObject.name +
            " [Pureza " +
            purezaFinal.ToString("0") +
            "%]";


        if (mostrarDebug)
        {
            Debug.Log(
                gameObject.name +
                " | Objetivo: " +
                purezaObjetivo.ToString("0") +
                "% | Real: " +
                purezaFinal.ToString("0.0") +
                "%"
            );
        }
    }
}