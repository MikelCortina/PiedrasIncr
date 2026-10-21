using UnityEngine;
using UnityEngine.UI;

public class GestorRuletasFPS : MonoBehaviour
{
    public Camera camaraPrincipal;

    [Tooltip("Selecciona AQUÍ la capa de Ruletas/Botones Y TAMBIÉN las capas de los edificios/suelo (Default, Edificios, etc.)")]
    public LayerMask capasDetectables;

    public float distanciaInteraccion = 3f;
    public float sensibilidadGiro = 1f;

    [Header("UI Mirilla")]
    public Image imagenMirilla;
    public Sprite mirillaPorDefecto;
    public Sprite mirillaInteractiva;

    [Header("Animación de Mirilla")]
    public float multiplicadorTamano = 1.5f;
    public float velocidadPop = 15f;

    private Vector3 escalaOriginalMirilla;
    private Vector3 escalaObjetivoMirilla;

    private RuletaInteractiva ruletaActiva = null;
    private RuletaInteractivaGasoil ruletaGasoilActiva = null; // NUEVA VARIABLE
    private float anguloPantallaAnterior;

    void Start()
    {
        if (imagenMirilla != null)
        {
            escalaOriginalMirilla = imagenMirilla.rectTransform.localScale;
            escalaObjetivoMirilla = escalaOriginalMirilla;
        }
    }

    void Update()
    {
        Vector2 centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray rayo = camaraPrincipal.ScreenPointToRay(centroPantalla);

        bool impacto = Physics.Raycast(rayo, out RaycastHit hit, distanciaInteraccion, capasDetectables);
        bool mirandoInteractuable = false;

        // =========================================================
        // 1. ¿DEBEMOS MOSTRAR LA MIRILLA INTERACTIVA?
        // =========================================================
        if (ruletaActiva != null || ruletaGasoilActiva != null)
        {
            // Si estamos agarrando CUALQUIER ruleta, forzamos el icono encendido
            mirandoInteractuable = true;
        }
        else if (impacto)
        {
            // Si no estamos agarrando nada, miramos a ver qué tenemos delante
            if (hit.collider.GetComponent<RuletaInteractiva>() != null ||
                hit.collider.GetComponent<RuletaInteractivaGasoil>() != null || // Añadido
                hit.collider.GetComponent<BotonTuberia>() != null ||
                hit.collider.GetComponentInParent<VentiladorPiedras>() != null)
            {
                mirandoInteractuable = true;
            }
        }

        // =========================================================
        // 2. SISTEMA DE UI Y ANIMACIÓN "POP"
        // =========================================================
        if (imagenMirilla != null)
        {
            if (mirandoInteractuable)
            {
                if (imagenMirilla.sprite != mirillaInteractiva)
                {
                    imagenMirilla.sprite = mirillaInteractiva;
                    escalaObjetivoMirilla = escalaOriginalMirilla * multiplicadorTamano;
                }
            }
            else
            {
                if (imagenMirilla.sprite != mirillaPorDefecto)
                {
                    imagenMirilla.sprite = mirillaPorDefecto;
                    escalaObjetivoMirilla = escalaOriginalMirilla;
                }
            }

            imagenMirilla.rectTransform.localScale = Vector3.Lerp(
                imagenMirilla.rectTransform.localScale,
                escalaObjetivoMirilla,
                Time.deltaTime * velocidadPop
            );
        }

        // =========================================================
        // 3. LÓGICA DE INTERACCIÓN (CLICS)
        // =========================================================
        if (Input.GetMouseButtonDown(0))
        {
            if (impacto)
            {
                // Intentamos coger la ruleta que hayamos mirado
                ruletaActiva = hit.collider.GetComponent<RuletaInteractiva>();
                ruletaGasoilActiva = hit.collider.GetComponent<RuletaInteractivaGasoil>();

                if (ruletaActiva != null)
                {
                    anguloPantallaAnterior = CalcularAnguloCircular(ruletaActiva.transform, centroPantalla);
                }
                else if (ruletaGasoilActiva != null)
                {
                    anguloPantallaAnterior = CalcularAnguloCircular(ruletaGasoilActiva.transform, centroPantalla);
                }
                else
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
            // Soltamos cualquier ruleta que estuviéramos agarrando
            ruletaActiva = null;
            ruletaGasoilActiva = null;
        }

        // =========================================================
        // 4. LÓGICA DE GIRO
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
    }

    // Usamos 'Transform' en lugar de la clase directa para que sirva para cualquier tipo de ruleta
    float CalcularAnguloCircular(Transform transformRuleta, Vector2 mirilla)
    {
        Vector3 centroRuletaEnPantalla = camaraPrincipal.WorldToScreenPoint(transformRuleta.position);
        Vector2 direccion = mirilla - new Vector2(centroRuletaEnPantalla.x, centroRuletaEnPantalla.y);
        return Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
    }
}