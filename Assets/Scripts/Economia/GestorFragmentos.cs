using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class GestorFragmentos : MonoBehaviour
{
    [Header("Fragmento")]
    [Tooltip("Prefab opcional (Rigidbody + Collider). Si está vacío se crea un cubito brillante.")]
    public GameObject prefabFragmento;
    [Min(0.01f)] public float tamanoFragmento = 0.12f;
    public Color colorFragmento = new Color(0.75f, 0.9f, 1f);
    public Color colorMonedaBonus = new Color(1f, 0.8f, 0.2f);
    [Min(1)] public int maximoVivos = 150;
    [Tooltip("Segundos hasta que un fragmento no recogido desaparece.")]
    [Min(1f)] public float vidaFragmento = 25f;
    [Min(0f)] public float parpadeoFinal = 4f;

    [Header("Salida")]
    public float impulsoArriba = 4f;
    public float impulsoLateral = 1.5f;
    [Min(0f)] public float inmunidadRecogida = 0.6f;

    [Header("Recogida a mano")]
    [Tooltip("El jugador recoge los fragmentos al pasar a esta distancia.")]
    [Tooltip("Imán automático: los restos vuelan solos hacia el jugador al pasar cerca. Desactivado: se recogen con clic izquierdo.")]
    public bool imanAutomatico = false;
    [Min(0.1f)] public float radioRecogidaMano = 1.6f;
    [Min(0f)] public float velocidadAtraccionMano = 12f;

    [Header("Sonido")]
    public AudioClip sonidoClink;
    public AudioClip sonidoEsferaPerfecta;
    [Range(0f, 1f)] public float volumenClink = 0.5f;
    [Min(0f)] public float ventanaCombo = 0.45f;
    [Min(0f)] public float subidaTonoPorCombo = 0.06f;
    [Min(1f)] public float tonoMaximo = 2f;

    [Header("Brillo de esfera perfecta")]
    public Color colorBrillo = new Color(1f, 0.85f, 0.4f);
    [Min(0f)] public float intensidadBrillo = 6f;
    [Min(0.05f)] public float duracionBrillo = 1.2f;

    [Header("Recogida con clic izquierdo")]
    [Tooltip("Cada resto recogido (con clic o con el Fusil) da una moneda entera, en vez del % de la hoja ECONOMIA.")]
    public bool unaMonedaPorFragmento = true;
    [Tooltip("Distancia máxima desde la cámara para coger un resto con clic.")]
    [Min(0.5f)] public float distanciaClic = 3f;
    [Tooltip("Tolerancia de puntería: lo lejos que puede estar el resto de la línea de la mirilla.")]
    [Min(0.05f)] public float toleranciaApuntado = 0.35f;
    [Tooltip("Al coger uno, también se cogen los que estén pegados a él (a esta distancia).")]
    [Min(0f)] public float radioGrupoClic = 0.5f;

    public static event Action<int> OnMonedasPorFragmentos;
    public static event Action<Vector3> OnEsferaPerfectaCelebrada;

    public int FragmentosVivos { get { return vivos.Count; } }
    public int FragmentosRecogidos { get; private set; }
    public int MonedasGeneradas { get; private set; }

    private static GestorFragmentos instancia;

    public static GestorFragmentos Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = FindAnyObjectByType<GestorFragmentos>();
                if (instancia == null) instancia = new GameObject("GestorFragmentos").AddComponent<GestorFragmentos>();
            }
            return instancia;
        }
    }

    private class Vivo
    {
        public GameObject objeto;
        public Rigidbody rb;
        public Renderer render;
        public float nacimiento;
        public bool bonus;
        public bool atraido;
        public float tasaAtraccion;
    }

    private readonly List<Vivo> vivos = new List<Vivo>();
    private readonly Stack<Vivo> libres = new Stack<Vivo>();
    private MaterialPropertyBlock bloque;
    private AudioSource audioSource;
    private Transform jugador;
    private Cartera cartera;
    private float acumulador;
    private int combo;
    private float ultimoClink = -999f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        instancia = null;
        OnMonedasPorFragmentos = null;
        OnEsferaPerfectaCelebrada = null;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(this);
            return;
        }
        instancia = this;
        bloque = new MaterialPropertyBlock();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    /// <summary>
    /// Guardado (F9): monedas que valen los fragmentos que siguen en el suelo, igual que si se recogieran
    /// (enteros con "Una Moneda Por Fragmento"; si no, a la tasa de recogida a mano).
    /// </summary>
    public int MonedasPendientesEnSuelo()
    {
        EconomiaDemo economia = ProveedorEconomia.Actual;
        int bonus = 0;
        float normales = acumulador;
        for (int i = 0; i < vivos.Count; i++)
        {
            Vivo vivo = vivos[i];
            if (vivo == null || vivo.objeto == null || !vivo.objeto.activeInHierarchy) continue;
            // Igual que Recoger: el bonus vale 1 moneda; los normales pasan por el acumulador.
            if (vivo.bonus) bonus++;
            else normales += (unaMonedaPorFragmento ? 1f : Mathf.Clamp01(economia.recogidaAMano)) * economia.monedasPorFragmento;
        }
        return bonus + Mathf.Max(0, Mathf.FloorToInt(normales + 0.0001f));
    }

    /// <summary>Guardado (F9): retira todos los fragmentos del suelo sin pagarlos (al cargar partida).</summary>
    public void RetirarTodos()
    {
        for (int i = vivos.Count - 1; i >= 0; i--) Devolver(i);
        acumulador = 0f;
        combo = 0;
    }

    public void Soltar(Vector3 posicion, int cantidad, bool bonus)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Vivo vivo = Obtener();
            vivo.bonus = bonus;
            vivo.atraido = false;
            vivo.nacimiento = Time.time;
            Transform t = vivo.objeto.transform;
            t.position = posicion + Vector3.up * 0.4f + UnityEngine.Random.insideUnitSphere * 0.2f;
            t.rotation = UnityEngine.Random.rotation;
            float escala = bonus ? tamanoFragmento * 1.3f : tamanoFragmento;
            t.localScale = Vector3.one * escala;
            Pintar(vivo, bonus ? colorMonedaBonus : colorFragmento, 1f);
            vivo.objeto.SetActive(true);
            if (vivo.rb != null)
            {
                vivo.rb.isKinematic = false;
                Vector2 lateral = UnityEngine.Random.insideUnitCircle * impulsoLateral;
                vivo.rb.AddForce(new Vector3(lateral.x, impulsoArriba * UnityEngine.Random.Range(0.8f, 1.2f), lateral.y), ForceMode.VelocityChange);
                vivo.rb.AddTorque(UnityEngine.Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
            }
            vivos.Add(vivo);
        }
    }

    public void CelebrarEsferaPerfecta(Vector3 posicion, int monedasBonus)
    {
        StartCoroutine(LluviaBonus(posicion, monedasBonus));
        StartCoroutine(Brillo(posicion));
        if (sonidoEsferaPerfecta != null) AudioSource.PlayClipAtPoint(sonidoEsferaPerfecta, posicion, 1f);
        if (OnEsferaPerfectaCelebrada != null) OnEsferaPerfectaCelebrada(posicion);
    }

    private IEnumerator LluviaBonus(Vector3 posicion, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Soltar(posicion + Vector3.up * 0.5f, 1, true);
            yield return new WaitForSeconds(0.06f);
        }
    }

    private IEnumerator Brillo(Vector3 posicion)
    {
        GameObject objeto = new GameObject("BrilloEsferaPerfecta");
        objeto.transform.position = posicion + Vector3.up * 0.5f;
        Light luz = objeto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorBrillo;
        luz.range = 6f;
        float t = 0f;
        while (t < duracionBrillo)
        {
            t += Time.deltaTime;
            luz.intensity = intensidadBrillo * (1f - t / duracionBrillo);
            yield return null;
        }
        Destroy(objeto);
    }

    public int AspirarEnCono(Vector3 origen, Vector3 direccion, float distancia, float anguloGrados, float velocidad, float tasaRecogida, float distanciaRecogida)
    {
        int recogidos = 0;
        float distanciaCuadrada = distancia * distancia;
        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            Vivo vivo = vivos[i];
            if (Time.time - vivo.nacimiento < inmunidadRecogida) continue;
            Vector3 hacia = vivo.objeto.transform.position - origen;
            float d2 = hacia.sqrMagnitude;
            if (d2 > distanciaCuadrada) continue;
            if (d2 > 9f && Vector3.Angle(direccion, hacia) > anguloGrados) continue;

            vivo.atraido = true;
            vivo.tasaAtraccion = tasaRecogida;
            if (vivo.rb != null) vivo.rb.isKinematic = true;
            vivo.objeto.transform.position = Vector3.MoveTowards(vivo.objeto.transform.position, origen, velocidad * Time.deltaTime);

            if ((vivo.objeto.transform.position - origen).sqrMagnitude <= distanciaRecogida * distanciaRecogida)
            {
                Recoger(i, tasaRecogida);
                recogidos++;
            }
        }
        return recogidos;
    }

    public void SoltarAtraidos()
    {
        for (int i = 0; i < vivos.Count; i++)
        {
            if (!vivos[i].atraido) continue;
            vivos[i].atraido = false;
            if (vivos[i].rb != null) vivos[i].rb.isKinematic = false;
        }
    }

    private void Update()
    {
        if (jugador == null)
        {
            MovimientoPersonaje movimiento = FindAnyObjectByType<MovimientoPersonaje>();
            if (movimiento != null) jugador = movimiento.transform;
        }

        float ahora = Time.time;
        float tasaMano = ProveedorEconomia.Actual.recogidaAMano;
        float radio2 = radioRecogidaMano * radioRecogidaMano;

        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            Vivo vivo = vivos[i];
            if (vivo.objeto == null)
            {
                vivos.RemoveAt(i);
                continue;
            }

            float edad = ahora - vivo.nacimiento;
            if (edad >= vidaFragmento)
            {
                Devolver(i);
                continue;
            }

            if (!vivo.atraido && edad > vidaFragmento - parpadeoFinal)
            {
                bool visible = Mathf.Repeat(edad * 8f, 1f) > 0.35f;
                if (vivo.render != null) vivo.render.enabled = visible;
            }

            if (!imanAutomatico || vivo.atraido || jugador == null || edad < inmunidadRecogida) continue;

            Vector3 centroJugador = jugador.position + Vector3.up * 0.8f;
            Vector3 hacia = centroJugador - vivo.objeto.transform.position;
            if (hacia.sqrMagnitude > radio2) continue;

            if (hacia.sqrMagnitude < 0.25f)
            {
                Recoger(i, tasaMano);
                continue;
            }

            if (vivo.rb != null) vivo.rb.isKinematic = true;
            vivo.objeto.transform.position = Vector3.MoveTowards(vivo.objeto.transform.position, centroJugador, velocidadAtraccionMano * Time.deltaTime);
        }
    }

    /// <summary>
    /// Clic izquierdo: coge el resto al que apunta la mirilla (el más cercano a la cámara) y los
    /// que estén pegados a él. Devuelve cuántos ha cogido (0 = no apuntaba a ninguno).
    /// </summary>
    public int RecogerApuntado(Ray rayo)
    {
        int elegido = -1;
        float masCerca = float.MaxValue;
        Vector3 direccion = rayo.direction.normalized;

        for (int i = 0; i < vivos.Count; i++)
        {
            Vivo vivo = vivos[i];
            if (vivo == null || vivo.objeto == null || !vivo.objeto.activeInHierarchy) continue;

            Vector3 haciaResto = vivo.objeto.transform.position - rayo.origin;
            float largo = Vector3.Dot(haciaResto, direccion);
            if (largo < 0f || largo > distanciaClic) continue;

            // Un poco más de margen cuanto más lejos, para que no haga falta puntería fina.
            float tolerancia = toleranciaApuntado + 0.05f * largo;
            if ((haciaResto - direccion * largo).sqrMagnitude > tolerancia * tolerancia) continue;

            if (largo < masCerca)
            {
                masCerca = largo;
                elegido = i;
            }
        }

        if (elegido < 0) return 0;

        Vector3 centro = vivos[elegido].objeto.transform.position;
        float grupo2 = radioGrupoClic * radioGrupoClic;
        int recogidos = 0;

        // Hacia atrás: Recoger quita el elemento de la lista.
        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            Vivo vivo = vivos[i];
            if (vivo == null || vivo.objeto == null) continue;
            if (i != elegido && (vivo.objeto.transform.position - centro).sqrMagnitude > grupo2) continue;

            Recoger(i, 1f);
            recogidos++;
        }
        return recogidos;
    }

    private void Recoger(int indice, float tasa)
    {
        if (unaMonedaPorFragmento) tasa = 1f;

        Vivo vivo = vivos[indice];
        bool bonus = vivo.bonus;
        Devolver(indice);
        FragmentosRecogidos++;

        int monedas = 0;
        if (bonus)
        {
            monedas = 1;
        }
        else
        {
            acumulador += Mathf.Clamp01(tasa) * ProveedorEconomia.Actual.monedasPorFragmento;
            while (acumulador >= 1f)
            {
                acumulador -= 1f;
                monedas++;
            }
        }

        if (monedas > 0)
        {
            if (cartera == null) cartera = FindAnyObjectByType<Cartera>();
            if (cartera != null) cartera.AnadirMonedas(monedas);
            MonedasGeneradas += monedas;
            if (OnMonedasPorFragmentos != null) OnMonedasPorFragmentos(monedas);
        }

        ReproducirClink();
    }

    private void ReproducirClink()
    {
        if (sonidoClink == null || audioSource == null) return;
        combo = Time.time - ultimoClink <= ventanaCombo ? combo + 1 : 0;
        ultimoClink = Time.time;
        audioSource.pitch = Mathf.Min(tonoMaximo, 1f + combo * subidaTonoPorCombo);
        audioSource.PlayOneShot(sonidoClink, volumenClink);
    }

    private Vivo Obtener()
    {
        if (vivos.Count >= maximoVivos) Devolver(0);

        while (libres.Count > 0)
        {
            Vivo libre = libres.Pop();
            if (libre.objeto != null) return libre;
        }

        GameObject objeto = prefabFragmento != null ? Instantiate(prefabFragmento) : CrearFragmentoBasico();
        objeto.name = "Fragmento";
        objeto.transform.SetParent(transform, true);
        Vivo nuevo = new Vivo();
        nuevo.objeto = objeto;
        nuevo.rb = objeto.GetComponent<Rigidbody>();
        nuevo.render = objeto.GetComponentInChildren<Renderer>();
        return nuevo;
    }

    private GameObject CrearFragmentoBasico()
    {
        GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objeto.layer = 2;
        Rigidbody rb = objeto.AddComponent<Rigidbody>();
        rb.mass = 0.05f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        return objeto;
    }

    private void Devolver(int indice)
    {
        Vivo vivo = vivos[indice];
        vivos.RemoveAt(indice);
        if (vivo.objeto == null) return;
        if (vivo.render != null) vivo.render.enabled = true;
        if (vivo.rb != null)
        {
            if (!vivo.rb.isKinematic)
            {
                vivo.rb.linearVelocity = Vector3.zero;
                vivo.rb.angularVelocity = Vector3.zero;
            }
            vivo.rb.isKinematic = true;
        }
        vivo.atraido = false;
        vivo.objeto.SetActive(false);
        libres.Push(vivo);
    }

    private void Pintar(Vivo vivo, Color color, float brillo)
    {
        if (vivo.render == null) return;
        vivo.render.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", color * brillo);
        bloque.SetColor("_Color", color * brillo);
        bloque.SetColor("_EmissionColor", color * 0.8f);
        vivo.render.SetPropertyBlock(bloque);
    }

    public void PagarFragmentosDelSuelo()
    {
        float tasa = ProveedorEconomia.Actual.recogidaAMano;
        for (int i = vivos.Count - 1; i >= 0; i--) Recoger(i, vivos[i].bonus ? 1f : tasa);
    }
}
