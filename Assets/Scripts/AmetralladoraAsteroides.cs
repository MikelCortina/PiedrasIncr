using System.Collections;
using UnityEngine;

public class AmetralladoraAsteroides : MonoBehaviour
{
    [Header("Referencias")]
    public LluviaMeteoritos gestorLluvia;

    [Header("Articulaciones de la Torreta")]
    public Transform pivoteBase;
    public Transform pivoteCanon;
    public Transform puntoDisparo;

    [Header("Configuración de Apuntado")]
    public float velocidadGiro = 10f;
    public float margenFijacion = 10f;

    [Header("Configuración de Disparo")]
    public float cadenciaDisparo = 1f;
    [Tooltip("Rango de detección para BUSCAR nuevos objetivos")]
    public float alcance = 300f;
    public float grosorLaser = 2f;
    public LayerMask capaAsteroides;

    // --- NUEVO: SISTEMA DE ANIMACIÓN POR BLEND SHAPE ---
    [Header("Animación (Blend Shape)")]
    [Tooltip("El SkinnedMeshRenderer del cañón o pieza que contiene la animación de retroceso")]
    public SkinnedMeshRenderer mallaArma;
    [Tooltip("Índice de la forma de mezcla en el modelo 3D (0 es la primera, 1 la segunda...)")]
    public int indiceBlendShape = 0;
    public float pesoMaximoBlendShape = 100f;
    public float velocidadRecuperacionBlend = 15f;

    [Header("Efectos Visuales y Gráfica de Velocidad")]
    public GameObject prefabProyectil;
    [Tooltip("Velocidad máxima base del proyectil")]
    public float velocidadMaximaProyectil = 150f;

