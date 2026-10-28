
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GestorRuletasFPS : MonoBehaviour
{
    public Camera camaraPrincipal;

    [Tooltip("Selecciona AQUÍ la capa de Ruletas/Botones Y TAMBIÉN las capas de los edificios/suelo (Default, Edificios, etc.)")]
    public LayerMask capasDetectables;

    public float distanciaInteraccion = 3f;
    public float sensibilidadGiro = 1f;

    [Tooltip("Sensibilidad para arrastrar palancas al mirar hacia abajo")]
    public float sensibilidadArrastre = 100f;

    [Header("UI Mirilla")]
    public Image imagenMirilla;
    public Sprite mirillaPorDefecto;
    public Sprite mirillaInteractiva;

    [Header("Animación de Mirilla")]
    public float multiplicadorTamano = 1.5f;
    public float velocidadPop = 15f;

    [Header("Debug")]
    [Tooltip("Dibuja el rayo en la vista Scene y escribe en consola qué collider toca cada vez que cambia")]
    public bool debugRaycast = false;
    [Tooltip("Con debug activo, cada cuántos segundos se escribe el informe completo (cámaras, origen del rayo y palancas)")]
    public float intervaloInformeDebug = 1f;

    // Qué gestor controla cada imagen de mirilla, para detectar duplicados que se pisen.
    private static readonly Dictionary<Image, GestorRuletasFPS> propietariosMirilla = new Dictionary<Image, GestorRuletasFPS>();

    private Vector3 escalaOriginalMirilla;
    private bool escalaOriginalGuardada;

    private Sprite spriteAplicado;
    private bool avisoSpriteExternoMostrado;

    private RuletaInteractiva ruletaActiva = null;
    private RuletaInteractivaGasoil ruletaGasoilActiva = null;
    private float anguloPantallaAnterior;

    // --- NUEVAS VARIABLES PARA LA PALANCA ---
    private PalancaTirador palancaActiva = null;
    private float desplazamientoYAcumulado = 0f;

    // Estado del debug para escribir solo cuando cambia lo que toca el rayo.
    private bool debugInicializado;
    private Collider ultimoColliderDebug;
    private Collider ultimoColliderSinFiltroDebug;
    private float siguienteInformeDebug;

    void Awake()
    {
        GuardarEscalaOriginalMirilla();
    }

    void OnEnable()
    {
        debugInicializado = false;
        siguienteInformeDebug = 0f;

        if (camaraPrincipal == null)
        {
            Debug.LogError("[GestorRuletasFPS] '" + name + "' no tiene 'Camara Principal' asignada: no podrá detectar nada.", this);
        }
        else if (debugRaycast)
        {
            Debug.Log("[GestorRuletasFPS] Activo en '" + name + "' | rayo desde '" + camaraPrincipal.name +
                      "' | capas: " + NombresCapas(capasDetectables) + " | distancia: " + distanciaInteraccion, this);
        }

        if (imagenMirilla == null)
            return;

        if (propietariosMirilla.TryGetValue(imagenMirilla, out GestorRuletasFPS otro) && otro != null && otro != this)
        {
            Debug.LogWarning("[GestorRuletasFPS] Hay DOS gestores usando la misma mirilla '" + imagenMirilla.name +
                             "': '" + otro.name + "' y '" + name + "'. Se pisarán el sprite y el tamaño; deja solo uno.", this);
        }

        propietariosMirilla[imagenMirilla] = this;
    }

    void OnDisable()
    {
        // Liberamos las palancas/ruletas agarradas y dejamos la mirilla en su estado por defecto
        // para que no se quede congelada (grande o con la mano) al desactivar este gestor.
        if (palancaActiva != null)
        {
            palancaActiva.SoltarArrastre();
            palancaActiva = null;
        }

        ruletaActiva = null;
        ruletaGasoilActiva = null;

        if (imagenMirilla != null)
        {
            if (mirillaPorDefecto != null)
                imagenMirilla.sprite = mirillaPorDefecto;

            if (escalaOriginalGuardada)
                imagenMirilla.rectTransform.localScale = escalaOriginalMirilla;

            if (propietariosMirilla.TryGetValue(imagenMirilla, out GestorRuletasFPS propietario) && propietario == this)
                propietariosMirilla.Remove(imagenMirilla);
        }

        spriteAplicado = null;
    }

    void GuardarEscalaOriginalMirilla()
    {
        if (escalaOriginalGuardada || imagenMirilla == null)
            return;

        escalaOriginalMirilla = imagenMirilla.rectTransform.localScale;
        escalaOriginalGuardada = true;
    }

    void Update()
    {
        if (camaraPrincipal == null)
            return;

        Vector2 centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray rayo = camaraPrincipal.ScreenPointToRay(centroPantalla);

        bool impacto = Physics.Raycast(rayo, out RaycastHit hit, distanciaInteraccion, capasDetectables);
        bool mirandoInteractuable = false;

        if (debugRaycast)
        {
            DebugRayo(rayo, impacto, hit);
            InformeDebugPeriodico(rayo);
        }

        // La palanca puede tener el collider en un hijo (ej: la manilla), por eso buscamos hacia arriba.
        PalancaTirador palancaDetectada = impacto ? hit.collider.GetComponentInParent<PalancaTirador>() : null;

        // =========================================================
        // 1. ¿DEBEMOS MOSTRAR LA MIRILLA INTERACTIVA?
        // =========================================================
        if (ruletaActiva != null || ruletaGasoilActiva != null || palancaActiva != null)
        {
            mirandoInteractuable = true;
        }
        else if (impacto)
        {
            if (hit.collider.GetComponent<RuletaInteractiva>() != null ||
                hit.collider.GetComponent<RuletaInteractivaGasoil>() != null ||
                hit.collider.GetComponent<BotonTuberia>() != null ||
                hit.collider.GetComponentInParent<VentiladorPiedras>() != null ||
                PalancaUsable(palancaDetectada))
            {
                mirandoInteractuable = true;
            }
        }

        // =========================================================
        // 2. SISTEMA DE UI Y ANIMACIÓN "POP"
        // =========================================================
        ActualizarMirilla(mirandoInteractuable);

        // =========================================================
        // 3. LÓGICA DE INTERACCIÓN (CLICS)
        // =========================================================
        if (Input.GetMouseButtonDown(0))
        {
            if (impacto)
            {
                ruletaActiva = hit.collider.GetComponent<RuletaInteractiva>();
                ruletaGasoilActiva = hit.collider.GetComponent<RuletaInteractivaGasoil>();
                palancaActiva = PalancaUsable(palancaDetectada) ? palancaDetectada : null;

                if (ruletaActiva != null)
                {
                    anguloPantallaAnterior = CalcularAnguloCircular(ruletaActiva.transform, centroPantalla);
                }
                else if (ruletaGasoilActiva != null)
                {
                    anguloPantallaAnterior = CalcularAnguloCircular(ruletaGasoilActiva.transform, centroPantalla);
                }
                else if (palancaActiva != null)
                {
                    // Reiniciamos el acumulador al enganchar la palanca
                    desplazamientoYAcumulado = 0f;
                    palancaActiva.IniciarArrastre();
                }
                else if (palancaDetectada == null)
                {
                    BotonTuberia boton = hit.collider.GetComponent<BotonTuberia>();
                    if (boton != null)
                    {
                        boton.Interaccionar();
                    }
                    else
                    {
                        VentiladorPiedras ventilador = hit.collider.GetComponentInParent<VentiladorPiedras>();
                        if (ventilador != null)
                        {
                            ventilador.Interaccionar();
                        }
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ruletaActiva = null;
            ruletaGasoilActiva = null;

            if (palancaActiva != null)
            {
                palancaActiva.SoltarArrastre();
                palancaActiva = null;
            }
        }

        // =========================================================
        // 4. LÓGICA DE GIRO (RULETAS) Y ARRASTRE (PALANCAS)
        // =========================================================
        if (ruletaActiva != null && Input.GetMouseButton(0))
        {
            float anguloActual = CalcularAnguloCircular(ruletaActiva.transform, centroPantalla);
            float deltaAngulo = Mathf.DeltaAngle(anguloPantallaAnterior, anguloActual);

            if (Mathf.Abs(deltaAngulo) > 0.01f)
            {
                ruletaActiva.ModificarDesdeGiroVisual(deltaAngulo * sensibilidadGiro);
                anguloPantallaAnterior = anguloActual;
            }
        }
        else if (ruletaGasoilActiva != null && Input.GetMouseButton(0))
        {
            float anguloActual = CalcularAnguloCircular(ruletaGasoilActiva.transform, centroPantalla);
            float deltaAngulo = Mathf.DeltaAngle(anguloPantallaAnterior, anguloActual);

            if (Mathf.Abs(deltaAngulo) > 0.01f)
            {
                ruletaGasoilActiva.ModificarDesdeGiroVisual(deltaAngulo * sensibilidadGiro);
                anguloPantallaAnterior = anguloActual;
            }
        }
        else if (palancaActiva != null && Input.GetMouseButton(0))
        {
            // Usamos Input.GetAxis("Mouse Y") para detectar el movimiento de la mirada.
            // Al mirar hacia abajo, Mouse Y es negativo, así que lo restamos para que sume al arrastre.
            desplazamientoYAcumulado -= Input.GetAxis("Mouse Y") * sensibilidadArrastre;

            // Limitamos a que no pueda empujar hacia arriba de la posición original
            desplazamientoYAcumulado = Mathf.Max(0f, desplazamientoYAcumulado);

            palancaActiva.ActualizarArrastre(desplazamientoYAcumulado);
        }
    }

    // El sprite y el tamaño se deciden cada frame solo por 'mirandoInteractuable',
    // sin depender del sprite actual: así nunca se queda atascada aunque otro script lo toque.
    void ActualizarMirilla(bool mirandoInteractuable)
    {
        if (imagenMirilla == null)
            return;

        GuardarEscalaOriginalMirilla();

        if (spriteAplicado != null && imagenMirilla.sprite != spriteAplicado && !avisoSpriteExternoMostrado)
        {
            avisoSpriteExternoMostrado = true;
            Debug.LogWarning("[GestorRuletasFPS] Otro script o Animator está cambiando el sprite de '" + imagenMirilla.name +
                             "' (ahora es '" + (imagenMirilla.sprite != null ? imagenMirilla.sprite.name : "ninguno") + "').", imagenMirilla);
        }

        Sprite spriteDeseado = mirandoInteractuable ? mirillaInteractiva : mirillaPorDefecto;

        if (spriteDeseado != null && imagenMirilla.sprite != spriteDeseado)
            imagenMirilla.sprite = spriteDeseado;

        spriteAplicado = imagenMirilla.sprite;

        Vector3 escalaObjetivo = mirandoInteractuable
            ? escalaOriginalMirilla * multiplicadorTamano
            : escalaOriginalMirilla;

        imagenMirilla.rectTransform.localScale = Vector3.Lerp(
            imagenMirilla.rectTransform.localScale,
            escalaObjetivo,
            Time.deltaTime * velocidadPop
        );
    }

    // Una palanca bloqueada (ej: la de la intro durante el vuelo) se ignora por completo.
    bool PalancaUsable(PalancaTirador palanca)
    {
        return palanca != null && !palanca.EstaBloqueada;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    void DebugRayo(Ray rayo, bool impacto, RaycastHit hit)
    {
        Vector3 fin = impacto ? hit.point : rayo.origin + rayo.direction * distanciaInteraccion;
        Debug.DrawLine(rayo.origin, fin, impacto ? Color.green : Color.red);

        Collider actual = impacto ? hit.collider : null;

        // Si no toca nada en nuestras capas, miramos qué tocaría sin filtro para saber si es un problema de capa.
        Collider sinFiltro = null;
        RaycastHit hitSinFiltro = default;

        if (!impacto && Physics.Raycast(rayo, out hitSinFiltro, distanciaInteraccion * 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            sinFiltro = hitSinFiltro.collider;

        if (debugInicializado && actual == ultimoColliderDebug && sinFiltro == ultimoColliderSinFiltroDebug)
            return;

        debugInicializado = true;
        ultimoColliderDebug = actual;
        ultimoColliderSinFiltroDebug = sinFiltro;

        if (actual == null)
        {
            if (sinFiltro == null)
            {
                Debug.Log("[GestorRuletasFPS] El rayo no toca nada, ni en las capas detectables ni ignorando capas (hasta " +
                          (distanciaInteraccion * 3f).ToString("0.0") + " m).");
            }
            else
            {
                Debug.Log("[GestorRuletasFPS] El rayo no toca nada en las capas detectables (" + NombresCapas(capasDetectables) +
                          "). Ignorando capas tocaría '" + sinFiltro.name + "' (capa " + LayerMask.LayerToName(sinFiltro.gameObject.layer) +
                          ", " + hitSinFiltro.distance.ToString("0.00") + " m)" + InfoPalanca(sinFiltro), sinFiltro);
            }

            return;
        }

        Debug.Log("[GestorRuletasFPS] El rayo toca '" + actual.name + "' (capa " + LayerMask.LayerToName(actual.gameObject.layer) +
                  ", distancia " + hit.distance.ToString("0.00") + ")" + InfoPalanca(actual), actual);
    }

    // Informe completo cada 'intervaloInformeDebug' segundos: qué cámara pinta la pantalla,
    // desde dónde sale el rayo y, para cada palanca, si el rayo pasaría por sus colliders.
    void InformeDebugPeriodico(Ray rayo)
    {
        if (Time.unscaledTime < siguienteInformeDebug)
            return;

        siguienteInformeDebug = Time.unscaledTime + Mathf.Max(0.1f, intervaloInformeDebug);

        StringBuilder informe = new StringBuilder();
        informe.AppendLine("[GestorRuletasFPS] INFORME");

        // 1. Cámaras que pintan en pantalla (sin RenderTexture). La de mayor depth es la que ves encima.
        informe.Append("  Cámaras en pantalla: ");
        Camera camaraEncima = null;

        foreach (Camera camara in Camera.allCameras)
        {
            if (camara.targetTexture != null)
                continue;

            informe.Append("'" + camara.name + "' (depth " + camara.depth + ") ");

            if (camaraEncima == null || camara.depth >= camaraEncima.depth)
                camaraEncima = camara;
        }

        informe.AppendLine();

        if (camaraEncima != null && camaraEncima != camaraPrincipal)
        {
            informe.AppendLine("  ¡OJO! La cámara que ves encima es '" + camaraEncima.name + "', pero el rayo sale de '" +
                               camaraPrincipal.name + "'. El rayo NO sale de donde estás mirando.");
        }

        // 2. Origen del rayo.
        informe.AppendLine("  Rayo desde '" + camaraPrincipal.name + "' en " + rayo.origin.ToString("F2") +
                           " mirando " + rayo.direction.ToString("F2"));

        // 3. Palancas de la escena.
        PalancaTirador[] palancas = FindObjectsByType<PalancaTirador>(FindObjectsSortMode.None);

        if (palancas.Length == 0)
            informe.AppendLine("  No hay ninguna PalancaTirador activa en la escena.");

        foreach (PalancaTirador palanca in palancas)
        {
            Vector3 haciaPalanca = palanca.transform.position - rayo.origin;
            float angulo = Vector3.Angle(rayo.direction, haciaPalanca);

            informe.AppendLine("  Palanca '" + palanca.name + "' | " + (palanca.EstaBloqueada ? "BLOQUEADA" : "usable") +
                               " | a " + haciaPalanca.magnitude.ToString("0.00") + " m | " + angulo.ToString("0") + "º del centro de la mirilla");

            Collider[] colliders = palanca.GetComponentsInChildren<Collider>(true);

            if (colliders.Length == 0)
                informe.AppendLine("    -> NO TIENE NINGÚN COLLIDER (ni en ella ni en sus hijos).");

            foreach (Collider collider in colliders)
            {
                bool capaDetectable = (capasDetectables.value & (1 << collider.gameObject.layer)) != 0;
                bool atravesaria = collider.enabled && collider.gameObject.activeInHierarchy &&
                                   collider.Raycast(rayo, out RaycastHit hitAislado, distanciaInteraccion * 3f);

                informe.AppendLine("    Collider '" + collider.name + "' (" + collider.GetType().Name + ")" +
                                   " | activo: " + (collider.enabled && collider.gameObject.activeInHierarchy ? "sí" : "NO") +
                                   " | trigger: " + (collider.isTrigger ? "SÍ" : "no") +
                                   " | capa: " + LayerMask.LayerToName(collider.gameObject.layer) + (capaDetectable ? " (detectable)" : " (NO detectable)") +
                                   " | el rayo lo atravesaría ahora mismo: " + (atravesaria ? "SÍ" : "no"));
            }
        }

        Debug.Log(informe.ToString(), this);
    }

    static string InfoPalanca(Collider collider)
    {
        PalancaTirador palanca = collider.GetComponentInParent<PalancaTirador>();

        if (palanca == null)
            return "";

        return palanca.EstaBloqueada ? " -> PALANCA (bloqueada)" : " -> PALANCA (usable)";
    }

    static string NombresCapas(LayerMask mascara)
    {
        List<string> nombres = new List<string>();

        for (int i = 0; i < 32; i++)
        {
            if ((mascara.value & (1 << i)) != 0)
                nombres.Add(LayerMask.LayerToName(i));
        }

        return nombres.Count > 0 ? string.Join(", ", nombres) : "NINGUNA";
    }

    float CalcularAnguloCircular(Transform transformRuleta, Vector2 mirilla)
    {
        Vector3 centroRuletaEnPantalla = camaraPrincipal.WorldToScreenPoint(transformRuleta.position);
        Vector2 direccion = mirilla - new Vector2(centroRuletaEnPantalla.x, centroRuletaEnPantalla.y);
        return Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
    }
}