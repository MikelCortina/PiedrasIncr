using System;
using UnityEngine;

public class GestorFasesLunares : MonoBehaviour
{
    [Serializable]
    public class FaseLunar
    {
        [Header("Fase")]
        public string nombreFase;

        [Tooltip("Impactos necesarios para pasar a la siguiente fase")]
        public int impactosParaSiguienteFase = 5;

        [Tooltip("Modelo visual correspondiente a esta fase")]
        public GameObject visualFase;

        [Header("Meteoritos")]
        public bool lluviaActiva = true;

        [Tooltip("Segundos entre oleadas")]
        public float tiempoEntreSpawns = 2f;

        [Min(1)]
        [Tooltip("Meteoritos que aparecen en cada oleada")]
        public int cantidadPorOleada = 1;
    }

    [Header("Fases")]
    public FaseLunar[] fases;

    [Header("Meteoritos")]
    public LluviaMeteoritos lluviaMeteoritos;

    [Header("Estado actual")]
    [SerializeField] private int faseActual = 0;
    [SerializeField] private int impactosActuales = 0;

    public int FaseActual => faseActual;
    public int ImpactosActuales => impactosActuales;

    public event Action<int> OnFaseCambiada;

    private void Start()
    {
        AplicarFaseActual();
    }

    public void RegistrarImpacto()
    {
        if (fases == null || fases.Length == 0)
            return;

        if (faseActual >= fases.Length - 1)
        {
            Debug.Log(
                "La Luna ya está en su fase máxima."
            );

            return;
        }

        impactosActuales++;

        Debug.Log(
            "Luna: " +
            impactosActuales +
            "/" +
            fases[faseActual].impactosParaSiguienteFase
        );

        if (impactosActuales >=
            fases[faseActual].impactosParaSiguienteFase)
        {
            AvanzarFase();
        }
    }

    private void AvanzarFase()
    {
        faseActual++;
        impactosActuales = 0;

        AplicarFaseActual();

        Debug.Log(
            "Luna avanzada a: " +
            fases[faseActual].nombreFase
        );

        OnFaseCambiada?.Invoke(faseActual);
    }

    private void AplicarFaseActual()
    {
        if (fases == null ||
            fases.Length == 0 ||
            faseActual < 0 ||
            faseActual >= fases.Length)
        {
            return;
        }

        // -------------------------
        // VISUAL DE LA LUNA
        // -------------------------
        for (int i = 0; i < fases.Length; i++)
        {
            if (fases[i].visualFase != null)
            {
                fases[i].visualFase.SetActive(
                    i == faseActual
                );
            }
        }

        // -------------------------
        // LLUVIA DE METEORITOS
        // -------------------------
        if (lluviaMeteoritos != null)
        {
            FaseLunar fase =
                fases[faseActual];

            lluviaMeteoritos.ConfigurarLluvia(
                fase.tiempoEntreSpawns,
                fase.cantidadPorOleada
            );

            lluviaMeteoritos.ActivarLluvia(
                fase.lluviaActiva
            );
        }
    }
}