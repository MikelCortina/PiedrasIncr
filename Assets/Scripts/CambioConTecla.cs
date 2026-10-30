using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Cada pulsación de la tecla aplica la siguiente fase: en cada una se desactivan unos objetos,
// se activan otros y se cambia la malla de otros por la que se indica para cada uno.
public class CambioConTecla : MonoBehaviour
{
    [System.Serializable]
    public class CambioMalla
    {
        [Tooltip("Objeto cuya malla se cambia. Si la malla está en un hijo (p. ej. el padre de un modelo importado), se usa la primera que se encuentre en sus hijos.")]
        public GameObject objeto;
        [Tooltip("Malla nueva para este objeto")]
        public Mesh mallaNueva;
        [Tooltip("Si el objeto tiene MeshCollider, también usa la malla nueva")]
        public bool actualizarCollider = true;

        [System.NonSerialized] public GameObject portador;   // el objeto (o hijo) que tiene la malla de verdad
        [System.NonSerialized] public Mesh mallaOriginal;
        [System.NonSerialized] public Mesh colliderOriginal;
    }

    [System.Serializable]
    public class Fase
    {
        [Tooltip("Solo para orientarse en el Inspector")]
        public string nombre;
        [Tooltip("Se desactivan al entrar en esta fase")]
        public List<GameObject> objetosDesactivar = new List<GameObject>();
        [Tooltip("Se activan al entrar en esta fase")]
        public List<GameObject> objetosActivar = new List<GameObject>();
        [Tooltip("Cambian su malla por la indicada en cada entrada")]
        public List<CambioMalla> cambiosMalla = new List<CambioMalla>();
    }

    [Header("Tecla")]
    public KeyCode tecla = KeyCode.O;
    [Tooltip("Tras la última fase, la siguiente pulsación deshace todas las fases (en orden inverso) y se vuelve a empezar.")]
    public bool reiniciarAlFinal = false;

    [Header("Fases (una por pulsación, en orden)")]
    public List<Fase> fases = new List<Fase>();

    [Header("Torbellino")]
    [Tooltip("Si se asigna, cada fase sube un nivel el radio del torbellino (hasta el 4)")]
    public HerramientaTorbellino torbellino;
    private int nivelRadioInicial = -1;

    [Header("Bloop (en cada pulsación)")]
    [Tooltip("Objeto que hace un bloop cada vez que se pulsa la tecla")]
    public Transform objetoBloop;
    [Tooltip("Cuánto crece en el bloop (1.15 = 15% más grande)")]
    public float multiplicadorBloop = 1.15f;
    [Tooltip("Velocidad a la que recupera su tamaño original")]
    public float velocidadBloop = 15f;
    private Vector3 escalaOriginalBloop;
    private bool escalaBloopGuardada;
    private Coroutine corrutinaBloop;

    [Header("Piedras extra (en cada pulsación)")]
    [Tooltip("Prefab de piedra que aparece en el ojo del torbellino")]
    public GameObject prefabPiedra;
    [Min(0)] public int piedrasPorPulsacion = 2;
    [Tooltip("Altura sobre el suelo a la que aparecen")]
    public float alturaPiedras = 0.5f;
    [Tooltip("Velocidad a la que crecen al aparecer (0 = aparecen ya a su tamaño)")]
    public float velocidadCrecimientoPiedras = 12f;
    [Tooltip("Si el torbellino no está apuntando a ningún sitio, las piedras aparecen aquí (vacío = en el último centro del torbellino)")]
    public Transform puntoPiedrasAlternativo;

    // Número de fases ya aplicadas (0 = ninguna).
    private int fasesAplicadas;

    public int FasesAplicadas => fasesAplicadas;

    private void Update()
    {
        if (!Input.GetKeyDown(tecla))
            return;

        // El bloop y las piedras ocurren en cada pulsación, también cuando ya no quedan fases.
        HacerBloop();
        GenerarPiedras();
        Avanzar();
    }

