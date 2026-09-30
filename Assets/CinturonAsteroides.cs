using System;
using System.Collections; // Necesario para las Corrutinas
using UnityEngine;

public class CinturonAsteroides : MonoBehaviour
{
    [Serializable]
    public struct FormaAnillo
    {
        public int cantidadAsteroides;
        public float radioInterior;
        public float radioExterior;
        public float elevacionCinturon;
        public float grosorAltura;
        public Vector3 escalaHumo;
    }

    public struct DatosSpawnMeteorito
    {
        public float anguloInicial;
        public float distanciaAlCentro;
        public float alturaY;
        public float velocidadOrbita;
    }

    private struct AsteroideOrbital
    {
        public Transform transformAsteroide;
        public float anguloActual;
        public float factorRadio;
        public float factorAltura;
        public float velocidadOrbita;
        public Vector3 ejeRotacionLocal;
        public float velocidadRotacionLocal;
        public float offsetActualExplosion;
        public float velocidadRadial;
    }

    [Header("Referencias")]
    public Transform objetivoOrbita;
    public GameObject[] prefabsAsteroides;

    [Header("Rotación del Objetivo Central")]
    public Vector3 ejeRotacionObjetivo = Vector3.up;
    public float velocidadRotacionObjetivo = 15f;
    [HideInInspector] public bool rotacionPausada = false;

    [Header("Orientación Dinámica")]
    public Transform jugador;
    public string tagJugador = "Player";
    public float inclinacionX = 15f;

    [Header("Generación (Solo si no usa Gestor)")]
    public int cantidadInicialAsteroides = 200;

    [Header("Transición de Forma")]
    public float velocidadTransicionForma = 2f;

    private FormaAnillo formaObjetivo;

    [HideInInspector] public float radioInterior = 40f;
    [HideInInspector] public float radioExterior = 60f;
    [HideInInspector] public float elevacionCinturon = 10f;
    [HideInInspector] public float grosorAltura = 5f;
    private Vector3 escalaBaseHumo = Vector3.one;

    [Header("Fracción de Caída (Drop Zone)")]
    [Range(0f, 360f)] public float anguloCentroCaida = 180f;
    [Range(10f, 360f)] public float aperturaCaida = 90f;

    [Header("Movimiento y Variación")]
    public Vector2 rangoVelocidadOrbita = new Vector2(20f, 40f);
    public Vector2 rangoVelocidadRotacionLocal = new Vector2(10f, 50f);
    public Vector2 rangoEscala = new Vector2(500f, 500f);

    [Header("Física de Explosión (Global)")]
    public float fuerzaExplosionMin = 40f;
    public float fuerzaExplosionMax = 100f;
    public float rigidezResorteAsteroides = 25f;
    public float amortiguacionAsteroides = 5f;

    [Header("Física de Explosión (Impactos Locales)")]
    public float radioImpactoLocal = 15f;
    public float fuerzaImpactoLocal = 60f;

    [Header("Física de Explosión (Humo Visual)")]
    public ParticleSystem particulasAnilloHumo;
    public float fuerzaExplosionHumo = 3f;
    public float rigidezResorteHumo = 25f;
    public float amortiguacionHumo = 5f;
    public float escalaHumoInvisible = 1.5f;

    private Color colorOriginalHumo;
    private float multiplicadorHumoActual = 1f;
    private float velocidadRadialHumo = 0f;

    private ParticleSystem.Particle[] particulasHumoBuffer;
    private AsteroideOrbital[] arrayAsteroides;

    // --- NUEVO: Control de autoreparación ---
    private float temporizadorReparacion = 0f;
    private float intervaloReparacion = 1f; // Comprueba si faltan asteroides cada segundo

