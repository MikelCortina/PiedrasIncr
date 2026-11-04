using UnityEngine;

/// <summary>
/// Rescate progresivo: el jugador asciende visiblemente desde el vacío,
/// se coloca junto al borde y permanece levitando hasta pulsar la tecla
/// de aterrizaje sobre una superficie transitable.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(MovimientoPersonaje))]
public class RescateVacioJugador : MonoBehaviour
{
    [Header("Detección del vacío")]
    [Tooltip("Profundidad bajo el último suelo seguro que activa el rescate.")]
    [Min(1f)] public float profundidadParaRescatar = 10f;

    [Header("Ascenso visible")]
    [Tooltip("Velocidad vertical aproximada en metros por segundo.")]
    [Min(0.1f)] public float velocidadAscenso = 8f;

    [Tooltip("Velocidad para acercarse al borde, después de subir.")]
    [Min(0.1f)] public float velocidadHorizontalRescate = 6f;

    [Tooltip("Altura de los pies sobre la altura del último suelo seguro.")]
    [Min(0.2f)] public float alturaLevitacion = 0.8f;

    [Header("Encontrar un lugar junto al borde")]
    [Min(0.5f)] public float distanciaMaximaBusqueda = 5f;
    [Min(0.1f)] public float pasoBusqueda = 0.35f;

    [Header("Volver al suelo voluntariamente")]
    [Tooltip("NO aterriza solo: pulsa esta tecla sobre terreno transitable.")]
    public KeyCode teclaAterrizar = KeyCode.G;

    [Tooltip("Distancia máxima entre los pies y el suelo cuando pulsas aterrizar.")]
    [Min(0.2f)] public float distanciaMaximaAterrizaje = 3f;

    [Header("Superficies válidas")]
    [Tooltip("Selecciona SOLO terreno/plataformas. Excluye Player, piedras y triggers.")]
    public LayerMask capasSuelo = ~0;
    public bool respetarPendienteMaxima = true;

    [Header("Depuración")]
    public bool mostrarGizmos = true;
    public bool mostrarMensajes = true;

    private enum Estado { Normal, Ascendiendo, VolviendoAlBorde, Levitando }
    private Estado estado = Estado.Normal;

    private CharacterController controller;
    private MovimientoPersonaje movimiento;

    private bool existeSueloSeguro;
    private bool estabaEnSuelo;
    private Vector3 ultimoPuntoSeguro;
    private Vector3 ultimaPosicionSegura;
    private Vector3 ultimaDireccionHorizontal = Vector3.forward;
    private Vector3 destinoRescate;

    private Vector3 inicioTramo;
    private Vector3 finTramo;
    private float progresoTramo;
    private float duracionTramo;

