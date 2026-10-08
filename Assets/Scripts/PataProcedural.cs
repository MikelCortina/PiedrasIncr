using UnityEngine;

public class PataProcedural : MonoBehaviour
{
    // =====================================================
    // REFERENCIAS
    // =====================================================

    [Header("Referencias")]

    public Transform cadera;
    public Transform rodilla;
    public Transform pie;
    public Transform objetivo;

    public Transform upperLeg;
    public Transform lowerLeg;


    // =====================================================
    // MOVIMIENTO DEL PASO
    // =====================================================

    [Header("Movimiento del paso")]

    public float distanciaPaso = 0.40f;
    public float velocidadPaso = 2.2f;
    public float alturaPaso = 0.10f;


    // =====================================================
    // PROTECCIÓN CONTRA ESTIRAMIENTO
    // =====================================================

    [Header("Protección contra estiramiento")]

    [Range(0.60f, 0.99f)]
    public float porcentajeExtensionMaxima = 0.72f;

    public float multiplicadorVelocidadEmergencia = 1.5f;


    // =====================================================
    // COORDINACIÓN
    // =====================================================

    [Header("Coordinación")]

    public bool controlExterno = false;


    public bool EstaDandoPaso
    {
        get
        {
            return dandoPaso;
        }
    }


    // =====================================================
    // ANTICIPACIÓN DE GIRO
    // =====================================================

    [Header("Anticipación de giro")]

    [Tooltip(
        "Activa la predicción de la posición futura del Target " +
        "cuando el cuerpo está girando."
    )]
    public bool anticiparGiro = true;


    [Tooltip(
        "Centro/orientación utilizada para medir el giro. " +
        "Si queda vacío se utilizará automáticamente " +
        "el padre del Target, normalmente SpiderVisual."
    )]
    public Transform referenciaGiro;


    [Tooltip(
        "Cuántos segundos hacia el futuro intentamos predecir."
    )]
    [Range(0f, 0.5f)]
    public float tiempoAnticipacionGiro = 0.16f;


    [Tooltip(
        "Velocidad angular mínima en grados/segundo " +
        "para considerar que realmente estamos girando."
    )]
    public float umbralVelocidadGiro = 15f;


    [Tooltip(
        "Suavizado de la velocidad angular detectada."
    )]
    public float suavizadoVelocidadGiro = 10f;


    [Tooltip(
        "Máximo ángulo que podemos anticipar."
    )]
    public float anguloMaximoAnticipacion = 20f;


    [Tooltip(
        "Máxima distancia que el Target anticipado " +
        "puede separarse del Target normal."
    )]
    public float distanciaMaximaAnticipacion = 0.35f;


    // =====================================================
    // INTENCIÓN DE GIRO NAVMESH
    // =====================================================

    [Header("Intención de giro NavMesh")]

    [Tooltip(
        "Cuánto pesa la intención futura del NavMesh frente " +
        "al giro que ya está realizando físicamente el cuerpo."
    )]
    [Range(0f, 1f)]
    public float pesoIntencionNavMesh = 0.75f;


    [Tooltip(
        "Ignora intenciones de giro demasiado pequeñas."
    )]
    public float umbralIntencionGiro = 4f;


    // =====================================================
    // PASO ANTICIPADO POR GIRO
    // =====================================================

    [Header("Paso anticipado por giro")]

    [Tooltip(
        "Permite que una pata empiece a recolocarse por el giro " +
        "antes de alcanzar la distancia normal de paso."
    )]
    public bool permitirPasoPorGiro = true;


    [Tooltip(
        "Ángulo de giro deseado mínimo para permitir un paso anticipado."
    )]
    public float umbralGiroParaPaso = 12f;


    [Tooltip(
        "Distancia horizontal mínima entre el pie plantado y el objetivo " +
        "anticipado para disparar un paso durante un giro."
    )]
    public float distanciaMinimaPasoGiro = 0.12f;


    // =====================================================
    // SUELO
    // =====================================================

    [Header("Detección de suelo")]

    public LayerMask capaSuelo = ~0;

    public float alturaRaycast = 1.5f;
    public float distanciaRaycast = 3f;
    public float offsetSuelo = 0.03f;


    // =====================================================
    // SUPERFICIE DE APOYO
    // =====================================================

    [Header("Superficie de apoyo")]

