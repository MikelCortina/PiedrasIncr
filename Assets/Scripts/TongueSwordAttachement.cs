using UnityEngine;

public class VisualSphereChain : MonoBehaviour
{
    [Header("Esferas de la cadena")]
    [Tooltip("Ordénalas desde la espada hasta la rana.")]
    [SerializeField] private Transform[] esferas;

    [Header("Anclas visuales")]
    [Tooltip("Punto del mango donde empieza la lengua.")]
    [SerializeField] private Transform anclaEspada;

    [Tooltip("Punto de la rana donde termina la lengua.")]
    [SerializeField] private Transform anclaRana;

    [Header("Rigidbody de la rana")]
    [SerializeField] private Rigidbody rigidbodyRana;

    [Header("Longitud de la lengua")]
    [Tooltip("Distancia entre esferas. Usa un valor bajo para una lengua corta.")]
    [SerializeField] private float longitudSegmento = 0.12f;

    [Header("Simulación visual")]
    [SerializeField] private Vector3 gravedad =
        new Vector3(0f, -0.5f, 0f);

    [Range(0f, 1f)]
    [SerializeField] private float amortiguacion = 0.94f;

    [SerializeField] private int subpasos = 10;

    [SerializeField] private int iteracionesRestriccion = 24;

    [Tooltip("Suavizado del movimiento del mango.")]
    [SerializeField] private float suavizadoAnclaEspada = 0.035f;

    [Header("Influencia de la rana")]
    [Tooltip("Influencia visual de la velocidad de la rana.")]
    [SerializeField] private float influenciaVelocidadRana = 0.02f;

    [Header("Arrastre físico de la rana")]
    [SerializeField] private bool arrastrarRana = true;

    [Tooltip("Fuerza del resorte que tira de la rana.")]
    [SerializeField] private float rigidezCadena = 120f;

    [Tooltip("Amortiguación para evitar vibraciones.")]
    [SerializeField] private float amortiguacionTension = 50f;

    [Tooltip("Límite máximo de la fuerza aplicada a la rana.")]
    [SerializeField] private float fuerzaMaximaArrastre = 600f;

    [Tooltip("Margen antes de empezar a tirar de la rana.")]
    [SerializeField] private float holguraCadena = 0.01f;

    [Header("Orientación visual")]
    [SerializeField] private bool orientarEsferas = true;

    [Header("Limpiar componentes físicos antiguos")]
    [SerializeField] private bool desactivarRigidbodies = true;

    [SerializeField] private bool desactivarColliders = true;

    private Vector3[] posiciones;
    private Vector3[] posicionesAnteriores;

    private Vector3 anclaEspadaSuavizada;
    private Vector3 velocidadAnclaEspada;

    private void Start()
    {
        InicializarCadena();
        ConfigurarRigidbodyRana();
    }

    private void FixedUpdate()
    {
        if (!arrastrarRana)
            return;

        AplicarTensionSobreLaRana();
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

        AplicarPosiciones();
    }

    private void InicializarCadena()
    {
        if (esferas == null ||
            esferas.Length < 3)
        {
            Debug.LogError(
                "La cadena necesita al menos tres esferas.",
                this
            );

            return;
        }

        if (anclaEspada == null)
        {
            Debug.LogError(
                "No se ha asignado el ancla de la espada.",
                this
            );

            return;
        }

        if (anclaRana == null)
        {
            Debug.LogError(
                "No se ha asignado el ancla de la rana.",
                this
            );

            return;
        }

        if (longitudSegmento <= 0.001f)
        {
            longitudSegmento = 0.12f;
        }

        posiciones =
            new Vector3[esferas.Length];

        posicionesAnteriores =
            new Vector3[esferas.Length];

        for (int i = 0;
             i < esferas.Length;
             i++)
        {
            if (esferas[i] == null)
            {
                Debug.LogError(
                    $"La esfera del índice {i} está vacía.",
                    this
                );

                continue;
            }

            posiciones[i] =
                esferas[i].position;

            posicionesAnteriores[i] =
                esferas[i].position;

            ConfigurarEsferaVisual(
                esferas[i]
            );
        }

        anclaEspadaSuavizada =
            anclaEspada.position;

        velocidadAnclaEspada =
            Vector3.zero;

        // Primer extremo: espada.
        posiciones[0] =
            anclaEspadaSuavizada;

        posicionesAnteriores[0] =
            posiciones[0];

        // Último extremo: rana.
        int ultimoIndice =
            posiciones.Length - 1;

        posiciones[ultimoIndice] =
            anclaRana.position;

        posicionesAnteriores[ultimoIndice] =
            posiciones[ultimoIndice];
    }

