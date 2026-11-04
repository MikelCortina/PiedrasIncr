
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restaura las conexiones entre edificios.
///
/// Soporta:
/// - Pared -> Rampa (RailRampa)
/// - Rampa -> Rampa (ConectorEntrada / ConectorSalida)
/// - Pared -> Pared (ConectorParedEntrada / ConectorParedSalida)
///
/// No crea ni destruye construcciones.
/// No modifica monedas ni inventario.
/// </summary>
public static class PlacedBuildingsConnectionsRestoreAdapter
{
#if UNITY_EDITOR
    // PASO 82:
    // Permite provocar un fallo controlado
    // despues de aplicar cada conexion.
    public static Action<int> FalloDiagnostico;
#endif

    private sealed class EnlacePendiente
    {
        public EdificioConstruido hijo;
        public EdificioConstruido padre;

        public Collider conectorPadre;
        public Collider conectorHijoBloqueado;

        public bool paredSobreRail;
    }

    public static int Restaurar(GameSaveData partida)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La restauracion requiere estar en Play."
            );
        }

        Scene escena = SceneManager.GetActiveScene();

        if (partida == null ||
            partida.sceneName != escena.name)
        {
            throw new InvalidOperationException(
                "La partida pertenece a otra escena."
            );
        }

        PlacedBuildingsSaveData datos =
            PlacedBuildingsSaveAdapter.Read(partida);

        // ======================================
        // 1. LOCALIZAR LOS EDIFICIOS
        // ======================================

        Dictionary<string, EdificioConstruido> actuales =
            new Dictionary<string, EdificioConstruido>(
                StringComparer.Ordinal
            );

        EdificioConstruido[] encontrados =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in encontrados)
        {
            if (edificio == null ||
                edificio.gameObject.scene != escena ||
                string.IsNullOrEmpty(edificio.IdPersistente))
            {
                continue;
            }

            if (!actuales.TryAdd(
                edificio.IdPersistente,
                edificio))
            {
                throw new InvalidOperationException(
                    "ID duplicado en la escena: " +
                    edificio.IdPersistente
                );
            }
        }

        HashSet<string> idsGuardados =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (PlacedBuildingSaveData registro
            in datos.edificios)
        {
            idsGuardados.Add(registro.idPersistente);

            EdificioConstruido actual;

            if (!actuales.TryGetValue(
                registro.idPersistente,
                out actual))
            {
                throw new InvalidOperationException(
                    "Falta reconstruir el edificio: " +
                    registro.idPersistente
                );
            }

            if (actual.tipo.ToString() != registro.tipo)
            {
                throw new InvalidOperationException(
                    "El tipo de edificio no coincide: " +
                    registro.idPersistente
                );
            }
        }

        // ======================================
        // 2. VALIDAR TODAS LAS CONEXIONES
        // ======================================

        List<EnlacePendiente> pendientes =
            new List<EnlacePendiente>();

        HashSet<Collider> conectoresUtilizados =
            new HashSet<Collider>();

        HashSet<Collider> extremosBloqueados =
            new HashSet<Collider>();

        foreach (PlacedBuildingSaveData registro
            in datos.edificios)
        {
            if (!registro.hasConnection)
                continue;

            if (!idsGuardados.Contains(
                registro.connectedToBuildingId))
            {
                throw new InvalidOperationException(
                    "El propietario del conector " +
                    "no aparece en el guardado."
                );
            }

            EdificioConstruido hijo =
                actuales[registro.idPersistente];

            EdificioConstruido padre =
                actuales[registro.connectedToBuildingId];

            if (hijo.GetComponent<
                    EfectoBloopDestruccion>() != null ||
                padre.GetComponent<
                    EfectoBloopDestruccion>() != null)
            {
                throw new InvalidOperationException(
                    "Hay un edificio en demolicion."
                );
            }

            Collider conectorPadre = ResolverConector(
                padre.transform,
                registro.connectorPath
            );

            if (conectorPadre.gameObject.tag !=
                registro.connectorTag)
            {
                throw new InvalidOperationException(
                    "El tag del conector no coincide: " +
                    registro.connectorPath
                );
            }

            if (conectorPadre.GetComponentInParent<
                    EdificioConstruido>(true) != padre)
            {
                throw new InvalidOperationException(
                    "El conector pertenece a otro edificio."
                );
            }

            if (!conectoresUtilizados.Add(conectorPadre))
            {
                throw new InvalidOperationException(
                    "Dos edificios utilizan el mismo conector."
                );
            }

            if (hijo.conectorUsado != null &&
                hijo.conectorUsado != conectorPadre)
            {
                throw new InvalidOperationException(
                    "El edificio ya utiliza otro conector."
                );
            }

            foreach (EdificioConstruido otro
                in actuales.Values)
            {
                if (otro != hijo &&
                    otro.conectorUsado == conectorPadre)
                {
                    throw new InvalidOperationException(
                        "El conector ya esta ocupado."
                    );
                }
            }

            EnlacePendiente enlace =
                new EnlacePendiente
                {
                    hijo = hijo,
                    padre = padre,
                    conectorPadre = conectorPadre
                };

            // ======================================
            // CASO 1: PARED -> RAMPA
            // ======================================

            if (hijo.tipo ==
                    SistemaConstruccion.TipoEdificio.Pared &&
                padre.tipo ==
                    SistemaConstruccion.TipoEdificio.Rampa &&
                registro.connectorTag == "RailRampa")
            {
                if (padre.edificiosDependientes == null)
                {
                    throw new InvalidOperationException(
                        "La rampa no tiene lista de dependencias."
                    );
                }

                enlace.paredSobreRail = true;
            }

            // ======================================
            // CASO 2: RAMPA -> RAMPA
            // ======================================

            else if (
                hijo.tipo ==
                    SistemaConstruccion.TipoEdificio.Rampa &&
                padre.tipo ==
                    SistemaConstruccion.TipoEdificio.Rampa &&
                (registro.connectorTag == "ConectorSalida" ||
                 registro.connectorTag == "ConectorEntrada"))
            {
                string nombreABloquear =
                    registro.connectorTag == "ConectorSalida"
                        ? "PuntoConexion_Entrada"
                        : "PuntoConexion_Salida";

                enlace.conectorHijoBloqueado =
                    BuscarConectorPorNombre(
                        hijo.transform,
                        nombreABloquear
                    );

                if (enlace.conectorHijoBloqueado == null)
                {
                    throw new InvalidOperationException(
                        "La rampa hija no tiene el conector: " +
                        nombreABloquear
                    );
                }

                if (padre.conectoresHijosBloqueados == null)
                {
                    throw new InvalidOperationException(
                        "La rampa padre no tiene lista " +
                        "de conectores bloqueados."
                    );
                }
            }

            // ======================================
            // CASO 3: PARED -> PARED
            // ======================================

            else if (
                hijo.tipo ==
                    SistemaConstruccion.TipoEdificio.Pared &&
                padre.tipo ==
                    SistemaConstruccion.TipoEdificio.Pared &&
                (registro.connectorTag ==
                    "ConectorParedSalida" ||
                 registro.connectorTag ==
                    "ConectorParedEntrada"))
            {
                // Salida del padre -> Entrada del hijo.
                // Entrada del padre -> Salida del hijo.

                string nombreABloquear =
                    registro.connectorTag ==
                        "ConectorParedSalida"
                        ? "PuntoConexionPared_Entrada"
                        : "PuntoConexionPared_Salida";

                enlace.conectorHijoBloqueado =
                    BuscarConectorPorNombre(
                        hijo.transform,
                        nombreABloquear
                    );

                if (enlace.conectorHijoBloqueado == null)
                {
                    throw new InvalidOperationException(
                        "La pared hija no tiene el conector: " +
                        nombreABloquear
                    );
                }

                if (padre.conectoresHijosBloqueados == null)
                {
                    throw new InvalidOperationException(
                        "La pared padre no tiene lista " +
                        "de conectores bloqueados."
                    );
                }
            }

            // ======================================
            // CONEXION NO SOPORTADA
            // ======================================

            else
            {
                throw new InvalidOperationException(
                    "Conexion todavia no soportada: " +
                    hijo.tipo + " -> " + padre.tipo +
                    ", tag=" + registro.connectorTag
                );
            }

            // Evitar reservar un mismo extremo
            // para varias conexiones.
            if (enlace.conectorHijoBloqueado != null &&
                !extremosBloqueados.Add(
                    enlace.conectorHijoBloqueado))
            {
                throw new InvalidOperationException(
                    "Un conector hijo aparece bloqueado " +
                    "por varias conexiones."
                );
            }

            pendientes.Add(enlace);
        }

        // ======================================
        // 3. RESTAURAR LAS CONEXIONES
        // ======================================

        // Hasta aqui no se ha modificado ningun
        // conector ni dependencia.

        // PASO 82: contar las conexiones aplicadas.
        int conexionesAplicadas = 0;

        foreach (EnlacePendiente enlace in pendientes)
        {
            // El hijo recuerda el conector
            // del edificio padre.
            enlace.hijo.conectorUsado =
                enlace.conectorPadre;

            // Ocupar el conector del padre.
            enlace.conectorPadre.enabled = false;

            // ======================================
            // PARED COLOCADA SOBRE RAILRAMPA
            // ======================================

            if (enlace.paredSobreRail)
            {
                // Registrar la pared dependiente.
                if (!enlace.padre.edificiosDependientes.Contains(
                    enlace.hijo.gameObject))
                {
                    enlace.padre.edificiosDependientes.Add(
                        enlace.hijo.gameObject
                    );
                }

                // Deshabilitar los conectores propios
                // de la pared situada sobre la rampa.
                Collider[] colliders =
                    enlace.hijo.GetComponentsInChildren<
                        Collider>(true);

                foreach (Collider collider in colliders)
                {
                    if (collider.CompareTag(
                            "ConectorParedSalida") ||
                        collider.CompareTag(
                            "ConectorParedEntrada"))
                    {
                        collider.enabled = false;
                    }
                }
            }

            // ======================================
            // RAMPA -> RAMPA Y PARED -> PARED
            // ======================================

            else if (enlace.conectorHijoBloqueado != null)
            {
                Collider bloqueado =
                    enlace.conectorHijoBloqueado;

                // Bloquear extremo del hijo.
                bloqueado.enabled = false;

                // Registrar el conector bloqueado
                // para liberarlo durante la demolicion.
                if (!enlace.padre.conectoresHijosBloqueados
                        .Contains(bloqueado))
                {
                    enlace.padre.conectoresHijosBloqueados
                        .Add(bloqueado);
                }
            }

            // ======================================
            // PASO 82: FALLO INTERNO CONTROLADO
            // ======================================

            // La conexion fisica ya se ha aplicado.
            conexionesAplicadas++;

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke(
                conexionesAplicadas
            );
#endif
        }

        return pendientes.Count;
    }

    // ======================================
    // BUSCAR CONECTOR POR NOMBRE
    // ======================================

    private static Collider BuscarConectorPorNombre(
        Transform raiz,
        string nombre)
    {
        Collider encontrado = null;

        Transform[] hijos =
            raiz.GetComponentsInChildren<Transform>(true);

        foreach (Transform hijo in hijos)
        {
            if (hijo.name != nombre)
                continue;

            Collider collider =
                hijo.GetComponent<Collider>();

            if (collider == null)
                continue;

            if (encontrado != null)
            {
                throw new InvalidOperationException(
                    "Conector ambiguo: " + nombre
                );
            }

            encontrado = collider;
        }

        return encontrado;
    }

    // ======================================
    // LOCALIZAR CONECTOR POR SU RUTA GUARDADA
    // ======================================

    private static Collider ResolverConector(
        Transform raiz,
        string ruta)
    {
        if (raiz == null ||
            string.IsNullOrEmpty(ruta))
        {
            throw new InvalidOperationException(
                "Ruta de conector invalida."
            );
        }

        Transform actual = raiz;

        if (ruta != "(raiz)")
        {
            string[] partes = ruta.Split('/');

            foreach (string parte in partes)
            {
                int apertura = parte.LastIndexOf('[');

                if (apertura <= 0 ||
                    !parte.EndsWith("]"))
                {
                    throw new InvalidOperationException(
                        "Formato de ruta invalido: " + parte
                    );
                }

                string nombre =
                    parte.Substring(0, apertura);

                string indiceTexto = parte.Substring(
                    apertura + 1,
                    parte.Length - apertura - 2
                );

                int indice;

                if (!int.TryParse(indiceTexto, out indice) ||
                    indice < 0 ||
                    indice >= actual.childCount)
                {
                    throw new InvalidOperationException(
                        "Indice de conector invalido."
                    );
                }

                actual = actual.GetChild(indice);

                if (actual.name != nombre)
                {
                    throw new InvalidOperationException(
                        "El nombre del conector no coincide."
                    );
                }
            }
        }

        Collider resultado =
            actual.GetComponent<Collider>();

        if (resultado == null)
        {
            throw new InvalidOperationException(
                "El conector no tiene Collider."
            );
        }

        return resultado;
    }
}
