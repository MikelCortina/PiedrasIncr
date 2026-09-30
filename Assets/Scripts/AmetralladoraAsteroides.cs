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
    public float alcance = 300f;
    public float grosorLaser = 2f;
    public LayerMask capaAsteroides;

    [Header("Efectos Visuales")]
    [Tooltip("El misil/láser visual que volará hacia el objetivo")]
    public GameObject prefabProyectil;
    [Tooltip("Velocidad de vuelo del proyectil visual")]
    public float velocidadProyectil = 150f;
    [Tooltip("El sistema de partículas que aparecerá al destruir el asteroide")]
    public GameObject prefabExplosionAsteroide;

    private Transform asteroideObjetivo;
    private float timerCooldown = 0f;

    private void Update()
    {
        if (timerCooldown > 0f)
        {
            timerCooldown -= Time.deltaTime;
        }

        if (asteroideObjetivo != null)
        {
            if (Vector3.Distance(transform.position, asteroideObjetivo.position) > alcance)
            {
                asteroideObjetivo = null;
            }
        }

        if (asteroideObjetivo == null)
        {
            BuscarObjetivo();
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

    private void BuscarObjetivo()
    {
        Collider[] asteroidesCercanos = Physics.OverlapSphere(transform.position, alcance, capaAsteroides);
        if (asteroidesCercanos.Length > 0)
        {
            asteroideObjetivo = asteroidesCercanos[Random.Range(0, asteroidesCercanos.Length)].transform;
        }
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

            // El SphereCast asegura lógicamente el tiro al instante
            if (Physics.SphereCast(puntoDisparo.position, grosorLaser, direccionDisparo, out RaycastHit hit, alcance, capaAsteroides, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.transform.root == this.transform.root) return;

                // --- NUEVO: Iniciamos el vuelo del proyectil hacia el objetivo asegurado ---
                StartCoroutine(RutinaVueloProyectil(hit.collider.gameObject));

                asteroideObjetivo = null; // Soltamos el objetivo para buscar otro inmediatamente
                timerCooldown = cadenciaDisparo;
            }
        }
    }

    // =================================================================================
    // NUEVO: Corrutina que mueve el proyectil visual en línea recta hasta el asteroide
    // =================================================================================
    private IEnumerator RutinaVueloProyectil(GameObject asteroideHit)
    {
        GameObject proyectil = null;

        // 1. Instanciamos el misil/láser visual
        if (prefabProyectil != null && puntoDisparo != null)
        {
            proyectil = Instantiate(prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
        }

        bool impactoConfirmado = false;

        // 2. Lo movemos frame a frame en línea recta hacia el asteroide
        while (proyectil != null && asteroideHit != null)
        {
            Vector3 direccion = (asteroideHit.transform.position - proyectil.transform.position).normalized;
            float distanciaAvance = velocidadProyectil * Time.deltaTime;

            proyectil.transform.position += direccion * distanciaAvance;
            if (direccion != Vector3.zero) proyectil.transform.rotation = Quaternion.LookRotation(direccion);

            // Si está lo suficientemente cerca, consideramos que ha impactado
            if (Vector3.Distance(proyectil.transform.position, asteroideHit.transform.position) <= distanciaAvance + 1f)
            {
                impactoConfirmado = true;
                break;
            }

            yield return null;
        }

        // 3. Destruimos el proyectil visual
        if (proyectil != null) Destroy(proyectil);

        // 4. Si el asteroide sigue vivo y logramos llegar a él, ejecutamos la destrucción real
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