    public void HacerBloop()
    {
        if (objetoBloop == null)
            return;

        if (!escalaBloopGuardada)
        {
            escalaOriginalBloop = objetoBloop.localScale;
            escalaBloopGuardada = true;
        }

        if (corrutinaBloop != null)
            StopCoroutine(corrutinaBloop);
        corrutinaBloop = StartCoroutine(AnimarBloop());
    }

    private IEnumerator AnimarBloop()
    {
        objetoBloop.localScale = escalaOriginalBloop * multiplicadorBloop;

        while (Vector3.Distance(objetoBloop.localScale, escalaOriginalBloop) > escalaOriginalBloop.magnitude * 0.01f + 0.001f)
        {
            objetoBloop.localScale = Vector3.Lerp(objetoBloop.localScale, escalaOriginalBloop, Time.deltaTime * velocidadBloop);
            yield return null;
        }

        objetoBloop.localScale = escalaOriginalBloop;
        corrutinaBloop = null;
    }

    // Las piedras aparecen repartidas por el anillo del ojo del torbellino, donde orbitan.
    public void GenerarPiedras()
    {
        if (prefabPiedra == null || piedrasPorPulsacion <= 0)
            return;

        Vector3 centro;
        Vector3 arriba = Vector3.up;
        float radioOjo = 0f;

        if (torbellino != null && (torbellino.TorbellinoActivo || puntoPiedrasAlternativo == null))
        {
            centro = torbellino.CentroTorbellino;
            arriba = torbellino.NormalTorbellino.sqrMagnitude > 0.01f ? torbellino.NormalTorbellino.normalized : Vector3.up;
            radioOjo = torbellino.radioOjoTornado;
        }
        else if (puntoPiedrasAlternativo != null)
        {
            centro = puntoPiedrasAlternativo.position;
        }
        else
        {
            Debug.LogWarning("CambioConTecla: asigna el torbellino o un punto alternativo para generar las piedras.", this);
            return;
        }

        Vector3 lateral = Vector3.Cross(arriba, Vector3.forward);
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.Cross(arriba, Vector3.right);
        lateral.Normalize();

        float anguloInicial = Random.Range(0f, 360f);
        for (int i = 0; i < piedrasPorPulsacion; i++)
        {
            float angulo = anguloInicial + 360f * i / piedrasPorPulsacion;
            Vector3 desplazamiento = Quaternion.AngleAxis(angulo, arriba) * lateral * radioOjo;
            Vector3 posicion = centro + desplazamiento + arriba * alturaPiedras;

            GameObject piedra = Instantiate(prefabPiedra, posicion, Random.rotation);

            if (velocidadCrecimientoPiedras > 0f)
            {
                CrecimientoInicialPiedra crecimiento = piedra.AddComponent<CrecimientoInicialPiedra>();
                crecimiento.IniciarCrecimiento(prefabPiedra.transform.localScale, velocidadCrecimientoPiedras);
            }
        }
    }

    [ContextMenu("Siguiente fase")]
    public void Avanzar()
    {
        if (fasesAplicadas < fases.Count)
        {
            AplicarFase(fases[fasesAplicadas]);
            fasesAplicadas++;

            if (torbellino != null)
            {
                if (nivelRadioInicial < 0)
                    nivelRadioInicial = torbellino.NivelRadio;
                torbellino.MejorarRadio();
            }
        }
        else if (reiniciarAlFinal)
        {
            Reiniciar();
        }
    }

    [ContextMenu("Deshacer todas las fases")]
    public void Reiniciar()
    {
        while (fasesAplicadas > 0)
        {
            fasesAplicadas--;
            DeshacerFase(fases[fasesAplicadas]);
        }

        if (torbellino != null && nivelRadioInicial > 0)
            torbellino.EstablecerNivelRadio(nivelRadioInicial);
    }