    public bool EstaLevitando => estado == Estado.Levitando;
    public bool RescateActivo => estado != Estado.Normal;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movimiento = GetComponent<MovimientoPersonaje>();
    }

    private void Update()
    {
        if (estado == Estado.Ascendiendo || estado == Estado.VolviendoAlBorde)
        {
            AvanzarAnimacion();
        }
        // Aterriza solo si el jugador lo solicita. Nunca automáticamente.
        else if (estado == Estado.Levitando && Input.GetKeyDown(teclaAterrizar))
        {
            IntentarAterrizar();
        }
    }

    // Tras MovimientoPersonaje.Update, ya conocemos el apoyo real del fotograma.
    private void LateUpdate()
    {
        if (estado != Estado.Normal || controller == null || movimiento == null ||
            !controller.enabled)
            return;

        bool sobreSuelo = movimiento.SueloDetectado &&
                          controller.isGrounded &&
                          EsPendienteValida(movimiento.NormalSueloDetectado) &&
                          EsCapaSueloValida(movimiento.ColliderSueloDetectado);

        if (sobreSuelo)
        {
            if (existeSueloSeguro)
            {
                Vector3 desplazamiento = transform.position - ultimaPosicionSegura;
                desplazamiento.y = 0f;
                if (desplazamiento.sqrMagnitude > 0.0004f)
                    ultimaDireccionHorizontal = desplazamiento.normalized;
            }

            ultimaPosicionSegura = transform.position;
            ultimoPuntoSeguro = movimiento.PuntoSueloDetectado;
            existeSueloSeguro = true;
        }
        else if (estabaEnSuelo && existeSueloSeguro)
        {
            Vector3 salida = transform.position - ultimaPosicionSegura;
            salida.y = 0f;
            if (salida.sqrMagnitude > 0.0004f)
                ultimaDireccionHorizontal = salida.normalized;
        }

        estabaEnSuelo = sobreSuelo;

        if (existeSueloSeguro &&
            transform.position.y < ultimoPuntoSeguro.y - profundidadParaRescatar)
        {
            IniciarRescate();
        }
    }


    /// <summary>
    /// Borra las referencias al suelo anterior y cualquier fase de rescate,
    /// para que LateUpdate reconozca el nuevo suelo despues de una carga.
    /// El adaptador de guardado controla el CharacterController y el Transform.
    /// </summary>
    public void RestablecerEstadoTrasCarga()
    {
        estado = Estado.Normal;
        existeSueloSeguro = false;
        estabaEnSuelo = false;
        ultimoPuntoSeguro = Vector3.zero;
        ultimaPosicionSegura = Vector3.zero;
        ultimaDireccionHorizontal = Vector3.forward;
        destinoRescate = Vector3.zero;
        inicioTramo = Vector3.zero;
        finTramo = Vector3.zero;
        progresoTramo = 0f;
        duracionTramo = 0f;
    }

    private bool EsCapaSueloValida(Collider col)
    {
        return col != null && (capasSuelo.value & (1 << col.gameObject.layer)) != 0;
    }

    private bool EsPendienteValida(Vector3 normal)
    {
        if (!respetarPendienteMaxima) return true;
        return Vector3.Angle(normal, Vector3.up) <= controller.slopeLimit;
    }

    private void IniciarRescate()
    {
        Vector3 direccion = transform.position - ultimaPosicionSegura;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.05f)
            direccion = ultimaDireccionHorizontal;
        if (direccion.sqrMagnitude < 0.001f)
            direccion = transform.forward;

        direccion.y = 0f;
        direccion.Normalize();

        // Busca un punto en el vacío justo al lado del borde por donde cayó.
        // Si no hay borde cercano, utiliza la última posición segura.
        Vector3 puntoHorizontal = ultimaPosicionSegura;
        for (float d = Mathf.Max(0.1f, controller.radius);
             d <= distanciaMaximaBusqueda; d += pasoBusqueda)
        {
            Vector3 candidato = ultimaPosicionSegura + direccion * d;
            if (!TieneSueloALaAlturaSegura(candidato))
            {
                puntoHorizontal = candidato;
                break;
            }
        }

        float escalaY = Mathf.Abs(transform.lossyScale.y);
        float desplazamientoRaizSobrePies =
            (controller.height * 0.5f - controller.center.y) * escalaY;

        destinoRescate = new Vector3(
            puntoHorizontal.x,
            ultimoPuntoSeguro.y + alturaLevitacion + desplazamientoRaizSobrePies,
            puntoHorizontal.z
        );

        // El CharacterController no debe aplicar gravedad ni bloquear el ascenso.
        // Se desactiva SOLO durante la animación y se reactiva al terminar.
        movimiento.ComenzarAscensoRescate();
        controller.enabled = false;

        // Primero subimos desde el punto de caída (sin teletransportar).
        // Si la meta está por debajo del jugador, no lo hacemos descender.
        Vector3 metaVertical = new Vector3(
            transform.position.x,
            Mathf.Max(transform.position.y, destinoRescate.y),
            transform.position.z
        );

        estado = Estado.Ascendiendo;
        IniciarTramo(metaVertical, velocidadAscenso);

        if (mostrarMensajes)
            Debug.Log("Rescate: ascendiendo desde el vacío...", this);
    }

    private void IniciarTramo(Vector3 destino, float velocidad)
    {
        inicioTramo = transform.position;
        finTramo = destino;
        progresoTramo = 0f;
        duracionTramo = Mathf.Max(0.01f,
            Vector3.Distance(inicioTramo, finTramo) / Mathf.Max(0.1f, velocidad));
    }

    private void AvanzarAnimacion()
    {
        progresoTramo += Time.deltaTime / duracionTramo;
        float t = Mathf.Clamp01(progresoTramo);
        // Suaviza el inicio y la llegada, manteniendo un ascenso visible.
        float tSuave = t * t * (3f - 2f * t);
        transform.position = Vector3.LerpUnclamped(inicioTramo, finTramo, tSuave);

        if (t < 1f) return;

        transform.position = finTramo;

        if (estado == Estado.Ascendiendo)
        {
            estado = Estado.VolviendoAlBorde;
            // Moverse horizontalmente hasta la zona del borde.
            // Si está ya cerca, esta etapa acabará casi inmediatamente.
            IniciarTramo(new Vector3(destinoRescate.x, transform.position.y,
                destinoRescate.z), velocidadHorizontalRescate);
        }
        else if (estado == Estado.VolviendoAlBorde)
        {
            // Restauramos colisiones antes de permitir el movimiento normal de vuelo.
            if (controller != null)
                controller.enabled = true;

            movimiento.ComenzarLevitacionRescate();
            estado = Estado.Levitando;

            if (mostrarMensajes)
                Debug.Log("Rescate: flotando. Vuelve sobre el terreno y pulsa " +
                    teclaAterrizar + " para aterrizar.", this);
        }
    }

    private bool TieneSueloALaAlturaSegura(Vector3 candidato)
    {
        Vector3 origen = new Vector3(candidato.x, ultimoPuntoSeguro.y + 1.25f,
            candidato.z);

        if (!Physics.Raycast(origen, Vector3.down, out RaycastHit hit,
                2f, capasSuelo, QueryTriggerInteraction.Ignore))
            return false;

        return EsPendienteValida(hit.normal);
    }

    private void IntentarAterrizar()
    {
        if (!HayTerrenoDebajo())
        {
            if (mostrarMensajes)
                Debug.Log("No hay suelo seguro cerca. Acércate al mapa y pulsa " +
                    teclaAterrizar + " otra vez.", this);
            return;
        }

        // La siguiente actualización de MovimientoPersonaje vuelve a aplicar
        // la gravedad: caída natural desde la altura de levitación.
        movimiento.TerminarLevitacionRescate();
        estado = Estado.Normal;
        estabaEnSuelo = false;

        if (mostrarMensajes)
            Debug.Log("Rescate: aterrizaje solicitado; gravedad restaurada.", this);
    }

    private bool HayTerrenoDebajo()
    {
        float escalaY = Mathf.Abs(transform.lossyScale.y);
        float piesY = transform.position.y +
            (controller.center.y - controller.height * 0.5f) * escalaY;

        Vector3 origen = new Vector3(transform.position.x, piesY + 0.1f,
            transform.position.z);

        if (!Physics.Raycast(origen, Vector3.down, out RaycastHit hit,
                distanciaMaximaAterrizaje + 0.1f, capasSuelo,
                QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider.transform == transform ||
            hit.collider.transform.IsChildOf(transform))
            return false;

        return EsPendienteValida(hit.normal);
    }

    private void OnDisable()
    {
        // Si se desactiva este componente a mitad del ascenso, no dejes
        // al jugador con el CharacterController deshabilitado.
        if (controller != null && !controller.enabled)
            controller.enabled = true;

        if (movimiento != null && estado != Estado.Normal)
            movimiento.TerminarLevitacionRescate();

        estado = Estado.Normal;
    }

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos || !Application.isPlaying || !existeSueloSeguro)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(ultimoPuntoSeguro, 0.25f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(destinoRescate, 0.35f);
        Gizmos.DrawLine(ultimoPuntoSeguro, destinoRescate);
    }
}
