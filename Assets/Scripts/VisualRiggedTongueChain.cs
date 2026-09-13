using UnityEngine;

public class VisualRiggedTongueChain : MonoBehaviour
{
    [Header("Huesos de la lengua")]
    [Tooltip("Ordénalos desde la espada hasta la rana.")]
    [SerializeField] private Transform[] huesosLengua;

    [Header("Anclas")]
    [SerializeField] private Transform anclaEspada;
    [SerializeField] private Transform anclaRana;

    [Header("Rigidbody de la rana")]
    [SerializeField] private Rigidbody rigidbodyRana;

    [Header("Longitud")]
    [Tooltip("Si es 0, se calcula automáticamente.")]
    [SerializeField] private float longitudSegmento = 0f;

    [SerializeField] private float multiplicadorLongitud = 1f;

    [Header("Simulación Verlet")]
    [SerializeField]
    private Vector3 gravedad =
        new Vector3(0f, -0.5f, 0f);

    [Range(0f, 1f)]
    [SerializeField] private float amortiguacion = 0.90f;

    [SerializeField] private int subpasos = 10;

    [SerializeField] private int iteracionesRestriccion = 10;

    [Header("Ancla de espada")]
    [SerializeField] private bool suavizarAnclaEspada = false;

    [SerializeField] private float suavizadoAnclaEspada = 0.025f;

    [Header("Curvatura general")]
    [SerializeField] private bool aplicarCurvatura = true;

    [Range(0f, 1f)]
    [SerializeField] private float fuerzaCurvatura = 0.08f;

    [Range(0f, 1f)]
    [SerializeField] private float suavizadoCurvatura = 0.25f;

    [Header("Curva cuando la rana está cerca")]
    [SerializeField] private bool usarCurvaCercania = true;

    [Tooltip("Distancia a partir de la cual se activa la curva estable.")]
    [SerializeField] private float umbralCercania = 1.5f;

    [SerializeField] private float alturaCurvaCercania = 0.30f;

    [Range(0f, 1f)]
    [SerializeField] private float suavizadoCurvaCercania = 0.18f;

    [Range(0f, 1f)]
    [SerializeField] private float amortiguacionCercania = 0.80f;

    [Header("Compresión")]
    [SerializeField] private bool permitirCompresion = true;

    [Range(0f, 1f)]
    [SerializeField] private float amortiguacionCompresion = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float fuerzaColapsoCercania = 0.35f;

    [Header("Eje longitudinal")]
    [Tooltip("Eje local que apunta hacia el siguiente hueso.")]
    [SerializeField]
    private Vector3 ejeLongitudinalLocal =
        Vector3.forward;

    [Header("Influencia de la rana")]
    [SerializeField] private float influenciaVelocidadRana = 0.01f;

    [Header("Tensión sobre la rana")]
    [SerializeField] private bool arrastrarRana = true;

    [SerializeField] private float rigidezCadena = 60f;

    [SerializeField] private float amortiguacionTension = 80f;

    [SerializeField] private float fuerzaMaximaArrastre = 500f;

    [SerializeField] private float holguraCadena = 0f;

    [Header("Límite de longitud")]
    [SerializeField] private bool limitarLongitudTotal = true;

    [SerializeField] private float margenLongitudMaxima = 0f;

    [SerializeField] private bool corregirPosicionRana = true;

    [Header("Visual")]
    [SerializeField] private bool suavizarPosiciones = true;

    [SerializeField] private bool mantenerEscalaOriginal = true;

    [SerializeField] private bool actualizarSkinnedMeshFueraDeCamara = true;

    private Vector3[] posiciones;
    private Vector3[] posicionesAnteriores;

    private Quaternion[] rotacionesIniciales;
    private Vector3[] escalasIniciales;

    private Vector3 anclaEspadaSuavizada;
    private Vector3 velocidadAnclaEspada;

    private void Start()
    {
        InicializarCadena();
        ConfigurarRigidbodyRana();
        ConfigurarSkinnedMeshRenderer();
    }