    [Tooltip(
        "Normal actual de la superficie sobre la que trabaja la pata. " +
        "Vector3.up equivale al suelo horizontal. Durante la escalada " +
        "el GestorPatasArana cambiará esta normal hacia la pared."
    )]
    [SerializeField]
    private Vector3 normalSuperficieActual = Vector3.up;


    public Vector3 NormalSuperficieActual
    {
        get
        {
            return ObtenerNormalSuperficieSegura();
        }
    }


    // =====================================================
    // FORMA
    // =====================================================

    [Header("Forma")]

    public float grosorPata = 0.10f;

    public float direccionRodilla = 1f;


    // =====================================================
    // DEBUG
    // =====================================================

    [Header("Debug")]

    public bool mostrarDebug = true;

    public bool mostrarTargetAnticipado = true;


    // =====================================================
    // INTERNAS DE LA PATA
    // =====================================================

    private float longitudUpper;
    private float longitudLower;

    private Vector3 posicionPieActual;

    private Vector3 inicioPaso;
    private Vector3 destinoPaso;

    private float progresoPaso;

    private bool dandoPaso = false;
    private bool pasoEmergencia = false;


    // =====================================================
    // INTERNAS DE ANTICIPACIÓN
    // =====================================================

    private float yawAnterior;

    private float velocidadYawSuavizada;

    private bool prediccionInicializada = false;

    private int ultimoFramePrediccion = -1;

    private Vector3 ultimoObjetivoAnticipado;

    private bool existeObjetivoAnticipado = false;


    // Intención de giro enviada por GestorPatasArana.
    private float anguloGiroDeseadoExterno = 0f;

    private float intensidadMovimientoExterna = 0f;

    private bool tieneIntencionGiroExterna = false;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (cadera == null)
        {
            cadera = transform;
        }


        if (rodilla == null ||
            pie == null ||
            objetivo == null ||
            upperLeg == null ||
            lowerLeg == null)
        {
            Debug.LogError(
                name +
                ": faltan referencias en PataProcedural."
            );

            enabled = false;

            return;
        }


        // =================================================
        // REFERENCIA AUTOMÁTICA PARA EL GIRO
        // =================================================

        if (referenciaGiro == null &&
            objetivo.parent != null)
        {
            referenciaGiro =
                objetivo.parent;
        }


        if (referenciaGiro != null)
        {
            yawAnterior =
                referenciaGiro.eulerAngles.y;

            prediccionInicializada =
                true;
        }


        // =================================================
        // LONGITUD DE LA PATA
        // =================================================

        longitudUpper =
            Vector3.Distance(
                cadera.position,
                rodilla.position
            );


        longitudLower =
            Vector3.Distance(
                rodilla.position,
                pie.position
            );


        longitudUpper =
            Mathf.Max(
                0.05f,
                longitudUpper
            );


        longitudLower =
            Mathf.Max(
                0.05f,
                longitudLower
            );


        // =================================================
        // POSICIÓN INICIAL DEL PIE
        // =================================================

        posicionPieActual =
            BuscarSuelo(
                pie.position
            );


        pie.position =
            posicionPieActual;


        ultimoObjetivoAnticipado =
            objetivo.position;


