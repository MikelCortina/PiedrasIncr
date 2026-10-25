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

    private Vector3 escalaOriginalMirilla;
    private Vector3 escalaObjetivoMirilla;

    private RuletaInteractiva ruletaActiva = null;
    private RuletaInteractivaGasoil ruletaGasoilActiva = null;
    private float anguloPantallaAnterior;

    // --- NUEVAS VARIABLES PARA LA PALANCA ---
    private PalancaTirador palancaActiva = null;
    private float desplazamientoYAcumulado = 0f;

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
                hit.collider.GetComponent<PalancaTirador>() != null)
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
                ruletaActiva = hit.collider.GetComponent<RuletaInteractiva>();
                ruletaGasoilActiva = hit.collider.GetComponent<RuletaInteractivaGasoil>();
                palancaActiva = hit.collider.GetComponent<PalancaTirador>();

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

    float CalcularAnguloCircular(Transform transformRuleta, Vector2 mirilla)
    {
        Vector3 centroRuletaEnPantalla = camaraPrincipal.WorldToScreenPoint(transformRuleta.position);
        Vector2 direccion = mirilla - new Vector2(centroRuletaEnPantalla.x, centroRuletaEnPantalla.y);
        return Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
    }
}