    private void ConfigurarRigidbodyRana()
    {
        if (rigidbodyRana == null)
        {
            Debug.LogWarning(
                "No se ha asignado el Rigidbody de la rana.",
                this
            );

            return;
        }

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

        // Unity 6.
        rigidbodyRana.linearDamping =
            2f;

        rigidbodyRana.angularDamping =
            4f;

        rigidbodyRana.maxAngularVelocity =
            15f;
    }

    private void SimularSubpaso(
        float deltaTime)
    {
        int ultimoIndice =
            posiciones.Length - 1;

        /*
         * Suavizar el movimiento de la espada.
         */
        anclaEspadaSuavizada =
            Vector3.SmoothDamp(
                anclaEspadaSuavizada,
                anclaEspada.position,
                ref velocidadAnclaEspada,
                suavizadoAnclaEspada,
                Mathf.Infinity,
                deltaTime
            );

        /*
         * La primera esfera sigue suavemente a la espada.
         */
        posiciones[0] =
            anclaEspadaSuavizada;

        posicionesAnteriores[0] =
            posiciones[0];

        /*
         * Integración Verlet de las esferas intermedias.
         */
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
                velocidad * amortiguacion +
                gravedad *
                deltaTime *
                deltaTime;
        }

        /*
         * La última esfera sigue visualmente a la rana.
         */
        Vector3 posicionRana =
            anclaRana.position;

        if (rigidbodyRana != null &&
            influenciaVelocidadRana > 0f)
        {
            Vector3 velocidadRana =
                rigidbodyRana.GetPointVelocity(
                    anclaRana.position
                );

            posicionRana +=
                velocidadRana *
                influenciaVelocidadRana *
                deltaTime;
        }

        posiciones[ultimoIndice] =
            posicionRana;

        posicionesAnteriores[ultimoIndice] =
            posiciones[ultimoIndice];

        /*
         * Resolver restricciones.
         */
        int iteracionesSeguras =
            Mathf.Max(
                1,
                iteracionesRestriccion
            );

        for (int iteracion = 0;
             iteracion < iteracionesSeguras;
             iteracion++)
        {
            posiciones[0] =
                anclaEspadaSuavizada;

            posiciones[ultimoIndice] =
                posicionRana;

            posicionesAnteriores[0] =
                posiciones[0];

            posicionesAnteriores[ultimoIndice] =
                posiciones[ultimoIndice];

            for (int i = 0;
                 i < posiciones.Length - 1;
                 i++)
            {
                AplicarRestriccionDistancia(i);
            }
        }

        posiciones[0] =
            anclaEspadaSuavizada;

        posiciones[ultimoIndice] =
            posicionRana;

        posicionesAnteriores[0] =
            posiciones[0];

        posicionesAnteriores[ultimoIndice] =
            posiciones[ultimoIndice];
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

        bool esPrimerSegmento =
            indice == 0;

        bool esUltimoSegmento =
            siguiente == posiciones.Length - 1;

        /*
         * La primera esfera está fijada a la espada.
         */
        if (esPrimerSegmento)
        {
            posiciones[siguiente] =
                posiciones[indice] +
                direccion *
                longitudSegmento;

            return;
        }