        ActualizarIK();
    }


    // =====================================================
    // LATE UPDATE
    // =====================================================

    private void LateUpdate()
    {
        if (objetivo == null ||
            cadera == null)
        {
            return;
        }


        // Actualizamos la información del giro.
        ActualizarPrediccionGiro();


        // =================================================
        // FUNCIONAMIENTO SIN GESTOR
        // =================================================

        if (!controlExterno &&
            !dandoPaso)
        {
            if (NecesitaDarPaso())
            {
                OrdenarPaso();
            }
        }


        // =================================================
        // REALIZAR PASO
        // =================================================

        if (dandoPaso)
        {
            ActualizarPaso();
        }


        // =================================================
        // IK
        // =================================================

        ActualizarIK();
    }


    // =====================================================
    // ACTUALIZAR VELOCIDAD DE GIRO
    // =====================================================

    private void ActualizarPrediccionGiro()
    {
        // Evitamos calcular varias veces durante
        // el mismo frame.
        if (ultimoFramePrediccion ==
            Time.frameCount)
        {
            return;
        }


        ultimoFramePrediccion =
            Time.frameCount;


        if (!anticiparGiro ||
            referenciaGiro == null)
        {
            velocidadYawSuavizada =
                0f;

            return;
        }


        float yawActual =
            referenciaGiro.eulerAngles.y;


        if (!prediccionInicializada)
        {
            yawAnterior =
                yawActual;

            velocidadYawSuavizada =
                0f;

            prediccionInicializada =
                true;

            return;
        }


        float deltaTime =
            Mathf.Max(
                Time.deltaTime,
                0.0001f
            );


        // DeltaAngle evita el problema:
        //
        // 359º -> 0º
        //
        // y nos devuelve correctamente +1º.
        float deltaYaw =
            Mathf.DeltaAngle(
                yawAnterior,
                yawActual
            );


        float velocidadYawActual =
            deltaYaw /
            deltaTime;


        // =================================================
        // SUAVIZAR
        // =================================================

        float factor =
            1f -
            Mathf.Exp(
                -suavizadoVelocidadGiro *
                deltaTime
            );


        velocidadYawSuavizada =
            Mathf.Lerp(
                velocidadYawSuavizada,
                velocidadYawActual,
                factor
            );


        yawAnterior =
            yawActual;
    }


    // =====================================================
    // RECIBIR INTENCIÓN DE GIRO DEL GESTOR
    // =====================================================

    public void EstablecerIntencionGiro(
        float anguloDeseado,
        float intensidadMovimiento)
    {
        anguloGiroDeseadoExterno =
            anguloDeseado;


        intensidadMovimientoExterna =
            Mathf.Clamp01(
                intensidadMovimiento
            );


        tieneIntencionGiroExterna =
            true;
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
    }


    public void RestaurarNormalSuelo()
    {
        normalSuperficieActual =
            Vector3.up;
    }


    private Vector3 ObtenerNormalSuperficieSegura()
    {
        if (normalSuperficieActual.sqrMagnitude <
            0.0001f)
        {
            return Vector3.up;
        }


        return normalSuperficieActual.normalized;
    }


    // =====================================================
    // OBTENER POSICIÓN IDEAL
    // =====================================================

    private Vector3 ObtenerObjetivoParaPaso()
    {
        if (objetivo == null)
        {
            return transform.position;
        }


        ActualizarPrediccionGiro();


        Vector3 posicionNormal =
            objetivo.position;


        // =================================================
        // ¿HAY GIRO REAL?
        // =================================================

        bool hayGiroReal =
            anticiparGiro &&
            referenciaGiro != null &&
            Mathf.Abs(
                velocidadYawSuavizada
            ) >=
            umbralVelocidadGiro;


        // =================================================
        // ¿HAY INTENCIÓN FUTURA DEL NAVMESH?
        // =================================================

        bool hayIntencionNavMesh =
            anticiparGiro &&
            referenciaGiro != null &&
            tieneIntencionGiroExterna &&
            intensidadMovimientoExterna > 0.01f &&
            Mathf.Abs(
                anguloGiroDeseadoExterno
            ) >=
            umbralIntencionGiro;


        // =================================================
        // SIN ANTICIPACIÓN
        // =================================================
        //
        // IMPORTANTE:
        //
        // Antes solo mirábamos el yaw real. Eso hacía que
        // la anticipación empezase DESPUÉS de que el cuerpo
        // ya hubiese comenzado a girar.
        //
        // Ahora también aceptamos la intención del NavMesh.
        // =================================================

        if (!hayGiroReal &&
            !hayIntencionNavMesh)
        {
            ultimoObjetivoAnticipado =
                BuscarSuelo(
                    posicionNormal
                );


            existeObjetivoAnticipado =
                false;


            return
                ultimoObjetivoAnticipado;
        }


        // =================================================
        // 1. GIRO QUE YA ESTÁ OCURRIENDO
        // =================================================

        float anguloPorYaw =
            0f;


        if (hayGiroReal)
        {
            anguloPorYaw =
                velocidadYawSuavizada *
                tiempoAnticipacionGiro;
        }


        // =================================================
        // 2. GIRO QUE EL NAVMESH QUIERE HACER
        // =================================================

        float anguloPorIntencion =
            0f;


        if (hayIntencionNavMesh)
        {
            anguloPorIntencion =
                anguloGiroDeseadoExterno *
                intensidadMovimientoExterna;
        }


        // =================================================
        // 3. COMBINAR LAS DOS SEÑALES
        // =================================================
        //
        // Si únicamente tenemos intención NavMesh, usamos
        // esa intención directamente.
        //
        // Si únicamente existe giro físico, conservamos el
        // sistema anterior basado en yaw.
        //
        // Si existen ambos, mezclamos según
        // pesoIntencionNavMesh.
        // =================================================

        float anguloFuturo;


        if (hayGiroReal &&
            hayIntencionNavMesh)
        {
            anguloFuturo =
                Mathf.Lerp(
                    anguloPorYaw,
                    anguloPorIntencion,
                    pesoIntencionNavMesh
                );
        }
        else if (hayIntencionNavMesh)
        {
            anguloFuturo =
                anguloPorIntencion;
        }
        else
        {
            anguloFuturo =
                anguloPorYaw;
        }


        // =================================================
        // LIMITAR ÁNGULO
        // =================================================

        anguloFuturo =
            Mathf.Clamp(
                anguloFuturo,
                -anguloMaximoAnticipacion,
                anguloMaximoAnticipacion
            );


        // =================================================
        // OFFSET DEL TARGET RESPECTO AL CUERPO
        // =================================================

        Vector3 offset =
            posicionNormal -
            referenciaGiro.position;


        // =================================================
        // GIRAR EL OFFSET HACIA LA POSICIÓN FUTURA
        // =================================================

        Vector3 normalSuperficie =
            ObtenerNormalSuperficieSegura();


        Quaternion giroFuturo =
            Quaternion.AngleAxis(
                anguloFuturo,
                normalSuperficie
            );


        Vector3 posicionAnticipada =
            referenciaGiro.position +
            giroFuturo *
            offset;


        // =================================================
        // LIMITAR DISTANCIA DE ANTICIPACIÓN
        // =================================================

        Vector3 desplazamiento =
            posicionAnticipada -
            posicionNormal;


        Vector3 desplazamientoPlano =
            Vector3.ProjectOnPlane(
                desplazamiento,
                normalSuperficie
            );


        if (desplazamientoPlano.magnitude >
            distanciaMaximaAnticipacion)
        {
            desplazamientoPlano =
                desplazamientoPlano.normalized *
                distanciaMaximaAnticipacion;


            posicionAnticipada =
                posicionNormal +
                desplazamientoPlano;
        }


        // Eliminamos cualquier desplazamiento accidental
        // perpendicular a la superficie. En suelo horizontal
        // esto equivale exactamente a conservar la misma Y.
        float desplazamientoNormal =
            Vector3.Dot(
                posicionAnticipada -
                posicionNormal,
                normalSuperficie
            );


        posicionAnticipada -=
            normalSuperficie *
            desplazamientoNormal;


        // =================================================
        // PROYECTAR AL SUELO
        // =================================================

        posicionAnticipada =
            BuscarSuelo(
                posicionAnticipada
            );


        ultimoObjetivoAnticipado =
            posicionAnticipada;


        existeObjetivoAnticipado =
            true;


        return posicionAnticipada;
    }


    // =====================================================
    // ¿NECESITA PASO?
    // =====================================================

    public bool NecesitaDarPaso()
    {
        if (objetivo == null ||
            cadera == null ||
            dandoPaso)
        {
            return false;
        }


        Vector3 objetivoPaso =
            ObtenerObjetivoParaPaso();


        // =================================================
        // PIE -> OBJETIVO FUTURO
        // =================================================

        float distanciaObjetivo =
            Vector3.Distance(
                posicionPieActual,
                objetivoPaso
            );


        // =================================================
        // CADERA -> PIE
        // =================================================

        float distanciaExtension =
            Vector3.Distance(
                cadera.position,
                posicionPieActual
            );


        float longitudTotal =
            longitudUpper +
            longitudLower;


        float extensionSegura =
            longitudTotal *
            porcentajeExtensionMaxima;


        // =================================================
        // PASO ANTICIPADO POR GIRO
        // =================================================
        //
        // Durante un giro no esperamos a que la pata llegue
        // a la distancia normal de paso. Si el NavMesh ya
        // anuncia un giro suficientemente grande y el apoyo
        // futuro se ha desplazado una distancia mínima, la
        // pata puede recolocarse antes de quedar estirada.
        // =================================================

        Vector3 diferenciaPlano =
            Vector3.ProjectOnPlane(
                objetivoPaso -
                posicionPieActual,
                ObtenerNormalSuperficieSegura()
            );


        float distanciaHorizontalGiro =
            diferenciaPlano.magnitude;


        bool hayIntencionFuerteDeGiro =
            tieneIntencionGiroExterna &&
            intensidadMovimientoExterna > 0.01f &&
            Mathf.Abs(
                anguloGiroDeseadoExterno
            ) >=
            umbralGiroParaPaso;


        bool necesitaPorGiro =
            permitirPasoPorGiro &&
            anticiparGiro &&
            hayIntencionFuerteDeGiro &&
            distanciaHorizontalGiro >=
                distanciaMinimaPasoGiro;


        return
            distanciaObjetivo >=
                distanciaPaso
            ||
            distanciaExtension >=
                extensionSegura
            ||
            necesitaPorGiro;
    }


    // =====================================================
    // ORDENAR PASO
    // =====================================================

    public void OrdenarPaso()
    {
        if (objetivo == null ||
            cadera == null ||
            dandoPaso)
        {
            return;
        }


        // AQUÍ ESTÁ EL CAMBIO IMPORTANTE:
        //
        // ya no pisamos necesariamente hacia
        // objetivo.position.
        //
        // Pisamos hacia la posición anticipada.
        Vector3 nuevoDestino =
            ObtenerObjetivoParaPaso();


        float distanciaExtension =
            Vector3.Distance(
                cadera.position,
                posicionPieActual
            );


        float longitudTotal =
            longitudUpper +
            longitudLower;


        bool emergencia =
            distanciaExtension >=
            longitudTotal *
            porcentajeExtensionMaxima;


        EmpezarPaso(
            nuevoDestino,
            emergencia
        );
    }


    // =====================================================
    // EMPEZAR PASO
    // =====================================================

    private void EmpezarPaso(
        Vector3 nuevoDestino,
        bool emergencia = false)
    {
        dandoPaso =
            true;


        pasoEmergencia =
            emergencia;


        progresoPaso =
            0f;


        inicioPaso =
            posicionPieActual;


        destinoPaso =
            nuevoDestino;
    }


    // =====================================================
    // ACTUALIZAR PASO
    // =====================================================

    private void ActualizarPaso()
    {
        float velocidadActual =
            velocidadPaso;


        if (pasoEmergencia)
        {
            velocidadActual *=
                multiplicadorVelocidadEmergencia;
        }


        progresoPaso +=
            Time.deltaTime *
            velocidadActual;


        float t =
            Mathf.Clamp01(
                progresoPaso
            );


        // =================================================
        // SUAVIZADO
        // =================================================

        float tSuave =
            t *
            t *
            (
                3f -
                2f * t
            );


        // =================================================
        // MOVIMIENTO
        // =================================================

        Vector3 posicion =
            Vector3.Lerp(
                inicioPaso,
                destinoPaso,
                tSuave
            );


        // =================================================
        // ARCO
        // =================================================

        posicion +=
            ObtenerNormalSuperficieSegura() *
            (
                Mathf.Sin(
                    t *
                    Mathf.PI
                )
                *
                alturaPaso
            );


        posicionPieActual =
            posicion;


        // =================================================
        // FINAL
        // =================================================

        if (t >= 1f)
        {
            posicionPieActual =
                destinoPaso;


            dandoPaso =
                false;


            pasoEmergencia =
                false;
        }
    }


    // =====================================================
    // BUSCAR SUELO
    // =====================================================

    private Vector3 BuscarSuelo(
        Vector3 posicion)
    {
        Vector3 normalSuperficie =
            ObtenerNormalSuperficieSegura();


        Vector3 origen =
            posicion +
            normalSuperficie *
            alturaRaycast;


        Vector3 direccionRaycast =
            -normalSuperficie;


        if (Physics.Raycast(
                origen,
                direccionRaycast,
                out RaycastHit hit,
                distanciaRaycast,
                capaSuelo,
                QueryTriggerInteraction.Ignore))
        {
            return
                hit.point +
                hit.normal *
                offsetSuelo;
        }


        return posicion;
    }


    // =====================================================
    // IK
    // =====================================================

    private void ActualizarIK()
    {
        if (cadera == null ||
            rodilla == null ||
            pie == null)
        {
            return;
        }


        Vector3 posicionCadera =
            cadera.position;


        Vector3 posicionPie =
            posicionPieActual;


        Vector3 haciaPie =
            posicionPie -
            posicionCadera;


        float distanciaReal =
            haciaPie.magnitude;


        if (distanciaReal <
            0.001f)
        {
            return;
        }


        Vector3 direccion =
            haciaPie.normalized;


        // =================================================
        // ALCANCE MÁXIMO
        // =================================================

        float distanciaMaxima =
            longitudUpper +
            longitudLower -
            0.001f;


        // =================================================
        // LIMITAR VISUALMENTE
        // =================================================

        if (distanciaReal >
            distanciaMaxima)
        {
            posicionPie =
                posicionCadera +
                direccion *
                distanciaMaxima;


            distanciaReal =
                distanciaMaxima;
        }


        float distancia =
            distanciaReal;


        // =================================================
        // TRIÁNGULO
        // =================================================

        float x =
            (
                longitudUpper *
                longitudUpper
                -
                longitudLower *
                longitudLower
                +
                distancia *
                distancia
            )
            /
            (
                2f *
                distancia
            );


        float alturaCuadrado =
            longitudUpper *
            longitudUpper
            -
            x *
            x;


        float altura =
            Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    alturaCuadrado
                )
            );


        Vector3 centro =
            posicionCadera +
            direccion *
            x;


        // =================================================
        // DIRECCIÓN DE LA RODILLA
        // =================================================

        Vector3 normalSuperficie =
            ObtenerNormalSuperficieSegura();


        Vector3 direccionDoblez =
            normalSuperficie
            -
            direccion *
            Vector3.Dot(
                normalSuperficie,
                direccion
            );


        if (direccionDoblez.sqrMagnitude <
            0.001f)
        {
            Vector3 referenciaAlternativa =
                referenciaGiro != null
                ? referenciaGiro.forward
                : transform.forward;


            direccionDoblez =
                Vector3.ProjectOnPlane(
                    referenciaAlternativa,
                    direccion
                );
        }


        if (direccionDoblez.sqrMagnitude <
            0.001f)
        {
            direccionDoblez =
                Vector3.ProjectOnPlane(
                    transform.right,
                    direccion
                );
        }


        direccionDoblez.Normalize();


        direccionDoblez *=
            Mathf.Sign(
                direccionRodilla
            );


        Vector3 posicionRodilla =
            centro +
            direccionDoblez *
            altura;


        rodilla.position =
            posicionRodilla;


        pie.position =
            posicionPie;


        // =================================================
        // SEGMENTOS
        // =================================================

        ColocarSegmento(
            upperLeg,
            posicionCadera,
            posicionRodilla
        );


        ColocarSegmento(
            lowerLeg,
            posicionRodilla,
            posicionPie
        );
    }


    // =====================================================
    // COLOCAR CILINDRO
    // =====================================================

    private void ColocarSegmento(
        Transform segmento,
        Vector3 inicio,
        Vector3 final)
    {
        if (segmento == null)
            return;


        Vector3 direccion =
            final -
            inicio;


        float longitud =
            direccion.magnitude;


        if (longitud <
            0.001f)
        {
            return;
        }


        segmento.position =
            (
                inicio +
                final
            )
            *
            0.5f;


        segmento.up =
            direccion.normalized;


        segmento.localScale =
            new Vector3(
                grosorPata,
                longitud *
                0.5f,
                grosorPata
            );
    }


    // =====================================================
    // DEBUG
    // =====================================================

    private void OnDrawGizmos()
    {
        if (!mostrarDebug)
            return;


        // =================================================
        // TARGET NORMAL - VERDE
        // =================================================

        if (objetivo != null)
        {
            Gizmos.color =
                Color.green;


            Gizmos.DrawWireSphere(
                objetivo.position,
                0.07f
            );
        }


        // =================================================
        // TARGET ANTICIPADO - MAGENTA
        // =================================================

        if (Application.isPlaying &&
            mostrarTargetAnticipado &&
            existeObjetivoAnticipado)
        {
            Gizmos.color =
                Color.magenta;


            Gizmos.DrawWireSphere(
                ultimoObjetivoAnticipado,
                0.09f
            );


            if (objetivo != null)
            {
                Gizmos.DrawLine(
                    objetivo.position,
                    ultimoObjetivoAnticipado
                );
            }
        }


        // =================================================
        // PIE - CYAN
        // =================================================

        if (pie != null)
        {
            Gizmos.color =
                Color.cyan;


            Gizmos.DrawWireSphere(
                pie.position,
                0.07f
            );
        }


        // =================================================
        // RODILLA - AMARILLO
        // =================================================

        if (rodilla != null)
        {
            Gizmos.color =
                Color.yellow;


            Gizmos.DrawWireSphere(
                rodilla.position,
                0.07f
            );
        }


        // =================================================
        // NORMAL DE SUPERFICIE - AZUL
        // =================================================

        if (Application.isPlaying &&
            cadera != null)
        {
            Gizmos.color =
                Color.blue;


            Gizmos.DrawLine(
                cadera.position,
                cadera.position +
                ObtenerNormalSuperficieSegura() *
                0.45f
            );
        }
    }
}