#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// F9 · Pruebas del guardado de la demo. Trabajan en una carpeta temporal:
/// NO tocan las partidas reales.
/// Menú: Tools > PiedrasIncr > Guardado > Probar guardado de la demo (F9)
/// </summary>
public static class PiedrasIncrDemoSaveTests
{
    [MenuItem("Tools/PiedrasIncr/Guardado/Probar guardado de la demo (F9)")]
    public static void Run()
    {
        string carpeta = Path.Combine(Path.GetTempPath(),
            "PiedrasIncr_DemoSaveTest_" + Guid.NewGuid().ToString("N"));

        try
        {
            SaveStorage storage = new SaveStorage(carpeta);

            // 1: los datos de la demo sobreviven a escribir y leer el archivo.
            GameSaveData partida = new GameSaveData();
            partida.economy.coins = 123;
            partida.demo = DatosDeEjemplo();
            partida.hasDemoData = true;
            DemoSaveAdapter.Validate(partida.demo);
            storage.Save(1, partida);

            GameSaveData leida;
            bool respaldo;
            Check(storage.TryLoad(1, out leida, out respaldo), "No se pudo leer la partida con datos de demo");
            Check(leida.formatVersion == GameSaveData.CurrentFormatVersion, "Versión de formato incorrecta");
            Check(leida.hasDemoData && leida.demo != null, "Se han perdido los datos de la demo");
            DemoSaveData d = leida.demo;
            Check(Mathf.Approximately(d.tanqueFraccion, 0.0044f), "Tanque mal guardado");
            Check(d.nivelFusil == 3 && d.nivelCapacidadTorbellino == 4, "Niveles de Fusil/Torbellino mal guardados");
            Check(d.lanzapiedras.hasData && d.lanzapiedras.construida && d.lanzapiedras.nivel == 2 &&
                  d.lanzapiedras.monedasAportadas == 10, "Lanzapiedras mal guardado");
            Check(d.armaLunar.hasData && !d.armaLunar.construida, "Arma Lunar mal guardada");
            Check(d.edificiosComprados.Count == 2 && d.edificiosComprados[0].tipo == "Rampa" &&
                  d.edificiosComprados[0].unidades == 3, "Cintas/paredes compradas mal guardadas");
            Check(d.hasFisionador && d.fisionadorDesbloqueado && d.esferasEnFisionador == 5 &&
                  d.misilesSinLanzar == 1, "Fisionador mal guardado");
            Check(d.hasFaseLunar && d.faseLunar == 1, "Fase lunar mal guardada");
            Check(d.hasPiedras && d.piedras.Count == 2, "Piedras mal guardadas");
            Check(Mathf.Approximately(d.piedras[1].desgaste, 1f) && d.piedras[1].esferaPremiada, "Desgaste de la piedra mal guardado");
            Check(Mathf.Approximately(d.piedras[0].positionY, 2.5f), "Posición de la piedra mal guardada");

            // 2: una partida del formato 1 (sin demo) se sigue cargando, con la demo por defecto.
            File.WriteAllText(storage.GetPrimaryPathForDiagnostics(2), SobreDeVersion1(2), new System.Text.UTF8Encoding(false));
            GameSaveData antigua;
            Check(storage.TryLoad(2, out antigua, out respaldo), "La partida antigua (formato 1) ya no se puede cargar");
            Check(antigua.formatVersion == GameSaveData.CurrentFormatVersion, "La partida antigua no se ha migrado");
            Check(!antigua.hasDemoData && antigua.demo == null, "La partida antigua no debería traer datos de demo");
            Check(antigua.economy.coins == 77, "La partida antigua ha perdido sus monedas");

            // 3: una versión futura no se carga.
            GameSaveData futura = new GameSaveData { formatVersion = GameSaveData.CurrentFormatVersion + 1 };
            Check(!GameSaveData.Migrar(futura), "Se aceptaría una partida de una versión futura");

            // 4: la validación rechaza datos rotos.
            CheckRechaza(x => x.tanqueFraccion = 1.5f, "tanque > 100 %");
            CheckRechaza(x => x.nivelFusil = 6, "Fusil nivel 6");
            CheckRechaza(x => x.lanzapiedras.nivel = 4, "Lanzapiedras nivel 4");
            CheckRechaza(x => x.piedras[0].desgaste = -0.1f, "desgaste negativo");
            CheckRechaza(x => x.piedras[0].scaleX = 0f, "piedra de escala 0");
            CheckRechaza(x => x.piedras[0].positionX = float.NaN, "posición NaN");
            CheckRechaza(x => x.misilesSinLanzar = DemoSaveAdapter.MaximoMisiles + 1, "demasiados misiles");
            CheckRechaza(x => x.edificiosComprados[0].unidades = -1, "compras negativas");

            // 5: los fragmentos del suelo se pagan al guardar (sin gestor en escena: 0 monedas).
            GameSaveData conFragmentos = new GameSaveData();
            conFragmentos.economy.coins = 10;
            conFragmentos.demo = DatosDeEjemplo();
            conFragmentos.hasDemoData = true;
            if (UnityEngine.Object.FindAnyObjectByType<GestorFragmentos>() == null)
            {
                DemoSaveAdapter.LiquidarFragmentos(conFragmentos);
                Check(conFragmentos.economy.coins == 10, "Sin fragmentos no deberían sumarse monedas");
            }

            Debug.Log("[GUARDADO] F9 PRUEBAS SUPERADAS: datos de la demo, partidas antiguas, versiones futuras y validación.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GUARDADO] F9 PRUEBA FALLIDA: " + ex);
            throw;
        }
        finally
        {
            if (Directory.Exists(carpeta))
                Directory.Delete(carpeta, true);
        }
    }

