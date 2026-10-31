
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrProcessorTests
{
    [MenuItem("Tools/PiedrasIncr/Guardado/Probar procesadora (aislado)")]
    public static void Run()
    {
        string carpeta = Path.Combine(
            Path.GetTempPath(),
            "PiedrasIncr_ProcessorTest_" +
            Guid.NewGuid().ToString("N")
        );

        try
        {
            SaveStorage storage = new SaveStorage(carpeta);

            // RANURA 1: procesadora sin construir.
            // El jugador ha aportado 7 de 20 monedas.
            GameSaveData partida1 = CrearPartida(
                100, false, 1, 7, 0
            );

            // RANURA 2: procesadora construida.
            // Nivel 1, con 4 monedas pagadas de 10.
            GameSaveData partida2 = CrearPartida(
                200, true, 1, 20, 4
            );

            // RANURA 3: procesadora al nivel maximo.
            GameSaveData partida3 = CrearPartida(
                300, true, 3, 20, 0
            );

            storage.Save(1, partida1);
            storage.Save(2, partida2);
            storage.Save(3, partida3);

            // Comprobar cada ranura independientemente.
            ComprobarRanura(
                storage, 1, 100, false, 1, 7, 0
            );

            ComprobarRanura(
                storage, 2, 200, true, 1, 20, 4
            );

            ComprobarRanura(
                storage, 3, 300, true, 3, 20, 0
            );

            // Los guardados antiguos no deben inventar
            // un estado para la procesadora.
            GameSaveData antiguo = new GameSaveData();

            storage.Save(1, antiguo);

            GameSaveData lecturaAntigua;
            bool respaldo;

            Comprobar(
                storage.TryLoad(
                    1, out lecturaAntigua, out respaldo
                ),
                "No se puede leer el guardado antiguo."
            );

            Comprobar(
                !lecturaAntigua.hasProcessorData,
                "El guardado antiguo ha inventado datos."
            );

            bool rechazado = false;

            try
            {
                ProcessorSaveAdapter.Read(lecturaAntigua);
            }
            catch (InvalidOperationException)
            {
                rechazado = true;
            }

            Comprobar(
                rechazado,
                "Se han aceptado datos inexistentes."
            );

            // Comprobar rechazo de datos incorrectos.
            ProbarDatosInvalidos();

            Debug.Log(
                "[GUARDADO] PROCESADORA SUPERADA: " +
                "3 ranuras, construccion parcial, " +
                "mejora parcial, nivel maximo, " +
                "monedas independientes y compatibilidad v1."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PROCESADORA FALLIDA: " + ex
            );

            throw;
        }
        finally
        {
            if (Directory.Exists(carpeta))
                Directory.Delete(carpeta, true);
        }
    }

    private static GameSaveData CrearPartida(
        int monedas,
        bool desbloqueada,
        int nivel,
        int aportadasConstruccion,
        int aportadasMejora)
    {
        GameObject objeto = new GameObject(
            "__PRUEBA_PROCESADORA__"
        );

        objeto.SetActive(false);
        objeto.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            MaquinaErosion maquina =
                objeto.AddComponent<MaquinaErosion>();

            DesbloqueoProcesadoraMonedas construccion =
                objeto.AddComponent<DesbloqueoProcesadoraMonedas>();

            construccion.maquinaErosion = maquina;

            // Configurar exclusivamente los componentes
            // temporales mediante SerializedObject.
            SerializedObject maquinaSO =
                new SerializedObject(maquina);

            maquinaSO.FindProperty(
                "procesadoraDesbloqueada"
            ).boolValue = desbloqueada;

            maquinaSO.FindProperty(
                "nivelProcesado"
            ).intValue = nivel;

            maquinaSO.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject construccionSO =
                new SerializedObject(construccion);

            construccionSO.FindProperty(
                "monedasRecibidas"
            ).intValue = aportadasConstruccion;

            construccionSO.FindProperty(
                "monedasMejoraRecibidas"
            ).intValue = aportadasMejora;

            construccionSO.FindProperty(
                "nivelDeMejoraEnPago"
            ).intValue = nivel + 1;

            construccionSO.ApplyModifiedPropertiesWithoutUndo();

            GameSaveData partida = new GameSaveData();

            partida.economy.coins = monedas;

            ProcessorSaveAdapter.Capture(
                maquina, construccion, partida
            );

            Comprobar(
                partida.economy.coins == monedas,
                "La captura ha alterado las monedas."
            );

            return partida;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(objeto);
        }
    }

    private static void ComprobarRanura(
        SaveStorage storage,
        int ranura,
        int monedas,
        bool desbloqueada,
        int nivel,
        int aportadasConstruccion,
        int aportadasMejora)
    {
        GameSaveData partida;
        bool desdeRespaldo;

        Comprobar(
            storage.TryLoad(
                ranura, out partida, out desdeRespaldo
            ),
            "No se puede leer la ranura " + ranura
        );

        Comprobar(
            !desdeRespaldo,
            "Respaldo inesperado en ranura " + ranura
        );

        ProcessorSaveData datos =
            ProcessorSaveAdapter.Read(partida);

        int precioEsperado = !desbloqueada ? 0 :
                             nivel == 1 ? 10 :
                             nivel == 2 ? 20 : 0;

        Comprobar(
            datos.desbloqueada == desbloqueada &&
            datos.nivelProcesado == nivel &&
            datos.monedasConstruccionAportadas ==
                aportadasConstruccion &&
            datos.monedasConstruccionNecesarias == 20 &&
            datos.monedasMejoraAportadas ==
                aportadasMejora &&
            datos.precioSiguienteMejora ==
                precioEsperado &&
            partida.economy.coins == monedas,
            "Datos incorrectos en ranura " + ranura
        );
    }

    private static void ProbarDatosInvalidos()
    {
        // No permitir una procesadora bloqueada
        // que aparezca en nivel 3.
        ProcessorSaveData datos = new ProcessorSaveData
        {
            desbloqueada = false,
            nivelProcesado = 3,
            monedasConstruccionAportadas = 7,
            monedasConstruccionNecesarias = 20,
            monedasMejoraAportadas = 0,
            precioSiguienteMejora = 0
        };

        bool rechazado = false;

        try
        {
            ProcessorSaveAdapter.Validate(datos);
        }
        catch (InvalidOperationException)
        {
            rechazado = true;
        }

        Comprobar(
            rechazado,
            "Se ha aceptado una maquina bloqueada en nivel 3."
        );

        // No permitir aportar mas monedas de las necesarias.
        datos.desbloqueada = false;
        datos.nivelProcesado = 1;
        datos.monedasConstruccionAportadas = 25;

        rechazado = false;

        try
        {
            ProcessorSaveAdapter.Validate(datos);
        }
        catch (InvalidOperationException)
        {
            rechazado = true;
        }

        Comprobar(
            rechazado,
            "Se ha aceptado un pago superior al precio."
        );
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
            throw new Exception(mensaje);
    }
}

#endif
