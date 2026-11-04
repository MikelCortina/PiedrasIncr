
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye un edificio desde su prefab original.
///
/// Por ahora admite edificios sin curvatura.
/// Las conexiones se restauraran en otro paso.
///
/// No gasta monedas ni consume inventario.
/// Evita crear dos edificios con el mismo ID.
/// </summary>
public static class PlacedBuildingsSpawnAdapter
{
    public static EdificioConstruido CrearORecuperar(
        SistemaConstruccion sistema,
        PlacedBuildingSaveData datos)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La reconstruccion requiere estar en Play."
            );
        }

        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        // Validar el registro antes de crear objetos.
        PlacedBuildingsSaveData comprobacion =
            new PlacedBuildingsSaveData();

        comprobacion.edificios.Add(datos);

        PlacedBuildingsSaveAdapter.Validate(comprobacion);

        SistemaConstruccion.TipoEdificio tipo =
            (SistemaConstruccion.TipoEdificio)
            Enum.Parse(
                typeof(SistemaConstruccion.TipoEdificio),
                datos.tipo
            );

        // Todavia no reconstruimos mallas curvadas.
        if (datos.curvaturaRampa != 0f)
        {
            throw new InvalidOperationException(
                "La restauracion de rampas curvadas " +
                "todavia no esta implementada."
            );
        }

        // Buscar el prefab exacto en el catalogo.
        GameObject prefab = BuscarPrefab(
            sistema,
            tipo,
            datos.variantePrefab
        );

        // Evitar duplicados.
        EdificioConstruido existente =
            BuscarPorId(datos.idPersistente);

        if (existente != null)
        {
            if (existente.GetComponent<
                EfectoBloopDestruccion>() != null)
            {
                throw new InvalidOperationException(
                    "El edificio guardado se esta demoliendo."
                );
            }

            ComprobarExistente(existente, datos, tipo);

            // Ya existe correctamente. No crear otro.
            return existente;
        }

        Vector3 posicion = new Vector3(
            datos.positionX,
            datos.positionY,
            datos.positionZ
        );

        Quaternion rotacion = new Quaternion(
            datos.rotationX,
            datos.rotationY,
            datos.rotationZ,
            datos.rotationW
        );

        Vector3 escala = new Vector3(
            datos.scaleX,
            datos.scaleY,
            datos.scaleZ
        );

        GameObject objeto = null;

        try
        {
            // Crear el prefab real.
            objeto = UnityEngine.Object.Instantiate(
                prefab,
                posicion,
                rotacion
            );

            // Desactivar las animaciones de aparicion
            // antes de que ejecuten Start.
            EfectoBloop[] efectos =
                objeto.GetComponentsInChildren<
                    EfectoBloop>(true);

            foreach (EfectoBloop efecto in efectos)
            {
                if (efecto != null)
                    efecto.enabled = false;
            }

            // Restaurar transformacion.
            objeto.transform.position = posicion;
            objeto.transform.rotation = rotacion;
            objeto.transform.localScale = escala;

            // Recuperar identidad sin generar un ID nuevo.
            EdificioConstruido edificio =
                objeto.GetComponent<EdificioConstruido>();

            if (edificio == null)
            {
                edificio =
                    objeto.AddComponent<EdificioConstruido>();
            }

            edificio.RestaurarIdentidadGuardada(
                datos.idPersistente,
                tipo,
                datos.variantePrefab,
                datos.curvaturaRampa
            );

            return edificio;
        }
        catch
        {
            // No dejar objetos incompletos.
            if (objeto != null)
            {
                objeto.SetActive(false);
                UnityEngine.Object.Destroy(objeto);
            }

            throw;
        }
    }

    // =========================================
    // BUSCAR PREFAB REAL
    // =========================================

    private static GameObject BuscarPrefab(
        SistemaConstruccion sistema,
        SistemaConstruccion.TipoEdificio tipo,
        int variante)
    {
        if (sistema.edificios == null)
        {
            throw new InvalidOperationException(
                "El catalogo de edificios no existe."
            );
        }

        GameObject encontrado = null;

        foreach (SistemaConstruccion.InfoEdificio info
            in sistema.edificios)
        {
            if (info.tipo != tipo)
                continue;

            if (info.prefabsReales == null ||
                variante >= info.prefabsReales.Length ||
                variante < 0 ||
                info.prefabsReales[variante] == null)
            {
                continue;
            }

            if (encontrado != null)
            {
                throw new InvalidOperationException(
                    "Hay varias entradas de catalogo para " +
                    tipo + " con variante " + variante
                );
            }

            encontrado = info.prefabsReales[variante];
        }

        if (encontrado == null)
        {
            throw new InvalidOperationException(
                "No existe el prefab real: " +
                tipo + " / variante " + variante
            );
        }

        return encontrado;
    }

    // =========================================
    // BUSCAR IDENTIDAD EXISTENTE
    // =========================================

    private static EdificioConstruido BuscarPorId(
        string id)
    {
        EdificioConstruido encontrado = null;

        Scene escena = SceneManager.GetActiveScene();

        EdificioConstruido[] edificios =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in edificios)
        {
            if (edificio == null ||
                edificio.gameObject.scene != escena)
            {
                continue;
            }

            if (edificio.IdPersistente != id)
                continue;

            if (encontrado != null)
            {
                throw new InvalidOperationException(
                    "Hay varios edificios con el ID " + id
                );
            }

            encontrado = edificio;
        }

        return encontrado;
    }

    // =========================================
    // COMPROBAR EDIFICIO YA EXISTENTE
    // =========================================

    private static void ComprobarExistente(
        EdificioConstruido edificio,
        PlacedBuildingSaveData datos,
        SistemaConstruccion.TipoEdificio tipo)
    {
        if (edificio.tipo != tipo ||
            edificio.VariantePrefab != datos.variantePrefab ||
            !Mathf.Approximately(
                edificio.CurvaturaRampa,
                datos.curvaturaRampa))
        {
            throw new InvalidOperationException(
                "El ID ya existe pero corresponde " +
                "a otro tipo, variante o curvatura."
            );
        }

        Vector3 posicion = new Vector3(
            datos.positionX,
            datos.positionY,
            datos.positionZ
        );

        Quaternion rotacion = new Quaternion(
            datos.rotationX,
            datos.rotationY,
            datos.rotationZ,
            datos.rotationW
        );

        Vector3 escala = new Vector3(
            datos.scaleX,
            datos.scaleY,
            datos.scaleZ
        );

        Transform t = edificio.transform;

        if (Vector3.Distance(t.position, posicion) > 0.001f ||
            Quaternion.Angle(t.rotation, rotacion) > 0.1f ||
            Vector3.Distance(t.localScale, escala) > 0.001f)
        {
            throw new InvalidOperationException(
                "El ID ya existe, pero su transformacion " +
                "no coincide con el guardado."
            );
        }
    }
}
