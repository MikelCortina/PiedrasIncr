
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrProcessorVisualTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar aspecto procesadora (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Entra en Play para probar el aspecto."
            );
            return;
        }

        var construcciones =
            UnityEngine.Object.FindObjectsByType<
                DesbloqueoProcesadoraMonedas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        var carteras =
            UnityEngine.Object.FindObjectsByType<Cartera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        DesbloqueoProcesadoraMonedas construccion = null;
        MaquinaErosion maquina = null;
        int parejas = 0;

        foreach (var candidata in construcciones)
        {
            if (candidata.maquinaErosion == null)
                continue;

            parejas++;
            construccion = candidata;
            maquina = candidata.maquinaErosion;
        }

        if (parejas != 1 || carteras.Length != 1 ||
            construccion == null || maquina == null)
        {
            Debug.LogError(
                "[GUARDADO] Seleccion automatica ambigua. " +
                "Procesadoras=" + parejas +
                ", carteras=" + carteras.Length
            );
            return;
        }

        Cartera cartera = carteras[0];
        int monedasIniciales = cartera.Monedas;

        GameSaveData original = new GameSaveData();

        try
        {
            ProcessorSaveAdapter.Capture(
                maquina, construccion, original
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] No se pudo capturar el estado inicial: " +
                ex.Message
            );
            return;
        }

        try
        {
            int costeConstruccion =
                construccion.MonedasNecesarias;

            int costeNivel2 =
                construccion.precioNivel2;

            int costeNivel3 =
                construccion.precioNivel3;

            // ESTADO 1: Construccion parcial
            ProbarEstado(
                maquina, construccion, cartera,
                monedasIniciales,
                false, 1,
                Mathf.Min(7, costeConstruccion - 1),
                0, 0
            );

            // ESTADO 2: Construida en nivel 1
            ProbarEstado(
                maquina, construccion, cartera,
                monedasIniciales,
                true, 1,
                costeConstruccion,
                Mathf.Min(4, costeNivel2 - 1),
                costeNivel2
            );

            // ESTADO 3: Nivel 2, mejora parcial
            ProbarEstado(
                maquina, construccion, cartera,
                monedasIniciales,
                true, 2,
                costeConstruccion,
                Mathf.Min(4, costeNivel3 - 1),
                costeNivel3
            );

            // ESTADO 4: Nivel maximo
            ProbarEstado(
                maquina, construccion, cartera,
                monedasIniciales,
                true, 3,
                costeConstruccion,
                0, 0
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] ASPECTO PROCESADORA FALLIDO: " + ex
            );
            return;
        }
        finally
        {
            try
            {
                ProcessorSaveAdapter.Restore(
                    maquina, construccion, original
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[GUARDADO] ERROR AL RECUPERAR " +
                    "EL ESTADO ORIGINAL: " + ex
                );
            }
        }

        try
        {
            GameSaveData final = new GameSaveData();

            ProcessorSaveAdapter.Capture(
                maquina, construccion, final
            );

            Comprobar(
                Coinciden(original.processor, final.processor),
                "No se ha recuperado el estado original."
            );

            Comprobar(
                cartera.Monedas == monedasIniciales,
                "Las monedas han cambiado."
            );

            Debug.Log(
                "[GUARDADO] ASPECTO PROCESADORA SUPERADO: " +
                "4 estados, piezas, contadores, " +
                "monedas intactas y estado original recuperado."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] VERIFICACION FINAL FALLIDA: " + ex
            );
        }
    }

    private static void ProbarEstado(
        MaquinaErosion maquina,
        DesbloqueoProcesadoraMonedas construccion,
        Cartera cartera,
        int monedasIniciales,
        bool desbloqueada,
        int nivel,
        int pagoConstruccion,
        int pagoMejora,
        int precioMejora)
    {
        ProcessorSaveData datos = new ProcessorSaveData
        {
            desbloqueada = desbloqueada,
            nivelProcesado = nivel,

            monedasConstruccionNecesarias =
                construccion.MonedasNecesarias,

            monedasConstruccionAportadas =
                pagoConstruccion,

            monedasMejoraAportadas =
                pagoMejora,

            precioSiguienteMejora =
                precioMejora
        };

        GameSaveData partida = new GameSaveData
        {
            hasProcessorData = true,
            processor = datos
        };

        ProcessorSaveAdapter.Restore(
            maquina, construccion, partida
        );

        // Revisar el estado principal.
        Comprobar(
            maquina.ProcesadoraDesbloqueada == desbloqueada,
            "Desbloqueo incorrecto."
        );

        Comprobar(
            maquina.NivelProcesado == nivel,
            "Nivel incorrecto."
        );

        // Ruinas y cuerpo principal.
        if (construccion.ruinasVisual != null)
        {
            Comprobar(
                construccion.ruinasVisual.activeSelf ==
                    !desbloqueada,
                "Visibilidad incorrecta de las ruinas."
            );
        }

        if (construccion.maquinaDesbloqueada != null)
        {
            Comprobar(
                construccion.maquinaDesbloqueada.activeSelf ==
                    desbloqueada,
                "Visibilidad incorrecta de la maquina."
            );
        }

        RevisarGrupo(
            construccion.partesRuinas,
            !desbloqueada,
            "Piezas de ruinas"
        );

        RevisarGrupo(
            construccion.piezasMaquinaBase,
            desbloqueada,
            "Piezas base"
        );

        if (construccion.piezasNivel2 != null)
        {
            RevisarGrupo(
                construccion.piezasNivel2.piezasNuevas,
                desbloqueada && nivel >= 2,
                "Piezas nuevas nivel 2"
            );

            if (desbloqueada)
            {
                RevisarGrupo(
                    construccion.piezasNivel2.piezasViejas,
                    nivel < 2,
                    "Piezas viejas nivel 2"
                );
            }
        }

        if (construccion.piezasNivel3 != null)
        {
            RevisarGrupo(
                construccion.piezasNivel3.piezasNuevas,
                desbloqueada && nivel >= 3,
                "Piezas nuevas nivel 3"
            );

            if (desbloqueada)
            {
                RevisarGrupo(
                    construccion.piezasNivel3.piezasViejas,
                    nivel < 3,
                    "Piezas viejas nivel 3"
                );
            }
        }

        // Texto de progreso.
        if (construccion.textoProgreso != null)
        {
            string esperado;

            if (!desbloqueada)
            {
                esperado =
                    "CONSTRUIR DESGASTADORA\n" +
                    pagoConstruccion + " / " +
                    construccion.MonedasNecesarias;
            }
            else if (nivel == 3)
            {
                esperado = "DESGASTADORA NIVEL MÁXIMO";
            }
            else
            {
                esperado =
                    "DESGASTADORA NIVEL " + nivel +
                    " → " + (nivel + 1) + "\n" +
                    pagoMejora + " / " + precioMejora;
            }

            Comprobar(
                construccion.textoProgreso.text == esperado,
                "Texto de progreso incorrecto en nivel " +
                nivel
            );
        }

        // Pantalla numerica de la antena.
        if (construccion.textoPantallaAntena != null)
        {
            int pendiente = !desbloqueada
                ? construccion.MonedasNecesarias -
                  pagoConstruccion
                : precioMejora - pagoMejora;

            string pantallaEsperada = nivel == 3
                ? "MAX"
                : pendiente.ToString(
                    construccion.formatoNumeros
                );

            Comprobar(
                construccion.textoPantallaAntena.text ==
                    pantallaEsperada,
                "Pantalla numerica incorrecta."
            );
        }

        Comprobar(
            cartera.Monedas == monedasIniciales,
            "Se han modificado las monedas."
        );

        // Comprobar tambien los datos persistentes.
        GameSaveData lectura = new GameSaveData();

        ProcessorSaveAdapter.Capture(
            maquina, construccion, lectura
        );

        Comprobar(
            Coinciden(datos, lectura.processor),
            "Los datos no coinciden tras restaurar."
        );

        Debug.Log(
            "[GUARDADO] VISUAL OK: desbloqueada=" +
            (desbloqueada ? "SI" : "NO") +
            ", nivel=" + nivel +
            ", construccion=" + pagoConstruccion +
            "/" + construccion.MonedasNecesarias +
            ", mejora=" + pagoMejora +
            "/" + precioMejora + "."
        );
    }

    private static void RevisarGrupo(
        GameObject[] piezas,
        bool debeEstarActivo,
        string nombre)
    {
        if (piezas == null)
            return;

        foreach (GameObject pieza in piezas)
        {
            if (pieza == null)
                continue;

            Comprobar(
                pieza.activeSelf == debeEstarActivo,
                nombre + ": estado incorrecto en " +
                pieza.name
            );
        }
    }

    private static bool Coinciden(
        ProcessorSaveData a,
        ProcessorSaveData b)
    {
        return
            a.desbloqueada == b.desbloqueada &&
            a.nivelProcesado == b.nivelProcesado &&
            a.monedasConstruccionAportadas ==
                b.monedasConstruccionAportadas &&
            a.monedasConstruccionNecesarias ==
                b.monedasConstruccionNecesarias &&
            a.monedasMejoraAportadas ==
                b.monedasMejoraAportadas &&
            a.precioSiguienteMejora ==
                b.precioSiguienteMejora;
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