        /*
         * La última esfera está fijada a la rana.
         */
        if (esUltimoSegmento)
        {
            posiciones[indice] =
                posiciones[siguiente] -
                direccion *
                longitudSegmento;

            return;
        }

        /*
         * Corrección de los segmentos intermedios.
         */
        Vector3 correccion =
            direccion *
            error *
            0.5f;

        posiciones[indice] +=
            correccion;

        posiciones[siguiente] -=
            correccion;
    }

    private void AplicarTensionSobreLaRana()
    {
        if (rigidbodyRana == null ||
            anclaRana == null ||
            anclaEspada == null ||
            esferas == null ||
            esferas.Length < 2)
        {
            return;
        }

        /*
         * Distancia directa entre la espada y la rana.
         */
        Vector3 posicionEspada =
            anclaEspada.position;

        Vector3 posicionRana =
            anclaRana.position;

        Vector3 diferencia =
            posicionEspada -
            posicionRana;

        float distanciaExtremos =
            diferencia.magnitude;

        if (distanciaExtremos < 0.001f)
            return;

        /*
         * Longitud máxima aproximada de la lengua.
         */
        float longitudCadena =
            longitudSegmento *
            (esferas.Length - 1);

        float distanciaPermitida =
            longitudCadena +
            holguraCadena;

        float extension =
            distanciaExtremos -
            distanciaPermitida;

        /*
         * Si la lengua no está estirada,
         * no aplicamos fuerza.
         */
        if (extension <= 0f)
            return;

        Vector3 direccionHaciaEspada =
            diferencia.normalized;

        Vector3 velocidadRana =
            rigidbodyRana.GetPointVelocity(
                anclaRana.position
            );

        float velocidadHaciaEspada =
            Vector3.Dot(
                velocidadRana,
                direccionHaciaEspada
            );

        /*
         * Fuerza elástica.
         */
        float fuerzaResorte =
            extension *
            rigidezCadena;

        /*
         * Calculamos una amortiguación crítica
         * aproximada para evitar vibraciones.
         */
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

        Vector3 fuerzaArrastre =
            direccionHaciaEspada *
            fuerzaTotal;

        rigidbodyRana.AddForceAtPosition(
            fuerzaArrastre,
            anclaRana.position,
            ForceMode.Force
        );
    }

    private void AplicarPosiciones()
    {
        for (int i = 0;
             i < esferas.Length;
             i++)
        {
            if (esferas[i] == null)
                continue;

            esferas[i].position =
                posiciones[i];
        }

        if (!orientarEsferas)
            return;

        for (int i = 0;
             i < esferas.Length;
             i++)
        {
            Vector3 direccion;

            if (i < esferas.Length - 1)
            {
                direccion =
                    posiciones[i + 1] -
                    posiciones[i];
            }
            else
            {
                direccion =
                    posiciones[i] -
                    posiciones[i - 1];
            }

            if (direccion.sqrMagnitude < 0.0001f)
                continue;

            esferas[i].rotation =
                Quaternion.LookRotation(
                    direccion.normalized,
                    Vector3.up
                );
        }
    }

    private void ConfigurarEsferaVisual(
        Transform esfera)
    {
        if (esfera == null)
            return;

        if (desactivarRigidbodies)
        {
            Rigidbody rb =
                esfera.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
            }
        }

        if (desactivarColliders)
        {
            Collider[] colliders =
                esfera.GetComponentsInChildren<Collider>();

            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        /*
         * La cadena ya no utiliza joints físicos.
         */
        Joint[] joints =
            esfera.GetComponents<Joint>();

        foreach (Joint joint in joints)
        {
            if (joint != null)
            {
                Destroy(joint);
            }
        }
    }

    private bool CadenaValida()
    {
        if (esferas == null ||
            esferas.Length < 3)
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
             i < esferas.Length;
             i++)
        {
            if (esferas[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    public Vector3[] ObtenerPosiciones()
    {
        return posiciones;
    }
}