    private void FixedUpdate()
    {
        if (!CadenaValida())
            return;

        if (rigidbodyRana == null)
            return;

        bool ranaEnElLimite = false;

        if (limitarLongitudTotal)
        {
            ranaEnElLimite =
                LimitarDistanciaMaximaRana();
        }

        if (arrastrarRana &&
            !ranaEnElLimite)
        {
            AplicarTensionSobreLaRana();
        }
    }

    private void LateUpdate()
    {
        if (!CadenaValida())
            return;

        float deltaTime =
            Mathf.Min(
                Time.deltaTime,
                0.033f
            );

        int subpasosSeguros =
            Mathf.Max(
                1,
                subpasos
            );

        float deltaTimeSubpaso =
            deltaTime /
            subpasosSeguros;

        for (int i = 0;
             i < subpasosSeguros;
             i++)
        {
            SimularSubpaso(
                deltaTimeSubpaso
            );
        }

        FijarExtremos();

        AplicarTransformacionesALosHuesos();
    }

    private void InicializarCadena()
    {
        if (huesosLengua == null ||
            huesosLengua.Length < 3)
        {
            Debug.LogError(
                "La lengua necesita al menos tres huesos.",
                this
            );

            return;
        }

        if (anclaEspada == null ||
            anclaRana == null)
        {
            Debug.LogError(
                "Falta el ancla de la espada o el ancla de la rana.",
                this
            );

            return;
        }

        int cantidadHuesos =
            huesosLengua.Length;

        posiciones =
            new Vector3[cantidadHuesos];

        posicionesAnteriores =
            new Vector3[cantidadHuesos];

        rotacionesIniciales =
            new Quaternion[cantidadHuesos];

        escalasIniciales =
            new Vector3[cantidadHuesos];

        for (int i = 0;
             i < cantidadHuesos;
             i++)
        {
            if (huesosLengua[i] == null)
            {
                Debug.LogError(
                    $"El hueso {i} está vacío.",
                    this
                );

                continue;
            }

            posiciones[i] =
                huesosLengua[i].position;

            posicionesAnteriores[i] =
                huesosLengua[i].position;

            rotacionesIniciales[i] =
                huesosLengua[i].rotation;

            escalasIniciales[i] =
                huesosLengua[i].localScale;
        }

        if (longitudSegmento <= 0.001f)
        {
            longitudSegmento =
                CalcularLongitudInicial();
        }

        longitudSegmento *=
            Mathf.Max(
                0.01f,
                multiplicadorLongitud
            );

        anclaEspadaSuavizada =
            anclaEspada.position;

        velocidadAnclaEspada =
            Vector3.zero;

        FijarExtremos();
    }

    private float CalcularLongitudInicial()
    {
        float longitudTotal = 0f;
        int segmentosValidos = 0;

        for (int i = 0;
             i < huesosLengua.Length - 1;
             i++)
        {
            if (huesosLengua[i] == null ||
                huesosLengua[i + 1] == null)
            {
                continue;
            }

            longitudTotal +=
                Vector3.Distance(
                    huesosLengua[i].position,
                    huesosLengua[i + 1].position
                );

            segmentosValidos++;
        }

        if (segmentosValidos == 0)
            return 0.1f;

        return longitudTotal /
               segmentosValidos;
    }

    private void ConfigurarRigidbodyRana()
    {
        if (rigidbodyRana == null)
            return;

        rigidbodyRana.isKinematic =
            false;

        rigidbodyRana.useGravity =
            true;

        rigidbodyRana.interpolation =
            RigidbodyInterpolation.Interpolate;

        rigidbodyRana.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        rigidbodyRana.solverIterations =
            12;

        rigidbodyRana.solverVelocityIterations =
            12;

#if UNITY_6000_0_OR_NEWER
        rigidbodyRana.linearDamping =
            2f;

        rigidbodyRana.angularDamping =
            4f;
#else
        rigidbodyRana.drag =
            2f;

        rigidbodyRana.angularDrag =
            4f;
#endif

        rigidbodyRana.maxAngularVelocity =
            15f;
    }