    [Tooltip("Gráfica de velocidad (Eje X: 0 es inicio del vuelo, 1 es el impacto | Eje Y: multiplicador de velocidad)")]
    public AnimationCurve curvaVelocidadProyectil = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.7f, 0.8f),
        new Keyframe(1f, 0.15f)
    );

    public GameObject prefabExplosionAsteroide;

    private Transform asteroideObjetivo;
    private float timerCooldown = 0f;
    private float pesoActualBlend = 0f; // Control del valor actual de deformación

    private void Update()
    {
        // 1. Recuperación continua del Blend Shape hacia 0
        ManejarBlendShape();

        if (timerCooldown > 0f)
        {
            timerCooldown -= Time.deltaTime;
        }

        // 2. Comprobación del objetivo fijado
        if (asteroideObjetivo != null)
        {
            if (!asteroideObjetivo.gameObject.activeInHierarchy)
            {
                PerderObjetivo();
            }
        }

        // 3. Búsqueda o seguimiento
        if (asteroideObjetivo == null)
        {
            BuscarMejorObjetivo();
        }
        else
        {
            ApuntarAlObjetivo();

            if (timerCooldown <= 0f)
            {
                IntentarDisparo();
            }
        }
    }

    private void ManejarBlendShape()
    {
        if (mallaArma != null && (pesoActualBlend > 0.01f || mallaArma.GetBlendShapeWeight(indiceBlendShape) > 0.01f))
        {
            pesoActualBlend = Mathf.Lerp(pesoActualBlend, 0f, Time.deltaTime * velocidadRecuperacionBlend);
            mallaArma.SetBlendShapeWeight(indiceBlendShape, pesoActualBlend);
        }
    }

    private void BuscarMejorObjetivo()
    {
        Collider[] asteroidesCercanos = Physics.OverlapSphere(transform.position, alcance, capaAsteroides);

        Transform objetivoMasCercano = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < asteroidesCercanos.Length; i++)
        {
            if (asteroidesCercanos[i] == null) continue;

            float d = Vector3.Distance(transform.position, asteroidesCercanos[i].transform.position);
            if (d < menorDistancia)
            {
                menorDistancia = d;
                objetivoMasCercano = asteroidesCercanos[i].transform;
            }
        }

        asteroideObjetivo = objetivoMasCercano;
    }

    private void PerderObjetivo()
    {
        asteroideObjetivo = null;
    }

    private void ApuntarAlObjetivo()
    {
        if (pivoteBase != null && pivoteBase.parent != null)
        {
            Vector3 ejeRotacionBase = pivoteBase.forward;
            Vector3 haciaObjetivo = (asteroideObjetivo.position - pivoteBase.position).normalized;

            Vector3 apuntadoActual = Vector3.ProjectOnPlane(puntoDisparo.forward, ejeRotacionBase);
            Vector3 apuntadoDeseado = Vector3.ProjectOnPlane(haciaObjetivo, ejeRotacionBase);

            float anguloRestante = Vector3.SignedAngle(apuntadoActual, apuntadoDeseado, ejeRotacionBase);
            pivoteBase.Rotate(Vector3.forward, anguloRestante * Time.deltaTime * velocidadGiro, Space.Self);
        }

        if (pivoteCanon != null && pivoteCanon.parent != null)
        {
            Vector3 ejeRotacionCanon = pivoteCanon.right;
            Vector3 haciaObjetivo = (asteroideObjetivo.position - pivoteCanon.position).normalized;

            Vector3 apuntadoActual = Vector3.ProjectOnPlane(puntoDisparo.forward, ejeRotacionCanon);
            Vector3 apuntadoDeseado = Vector3.ProjectOnPlane(haciaObjetivo, ejeRotacionCanon);

            float anguloRestante = Vector3.SignedAngle(apuntadoActual, apuntadoDeseado, ejeRotacionCanon);
            pivoteCanon.Rotate(Vector3.right, anguloRestante * Time.deltaTime * velocidadGiro, Space.Self);
        }
    }

    private void IntentarDisparo()
    {
        Vector3 direccionHaciaObjetivo = (asteroideObjetivo.position - puntoDisparo.position).normalized;
        float anguloAlineacion = Vector3.Angle(puntoDisparo.forward, direccionHaciaObjetivo);

        if (anguloAlineacion <= margenFijacion)
        {
            Vector3 direccionDisparo = puntoDisparo.forward;

            float distanciaAlObjetivo = Vector3.Distance(puntoDisparo.position, asteroideObjetivo.position);
            float alcanceDisparo = distanciaAlObjetivo + 20f;

            if (Physics.SphereCast(puntoDisparo.position, grosorLaser, direccionDisparo, out RaycastHit hit, alcanceDisparo, capaAsteroides, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.transform.root == this.transform.root) return;

                // --- DISPARO DEL BLEND SHAPE ---
                pesoActualBlend = pesoMaximoBlendShape;

                StartCoroutine(RutinaVueloProyectil(hit.collider.gameObject));

                PerderObjetivo();
                timerCooldown = cadenciaDisparo;
            }
        }
    }

    private IEnumerator RutinaVueloProyectil(GameObject asteroideHit)
    {
        GameObject proyectil = null;

        if (prefabProyectil != null && puntoDisparo != null)
        {
            proyectil = Instantiate(prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
        }

        bool impactoConfirmado = false;
        float distanciaInicial = asteroideHit != null ? Vector3.Distance(puntoDisparo.position, asteroideHit.transform.position) : 1f;

        while (proyectil != null && asteroideHit != null)
        {
            Vector3 direccion = (asteroideHit.transform.position - proyectil.transform.position).normalized;
            float distanciaRestante = Vector3.Distance(proyectil.transform.position, asteroideHit.transform.position);

            float progresoVuelo = Mathf.Clamp01(1f - (distanciaRestante / distanciaInicial));
            float multiplicadorVelocidad = curvaVelocidadProyectil.Evaluate(progresoVuelo);
            float velocidadActual = velocidadMaximaProyectil * multiplicadorVelocidad;

            float distanciaAvance = velocidadActual * Time.deltaTime;

            proyectil.transform.position += direccion * distanciaAvance;
            if (direccion != Vector3.zero) proyectil.transform.rotation = Quaternion.LookRotation(direccion);

            if (distanciaRestante <= distanciaAvance + 1.5f)
            {
                impactoConfirmado = true;
                break;
            }

            yield return null;
        }

        if (proyectil != null)
        {
            MeshRenderer[] mallas = proyectil.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer m in mallas) m.enabled = false;

            ParticleSystem[] sistemasParticulas = proyectil.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in sistemasParticulas)
            {
                var emision = ps.emission;
                emision.enabled = false;
            }

            Destroy(proyectil, 2f);
        }

        if (impactoConfirmado && asteroideHit != null)
        {
            Vector3 posicionRotura = asteroideHit.transform.position;

            if (prefabExplosionAsteroide != null)
            {
                GameObject explosion = Instantiate(prefabExplosionAsteroide, posicionRotura, Quaternion.identity);
                Destroy(explosion, 3f);
            }

            if (gestorLluvia != null && gestorLluvia.cinturonOrigen != null)
            {
                gestorLluvia.cinturonOrigen.ExplotarLocal(posicionRotura);
            }

            Destroy(asteroideHit);

            if (gestorLluvia != null)
            {
                gestorLluvia.SpawnearMeteoritoDesdeAsteroideRoto(posicionRotura);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, alcance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcance);

        if (puntoDisparo != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(puntoDisparo.position, puntoDisparo.forward * alcance);
        }
    }
}