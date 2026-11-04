
using System;
using UnityEngine;

/// <summary>
/// Captura, valida, lee y restaura el progreso
/// permanente de la procesadora.
/// No realiza compras ni modifica la cartera.
/// </summary>
public static class ProcessorSaveAdapter
{
    // ==========================================
    // CAPTURAR
    // ==========================================

    public static void Capture(
        MaquinaErosion maquina,
        DesbloqueoProcesadoraMonedas construccion,
        GameSaveData save)
    {
        ComprobarReferencias(maquina, construccion);

        if (save == null)
            throw new ArgumentNullException(nameof(save));

        ProcessorSaveData datos = new ProcessorSaveData
        {
            desbloqueada = maquina.ProcesadoraDesbloqueada,
            nivelProcesado = maquina.NivelProcesado,

            monedasConstruccionAportadas =
                construccion.MonedasRecibidas,

            monedasConstruccionNecesarias =
                construccion.MonedasNecesarias,

            monedasMejoraAportadas =
                construccion.MonedasMejoraRecibidas,

            precioSiguienteMejora =
                construccion.PrecioMejoraActual
        };

        Validate(datos);

        save.processor = datos;
        save.hasProcessorData = true;
    }

    // ==========================================
    // LEER
    // ==========================================

    public static ProcessorSaveData Read(GameSaveData save)
    {
        if (save == null)
            throw new ArgumentNullException(nameof(save));

        if (!save.hasProcessorData || save.processor == null)
        {
            throw new InvalidOperationException(
                "Esta partida no contiene datos de la procesadora."
            );
        }

        Validate(save.processor);
        return save.processor;
    }

    // ==========================================
    // RESTAURAR
    // ==========================================

    public static void Restore(
        MaquinaErosion maquina,
        DesbloqueoProcesadoraMonedas construccion,
        GameSaveData save)
    {
        ComprobarReferencias(maquina, construccion);

        // Leer y validar antes de modificar la escena.
        ProcessorSaveData datos = Read(save);

        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La restauracion requiere estar en Play."
            );
        }

        if (!construccion.isActiveAndEnabled)
        {
            throw new InvalidOperationException(
                "El componente de construccion no esta activo."
            );
        }

        // Verificar que los precios actuales coinciden
        // con los utilizados en la partida guardada.
        if (construccion.MonedasNecesarias !=
            datos.monedasConstruccionNecesarias)
        {
            throw new InvalidOperationException(
                "El precio de construccion ha cambiado."
            );
        }

        int precioEsperado = 0;

        if (datos.desbloqueada)
        {
            if (datos.nivelProcesado == 1)
                precioEsperado = construccion.precioNivel2;

            else if (datos.nivelProcesado == 2)
                precioEsperado = construccion.precioNivel3;
        }

        if (precioEsperado != datos.precioSiguienteMejora)
        {
            throw new InvalidOperationException(
                "El precio de la mejora ha cambiado."
            );
        }

        // Guardar el estado anterior para intentar
        // recuperarlo si falla la restauracion.
        GameSaveData anterior = new GameSaveData();

        Capture(maquina, construccion, anterior);

        ProcessorSaveData estadoAnterior = anterior.processor;

        try
        {
            // 1. Restaurar el nivel y desbloqueo.
            maquina.RestaurarEstadoGuardado(
                datos.desbloqueada,
                datos.nivelProcesado
            );

            // 2. Restaurar pagos, piezas y contadores.
            construccion.RestaurarPagosYAspecto(datos);

            // 3. Verificar el estado final.
            GameSaveData comprobacion = new GameSaveData();

            Capture(maquina, construccion, comprobacion);

            ProcessorSaveData resultado = comprobacion.processor;

            if (!Coinciden(datos, resultado))
            {
                throw new InvalidOperationException(
                    "La procesadora no ha recuperado todos los datos."
                );
            }
        }
        catch
        {
            // Recuperacion de emergencia.
            // Se intenta devolver la escena a su estado anterior.
            try
            {
                maquina.RestaurarEstadoGuardado(
                    estadoAnterior.desbloqueada,
                    estadoAnterior.nivelProcesado
                );

                construccion.RestaurarPagosYAspecto(
                    estadoAnterior
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[GUARDADO] No se ha podido recuperar " +
                    "completamente el estado anterior de la procesadora."
                );

                Debug.LogException(ex);
            }

            throw;
        }
    }

    // ==========================================
    // COMPARAR DATOS
    // ==========================================

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

    // ==========================================
    // COMPROBAR REFERENCIAS
    // ==========================================

    private static void ComprobarReferencias(
        MaquinaErosion maquina,
        DesbloqueoProcesadoraMonedas construccion)
    {
        if (maquina == null)
            throw new ArgumentNullException(nameof(maquina));

        if (construccion == null)
            throw new ArgumentNullException(nameof(construccion));

        if (construccion.maquinaErosion != maquina)
        {
            throw new InvalidOperationException(
                "Los componentes pertenecen a procesadoras diferentes."
            );
        }
    }

    // ==========================================
    // VALIDAR DATOS
    // ==========================================

    public static void Validate(ProcessorSaveData datos)
    {
        if (datos == null)
        {
            throw new InvalidOperationException(
                "No existen datos de la procesadora."
            );
        }

        if (datos.nivelProcesado < 1 ||
            datos.nivelProcesado > 3)
        {
            throw new InvalidOperationException(
                "Nivel de procesadora fuera de rango."
            );
        }

        if (!datos.desbloqueada &&
            datos.nivelProcesado != 1)
        {
            throw new InvalidOperationException(
                "Una procesadora bloqueada no puede tener mejoras."
            );
        }

        if (datos.monedasConstruccionNecesarias < 1)
        {
            throw new InvalidOperationException(
                "Precio de construccion invalido."
            );
        }

        if (datos.monedasConstruccionAportadas < 0 ||
            datos.monedasConstruccionAportadas >
            datos.monedasConstruccionNecesarias)
        {
            throw new InvalidOperationException(
                "Progreso de construccion invalido."
            );
        }

        if (datos.desbloqueada &&
            datos.monedasConstruccionAportadas !=
            datos.monedasConstruccionNecesarias)
        {
            throw new InvalidOperationException(
                "La procesadora construida no tiene el pago completo."
            );
        }

        if (!datos.desbloqueada &&
            datos.monedasConstruccionAportadas >=
            datos.monedasConstruccionNecesarias)
        {
            throw new InvalidOperationException(
                "La procesadora esta pagada pero no construida."
            );
        }

        if (datos.monedasMejoraAportadas < 0 ||
            datos.precioSiguienteMejora < 0)
        {
            throw new InvalidOperationException(
                "Progreso de mejora negativo."
            );
        }

        if (!datos.desbloqueada ||
            datos.nivelProcesado == 3)
        {
            if (datos.monedasMejoraAportadas != 0 ||
                datos.precioSiguienteMejora != 0)
            {
                throw new InvalidOperationException(
                    "Existen pagos para una mejora no disponible."
                );
            }
        }
        else
        {
            if (datos.precioSiguienteMejora < 1 ||
                datos.monedasMejoraAportadas >=
                datos.precioSiguienteMejora)
            {
                throw new InvalidOperationException(
                    "Progreso de mejora invalido."
                );
            }
        }
    }
}
