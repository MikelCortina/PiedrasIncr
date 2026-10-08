using UnityEngine;
using UnityEngine.AI;

public class GestorPatasArana : MonoBehaviour
{
    // =====================================================
    // PATAS DELANTERAS
    // =====================================================

    [Header("Patas delanteras")]

    public PataProcedural pataFL;
    public PataProcedural pataFR;


    // =====================================================
    // PATAS MEDIAS
    // =====================================================

    [Header("Patas medias")]

    public PataProcedural pataML;
    public PataProcedural pataMR;


    // =====================================================
    // PATAS TRASERAS
    // =====================================================

    [Header("Patas traseras")]

    public PataProcedural pataBL;
    public PataProcedural pataBR;


    // =====================================================
    // ANTICIPACIÓN NAVMESH
    // =====================================================

    [Header("Anticipación de giro NavMesh")]

    [Tooltip(
        "NavMeshAgent del Bot. " +
        "Si está vacío se busca automáticamente en los padres."
    )]
    public NavMeshAgent agente;


    [Tooltip(
        "Permite anticipar hacia dónde quiere girar el NavMesh."
    )]
    public bool usarAnticipacionNavMesh = true;


    [Tooltip(
        "Velocidad mínima del desiredVelocity para considerar " +
        "que existe una dirección de movimiento válida."
    )]
    public float umbralVelocidadDeseada = 0.05f;


    [Tooltip(
        "Suavizado del ángulo de giro deseado."
    )]
    public float suavizadoAnguloDeseado = 12f;


    [Tooltip(
        "Por debajo de este ángulo consideramos que va prácticamente recto."
    )]
    public float umbralAnguloRecto = 3f;


    // =====================================================
    // SUPERFICIE DE APOYO
    // =====================================================

    [Header("Superficie de apoyo")]

    [Tooltip(
        "Normal que se envía a las seis patas. " +
        "Vector3.up mantiene el comportamiento normal de suelo."
    )]
    [SerializeField]
    private Vector3 normalSuperficieActual = Vector3.up;


    public Vector3 NormalSuperficieActual
    {
        get
        {
            if (normalSuperficieActual.sqrMagnitude <
                0.0001f)
            {
                return Vector3.up;
            }


            return normalSuperficieActual.normalized;
        }
    }


    // =====================================================
    // TRANSICIÓN DE SUPERFICIE
    // =====================================================

    [Header("Transición de superficie")]

    [Tooltip(
        "Indica si las seis patas están cambiando progresivamente " +
        "de una normal de apoyo a otra."
    )]
    [SerializeField]
    private bool transicionSuperficieEnCurso = false;


    [Tooltip(
        "Normal desde la que comenzó la transición actual."
    )]
    [SerializeField]
    private Vector3 normalInicioTransicion = Vector3.up;


    [Tooltip(
        "Normal final hacia la que se están adaptando las patas."
    )]
    [SerializeField]
    private Vector3 normalObjetivoTransicion = Vector3.up;


    [Tooltip(
        "Progreso de la transición actual. 0 = inicio, 1 = terminada."
    )]
    [SerializeField]
    [Range(0f, 1f)]
    private float progresoTransicionSuperficie = 1f;


    private float duracionTransicionSuperficie = 0.6f;

    private float tiempoTransicionSuperficie = 0f;


    public bool TransicionSuperficieEnCurso
    {
        get { return transicionSuperficieEnCurso; }
    }


    public float ProgresoTransicionSuperficie
    {
        get { return progresoTransicionSuperficie; }
    }


    public Vector3 NormalObjetivoSuperficie
    {
        get
        {
            if (normalObjetivoTransicion.sqrMagnitude < 0.0001f)
                return Vector3.up;

            return normalObjetivoTransicion.normalized;
        }
    }


    // =====================================================
    // DEBUG
    // =====================================================

    [Header("Debug")]

    public bool mostrarDebug = false;

    public bool mostrarDebugGiro = false;


    // =====================================================
    // INTERNAS
    // =====================================================

    private bool turnoGrupoA = true;

    private float anguloDeseadoSuavizado = 0f;

    private float intensidadMovimiento = 0f;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        // =================================================
        // BUSCAR NAVMESH AGENT
        // =================================================

        if (agente == null)
        {
            agente =
                GetComponentInParent<NavMeshAgent>();
        }


