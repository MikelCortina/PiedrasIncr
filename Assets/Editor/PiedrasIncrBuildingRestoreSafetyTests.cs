
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingRestoreSafetyTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar seguridad restauracion edificios (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Entra en Play para realizar la prueba."
            );
            return;
        }

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
                cartera = inventario.GetComponentInParent<Cartera>();

            Comprobar(
                cartera != null,
                "No se encuentra la Cartera."
            );

            int monedasOriginales = cartera.Monedas;

            Dictionary<SistemaConstruccion.TipoEdificio, int>
                inventarioOriginal =
                    CapturarInventario(inventario);

            // ======================================
            // CAPTURAR LA ESCENA REAL
            // ======================================

            GameSaveData original = new GameSaveData
            {
                sceneName =
                    SceneManager.GetActiveScene().name
            };

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                original
            );

            int total =
                original.placedBuildings.edificios.Count;

            Comprobar(
                total >= 2,
                "Coloca al menos 2 edificios."
            );

            int indiceRampa = -1;

            for (int i = 0; i < total; i++)
            {
                if (original.placedBuildings.edificios[i].tipo ==
                    "Rampa")
                {
                    indiceRampa = i;
                    break;
                }
            }

            Comprobar(
                indiceRampa >= 0,
                "Coloca al menos una rampa para probar " +
                "la proteccion de curvaturas."
            );

            string escenaAntes =
                JsonUtility.ToJson(
                    original.placedBuildings
                );

            // ======================================
            // 1. PARTIDA IDENTICA A LA ESCENA
            // ======================================

            ProbarEscenario(
                sistema,
                Copiar(original),
                total, 0, 0, 0,
                "PARTIDA IDENTICA"
            );

            // ======================================
            // 2. PARTIDA SIN EDIFICIOS
            // ======================================

            GameSaveData vacia = Copiar(original);

            vacia.placedBuildings.edificios.Clear();

            ProbarEscenario(
                sistema,
                vacia,
                0, 0, total, 0,
                "PARTIDA VACIA"
            );

            // ======================================
            // 3. UN EDIFICIO SOBRANTE
            // ======================================

            GameSaveData sobrante = Copiar(original);

            sobrante.placedBuildings.edificios.RemoveAt(0);

            ProbarEscenario(
                sistema,
                sobrante,
                total - 1, 0, 1, 0,
                "EDIFICIO SOBRANTE"
            );

            // ======================================
            // 4. UN EDIFICIO PENDIENTE DE CREAR
            // ======================================

            GameSaveData pendiente = Copiar(original);

            PlacedBuildingSaveData nuevo =
                CopiarEdificio(
                    pendiente.placedBuildings.edificios[0]
                );

            nuevo.idPersistente =
                Guid.NewGuid().ToString("N");

            nuevo.positionX += 100f;

            pendiente.placedBuildings.edificios.Add(nuevo);

            ProbarEscenario(
                sistema,
                pendiente,
                total, 1, 0, 0,
                "EDIFICIO PENDIENTE"
            );

            // ======================================
            // 5. MISMO ID, DISTINTA POSICION
            // ======================================

            GameSaveData conflicto = Copiar(original);

            conflicto.placedBuildings.edificios[0]
                .positionX += 10f;

            ProbarEscenario(
                sistema,
                conflicto,
                total - 1, 0, 0, 1,
                "EDIFICIO CONFLICTIVO"
            );

            // ======================================
            // 6. RAMPA CON CURVATURA DIFERENTE
            // ======================================

            GameSaveData curvada = Copiar(original);

            PlacedBuildingSaveData rampa =
                curvada.placedBuildings.edificios[indiceRampa];

            rampa.curvaturaRampa =
                Mathf.Approximately(rampa.curvaturaRampa, 0f)
                    ? 1.25f
                    : rampa.curvaturaRampa + 1f;

            ProbarEscenario(
                sistema,
                curvada,
                total - 1, 0, 0, 1,
                "RAMPA CURVADA"
            );

            // ======================================
            // 7. SITUACION MIXTA
            // ======================================

            GameSaveData mixta = Copiar(original);

            // Un edificio sobra en la escena.
            mixta.placedBuildings.edificios.RemoveAt(0);

            // Otro presenta un conflicto.
            mixta.placedBuildings.edificios[0]
                .positionZ += 15f;

            // Otro debe reconstruirse.
            PlacedBuildingSaveData adicional =
                CopiarEdificio(
                    mixta.placedBuildings.edificios[0]
                );

            adicional.idPersistente =
                Guid.NewGuid().ToString("N");

            adicional.positionX += 150f;

            mixta.placedBuildings.edificios.Add(adicional);

            ProbarEscenario(
                sistema,
                mixta,
                total - 2, 1, 1, 1,
                "SITUACION MIXTA"
            );

            // ======================================
            // 8. PARTIDA DE OTRA ESCENA
            // ======================================

            GameSaveData otraEscena = Copiar(original);

            otraEscena.sceneName =
                "__ESCENA_INCORRECTA__";

            bool escenaRechazada = false;

            try
            {
                PlacedBuildingsRestoreSafety.Evaluar(
                    sistema,
                    otraEscena
                );
            }
            catch (InvalidOperationException)
            {
                escenaRechazada = true;
            }

            Comprobar(
                escenaRechazada,
                "Se ha aceptado una partida de otra escena."
            );

            Debug.Log(
                "[GUARDADO] SEGURIDAD OK: " +
                "partida de otra escena rechazada."
            );

            // ======================================
            // COMPROBAR QUE NADA HA CAMBIADO
            // ======================================

            GameSaveData final = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                final
            );

            string escenaDespues =
                JsonUtility.ToJson(
                    final.placedBuildings
                );

            Comprobar(
                escenaAntes == escenaDespues,
                "Las construcciones reales han cambiado."
            );

            Comprobar(
                cartera.Monedas == monedasOriginales,
                "Las monedas han cambiado."
            );

            ComprobarInventario(
                inventario,
                inventarioOriginal
            );

            Debug.Log(
                "[GUARDADO] SEGURIDAD RESTAURACION SUPERADA: " +
                "8 escenarios, bloqueos correctos, " +
                "rampas curvadas detectadas, " +
                "escena, inventario y monedas intactos=" +
                monedasOriginales + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] SEGURIDAD RESTAURACION FALLIDA: " +
                ex
            );
        }
    }

    // ======================================
    // PROBAR UN ESCENARIO
    // ======================================

    private static void ProbarEscenario(
        SistemaConstruccion sistema,
        GameSaveData partida,
        int coincidentes,
        int porCrear,
        int sobrantes,
        int conflictos,
        string nombre)
    {
        PlacedBuildingsRestoreSafetyReport informe =
            PlacedBuildingsRestoreSafety.Evaluar(
                sistema,
                partida
            );

        PlacedBuildingsRestorePlan plan = informe.plan;

        Comprobar(
            plan.coincidentes == coincidentes,
            nombre + ": coincidentes incorrectos."
        );

        Comprobar(
            plan.porCrear == porCrear,
            nombre + ": edificios por crear incorrectos."
        );

        Comprobar(
            plan.sobrantes == sobrantes,
            nombre + ": sobrantes incorrectos."
        );

        Comprobar(
            plan.conflictos == conflictos,
            nombre + ": conflictos incorrectos."
        );

        // Contar rampas curvadas del guardado ficticio.
        int curvadas = 0;

        foreach (PlacedBuildingSaveData edificio
            in partida.placedBuildings.edificios)
        {
            if (edificio.curvaturaRampa != 0f)
                curvadas++;
        }

        Comprobar(
            plan.rampasCurvadas == curvadas,
            nombre + ": rampas curvadas incorrectas."
        );

        // Cada situacion peligrosa debe generar
        // su correspondiente bloqueo.
        int bloqueosEsperados = 0;

        if (sobrantes > 0)
            bloqueosEsperados++;

        if (conflictos > 0)
            bloqueosEsperados++;

        if (curvadas > 0)
            bloqueosEsperados++;

        if (porCrear > 0)
            bloqueosEsperados++;

        Comprobar(
            informe.bloqueos.Count == bloqueosEsperados,
            nombre + ": cantidad incorrecta de bloqueos."
        );

        Comprobar(
            informe.RequiereTrabajoAdicional ==
                (bloqueosEsperados > 0),
            nombre + ": seguridad incorrecta."
        );

        Comprobar(
            informe.SinCambiosPendientes ==
                (porCrear == 0 &&
                 sobrantes == 0 &&
                 conflictos == 0),
            nombre + ": pendientes incorrectos."
        );

        // Verificar que el metodo de proteccion
        // permite o bloquea segun corresponda.
        bool bloqueado = false;

        try
        {
            PlacedBuildingsRestoreSafety
                .ExigirRestauracionSinCambios(
                    sistema,
                    partida
                );
        }
        catch (InvalidOperationException)
        {
            bloqueado = true;
        }

        Comprobar(
            bloqueado == (bloqueosEsperados > 0),
            nombre + ": la barrera de seguridad fallo."
        );

        Debug.Log(
            "[GUARDADO] SEGURIDAD OK " + nombre +
            ": " + informe.Resumen()
        );
    }

    // ======================================
    // COPIAS EN MEMORIA
    // ======================================

    private static GameSaveData Copiar(GameSaveData partida)
    {
        return JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(partida)
        );
    }

    private static PlacedBuildingSaveData CopiarEdificio(
        PlacedBuildingSaveData edificio)
    {
        return JsonUtility.FromJson<PlacedBuildingSaveData>(
            JsonUtility.ToJson(edificio)
        );
    }

    // ======================================
    // INVENTARIO
    // ======================================

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        CapturarInventario(InventarioEdificios inventario)
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
                "Inventario modificado: " + par.Key
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