    private void ConfigurarSkinnedMeshRenderer()
    {
        if (!actualizarSkinnedMeshFueraDeCamara)
            return;

        SkinnedMeshRenderer skinnedMesh =
            GetComponentInChildren<
                SkinnedMeshRenderer
            >();

        if (skinnedMesh != null)
        {
            skinnedMesh.updateWhenOffscreen =
                true;
        }
    }

    private void SimularSubpaso(
        float deltaTime)
    {
        int ultimoIndice =
            posiciones.Length - 1;

        ActualizarAnclaEspada(
            deltaTime
        );

        Vector3 posicionRana =
            ObtenerPosicionAnclaRana();

        float distanciaExtremos =
            Vector3.Distance(
                anclaEspadaSuavizada,
                posicionRana
            );

        if (usarCurvaCercania &&
            distanciaExtremos < umbralCercania)
        {
            SimularCurvaCercania(
                posicionRana,
                deltaTime
            );

            FijarExtremos();

            return;
        }

        for (int i = 1;
             i < ultimoIndice;
             i++)
        {
            Vector3 posicionActual =
                posiciones[i];

            Vector3 velocidad =
                posiciones[i] -
                posicionesAnteriores[i];

            posicionesAnteriores[i] =
                posicionActual;

            posiciones[i] =
                posicionActual +
                velocidad *
                amortiguacion +
                gravedad *
                deltaTime *
                deltaTime;
        }

        FijarExtremos();

        int iteracionesSeguras =
            Mathf.Max(
                1,
                iteracionesRestriccion
            );

        for (int iteracion = 0;
             iteracion < iteracionesSeguras;
             iteracion++)
        {
            FijarExtremos();

            for (int i = 0;
                 i < posiciones.Length - 1;
                 i++)
            {
                AplicarRestriccionDistancia(i);
            }

            for (int i = posiciones.Length - 2;
                 i >= 0;
                 i--)
            {
                AplicarRestriccionDistancia(i);
            }

            if (aplicarCurvatura)
            {
                AplicarCurvatura();
            }

            ResolverCadenaComprimida();

            FijarExtremos();
        }

        FijarExtremos();
    }

    private void ActualizarAnclaEspada(
        float deltaTime)
    {
        if (!suavizarAnclaEspada)
        {
            anclaEspadaSuavizada =
                anclaEspada.position;

            velocidadAnclaEspada =
                Vector3.zero;

            return;
        }

        anclaEspadaSuavizada =
            Vector3.SmoothDamp(
                anclaEspadaSuavizada,
                anclaEspada.position,
                ref velocidadAnclaEspada,
                suavizadoAnclaEspada,
                Mathf.Infinity,
                deltaTime
            );
    }

    private void FijarExtremos()
    {
        int ultimoIndice =
            posiciones.Length - 1;

        posiciones[0] =
            anclaEspadaSuavizada;

        posicionesAnteriores[0] =
            posiciones[0];

        posiciones[ultimoIndice] =
            ObtenerPosicionAnclaRana();

        posicionesAnteriores[ultimoIndice] =
            posiciones[ultimoIndice];
    }

    private Vector3 ObtenerPosicionAnclaRana()
    {
        Vector3 posicionRana =
            anclaRana.position;

        if (rigidbodyRana == null ||
            influenciaVelocidadRana <= 0f)
        {
            return posicionRana;
        }

        Vector3 velocidadRana =
            rigidbodyRana.GetPointVelocity(
                anclaRana.position
            );

        posicionRana +=
            velocidadRana *
            influenciaVelocidadRana *
            Time.fixedDeltaTime;

        return posicionRana;
    }