        // =================================================
        // ACTIVAR CONTROL EXTERNO
        // =================================================

        ActivarControlExterno(pataFL);
        ActivarControlExterno(pataFR);

        ActivarControlExterno(pataML);
        ActivarControlExterno(pataMR);

        ActivarControlExterno(pataBL);
        ActivarControlExterno(pataBR);


        // =================================================
        // SUPERFICIE INICIAL
        // =================================================

        EstablecerNormalSuperficie(
            normalSuperficieActual
        );
    }


    // =====================================================
    // ACTIVAR CONTROL EXTERNO
    // =====================================================

    private void ActivarControlExterno(
        PataProcedural pata)
    {
        if (pata == null)
            return;


        pata.controlExterno = true;
    }


    // =====================================================
    // SUPERFICIE DE APOYO
    // =====================================================

    public void EstablecerNormalSuperficie(
        Vector3 nuevaNormal)
    {
        if (nuevaNormal.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        normalSuperficieActual =
            nuevaNormal.normalized;


        EnviarNormalSuperficie(pataFL);
        EnviarNormalSuperficie(pataFR);

        EnviarNormalSuperficie(pataML);
        EnviarNormalSuperficie(pataMR);

        EnviarNormalSuperficie(pataBL);
        EnviarNormalSuperficie(pataBR);
    }


    public void RestaurarNormalSuelo()
    {
        transicionSuperficieEnCurso = false;
        progresoTransicionSuperficie = 1f;
        tiempoTransicionSuperficie = 0f;

        EstablecerNormalSuperficie(
            Vector3.up
        );
    }


    public void IniciarTransicionSuperficie(
        Vector3 nuevaNormal,
        float duracion)
    {
        if (nuevaNormal.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        Vector3 normalFinal =
            nuevaNormal.normalized;


        // Si prácticamente ya estamos en esa superficie,
        // fijamos el resultado directamente.
        if (Vector3.Angle(
                NormalSuperficieActual,
                normalFinal) <= 0.1f ||
            duracion <= 0.001f)
        {
            transicionSuperficieEnCurso = false;
            normalInicioTransicion = normalFinal;
            normalObjetivoTransicion = normalFinal;
            progresoTransicionSuperficie = 1f;
            tiempoTransicionSuperficie = 0f;

            EstablecerNormalSuperficie(
                normalFinal
            );

            return;
        }


        normalInicioTransicion =
            NormalSuperficieActual;


        normalObjetivoTransicion =
            normalFinal;


        duracionTransicionSuperficie =
            Mathf.Max(
                0.01f,
                duracion
            );


        tiempoTransicionSuperficie = 0f;
        progresoTransicionSuperficie = 0f;
        transicionSuperficieEnCurso = true;
    }


    public void IniciarTransicionASuelo(
        float duracion)
    {
        IniciarTransicionSuperficie(
            Vector3.up,
            duracion
        );
    }


    private void ActualizarTransicionSuperficie()
    {
        if (!transicionSuperficieEnCurso)
            return;


        tiempoTransicionSuperficie +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                tiempoTransicionSuperficie /
                Mathf.Max(
                    0.01f,
                    duracionTransicionSuperficie
                )
            );


        // SmoothStep evita que el cambio de normal empiece
        // o termine de golpe.
        float tSuave =
            t * t *
            (3f - 2f * t);


        Vector3 normalInterpolada =
            Vector3.Slerp(
                normalInicioTransicion,
                normalObjetivoTransicion,
                tSuave
            );


        if (normalInterpolada.sqrMagnitude >
            0.0001f)
        {
            EstablecerNormalSuperficie(
                normalInterpolada.normalized
            );
        }


        progresoTransicionSuperficie = t;


        if (t >= 1f)
        {
            transicionSuperficieEnCurso = false;
            progresoTransicionSuperficie = 1f;

            EstablecerNormalSuperficie(
                normalObjetivoTransicion
            );
        }
    }


    private void EnviarNormalSuperficie(
        PataProcedural pata)
    {
        if (pata == null)
            return;


        pata.EstablecerNormalSuperficie(
            normalSuperficieActual
        );
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // =================================================
        // TRANSICIÓN PROGRESIVA DE LA NORMAL DE APOYO
        // =================================================

        ActualizarTransicionSuperficie();


        // =================================================
        // PRIMERO CALCULAMOS HACIA DÓNDE QUIERE GIRAR
        // =================================================

        ActualizarIntencionGiroNavMesh();


        // =================================================
        // COMPROBAR PATAS
        // =================================================

        if (pataFL == null ||
            pataFR == null ||
            pataML == null ||
            pataMR == null ||
            pataBL == null ||
            pataBR == null)
        {
            return;
        }


        // =================================================
        // SI ALGUNA PATA ESTÁ EN EL AIRE
        // ESPERAMOS
        // =================================================

        if (AlgunaPataEstaMoviendose())
        {
            return;
        }


        // =================================================
        // NECESIDAD INDIVIDUAL
        // =================================================

        bool necesitaFL =
            pataFL.NecesitaDarPaso();

        bool necesitaFR =
            pataFR.NecesitaDarPaso();

        bool necesitaML =
            pataML.NecesitaDarPaso();

        bool necesitaMR =
            pataMR.NecesitaDarPaso();

        bool necesitaBL =
            pataBL.NecesitaDarPaso();

        bool necesitaBR =
            pataBR.NecesitaDarPaso();


        // =================================================
        // GRUPO A
        //
        // FL + MR + BL
        // =================================================

        bool necesitaGrupoA =
            necesitaFL ||
            necesitaMR ||
            necesitaBL;


        // =================================================
        // GRUPO B
        //
        // FR + ML + BR
        // =================================================

        bool necesitaGrupoB =
            necesitaFR ||
            necesitaML ||
            necesitaBR;


        // =================================================
        // NADIE NECESITA MOVERSE
        // =================================================

        if (!necesitaGrupoA &&
            !necesitaGrupoB)
        {
            return;
        }


        // =================================================
        // AMBOS GRUPOS
        // =================================================

        if (necesitaGrupoA &&
            necesitaGrupoB)
        {
            if (turnoGrupoA)
            {
                MoverGrupoA(
                    necesitaFL,
                    necesitaMR,
                    necesitaBL
                );
            }
            else
            {
                MoverGrupoB(
                    necesitaFR,
                    necesitaML,
                    necesitaBR
                );
            }


            return;
        }


        // =================================================
        // SOLO GRUPO A
        // =================================================

        if (necesitaGrupoA)
        {
            MoverGrupoA(
                necesitaFL,
                necesitaMR,
                necesitaBL
            );


            return;
        }


        // =================================================
        // SOLO GRUPO B
        // =================================================

        if (necesitaGrupoB)
        {
            MoverGrupoB(
                necesitaFR,
                necesitaML,
                necesitaBR
            );
        }
    }


    // =====================================================
    // INTENCIÓN DE GIRO DEL NAVMESH
    // =====================================================

    private void ActualizarIntencionGiroNavMesh()
    {
        float anguloObjetivo = 0f;

        float intensidadObjetivo = 0f;


        // =================================================
        // ¿TENEMOS UNA DIRECCIÓN VÁLIDA?
        // =================================================

        if (usarAnticipacionNavMesh &&
            agente != null &&
            agente.enabled &&
            agente.isOnNavMesh)
        {
            Vector3 velocidadDeseada =
                agente.desiredVelocity;


            velocidadDeseada.y = 0f;


            float velocidad =
                velocidadDeseada.magnitude;


            if (velocidad >
                umbralVelocidadDeseada)
            {
                Vector3 direccionDeseada =
                    velocidadDeseada.normalized;


                Vector3 forward =
                    agente.transform.forward;


                forward.y = 0f;


                if (forward.sqrMagnitude >
                    0.001f)
                {
                    forward.Normalize();


                    // =========================================
                    // ÁNGULO QUE TODAVÍA LE FALTA POR GIRAR
                    // =========================================

                    anguloObjetivo =
                        Vector3.SignedAngle(
                            forward,
                            direccionDeseada,
                            Vector3.up
                        );


                    // =========================================
                    // INTENSIDAD DEL MOVIMIENTO
                    // =========================================

                    if (agente.speed >
                        0.01f)
                    {
                        intensidadObjetivo =
                            Mathf.Clamp01(
                                velocidad /
                                agente.speed
                            );
                    }
                    else
                    {
                        intensidadObjetivo = 1f;
                    }
                }
            }
        }


        // =================================================
        // PEQUEÑOS CAMBIOS = RECTA
        // =================================================

        if (Mathf.Abs(
                anguloObjetivo
            ) <
            umbralAnguloRecto)
        {
            anguloObjetivo = 0f;
        }


        // =================================================
        // SUAVIZAR
        // =================================================

        float factor =
            1f -
            Mathf.Exp(
                -suavizadoAnguloDeseado *
                Time.deltaTime
            );


        anguloDeseadoSuavizado =
            Mathf.LerpAngle(
                anguloDeseadoSuavizado,
                anguloObjetivo,
                factor
            );


        intensidadMovimiento =
            Mathf.Lerp(
                intensidadMovimiento,
                intensidadObjetivo,
                factor
            );


        // =================================================
        // ENVIAR A LAS SEIS PATAS
        // =================================================

        EnviarIntencion(
            pataFL
        );

        EnviarIntencion(
            pataFR
        );

        EnviarIntencion(
            pataML
        );

        EnviarIntencion(
            pataMR
        );

        EnviarIntencion(
            pataBL
        );

        EnviarIntencion(
            pataBR
        );


        // =================================================
        // DEBUG
        // =================================================

        if (mostrarDebugGiro &&
            Mathf.Abs(
                anguloDeseadoSuavizado
            ) >
            1f)
        {
            Debug.Log(
                name +
                " | Giro futuro NavMesh: " +
                anguloDeseadoSuavizado.ToString("0.0") +
                "° | Intensidad: " +
                intensidadMovimiento.ToString("0.00")
            );
        }
    }


    // =====================================================
    // ENVIAR INTENCIÓN A UNA PATA
    // =====================================================

    private void EnviarIntencion(
        PataProcedural pata)
    {
        if (pata == null)
            return;


        pata.EstablecerIntencionGiro(
            anguloDeseadoSuavizado,
            intensidadMovimiento
        );
    }


    // =====================================================
    // ¿ALGUNA PATA ESTÁ MOVIÉNDOSE?
    // =====================================================

    private bool AlgunaPataEstaMoviendose()
    {
        return
            pataFL.EstaDandoPaso ||
            pataFR.EstaDandoPaso ||
            pataML.EstaDandoPaso ||
            pataMR.EstaDandoPaso ||
            pataBL.EstaDandoPaso ||
            pataBR.EstaDandoPaso;
    }


    // =====================================================
    // GRUPO A
    //
    // FL + MR + BL
    // =====================================================

    private void MoverGrupoA(
        bool moverFL,
        bool moverMR,
        bool moverBL)
    {
        int patasMovidas = 0;


        if (moverFL)
        {
            pataFL.OrdenarPaso();
            patasMovidas++;
        }


        if (moverMR)
        {
            pataMR.OrdenarPaso();
            patasMovidas++;
        }


        if (moverBL)
        {
            pataBL.OrdenarPaso();
            patasMovidas++;
        }


        turnoGrupoA = false;


        if (mostrarDebug)
        {
            string mensaje =
                name +
                " -> GRUPO A |";


            if (moverFL)
                mensaje += " FL";

            if (moverMR)
                mensaje += " MR";

            if (moverBL)
                mensaje += " BL";


            mensaje +=
                " | Total: " +
                patasMovidas;


            Debug.Log(
                mensaje
            );
        }
    }


    // =====================================================
    // GRUPO B
    //
    // FR + ML + BR
    // =====================================================

    private void MoverGrupoB(
        bool moverFR,
        bool moverML,
        bool moverBR)
    {
        int patasMovidas = 0;


        if (moverFR)
        {
            pataFR.OrdenarPaso();
            patasMovidas++;
        }


        if (moverML)
        {
            pataML.OrdenarPaso();
            patasMovidas++;
        }


        if (moverBR)
        {
            pataBR.OrdenarPaso();
            patasMovidas++;
        }


        turnoGrupoA = true;


        if (mostrarDebug)
        {
            string mensaje =
                name +
                " -> GRUPO B |";


            if (moverFR)
                mensaje += " FR";

            if (moverML)
                mensaje += " ML";

            if (moverBR)
                mensaje += " BR";


            mensaje +=
                " | Total: " +
                patasMovidas;


            Debug.Log(
                mensaje
            );
        }
    }
}