    private static void AplicarFase(Fase fase)
    {
        if (fase == null)
            return;

        CambiarActivos(fase, true);

        foreach (CambioMalla cambio in fase.cambiosMalla)
        {
            if (cambio == null || cambio.objeto == null || cambio.mallaNueva == null)
                continue;

            cambio.portador = BuscarPortador(cambio.objeto);
            if (cambio.portador == null)
            {
                Debug.LogWarning("CambioConTecla: ni '" + cambio.objeto.name + "' ni sus hijos tienen MeshFilter o SkinnedMeshRenderer.", cambio.objeto);
                continue;
            }

            cambio.mallaOriginal = LeerMalla(cambio.portador);
            PonerMalla(cambio.portador, cambio.mallaNueva);

            if (cambio.actualizarCollider && cambio.portador.TryGetComponent(out MeshCollider collider))
            {
                cambio.colliderOriginal = collider.sharedMesh;
                collider.sharedMesh = cambio.mallaNueva;
            }
        }
    }

    private static void DeshacerFase(Fase fase)
    {
        if (fase == null)
            return;

        CambiarActivos(fase, false);

        // En orden inverso, por si un mismo objeto cambia de malla dos veces en la misma fase.
        for (int i = fase.cambiosMalla.Count - 1; i >= 0; i--)
        {
            CambioMalla cambio = fase.cambiosMalla[i];
            if (cambio == null || cambio.portador == null || cambio.mallaOriginal == null)
                continue;

            PonerMalla(cambio.portador, cambio.mallaOriginal);

            if (cambio.actualizarCollider && cambio.portador.TryGetComponent(out MeshCollider collider))
                collider.sharedMesh = cambio.colliderOriginal;
        }
    }

    // aplicar = true: desactiva/activa según las listas de la fase; false: lo contrario.
    private static void CambiarActivos(Fase fase, bool aplicar)
    {
        foreach (GameObject objeto in fase.objetosDesactivar)
        {
            if (objeto != null)
                objeto.SetActive(!aplicar);
        }

        foreach (GameObject objeto in fase.objetosActivar)
        {
            if (objeto != null)
                objeto.SetActive(aplicar);
        }
    }

    // El propio objeto si tiene la malla; si no, el primer hijo que la tenga (avisa si hay varios).
    private static GameObject BuscarPortador(GameObject objeto)
    {
        if (objeto.TryGetComponent(out MeshFilter _) || objeto.TryGetComponent(out SkinnedMeshRenderer _))
            return objeto;

        MeshFilter[] filtros = objeto.GetComponentsInChildren<MeshFilter>(true);
        SkinnedMeshRenderer[] pieles = objeto.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int total = filtros.Length + pieles.Length;
        if (total == 0)
            return null;

        GameObject elegido = filtros.Length > 0 ? filtros[0].gameObject : pieles[0].gameObject;
        if (total > 1)
            Debug.LogWarning("CambioConTecla: '" + objeto.name + "' tiene " + total + " mallas en sus hijos; se cambia la de '" + elegido.name + "'. Arrastra el hijo concreto si querías otra.", objeto);

        return elegido;
    }

    private static Mesh LeerMalla(GameObject objeto)
    {
        if (objeto.TryGetComponent(out MeshFilter filtro))
            return filtro.sharedMesh;

        if (objeto.TryGetComponent(out SkinnedMeshRenderer piel))
            return piel.sharedMesh;

        return null;
    }

    private static void PonerMalla(GameObject objeto, Mesh malla)
    {
        if (objeto.TryGetComponent(out MeshFilter filtro))
            filtro.sharedMesh = malla;
        else if (objeto.TryGetComponent(out SkinnedMeshRenderer piel))
            piel.sharedMesh = malla;
        else
            Debug.LogWarning("CambioConTecla: '" + objeto.name + "' no tiene MeshFilter ni SkinnedMeshRenderer.", objeto);
    }
}