    private void AplicarRestriccionDistancia(
        int indice)
    {
        int siguiente =
            indice + 1;

        Vector3 diferencia =
            posiciones[siguiente] -
            posiciones[indice];

        float distancia =
            diferencia.magnitude;

        if (distancia < 0.0001f)
            return;

        Vector3 direccion =
            diferencia /
            distancia;

        float error =
            distancia -
            longitudSegmento;

        bool actualFijo =
            indice == 0;

        bool siguienteFijo =
            siguiente ==
            posiciones.Length - 1;

        if (permitirCompresion &&
            EstaCadenaComprimida())
        {
            if (!actualFijo &&
                !siguienteFijo)
            {
                Vector3 correccionCompresion =
                    direccion *
                    error *
                    0.15f;

                posiciones[indice] +=
                    correccionCompresion;

                posiciones[siguiente] -=
                    correccionCompresion;
            }

            return;
        }

        if (actualFijo &&
            siguienteFijo)
        {
            return;
        }

        if (actualFijo)
        {
            posiciones[siguiente] =
                posiciones[indice] +
                direccion *
                longitudSegmento;

            return;
        }

        if (siguienteFijo)
        {
            posiciones[indice] =
                posiciones[siguiente] -
                direccion *
                longitudSegmento;

            return;
        }

        Vector3 correccion =
            direccion *
            error *
            0.5f;

        posiciones[indice] +=
            correccion;

        posiciones[siguiente] -=
            correccion;
    }

    private bool EstaCadenaComprimida()
    {
        if (posiciones == null ||
            posiciones.Length < 2)
        {
            return false;
        }

        int ultimoIndice =
            posiciones.Length - 1;

        float distanciaExtremos =
            Vector3.Distance(
                posiciones[0],
                posiciones[ultimoIndice]
            );

        float longitudCadena =
            longitudSegmento *
            (posiciones.Length - 1);

        return distanciaExtremos <
               longitudCadena;
    }

    private void ResolverCadenaComprimida()
    {
        if (!permitirCompresion)
            return;

        int ultimoIndice =
            posiciones.Length - 1;

        Vector3 inicio =
            posiciones[0];

        Vector3 final =
            posiciones[ultimoIndice];

        float distanciaExtremos =
            Vector3.Distance(
                inicio,
                final
            );

        float longitudCadena =
            longitudSegmento *
            (posiciones.Length - 1);

        if (distanciaExtremos >= longitudCadena)
            return;

        float factorCercania =
            1f -
            Mathf.Clamp01(
                distanciaExtremos /
                Mathf.Max(
                    longitudCadena,
                    0.0001f
                )
            );

        for (int i = 1;
             i < ultimoIndice;
             i++)
        {
            float t =
                (float)i /
                ultimoIndice;

            Vector3 posicionRecta =
                Vector3.Lerp(
                    inicio,
                    final,
                    t
                );

            Vector3 posicionActual =
                posiciones[i];

            posiciones[i] =
                Vector3.Lerp(
                    posicionActual,
                    posicionRecta,
                    fuerzaColapsoCercania *
                    factorCercania
                );

            Vector3 velocidad =
                posiciones[i] -
                posicionesAnteriores[i];

            posicionesAnteriores[i] =
                posiciones[i] -
                velocidad *
                amortiguacionCompresion;
        }
    }

    private void SimularCurvaCercania(
        Vector3 posicionRana,
        float deltaTime)
    {
        int ultimoIndice =
            posiciones.Length - 1;

        Vector3 inicio =
            anclaEspadaSuavizada;

        Vector3 final =
            posicionRana;

        Vector3 direccion =
            final - inicio;

        float distancia =
            direccion.magnitude;

        if (distancia < 0.0001f)
        {
            direccion =
                anclaEspada.forward;
        }
        else
        {
            direccion.Normalize();
        }

        Vector3 ejeCurva =
            Vector3.Cross(
                direccion,
                Vector3.up
            );

        if (ejeCurva.sqrMagnitude < 0.0001f)
        {
            ejeCurva =
                Vector3.Cross(
                    direccion,
                    Vector3.right
                );
        }

        ejeCurva.Normalize();

        float factorCercania =
            1f -
            Mathf.Clamp01(
                distancia /
                Mathf.Max(
                    umbralCercania,
                    0.0001f
                )
            );

        float altura =
            alturaCurvaCercania *
            factorCercania;

        for (int i = 1;
             i < ultimoIndice;
             i++)
        {
            float t =
                (float)i /
                ultimoIndice;

            float curva =
                Mathf.Sin(
                    t *
                    Mathf.PI
                );

            Vector3 posicionCurva =
                Vector3.Lerp(
                    inicio,
                    final,
                    t
                );

            posicionCurva +=
                ejeCurva *
                curva *
                altura;

            Vector3 velocidad =
                posiciones[i] -
                posicionesAnteriores[i];

            posicionesAnteriores[i] =
                posiciones[i];

            Vector3 posicionConInercia =
                posiciones[i] +
                velocidad *
                amortiguacionCercania;

            posiciones[i] =
                Vector3.Lerp(
                    posicionConInercia,
                    posicionCurva,
                    suavizadoCurvaCercania
                );
        }

        FijarExtremos();
    }