    private static DemoSaveData DatosDeEjemplo()
    {
        DemoSaveData d = new DemoSaveData
        {
            tanqueFraccion = 0.0044f,
            nivelFusil = 3,
            nivelCapacidadTorbellino = 4,
            lanzapiedras = new DemoConstruccionSaveData { hasData = true, construida = true, nivel = 2, monedasAportadas = 10 },
            armaLunar = new DemoConstruccionSaveData { hasData = true, construida = false, nivel = 1, monedasAportadas = 40 },
            hasFisionador = true,
            fisionadorDesbloqueado = true,
            esferasEnFisionador = 5,
            misilesSinLanzar = 1,
            hasFaseLunar = true,
            faseLunar = 1,
            hasPiedras = true
        };
        d.edificiosComprados.Add(new BuildingInventoryItemSaveData { tipo = "Rampa", unidades = 3 });
        d.edificiosComprados.Add(new BuildingInventoryItemSaveData { tipo = "Pared", unidades = 6 });
        d.piedras.Add(Piedra(1f, 2.5f, 3f, 0.4f, false));
        d.piedras.Add(Piedra(-4f, 1f, 8f, 1f, true));
        return d;
    }

    private static DemoPiedraSaveData Piedra(float x, float y, float z, float desgaste, bool premiada)
    {
        return new DemoPiedraSaveData
        {
            positionX = x, positionY = y, positionZ = z,
            rotationW = 1f,
            scaleX = 1f, scaleY = 1f, scaleZ = 1f,
            desgaste = desgaste,
            esferaPremiada = premiada
        };
    }

    // Archivo tal y como lo escribía el formato 1 (sin los campos de la demo).
    private static string SobreDeVersion1(int ranura)
    {
        string payload = "{\"formatVersion\":1,\"slotIndex\":" + ranura +
                         ",\"saveId\":\"antigua\",\"createdUtc\":\"2026-11-01T00:00:00Z\",\"savedUtc\":\"2026-11-01T00:00:00Z\"" +
                         ",\"totalPlaySeconds\":60.0,\"sceneName\":\"Piedra\",\"economy\":{\"coins\":77}}";

        string hash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            hash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        Sobre sobre = new Sobre { envelopeVersion = 1, payload = payload, sha256 = hash };
        return JsonUtility.ToJson(sobre);
    }

    [Serializable]
    private class Sobre
    {
        public int envelopeVersion;
        public string payload;
        public string sha256;
    }

    private static void CheckRechaza(Action<DemoSaveData> estropear, string caso)
    {
        DemoSaveData d = DatosDeEjemplo();
        estropear(d);
        try
        {
            DemoSaveAdapter.Validate(d);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new Exception("La validación acepta datos rotos: " + caso);
    }

    private static void Check(bool condicion, string descripcion)
    {
        if (!condicion)
            throw new Exception(descripcion);
    }
}
#endif
