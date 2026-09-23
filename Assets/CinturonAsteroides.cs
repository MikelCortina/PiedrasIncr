using System;
using UnityEngine;

public class CinturonAsteroides : MonoBehaviour
{
    [Serializable]
    public struct FormaAnillo
    {
        // --- NUEVO: Cantidad configurable por fase ---
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

    [Header("Física de Explosión (Asteroides)")]
    public float fuerzaExplosionMin = 40f;
    public float fuerzaExplosionMax = 100f;
    public float rigidezResorteAsteroides = 25f;
    public float amortiguacionAsteroides = 5f;

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

    void Start()
    {
        if (objetivoOrbita == null || prefabsAsteroides == null || prefabsAsteroides.Length == 0) return;

        if (jugador == null)
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

        // Si el Gestor no lo ha inicializado antes, lo hacemos aquí con el valor base
        if (arrayAsteroides == null || arrayAsteroides.Length == 0)
        {
            AjustarCantidadAsteroides(cantidadInicialAsteroides);
        }
    }

    // --- NUEVO: Motor dinámico para cambiar la cantidad de pedazos ---
    private void AjustarCantidadAsteroides(int nuevaCantidad)
    {
        if (arrayAsteroides == null)
            arrayAsteroides = new AsteroideOrbital[0];

        int cantidadActual = arrayAsteroides.Length;
        if (nuevaCantidad == cantidadActual) return;

        if (nuevaCantidad < cantidadActual)
        {
            // Hay menos: Destruimos los que sobran
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
            // Hay más: Redimensionamos y generamos los que faltan
            Array.Resize(ref arrayAsteroides, nuevaCantidad);

            for (int i = cantidadActual; i < nuevaCantidad; i++)
            {
                GameObject prefabAleatorio = prefabsAsteroides[UnityEngine.Random.Range(0, prefabsAsteroides.Length)];
                GameObject nuevoAsteroide = Instantiate(prefabAleatorio, transform);
                Destroy(nuevoAsteroide.GetComponent<Rigidbody>());

                float escalaRandom = UnityEngine.Random.Range(rangoEscala.x, rangoEscala.y);
                nuevoAsteroide.transform.localScale = new Vector3(escalaRandom, escalaRandom, escalaRandom);

                arrayAsteroides[i] = new AsteroideOrbital
                {
                    transformAsteroide = nuevoAsteroide.transform,
                    anguloActual = UnityEngine.Random.Range(0f, 360f),
                    factorRadio = UnityEngine.Random.value,
                    factorAltura = UnityEngine.Random.Range(-1f, 1f),

                    velocidadOrbita = UnityEngine.Random.Range(rangoVelocidadOrbita.x, rangoVelocidadOrbita.y),
                    ejeRotacionLocal = UnityEngine.Random.onUnitSphere,
                    velocidadRotacionLocal = UnityEngine.Random.Range(rangoVelocidadRotacionLocal.x, rangoVelocidadRotacionLocal.y),
                    offsetActualExplosion = 0f,
                    velocidadRadial = 0f
                };

                if (UnityEngine.Random.value > 0.5f) arrayAsteroides[i].velocidadOrbita *= -1f;

                // Los colocamos en su sitio base para que no parpadeen en el centro antes de explotar
                float radioBase = Mathf.Lerp(radioInterior, radioExterior, arrayAsteroides[i].factorRadio);
                float alturaBaseY = grosorAltura * arrayAsteroides[i].factorAltura;
                nuevoAsteroide.transform.position = ObtenerPosicionEnCinturon(arrayAsteroides[i].anguloActual, radioBase, alturaBaseY);
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
        radioInterior = Mathf.Lerp(radioInterior, formaObjetivo.radioInterior, Time.deltaTime * velocidadTransicionForma);
        radioExterior = Mathf.Lerp(radioExterior, formaObjetivo.radioExterior, Time.deltaTime * velocidadTransicionForma);
        elevacionCinturon = Mathf.Lerp(elevacionCinturon, formaObjetivo.elevacionCinturon, Time.deltaTime * velocidadTransicionForma);
        grosorAltura = Mathf.Lerp(grosorAltura, formaObjetivo.grosorAltura, Time.deltaTime * velocidadTransicionForma);
        escalaBaseHumo = Vector3.Lerp(escalaBaseHumo, formaObjetivo.escalaHumo, Time.deltaTime * velocidadTransicionForma);

        if (particulasAnilloHumo != null)
        {
            float desplazamientoHumo = multiplicadorHumoActual - 1f;
            float aceleracionHumo = (-rigidezResorteHumo * desplazamientoHumo) - (amortiguacionHumo * velocidadRadialHumo);

            velocidadRadialHumo += aceleracionHumo * Time.deltaTime;
            multiplicadorHumoActual += velocidadRadialHumo * Time.deltaTime;

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
            objetivoOrbita.Rotate(ejeRotacionObjetivo, velocidadRotacionObjetivo * Time.deltaTime, Space.Self);
        }

        for (int i = 0; i < arrayAsteroides.Length; i++)
        {
            arrayAsteroides[i].anguloActual += arrayAsteroides[i].velocidadOrbita * Time.deltaTime;
            if (arrayAsteroides[i].anguloActual > 360f) arrayAsteroides[i].anguloActual -= 360f;
            else if (arrayAsteroides[i].anguloActual < 0f) arrayAsteroides[i].anguloActual += 360f;

            float desplazamientoAsteroide = arrayAsteroides[i].offsetActualExplosion;
            float aceleracionAsteroide = (-rigidezResorteAsteroides * desplazamientoAsteroide) - (amortiguacionAsteroides * arrayAsteroides[i].velocidadRadial);

            arrayAsteroides[i].velocidadRadial += aceleracionAsteroide * Time.deltaTime;
            arrayAsteroides[i].offsetActualExplosion += arrayAsteroides[i].velocidadRadial * Time.deltaTime;

            float radioBase = Mathf.Lerp(radioInterior, radioExterior, arrayAsteroides[i].factorRadio);
            float alturaBaseY = grosorAltura * arrayAsteroides[i].factorAltura;
            float distanciaTotal = radioBase + arrayAsteroides[i].offsetActualExplosion;

            Vector3 posicionFinal = ObtenerPosicionEnCinturon(
                arrayAsteroides[i].anguloActual,
                distanciaTotal,
                alturaBaseY
            );

            arrayAsteroides[i].transformAsteroide.position = posicionFinal;
            arrayAsteroides[i].transformAsteroide.Rotate(
                arrayAsteroides[i].ejeRotacionLocal,
                arrayAsteroides[i].velocidadRotacionLocal * Time.deltaTime,
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

        if (particulasAnilloHumo != null)
        {
            velocidadRadialHumo = fuerzaExplosionHumo;
        }
    }

    public void ObtenerMatrizCinturon(out Vector3 centro, out Quaternion rotacion)
    {
        centro = objetivoOrbita != null ? objetivoOrbita.position + new Vector3(0f, elevacionCinturon, 0f) : Vector3.zero;
        rotacion = Quaternion.identity;

        Transform targetJugador = jugador;
        if (targetJugador == null && !string.IsNullOrEmpty(tagJugador))
        {
            GameObject j = GameObject.FindGameObjectWithTag(tagJugador);
            if (j != null) targetJugador = j.transform;
        }

        if (targetJugador != null)
        {
            Vector3 direccionHaciaJugador = targetJugador.position - centro;
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
        float radianes = anguloGrados * Mathf.Deg2Rad;
        Vector3 posicionLocal = new Vector3(Mathf.Cos(radianes) * distancia, alturaY, Mathf.Sin(radianes) * distancia);
        return centro + (rotacion * posicionLocal);
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