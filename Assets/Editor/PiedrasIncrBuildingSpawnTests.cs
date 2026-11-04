
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingSpawnTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar reconstruccion edificio (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Entra en Play para ejecutar la prueba."
            );
            return;
        }

        EdificioConstruido temporal = null;

        try
        {
            SistemaConstruccion sistema =
                SistemaConstruccion.Instancia;

            Comprobar(
                sistema != null,
                "No se encuentra SistemaConstruccion."
            );

            InventarioEdificios inventario =
                sistema.inventarioEdificios;

            Comprobar(
                inventario != null,
                "No se encuentra InventarioEdificios."
            );

            Cartera cartera = inventario.cartera;

            if (cartera == null)
            {
                Cartera[] carteras =
                    UnityEngine.Object.FindObjectsByType<Cartera>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                Comprobar(
                    carteras.Length == 1,
                    "No se encuentra una Cartera unica."
                );

                cartera = carteras[0];
            }

            int monedasAntes = cartera.Monedas;

            Dictionary<SistemaConstruccion.TipoEdificio, int>
                mochilaAntes = LeerInventario(inventario);

            int cantidadAntes = ContarEdificios();

            // CAPTURAR LAS CONSTRUCCIONES REALES

            GameSaveData partida = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                partida
            );

            PlacedBuildingsSaveData datos =
                PlacedBuildingsSaveAdapter.Read(partida);

            // Elegir un edificio sin curvatura.
            PlacedBuildingSaveData referencia = null;

            foreach (PlacedBuildingSaveData edificio
                in datos.edificios)
            {
                if (Mathf.Approximately(
                    edificio.curvaturaRampa, 0f))
                {
                    referencia = edificio;
                    break;
                }
            }

            Comprobar(
                referencia != null,
                "Coloca al menos un edificio sin curvatura."
            );

            EdificioConstruido original =
                BuscarPorId(referencia.idPersistente);

            Comprobar(
                original != null,
                "No se encuentra el edificio original."
            );

            // ======================================
            // PRUEBA 1: NO DUPLICAR EL ORIGINAL
            // ======================================

            EdificioConstruido recuperado =
                PlacedBuildingsSpawnAdapter.CrearORecuperar(
                    sistema,
                    referencia
                );

            Comprobar(
                recuperado == original,
                "Se ha duplicado un edificio existente."
            );

            Comprobar(
                ContarEdificios() == cantidadAntes,
                "La consulta del edificio original " +
                "ha creado otra construccion."
            );

            Debug.Log(
                "[GUARDADO] EDIFICIO EXISTENTE OK: " +
                "ID conservado, sin duplicados."
            );

            // ======================================
            // PRUEBA 2: CREAR DESDE PREFAB GUARDADO
            // ======================================

            PlacedBuildingSaveData copia =
                JsonUtility.FromJson<PlacedBuildingSaveData>(
                    JsonUtility.ToJson(referencia)
                );

            // La copia de prueba necesita otro ID.
            copia.idPersistente =
                Guid.NewGuid().ToString("N");

            // Separarla del edificio original.
            copia.positionX += 5f;

            temporal =
                PlacedBuildingsSpawnAdapter.CrearORecuperar(
                    sistema,
                    copia
                );

            Comprobar(
                temporal != null && temporal != original,
                "No se ha creado el edificio temporal."
            );

            Comprobar(
                temporal.IdPersistente ==
                    copia.idPersistente,
                "El ID restaurado es incorrecto."
            );

            Comprobar(
                temporal.tipo.ToString() == copia.tipo,
                "El tipo restaurado es incorrecto."
            );

            Comprobar(
                temporal.VariantePrefab ==
                    copia.variantePrefab,
                "La variante restaurada es incorrecta."
            );

            Comprobar(
                Mathf.Approximately(
                    temporal.CurvaturaRampa,
                    copia.curvaturaRampa
                ),
                "La curvatura restaurada es incorrecta."
            );

            Vector3 posicionEsperada = new Vector3(
                copia.positionX,
                copia.positionY,
                copia.positionZ
            );

            Quaternion rotacionEsperada = new Quaternion(
                copia.rotationX,
                copia.rotationY,
                copia.rotationZ,
                copia.rotationW
            );

            Vector3 escalaEsperada = new Vector3(
                copia.scaleX,
                copia.scaleY,
                copia.scaleZ
            );

            Comprobar(
                Vector3.Distance(
                    temporal.transform.position,
                    posicionEsperada
                ) < 0.001f,
                "La posicion restaurada es incorrecta."
            );

            Comprobar(
                Quaternion.Angle(
                    temporal.transform.rotation,
                    rotacionEsperada
                ) < 0.1f,
                "La rotacion restaurada es incorrecta."
            );

            Comprobar(
                Vector3.Distance(
                    temporal.transform.localScale,
                    escalaEsperada
                ) < 0.001f,
                "La escala restaurada es incorrecta."
            );

            // El edificio restaurado no debe
            // reproducir la animacion de aparicion.
            EfectoBloop[] efectos =
                temporal.GetComponentsInChildren<
                    EfectoBloop>(true);

            foreach (EfectoBloop efecto in efectos)
            {
                Comprobar(
                    efecto == null ||
                    !efecto.isActiveAndEnabled,
                    "La reconstruccion ha activado EfectoBloop."
                );
            }

            Comprobar(
                ContarEdificios() == cantidadAntes + 1,
                "La cantidad de edificios es incorrecta."
            );

            Debug.Log(
                "[GUARDADO] PREFAB RESTAURADO OK: " +
                "tipo=" + copia.tipo +
                ", variante=" + copia.variantePrefab +
                ", ID=" + temporal.IdPersistente +
                ", posicion, rotacion y escala correctas."
            );

            // ======================================
            // PRUEBA 3: REPETIR LA RESTAURACION
            // ======================================

            EdificioConstruido segundaCarga =
                PlacedBuildingsSpawnAdapter.CrearORecuperar(
                    sistema,
                    copia
                );

            Comprobar(
                segundaCarga == temporal,
                "La segunda carga ha duplicado el edificio."
            );

            Comprobar(
                ContarEdificios() == cantidadAntes + 1,
                "Se ha creado un duplicado."
            );

            // ======================================
            // PRUEBA 4: ID CON DATOS DIFERENTES
            // ======================================

            PlacedBuildingSaveData datosIncompatibles =
                JsonUtility.FromJson<PlacedBuildingSaveData>(
                    JsonUtility.ToJson(copia)
                );

            datosIncompatibles.positionX += 1f;

            bool rechazado = false;

            try
            {
                PlacedBuildingsSpawnAdapter.CrearORecuperar(
                    sistema,
                    datosIncompatibles
                );
            }
            catch (InvalidOperationException)
            {
                rechazado = true;
            }

            Comprobar(
                rechazado,
                "Se ha aceptado un ID con otra posicion."
            );

            // ======================================
            // PRUEBA 5: ECONOMIA E INVENTARIO
            // ======================================

            Comprobar(
                cartera.Monedas == monedasAntes,
                "Se han modificado las monedas."
            );

            ComprobarInventario(
                inventario,
                mochilaAntes
            );

            Comprobar(
                original.IdPersistente ==
                    referencia.idPersistente,
                "Se ha modificado el edificio original."
            );

            Debug.Log(
                "[GUARDADO] RECONSTRUCCION EDIFICIO SUPERADA: " +
                "prefab real, ID original conservado, " +
                "transformacion correcta, sin duplicados, " +
                "datos incompatibles rechazados, " +
                "inventario y monedas intactos=" +
                monedasAntes + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] RECONSTRUCCION EDIFICIO FALLIDA: " +
                ex
            );
        }
        finally
        {
            // Desactivar inmediatamente y eliminar
            // el edificio temporal al terminar el frame.
            if (temporal != null)
            {
                temporal.gameObject.SetActive(false);

                UnityEngine.Object.Destroy(
                    temporal.gameObject
                );
            }
        }
    }

    private static EdificioConstruido BuscarPorId(
        string id)
    {
        Scene escena = SceneManager.GetActiveScene();

        EdificioConstruido[] edificios =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in edificios)
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena &&
                edificio.IdPersistente == id)
            {
                return edificio;
            }
        }

        return null;
    }

    private static int ContarEdificios()
    {
        Scene escena = SceneManager.GetActiveScene();
        int cantidad = 0;

        EdificioConstruido[] edificios =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in edificios)
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena)
            {
                cantidad++;
            }
        }

        return cantidad;
    }

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        LeerInventario(InventarioEdificios inventario)
    {
        var resultado = new Dictionary<
            SistemaConstruccion.TipoEdificio, int>();

        foreach (SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)))
        {
            InventarioEdificios.Articulo articulo =
                inventario.Obtener(tipo);

            Comprobar(
                articulo != null,
                "Falta el articulo " + tipo
            );

            resultado.Add(tipo, articulo.Unidades);
        }

        return resultado;
    }

    private static void ComprobarInventario(
        InventarioEdificios inventario,
        Dictionary<SistemaConstruccion.TipoEdificio, int>
            esperado)
    {
        foreach (var par in esperado)
        {
            InventarioEdificios.Articulo articulo =
                inventario.Obtener(par.Key);

            Comprobar(
                articulo != null &&
                articulo.Unidades == par.Value,
                "El inventario ha cambiado: " + par.Key
            );
        }
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}

#endif