    void Start()
    {
        if (objetivoOrbita == null || prefabsAsteroides == null || prefabsAsteroides.Length == 0) return;

        if (jugador == null && !string.IsNullOrEmpty(tagJugador))
        {
            GameObject objJugador = GameObject.FindGameObjectWithTag(tagJugador);
            if (objJugador != null) jugador = objJugador.transform;
        }

        if (particulasAnilloHumo != null)
        {
            escalaBaseHumo = particulasAnilloHumo.transform.localScale;
            colorOriginalHumo = particulasAnilloHumo.main.startColor.color;
            particulasHumoBuffer = new ParticleSystem.Particle[particulasAnilloHumo.main.maxParticles];
        }

        if (arrayAsteroides == null || arrayAsteroides.Length == 0)
        {
            // Inicializa formaObjetivo para que la autoreparación sepa la cantidad meta
            formaObjetivo = new FormaAnillo { cantidadAsteroides = cantidadInicialAsteroides, radioInterior = radioInterior, radioExterior = radioExterior, elevacionCinturon = elevacionCinturon, grosorAltura = grosorAltura };
            AjustarCantidadAsteroides(cantidadInicialAsteroides);
        }
    }

    private void AjustarCantidadAsteroides(int nuevaCantidad)
    {
        if (arrayAsteroides == null)
            arrayAsteroides = new AsteroideOrbital[0];

        int cantidadActual = arrayAsteroides.Length;
        if (nuevaCantidad == cantidadActual) return;

        if (nuevaCantidad < cantidadActual)
        {
            for (int i = nuevaCantidad; i < cantidadActual; i++)
            {
                if (arrayAsteroides[i].transformAsteroide != null)
                {
                    Destroy(arrayAsteroides[i].transformAsteroide.gameObject);
                }
            }
            Array.Resize(ref arrayAsteroides, nuevaCantidad);
        }
        else
        {
            Array.Resize(ref arrayAsteroides, nuevaCantidad);

            for (int i = cantidadActual; i < nuevaCantidad; i++)
            {
                CrearNuevoAsteroide(i, cantidadActual > 0);
            }
        }
    }

    // --- NUEVO: Método extraído para crear un solo asteroide y animarlo ---
    private void CrearNuevoAsteroide(int indice, bool animarEscala)
    {
        GameObject prefabAleatorio = prefabsAsteroides[UnityEngine.Random.Range(0, prefabsAsteroides.Length)];
        GameObject nuevoAsteroide = Instantiate(prefabAleatorio, transform);
        Destroy(nuevoAsteroide.GetComponent<Rigidbody>());

        float escalaRandom = UnityEngine.Random.Range(rangoEscala.x, rangoEscala.y);

        // Si necesitamos animar la escala (es una reparación o un cambio de fase), empieza en 0
        if (animarEscala)
        {
            nuevoAsteroide.transform.localScale = Vector3.zero;
            StartCoroutine(RutinaAparecerAsteroide(nuevoAsteroide.transform, Vector3.one * escalaRandom, 5f));
        }
        else
        {
            nuevoAsteroide.transform.localScale = new Vector3(escalaRandom, escalaRandom, escalaRandom);
        }

        float fRadio = UnityEngine.Random.value;
        float fAltura = UnityEngine.Random.Range(-1f, 1f);

        arrayAsteroides[indice] = new AsteroideOrbital
        {
            transformAsteroide = nuevoAsteroide.transform,
            anguloActual = UnityEngine.Random.Range(0f, 360f),
            factorRadio = fRadio,
            factorAltura = fAltura,
            velocidadOrbita = UnityEngine.Random.Range(rangoVelocidadOrbita.x, rangoVelocidadOrbita.y),
            ejeRotacionLocal = UnityEngine.Random.onUnitSphere,
            velocidadRotacionLocal = UnityEngine.Random.Range(rangoVelocidadRotacionLocal.x, rangoVelocidadRotacionLocal.y),
            offsetActualExplosion = 0f,
            velocidadRadial = 0f
        };

        if (UnityEngine.Random.value > 0.5f) arrayAsteroides[indice].velocidadOrbita *= -1f;

        // Lo colocamos en su posición inicial inmediatamente para que no nazca en 0,0,0
        ObtenerMatrizCinturon(out Vector3 centroGlobal, out Quaternion rotacionGlobal);
        float radioBaseCalculado = Mathf.Lerp(radioInterior, radioExterior, fRadio);
        float alturaBaseY = grosorAltura * fAltura;
        nuevoAsteroide.transform.position = ObtenerPosicionEnCinturonRapida(arrayAsteroides[indice].anguloActual, radioBaseCalculado, alturaBaseY, centroGlobal, rotacionGlobal);
    }