    private void AplicarCurvatura()
    {
        if (posiciones == null ||
            posiciones.Length < 3)
        {
            return;
        }

        int ultimoIndice =
            posiciones.Length - 1;

        for (int i = 1;
             i < ultimoIndice;
             i++)
        {
            Vector3 direccionAnterior =
                posiciones[i] -
                posiciones[i - 1];

            Vector3 direccionSiguiente =
                posiciones[i + 1] -
                posiciones[i];

            if (direccionAnterior.sqrMagnitude < 0.0001f ||
                direccionSiguiente.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            direccionAnterior.Normalize();
            direccionSiguiente.Normalize();

            Vector3 direccionMedia =
                direccionAnterior +
                direccionSiguiente;

            if (direccionMedia.sqrMagnitude < 0.0001f)
                continue;

            direccionMedia.Normalize();

            Vector3 posicionDeseada =
                posiciones[i - 1] +
                direccionMedia *
                longitudSegmento;

            posiciones[i] =
                Vector3.Lerp(
                    posiciones[i],
                    posicionDeseada,
                    fuerzaCurvatura
                );
        }

        FijarExtremos();
    }

    private Vector3 ObtenerTangenteSuavizada(
        int indice)
    {
        int ultimoIndice =
            posiciones.Length - 1;

        Vector3 tangente;

        if (indice == 0)
        {
            tangente =
                posiciones[1] -
                posiciones[0];
        }
        else if (indice == ultimoIndice)
        {
            tangente =
                posiciones[ultimoIndice] -
                posiciones[ultimoIndice - 1];
        }
        else
        {
            tangente =
                posiciones[indice + 1] -
                posiciones[indice - 1];
        }

        return tangente;
    }

    private void AplicarTransformacionesALosHuesos()
    {
        for (int i = 0;
             i < huesosLengua.Length;
             i++)
        {
            if (huesosLengua[i] == null)
                continue;

            if (i == 0)
            {
                huesosLengua[i].position =
                    anclaEspadaSuavizada;
            }
            else if (i == huesosLengua.Length - 1)
            {
                huesosLengua[i].position =
                    posiciones[i];
            }
            else if (suavizarPosiciones)
            {
                huesosLengua[i].position =
                    Vector3.Lerp(
                        huesosLengua[i].position,
                        posiciones[i],
                        suavizadoCurvatura
                    );
            }
            else
            {
                huesosLengua[i].position =
                    posiciones[i];
            }

            if (mantenerEscalaOriginal)
            {
                huesosLengua[i].localScale =
                    escalasIniciales[i];
            }
        }

        for (int i = 0;
             i < huesosLengua.Length;
             i++)
        {
            Vector3 tangente =
                ObtenerTangenteSuavizada(i);

            if (tangente.sqrMagnitude < 0.0001f)
                continue;

            tangente.Normalize();

            Vector3 ejeInicial =
                rotacionesIniciales[i] *
                ejeLongitudinalLocal.normalized;

            Quaternion rotacionObjetivo =
                Quaternion.FromToRotation(
                    ejeInicial,
                    tangente
                ) *
                rotacionesIniciales[i];

            if (i == 0 ||
                i == huesosLengua.Length - 1)
            {
                huesosLengua[i].rotation =
                    rotacionObjetivo;
            }
            else
            {
                huesosLengua[i].rotation =
                    Quaternion.Slerp(
                        huesosLengua[i].rotation,
                        rotacionObjetivo,
                        suavizadoCurvatura
                    );
            }
        }

        huesosLengua[0].position =
            anclaEspadaSuavizada;
    }

    private bool LimitarDistanciaMaximaRana()
    {
        if (rigidbodyRana == null ||
            anclaRana == null ||
            anclaEspada == null)
        {
            return false;
        }

        float longitudMaxima =
            longitudSegmento *
            (huesosLengua.Length - 1) +
            margenLongitudMaxima;

        Vector3 diferencia =
            anclaRana.position -
            anclaEspada.position;

        float distancia =
            diferencia.magnitude;

        if (distancia <= longitudMaxima ||
            distancia < 0.001f)
        {
            return false;
        }

        Vector3 direccion =
            diferencia.normalized;

        Vector3 posicionLimitada =
            anclaEspada.position +
            direccion *
            longitudMaxima;

        if (corregirPosicionRana)
        {
            Vector3 desplazamiento =
                posicionLimitada -
                anclaRana.position;

            rigidbodyRana.position +=
                desplazamiento;
        }

        QuitarVelocidadHaciaFuera(
            direccion
        );

        return true;
    }

    private void QuitarVelocidadHaciaFuera(
        Vector3 direccionDesdeEspada)
    {
#if UNITY_6000_0_OR_NEWER
        Vector3 velocidad =
            rigidbodyRana.linearVelocity;
#else
        Vector3 velocidad =
            rigidbodyRana.velocity;
#endif

        float componenteExterior =
            Vector3.Dot(
                velocidad,
                direccionDesdeEspada
            );

        if (componenteExterior <= 0f)
            return;

        Vector3 nuevaVelocidad =
            velocidad -
            direccionDesdeEspada *
            componenteExterior;

#if UNITY_6000_0_OR_NEWER
        rigidbodyRana.linearVelocity =
            nuevaVelocidad;
#else
        rigidbodyRana.velocity =
            nuevaVelocidad;
#endif
    }

    private void AplicarTensionSobreLaRana()
    {
        if (rigidbodyRana == null ||
            anclaRana == null ||
            anclaEspada == null)
        {
            return;
        }

        Vector3 diferencia =
            anclaEspada.position -
            anclaRana.position;

        float distancia =
            diferencia.magnitude;

        float longitudCadena =
            longitudSegmento *
            (huesosLengua.Length - 1);

        float extension =
            distancia -
            longitudCadena -
            holguraCadena;

        if (extension <= 0f)
            return;

        Vector3 direccion =
            diferencia.normalized;

        Vector3 velocidad =
            rigidbodyRana.GetPointVelocity(
                anclaRana.position
            );

        float velocidadHaciaEspada =
            Vector3.Dot(
                velocidad,
                direccion
            );

        float fuerzaResorte =
            extension *
            rigidezCadena;

        float amortiguacionCritica =
            2f *
            Mathf.Sqrt(
                rigidezCadena *
                rigidbodyRana.mass
            );

        float amortiguacionUsada =
            Mathf.Max(
                amortiguacionTension,
                amortiguacionCritica
            );

        float fuerzaAmortiguacion =
            velocidadHaciaEspada *
            amortiguacionUsada;

        float fuerzaTotal =
            fuerzaResorte -
            fuerzaAmortiguacion;

        fuerzaTotal =
            Mathf.Clamp(
                fuerzaTotal,
                0f,
                fuerzaMaximaArrastre
            );

        rigidbodyRana.AddForceAtPosition(
            direccion *
            fuerzaTotal,
            anclaRana.position,
            ForceMode.Force
        );
    }

    private bool CadenaValida()
    {
        if (huesosLengua == null ||
            huesosLengua.Length < 3)
        {
            return false;
        }

        if (anclaEspada == null ||
            anclaRana == null)
        {
            return false;
        }

        if (posiciones == null ||
            posicionesAnteriores == null)
        {
            return false;
        }

        for (int i = 0;
             i < huesosLengua.Length;
             i++)
        {
            if (huesosLengua[i] == null)
            {
                return false;
            }
        }

        return true;
    }
}