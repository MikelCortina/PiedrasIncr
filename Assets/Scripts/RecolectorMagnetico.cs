using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RecolectorMagnetico : MonoBehaviour
{
    [Header("Ajustes del Imán")]
    public float radioAtraccion = 5f;
    public float velocidadAtraccion = 15f;
    public float distanciaRecoleccion = 1.5f;

    [Header("Recogida manual - Clic izquierdo")]
    [Tooltip("Anchura aproximada de la mira para facilitar seleccionar monedas pequeñas")]
    [Min(0.01f)] public float radioApuntadoMoneda = 0.12f;

    [Header("Desbloqueo del Imán")]
    [SerializeField] private bool imanDesbloqueado = false;
    public bool ImanDesbloqueado => imanDesbloqueado;

    [Header("Mejora - Alcance del Imán")]
    [SerializeField, Range(1, 3)] private int nivelAlcance = 1;
    public float alcanceNivel1 = 5f;
    public float alcanceNivel2 = 8f;
    public float alcanceNivel3 = 12f;
    public int NivelAlcance => nivelAlcance;

    [Header("Protección de Creación")]
    [Tooltip("Tiempo mínimo desde que se crea la moneda para ser atraída por el imán")]
    public float tiempoInmunidadCreacion = 1.5f;

    [Header("Torque Magnético")]
    public float velocidadRotacionVuelo = 720f;

    [Header("Filtros")]
    public LayerMask capaRecolectables;
    public string tagRecolectable = "Recolectable";

    [Header("Sonido (Opcional)")]
    public AudioClip sonidoRecoger;
    [Range(0f, 1f)] public float volumenSonido = 1f;

    [Header("Seguro de Sonido")]
    public float cooldownSonido = 0.05f;

    private float ultimoTiempoSonido = 0f;
    private Cartera cartera;
    private AudioSource audioSource;
    private GestorEquipamiento gestorEquipamiento;
    private readonly List<GameObject> objetosAtrayendo = new List<GameObject>();

    // Si ambos componentes consultan el mismo clic, la moneda se cobra una sola vez
    // y SistemaPatada recibe el mismo resultado independientemente del orden de Update.
    private int ultimoFrameClick = -1;
    private bool monedaRecogidaUltimoClick = false;

    private void OnValidate()
    {
        nivelAlcance = Mathf.Clamp(nivelAlcance, 1, 3);
        AplicarNivelAlcance();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        cartera = GetComponent<Cartera>();
        gestorEquipamiento = GetComponent<GestorEquipamiento>();
        if (gestorEquipamiento == null)
            gestorEquipamiento = FindObjectOfType<GestorEquipamiento>();
        AplicarNivelAlcance();
    }

    private void Update()
    {
        // Nuevo: recogida manual de una moneda apuntada, con o sin imán.
        // Una herramienta equipada conserva el uso normal de clic izquierdo.
        if (Input.GetMouseButtonDown(0))
            IntentarRecogerMonedaConClick();

        if (!imanDesbloqueado)
        {
            // Conserva el comportamiento automático anterior para OTROS
            // recolectables; las monedas ahora requieren clic izquierdo.
            RecogerOtrosRecolectablesEnContacto();
            return;
        }

        // Todo el funcionamiento original del imán permanece igual.
        Collider[] objetosCercanos = Physics.OverlapSphere(
            transform.position, radioAtraccion, capaRecolectables);

        foreach (Collider col in objetosCercanos)
        {
            GameObject objetoReal = col.attachedRigidbody != null
                ? col.attachedRigidbody.gameObject
                : col.gameObject;

            if (objetoReal.CompareTag(tagRecolectable) &&
                !objetosAtrayendo.Contains(objetoReal))
            {
                EdadMoneda edad = objetoReal.GetComponent<EdadMoneda>();
                if (edad != null && !edad.HaSuperadoInmunidad(tiempoInmunidadCreacion))
                    continue;

                PrepararObjetoParaVolar(objetoReal);
            }
        }

        for (int i = objetosAtrayendo.Count - 1; i >= 0; i--)
        {
            GameObject obj = objetosAtrayendo[i];
            if (obj == null)
            {
                objetosAtrayendo.RemoveAt(i);
                continue;
            }

            obj.transform.position = Vector3.MoveTowards(
                obj.transform.position, transform.position,
                velocidadAtraccion * Time.deltaTime);

            Vector3 direccionVuelo = (transform.position - obj.transform.position).normalized;
            Vector3 ejeGiro = Vector3.Cross(Vector3.up, direccionVuelo) +
                              new Vector3(0.1f, 0.3f, 0.1f);

            obj.transform.Rotate(ejeGiro.normalized * velocidadRotacionVuelo *
                                 Time.deltaTime, Space.World);

            if (Vector3.Distance(transform.position, obj.transform.position) <=
                distanciaRecoleccion)
            {
                objetosAtrayendo.RemoveAt(i);
                Recoger(obj);
            }
        }
    }

    // SistemaPatada también lo llama: el resultado se memoriza durante el frame
    // para que no haya doble cobro ni se dispare una patada al recoger la moneda.
    public bool IntentarRecogerMonedaConClick()
    {
        if (ultimoFrameClick == Time.frameCount)
            return monedaRecogidaUltimoClick;

        ultimoFrameClick = Time.frameCount;
        monedaRecogidaUltimoClick = false;

        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return false;

        if (cartera == null || Camera.main == null)
            return false;

        Camera cam = Camera.main;
        // Lanzamos el rayo un poco más lejos para asegurar, luego filtramos por distancia
        float distanciaRayo = Mathf.Max(2f, distanciaRecoleccion + 2f);
        Ray rayo = new Ray(cam.transform.position, cam.transform.forward);

        RaycastHit[] impactos = Physics.SphereCastAll(
            rayo, radioApuntadoMoneda, distanciaRayo,
            ~0, QueryTriggerInteraction.Collide);

        // Ordenamos los impactos del más cercano al más lejano
        Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit impacto in impactos)
        {
            Collider col = impacto.collider;
            if (col == null) continue;

            // No bloquear la mira con los propios colliders del jugador.
            if (col.transform.IsChildOf(transform)) continue;

            Moneda moneda = col.GetComponentInParent<Moneda>();
            if (moneda == null && col.attachedRigidbody != null)
                moneda = col.attachedRigidbody.GetComponentInChildren<Moneda>();

            // LA SOLUCIÓN ESTÁ AQUÍ: 
            // Si el rayo choca con el suelo/pared, lo ignoramos (continue) en lugar de abortar (return false)
            if (moneda == null) continue;

            // Medimos la distancia real desde la cámara hasta la moneda
            if (impacto.distance > distanciaRecoleccion)
                continue;

            GameObject objetoMoneda = moneda.gameObject;
            monedaRecogidaUltimoClick = true;
            Recoger(objetoMoneda);
            return true;
        }

        return false;
    }

    private void RecogerOtrosRecolectablesEnContacto()
    {
        Collider[] objetosCercanos = Physics.OverlapSphere(
            transform.position, distanciaRecoleccion,
            capaRecolectables);

        HashSet<GameObject> procesados = new HashSet<GameObject>();
        foreach (Collider col in objetosCercanos)
        {
            GameObject objetoReal = col.attachedRigidbody != null
                ? col.attachedRigidbody.gameObject
                : col.gameObject;
            if (!objetoReal.CompareTag(tagRecolectable)) continue;

            // Las monedas se recogen por clic; el resto sigue automático.
            if (objetoReal.GetComponentInChildren<Moneda>() != null ||
                objetoReal.GetComponentInParent<Moneda>() != null)
                continue;

            if (procesados.Add(objetoReal))
                Recoger(objetoReal);
        }
    }

    private void PrepararObjetoParaVolar(GameObject objeto)
    {
        objetosAtrayendo.Add(objeto);
        Rigidbody rb = objeto.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider[] colliders = objeto.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;
    }

    private void Recoger(GameObject objeto)
    {
        if (objeto == null || !objeto.activeInHierarchy) return;

        Moneda moneda = objeto.GetComponent<Moneda>();
        if (moneda == null) moneda = objeto.GetComponentInChildren<Moneda>();
        if (moneda != null && cartera != null)
            cartera.AnadirMonedas(moneda.valor);

        if (sonidoRecoger != null &&
            Time.time >= ultimoTiempoSonido + cooldownSonido)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.90f, 1.10f);
            audioSource.PlayOneShot(sonidoRecoger, volumenSonido);
            ultimoTiempoSonido = Time.time;
        }

        // Iniciamos la corrutina de salto y voltereta. 
        // Como el objeto se va a destruir, le pasamos la responsabilidad a un componente temporal o corrutina.
        StartCoroutine(AnimarVolteretaYDestruir(objeto));
    }

    private System.Collections.IEnumerator AnimarVolteretaYDestruir(GameObject obj)
    {
        // 1. Desactivar colliders y físicas para que no interfiera más
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;

        // 2. Parámetros de la animación
        float duracion = 0.4f; // 400ms de saltito
        float tiempoPasado = 0f;

        Vector3 posInicial = obj.transform.position;
        Vector3 posObjetivo = posInicial + Vector3.up * 1.5f; // Salto hacia arriba
        Vector3 escalaInicial = obj.transform.localScale;
        Quaternion rotacionInicial = obj.transform.rotation;

        // 3. Cálculo del giro aleatorio: entre 180 y 360, y luego 50% de probabilidad de ser negativo
        float giroX = UnityEngine.Random.Range(180f, 360f);
        if (UnityEngine.Random.value > 0.5f)
        {
            giroX = -giroX;
        }

        while (tiempoPasado < duracion)
        {
            if (obj == null) yield break; // Por si se destruye por otro lado

            tiempoPasado += Time.deltaTime;
            float t = tiempoPasado / duracion;

            // Curva de salto tipo parábola (fácil con Seno)
            float alturaSeno = Mathf.Sin(t * Mathf.PI);

            // Interpolar posición (sube y baja un poquito mientras encoge)
            obj.transform.position = Vector3.Lerp(posInicial, posObjetivo, t) + (Vector3.up * alturaSeno * 0.5f);

            // Interpolar rotación forzando las vueltas completas (sin que Unity tome el camino corto)
            float anguloActual = Mathf.Lerp(0f, giroX, t);
            obj.transform.rotation = rotacionInicial * Quaternion.Euler(anguloActual, 0f, 0f);

            // Interpolar escala a cero
            obj.transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t);

            yield return null;
        }

        if (obj != null)
        {
            obj.SetActive(false);
            Destroy(obj);
        }
    }

    public bool MejorarAlcance()
    {
        if (nivelAlcance >= 3) return false;
        nivelAlcance++;
        AplicarNivelAlcance();
        Debug.Log("Imán mejorado a nivel " + nivelAlcance +
                  " | Radio: " + radioAtraccion);
        return true;
    }

    private void AplicarNivelAlcance()
    {
        switch (nivelAlcance)
        {
            case 1: radioAtraccion = alcanceNivel1; break;
            case 2: radioAtraccion = alcanceNivel2; break;
            case 3: radioAtraccion = alcanceNivel3; break;
        }
    }

    public bool AlcanceAlMaximo() => nivelAlcance >= 3;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioAtraccion);
    }

    public void DesbloquearIman()
    {
        imanDesbloqueado = true;
        Debug.Log("¡Imán desbloqueado!");
    }

    public bool EstaImanDesbloqueado() => imanDesbloqueado;
}