    // --- NUEVA CORRUTINA: Hace crecer el asteroide suavemente ---
    private IEnumerator RutinaAparecerAsteroide(Transform asteroide, Vector3 escalaFinal, float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            if (asteroide == null) yield break; // Si se destruye mientras crece, paramos

            tiempo += Time.deltaTime;
            float progreso = tiempo / duracion;
            // Usamos SmoothStep para un crecimiento más orgánico (rápido al principio, lento al final)
            float valorSuavizado = Mathf.SmoothStep(0f, 1f, progreso);

            asteroide.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, valorSuavizado);
            yield return null;
        }

        if (asteroide != null) asteroide.localScale = escalaFinal;
    }

    // --- NUEVO: Revisa si hay huecos (asteroides null) en el array y los rellena ---
    private void ComprobarYRepararAsteroides()
    {
        if (arrayAsteroides == null || formaObjetivo.cantidadAsteroides == 0) return;

        bool arrayModificado = false;

        // Comprobamos si hay algún asteroide destruido en la lista actual
        for (int i = 0; i < arrayAsteroides.Length; i++)
        {
            if (arrayAsteroides[i].transformAsteroide == null)
            {
                // Si falta, lo volvemos a crear en ese mismo hueco de la lista
                CrearNuevoAsteroide(i, true);
                arrayModificado = true;
            }
        }
    }

    public void CambiarForma(FormaAnillo nuevaForma)
    {
        formaObjetivo = nuevaForma;
        AjustarCantidadAsteroides(nuevaForma.cantidadAsteroides);
    }

    public void AplicarFormaInmediata(FormaAnillo forma)
    {
        formaObjetivo = forma;
        radioInterior = forma.radioInterior;
        radioExterior = forma.radioExterior;
        elevacionCinturon = forma.elevacionCinturon;
        grosorAltura = forma.grosorAltura;
        escalaBaseHumo = forma.escalaHumo;
        AjustarCantidadAsteroides(forma.cantidadAsteroides);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // --- NUEVO: Lógica de autoreparación (no se hace cada frame por rendimiento) ---
        temporizadorReparacion += dt;
        if (temporizadorReparacion >= intervaloReparacion)
        {
            temporizadorReparacion = 0f;
            ComprobarYRepararAsteroides();
        }

        radioInterior = Mathf.Lerp(radioInterior, formaObjetivo.radioInterior, dt * velocidadTransicionForma);
        radioExterior = Mathf.Lerp(radioExterior, formaObjetivo.radioExterior, dt * velocidadTransicionForma);
        elevacionCinturon = Mathf.Lerp(elevacionCinturon, formaObjetivo.elevacionCinturon, dt * velocidadTransicionForma);
        grosorAltura = Mathf.Lerp(grosorAltura, formaObjetivo.grosorAltura, dt * velocidadTransicionForma);
        escalaBaseHumo = Vector3.Lerp(escalaBaseHumo, formaObjetivo.escalaHumo, dt * velocidadTransicionForma);

        float diferenciaRadios = radioExterior - radioInterior;

        if (particulasAnilloHumo != null)
        {
            float desplazamientoHumo = multiplicadorHumoActual - 1f;
            float aceleracionHumo = (-rigidezResorteHumo * desplazamientoHumo) - (amortiguacionHumo * velocidadRadialHumo);

            velocidadRadialHumo += aceleracionHumo * dt;
            multiplicadorHumoActual += velocidadRadialHumo * dt;

            particulasAnilloHumo.transform.localScale = new Vector3(
                escalaBaseHumo.x * multiplicadorHumoActual,
                escalaBaseHumo.y * multiplicadorHumoActual,
                escalaBaseHumo.z
            );

            float porcentajeAlfa = Mathf.InverseLerp(escalaHumoInvisible, 1f, multiplicadorHumoActual);
            var main = particulasAnilloHumo.main;
            Color nuevoColor = colorOriginalHumo;
            nuevoColor.a *= porcentajeAlfa;
            main.startColor = nuevoColor;

            if (particulasHumoBuffer != null)
            {
                int particulasVivas = particulasAnilloHumo.GetParticles(particulasHumoBuffer);
                byte alphaCalculado = (byte)Mathf.Clamp(colorOriginalHumo.a * porcentajeAlfa * 255f, 0f, 255f);

                for (int i = 0; i < particulasVivas; i++)
                {
                    Color32 colorParticula = particulasHumoBuffer[i].startColor;
                    colorParticula.a = alphaCalculado;
                    particulasHumoBuffer[i].startColor = colorParticula;
                }
                particulasAnilloHumo.SetParticles(particulasHumoBuffer, particulasVivas);
            }
        }

        if (objetivoOrbita == null || arrayAsteroides == null) return;

        if (velocidadRotacionObjetivo != 0f && !rotacionPausada)
        {
            objetivoOrbita.Rotate(ejeRotacionObjetivo, velocidadRotacionObjetivo * dt, Space.Self);
        }

        ObtenerMatrizCinturon(out Vector3 centroGlobal, out Quaternion rotacionGlobal);

        for (int i = 0; i < arrayAsteroides.Length; i++)
        {
            if (arrayAsteroides[i].transformAsteroide == null) continue;

            arrayAsteroides[i].anguloActual += arrayAsteroides[i].velocidadOrbita * dt;
            if (arrayAsteroides[i].anguloActual > 360f) arrayAsteroides[i].anguloActual -= 360f;
            else if (arrayAsteroides[i].anguloActual < 0f) arrayAsteroides[i].anguloActual += 360f;

            float desplazamientoAsteroide = arrayAsteroides[i].offsetActualExplosion;
            float aceleracionAsteroide = (-rigidezResorteAsteroides * desplazamientoAsteroide) - (amortiguacionAsteroides * arrayAsteroides[i].velocidadRadial);

            arrayAsteroides[i].velocidadRadial += aceleracionAsteroide * dt;
            arrayAsteroides[i].offsetActualExplosion += arrayAsteroides[i].velocidadRadial * dt;

            float radioBase = radioInterior + (diferenciaRadios * arrayAsteroides[i].factorRadio);
            float alturaBaseY = grosorAltura * arrayAsteroides[i].factorAltura;
            float distanciaTotal = radioBase + arrayAsteroides[i].offsetActualExplosion;

            float radianes = arrayAsteroides[i].anguloActual * Mathf.Deg2Rad;
            Vector3 posicionLocal = new Vector3(Mathf.Cos(radianes) * distanciaTotal, alturaBaseY, Mathf.Sin(radianes) * distanciaTotal);

            arrayAsteroides[i].transformAsteroide.position = centroGlobal + (rotacionGlobal * posicionLocal);

            arrayAsteroides[i].transformAsteroide.Rotate(
                arrayAsteroides[i].ejeRotacionLocal,
                arrayAsteroides[i].velocidadRotacionLocal * dt,
                Space.Self
            );
        }
    }

    public void ExplotarCinturon()
    {
        if (arrayAsteroides == null) return;
        for (int i = 0; i < arrayAsteroides.Length; i++)
        {
            arrayAsteroides[i].velocidadRadial = UnityEngine.Random.Range(fuerzaExplosionMin, fuerzaExplosionMax);
        }
        if (particulasAnilloHumo != null) velocidadRadialHumo = fuerzaExplosionHumo;
    }

    public void ExplotarLocal(Vector3 puntoImpacto)
    {
        if (arrayAsteroides == null) return;

        for (int i = 0; i < arrayAsteroides.Length; i++)
        {
            if (arrayAsteroides[i].transformAsteroide == null) continue;

            float distancia = Vector3.Distance(arrayAsteroides[i].transformAsteroide.position, puntoImpacto);

            if (distancia < radioImpactoLocal)
            {
                float porcentajeFuerza = 1f - (distancia / radioImpactoLocal);
                arrayAsteroides[i].velocidadRadial += fuerzaImpactoLocal * porcentajeFuerza;
            }
        }
    }

    public void ObtenerMatrizCinturon(out Vector3 centro, out Quaternion rotacion)
    {
        centro = objetivoOrbita != null ? objetivoOrbita.position + new Vector3(0f, elevacionCinturon, 0f) : Vector3.zero;
        rotacion = Quaternion.identity;

        if (jugador == null && !string.IsNullOrEmpty(tagJugador))
        {
            GameObject j = GameObject.FindGameObjectWithTag(tagJugador);
            if (j != null) jugador = j.transform;
        }

        if (jugador != null)
        {
            Vector3 direccionHaciaJugador = jugador.position - centro;
            direccionHaciaJugador.y = 0f;
            if (direccionHaciaJugador.sqrMagnitude > 0.001f)
            {
                rotacion = Quaternion.LookRotation(direccionHaciaJugador) * Quaternion.Euler(inclinacionX, 0f, 0f);
            }
        }
    }

    public Vector3 ObtenerPosicionEnCinturon(float anguloGrados, float distancia, float alturaY)
    {
        ObtenerMatrizCinturon(out Vector3 centro, out Quaternion rotacion);
        return ObtenerPosicionEnCinturonRapida(anguloGrados, distancia, alturaY, centro, rotacion);
    }

    private Vector3 ObtenerPosicionEnCinturonRapida(float anguloGrados, float distancia, float alturaY, Vector3 centroGlobal, Quaternion rotacionGlobal)
    {
        float radianes = anguloGrados * Mathf.Deg2Rad;
        Vector3 posicionLocal = new Vector3(Mathf.Cos(radianes) * distancia, alturaY, Mathf.Sin(radianes) * distancia);
        return centroGlobal + (rotacionGlobal * posicionLocal);
    }

    public DatosSpawnMeteorito ObtenerDatosSpawnMeteorito()
    {
        float velocidadO = UnityEngine.Random.Range(rangoVelocidadOrbita.x, rangoVelocidadOrbita.y);
        if (UnityEngine.Random.value > 0.5f) velocidadO *= -1f;

        return new DatosSpawnMeteorito
        {
            anguloInicial = UnityEngine.Random.Range(0f, 360f),
            distanciaAlCentro = UnityEngine.Random.Range(radioInterior, radioExterior),
            alturaY = UnityEngine.Random.Range(-grosorAltura, grosorAltura),
            velocidadOrbita = velocidadO
        };
    }

    public bool EstaEnZonaDeCaida(float angulo)
    {
        float diferencia = Mathf.Abs(Mathf.DeltaAngle(anguloCentroCaida, angulo));
        return diferencia <= (aperturaCaida / 2f);
    }

    void OnDrawGizmosSelected()
    {
        if (objetivoOrbita == null) return;
        ObtenerMatrizCinturon(out Vector3 centroElevado, out Quaternion rotacionAnillo);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        DibujarArcoGizmo(centroElevado, radioInterior, rotacionAnillo, 0f, 360f);
        DibujarArcoGizmo(centroElevado, radioExterior, rotacionAnillo, 0f, 360f);
        Gizmos.color = Color.red;
        float anguloInicio = anguloCentroCaida - (aperturaCaida / 2f);
        float anguloFin = anguloCentroCaida + (aperturaCaida / 2f);
        DibujarArcoGizmo(centroElevado, radioInterior, rotacionAnillo, anguloInicio, anguloFin);
        DibujarArcoGizmo(centroElevado, radioExterior, rotacionAnillo, anguloInicio, anguloFin);
    }

    void DibujarArcoGizmo(Vector3 centro, float radio, Quaternion rotacionAnillo, float anguloInicio, float anguloFin)
    {
        int segmentos = 40;
        float rango = Mathf.Abs(Mathf.DeltaAngle(anguloInicio, anguloFin));
        if (rango < 0.1f && anguloFin - anguloInicio >= 360f) rango = 360f;
        float paso = rango / segmentos;
        float radInicial = anguloInicio * Mathf.Deg2Rad;
        Vector3 posInicialLocal = new Vector3(Mathf.Cos(radInicial) * radio, 0, Mathf.Sin(radInicial) * radio);
        Vector3 puntoAnterior = centro + (rotacionAnillo * posInicialLocal);
        for (int i = 1; i <= segmentos; i++)
        {
            float radianes = (anguloInicio + paso * i) * Mathf.Deg2Rad;
            Vector3 posLocal = new Vector3(Mathf.Cos(radianes) * radio, 0, Mathf.Sin(radianes) * radio);
            Vector3 puntoNuevo = centro + (rotacionAnillo * posLocal);
            Gizmos.DrawLine(puntoAnterior, puntoNuevo);
            puntoAnterior = puntoNuevo;
        }
    }
}