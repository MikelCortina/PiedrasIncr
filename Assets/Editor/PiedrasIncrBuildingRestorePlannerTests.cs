
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingRestorePlannerTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar plan restauracion edificios (Play)"
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
                cartera = inventario.GetComponentInParent<Cartera>();
            }

            Comprobar(
                cartera != null,
                "No se encuentra la Cartera."
            );

            int monedasOriginales = cartera.Monedas;

            Dictionary<SistemaConstruccion.TipoEdificio, int>
                inventarioOriginal =
                    CapturarInventario(inventario);

            // ==========================================
            // CAPTURAR LA ESCENA REAL
            // ==========================================

            GameSaveData original = new GameSaveData
            {
                sceneName =
                    SceneManager.GetActiveScene().name
            };

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                original
            );

            PlacedBuildingsSaveData datosOriginales =
                PlacedBuildingsSaveAdapter.Read(original);

            int total = datosOriginales.edificios.Count;

            Comprobar(
                total >= 2,
                "Coloca al menos 2 edificios antes de probar."
            );

            string jsonEscenaOriginal =
                JsonUtility.ToJson(datosOriginales);

            // ==========================================
            // PRUEBA 1: TODOS COINCIDEN
            // ==========================================

            GameSaveData iguales = Copiar(original);

            VerificarPlan(
                sistema,
                iguales,
                total, 0, 0, 0,
                "TODOS COINCIDEN"
            );

            // ==========================================
            // PRUEBA 2: PARTIDA SIN EDIFICIOS
            // ==========================================

            GameSaveData vacia = Copiar(original);

            vacia.placedBuildings.edificios.Clear();

            VerificarPlan(
                sistema,
                vacia,
                0, 0, total, 0,
                "PARTIDA VACIA"
            );

            // ==========================================
            // PRUEBA 3: UN EDIFICIO SOBRANTE
            // ==========================================

            GameSaveData sobrante = Copiar(original);

            sobrante.placedBuildings.edificios.RemoveAt(0);

            VerificarPlan(
                sistema,
                sobrante,
                total - 1, 0, 1, 0,
                "UN SOBRANTE"
            );

            // ==========================================
            // PRUEBA 4: UN EDIFICIO POR CREAR
            // ==========================================

            GameSaveData pendiente = Copiar(original);

            PlacedBuildingSaveData nuevo =
                CopiarEdificio(
                    pendiente.placedBuildings.edificios[0]
                );

            nuevo.idPersistente =
                Guid.NewGuid().ToString("N");

            // Solo cambiamos los datos ficticios.
            nuevo.positionX += 100f;

            pendiente.placedBuildings.edificios.Add(nuevo);

            VerificarPlan(
                sistema,
                pendiente,
                total, 1, 0, 0,
                "UN EDIFICIO POR CREAR"
            );

            // ==========================================
            // PRUEBA 5: MISMO ID, DISTINTA POSICION
            // ==========================================

            GameSaveData conflicto = Copiar(original);

            conflicto.placedBuildings.edificios[0]
                .positionX += 10f;

            VerificarPlan(
                sistema,
                conflicto,
                total - 1, 0, 0, 1,
                "UN CONFLICTO"
            );

            // ==========================================
            // PRUEBA 6: SITUACION MIXTA
            // ==========================================

            GameSaveData mixta = Copiar(original);

            // Un edificio de la escena ya no aparece.
            mixta.placedBuildings.edificios.RemoveAt(0);

            // Otro tiene una posicion incompatible.
            mixta.placedBuildings.edificios[0]
                .positionZ += 15f;

            // Y otro existe en el guardado,
            // pero todavia no esta en la escena.
            PlacedBuildingSaveData adicional =
                CopiarEdificio(
                    mixta.placedBuildings.edificios[0]
                );

            adicional.idPersistente =
                Guid.NewGuid().ToString("N");

            adicional.positionX += 150f;

            mixta.placedBuildings.edificios.Add(adicional);

            VerificarPlan(
                sistema,
                mixta,
                total - 2, 1, 1, 1,
                "SITUACION MIXTA"
            );

            // ==========================================
            // PRUEBA 7: ESCENA INCORRECTA
            // ==========================================

            GameSaveData escenaIncorrecta =
                Copiar(original);

            escenaIncorrecta.sceneName =
                "__ESCENA_INCORRECTA__";

            bool escenaRechazada = false;

            try
            {
                PlacedBuildingsRestorePlanner.Analizar(
                    sistema,
                    escenaIncorrecta
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

            // ==========================================
            // COMPROBAR QUE NADA HA CAMBIADO
            // ==========================================

            GameSaveData segundaCaptura =
                new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                segundaCaptura
            );

            string jsonEscenaFinal =
                JsonUtility.ToJson(
                    PlacedBuildingsSaveAdapter.Read(
                        segundaCaptura
                    )
                );

            Comprobar(
                jsonEscenaOriginal == jsonEscenaFinal,
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
                "[GUARDADO] PLAN RESTAURACION EDIFICIOS SUPERADO: " +
                "7 escenarios, coincidentes, pendientes, " +
                "sobrantes, conflictos y escena incorrecta; " +
                "edificios originales intactos, " +
                "inventario intacto, monedas=" +
                monedasOriginales + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PLAN RESTAURACION EDIFICIOS FALLIDO: " +
                ex
            );
        }
    }

    // ==========================================
    // VERIFICAR UN ESCENARIO
    // ==========================================

    private static void VerificarPlan(
        SistemaConstruccion sistema,
        GameSaveData partida,
        int coincidentes,
        int porCrear,
        int sobrantes,
        int conflictos,
        string descripcion)
    {
        PlacedBuildingsRestorePlan plan =
            PlacedBuildingsRestorePlanner.Analizar(
                sistema,
                partida
            );

        Comprobar(
            plan.coincidentes == coincidentes,
            descripcion + ": coincidentes incorrectos."
        );

        Comprobar(
            plan.porCrear == porCrear,
            descripcion + ": pendientes incorrectos."
        );

        Comprobar(
            plan.sobrantes == sobrantes,
            descripcion + ": sobrantes incorrectos."
        );

        Comprobar(
            plan.conflictos == conflictos,
            descripcion + ": conflictos incorrectos."
        );

        int guardados =
            partida.placedBuildings.edificios.Count;

        Comprobar(
            plan.TotalGuardados == guardados,
            descripcion + ": total guardado incorrecto."
        );

        int curvadas = 0;

        foreach (PlacedBuildingSaveData edificio
            in partida.placedBuildings.edificios)
        {
            if (edificio.curvaturaRampa != 0f)
                curvadas++;
        }

        Comprobar(
            plan.rampasCurvadas == curvadas,
            descripcion + ": rampas curvadas incorrectas."
        );

        Debug.Log(
            "[GUARDADO] PLAN OK " + descripcion +
            ": coincidentes=" + plan.coincidentes +
            ", porCrear=" + plan.porCrear +
            ", sobrantes=" + plan.sobrantes +
            ", conflictos=" + plan.conflictos +
            ", rampasCurvadas=" + plan.rampasCurvadas
        );
    }

    // ==========================================
    // COPIAS INDEPENDIENTES
    // ==========================================

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

    // ==========================================
    // INVENTARIO
    // ==========================================

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
