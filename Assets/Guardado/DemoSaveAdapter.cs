using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Componentes de la demo que guarda F9. Todos son opcionales: si alguno no está en la
/// escena, esa parte simplemente no se guarda ni se restaura.
/// </summary>
public sealed class DemoSceneReferences
{
    public ContadorGasolinera tanque;
    public ArmaLanzadora fusil;
    public HerramientaTorbellino torbellino;
    public ConstruccionDianaBloopMonedas lanzapiedras;
    public ConstruccionAmetralladoraMonedas armaLunar;
    public InventarioEdificios inventario;
    public Fisionador fisionador;
    public GestorFasesLunares luna;
    public ControladorFinDemo finDemo;
    public LluviaMeteoritos lluvia;
    public List<ConstruccionConMonedas> construccionesConMonedas = new List<ConstruccionConMonedas>();

    public static DemoSceneReferences Buscar(InventarioEdificios inventario, HerramientaTorbellino torbellino)
    {
        return new DemoSceneReferences
        {
            tanque = Primero<ContadorGasolinera>(),
            fusil = Primero<ArmaLanzadora>(),
            torbellino = torbellino != null ? torbellino : Primero<HerramientaTorbellino>(),
            lanzapiedras = Primero<ConstruccionDianaBloopMonedas>(),
            armaLunar = Primero<ConstruccionAmetralladoraMonedas>(),
            inventario = inventario != null ? inventario : Primero<InventarioEdificios>(),
            fisionador = Primero<Fisionador>(),
            luna = Primero<GestorFasesLunares>(),
            finDemo = Primero<ControladorFinDemo>(),
            lluvia = Primero<LluviaMeteoritos>(),
            construccionesConMonedas = Todos<ConstruccionConMonedas>()
        };
    }

    private static List<T> Todos<T>() where T : Component
    {
        List<T> lista = new List<T>();
        Scene escena = SceneManager.GetActiveScene();
        foreach (T componente in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (componente != null && componente.gameObject.scene == escena)
                lista.Add(componente);
        return lista;
    }

    private static T Primero<T>() where T : Component
    {
        Scene escena = SceneManager.GetActiveScene();
        foreach (T componente in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (componente != null && componente.gameObject.scene == escena)
                return componente;
        }
        return null;
    }
}

/// <summary>
/// F9 · Guardado de la demo: desgaste de cada piedra, tanque de gasolina, niveles de las mejoras
/// del Excel, cintas y paredes compradas, Fisionador, misiles sin lanzar, fase lunar y final.
///
/// - Capture no modifica el juego.
/// - Restore se ejecuta al final de la carga completa. No cobra, no anima y no lanza
///   OnCompraRealizada. Cada parte se restaura por separado: si una falla se avisa en la
///   consola y el resto sigue, para que un detalle de la demo nunca impida cargar la partida.
/// </summary>
public static class DemoSaveAdapter
{
    public const int MaximoPiedras = 2000;
    public const int MaximoMisiles = 10;
    public const int MaximoEdificiosComprados = 1000;

    // =====================================================
    // CAPTURA
    // =====================================================

    public static void Capture(DemoSceneReferences r, GameSaveData save)
    {
        if (r == null) throw new ArgumentNullException(nameof(r));
        if (save == null) throw new ArgumentNullException(nameof(save));

        DemoSaveData d = new DemoSaveData();

        if (r.tanque != null)
            d.tanqueFraccion = Mathf.Clamp01(r.tanque.FraccionActual);

        if (r.fusil != null)
            d.nivelFusil = r.fusil.NivelFusil;

        if (r.torbellino != null)
            d.nivelCapacidadTorbellino = r.torbellino.NivelCapacidad;

        if (r.lanzapiedras != null)
            d.lanzapiedras = CapturarConstruccion(
                r.lanzapiedras.EstaConstruida, r.lanzapiedras.NivelActual, r.lanzapiedras.MonedasAportadas);

        if (r.armaLunar != null)
            d.armaLunar = CapturarConstruccion(
                r.armaLunar.EstaConstruida, r.armaLunar.NivelActual, r.armaLunar.MonedasAportadas);

        if (r.inventario != null && r.inventario.articulos != null)
        {
            foreach (InventarioEdificios.Articulo articulo in r.inventario.articulos)
            {
                if (articulo == null) continue;
                d.edificiosComprados.Add(new BuildingInventoryItemSaveData
                {
                    tipo = articulo.tipo.ToString(),
                    unidades = articulo.CompradasTotal
                });
            }
        }

        if (r.fisionador != null)
        {
            d.hasFisionador = true;
            d.fisionadorDesbloqueado = r.fisionador.Desbloqueado;
            d.esferasEnFisionador = r.fisionador.EsferasActuales;
            d.fisionadorCompletado = r.fisionador.Completado;
            d.misilesSinLanzar = Mathf.Min(MaximoMisiles, r.fisionador.MisilesPendientes);
        }

        if (r.luna != null)
        {
            d.hasFaseLunar = true;
            d.faseLunar = r.luna.FaseActual;
        }

        if (r.construccionesConMonedas != null)
        {
            foreach (ConstruccionConMonedas c in r.construccionesConMonedas)
            {
                if (c == null || string.IsNullOrEmpty(c.IdGuardado)) continue;
                d.construccionesConMonedas.Add(new DemoConstruccionMonedasSaveData
                {
                    id = c.IdGuardado,
                    nivel = Mathf.Max(0, c.NivelActual),
                    monedasAportadas = Mathf.Max(0, c.MonedasAportadas)
                });
            }
        }

        d.demoCompletada = r.finDemo != null && r.finDemo.FinalEnCurso;

        d.hasPiedras = true;
        foreach (DesgastePiedra piedra in PiedrasDeJuego())
        {
            if (d.piedras.Count >= MaximoPiedras) break;

            Transform t = piedra.transform;
            Quaternion rotacion = t.rotation;
            d.piedras.Add(new DemoPiedraSaveData
            {
                positionX = t.position.x,
                positionY = t.position.y,
                positionZ = t.position.z,
                rotationX = rotacion.x,
                rotationY = rotacion.y,
                rotationZ = rotacion.z,
                rotationW = rotacion.w,
                scaleX = t.localScale.x,
                scaleY = t.localScale.y,
                scaleZ = t.localScale.z,
                desgaste = Mathf.Clamp01(piedra.Desgaste),
                esferaPremiada = piedra.EsferaPerfectaAlcanzada
            });
        }

        Validate(d);

        save.demo = d;
        save.hasDemoData = true;
    }

    private static DemoConstruccionSaveData CapturarConstruccion(bool construida, int nivel, int aportadas)
    {
        return new DemoConstruccionSaveData
        {
            hasData = true,
            construida = construida,
            nivel = construida ? Mathf.Clamp(nivel, 1, 3) : 1,
            monedasAportadas = Mathf.Max(0, aportadas)
        };
    }

    // Piedras normales de juego: tienen desgaste y no son de nivel 2.
    private static List<DesgastePiedra> PiedrasDeJuego()
    {
        List<DesgastePiedra> lista = new List<DesgastePiedra>();
        Scene escena = SceneManager.GetActiveScene();

        foreach (DesgastePiedra piedra in UnityEngine.Object.FindObjectsByType<DesgastePiedra>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (piedra == null || piedra.gameObject.scene != escena) continue;
            if (piedra.GetComponent<PiedraNivel2>() != null) continue;
            lista.Add(piedra);
        }
        return lista;
    }

    // =====================================================
    // VALIDACIÓN
    // =====================================================

    public static void Validate(DemoSaveData d)
    {
        if (d == null)
            throw new InvalidOperationException("Faltan los datos de la demo.");

        if (!Finito(d.tanqueFraccion) || d.tanqueFraccion < 0f || d.tanqueFraccion > 1f)
            throw new InvalidOperationException("Tanque de gasolina inválido.");

        if (d.nivelFusil < 0 || d.nivelFusil > 5 ||
            d.nivelCapacidadTorbellino < 0 || d.nivelCapacidadTorbellino > 5)
            throw new InvalidOperationException("Niveles del Fusil o del Torbellino inválidos.");

        ValidarConstruccion(d.lanzapiedras, "Lanzapiedras");
        ValidarConstruccion(d.armaLunar, "Arma Lunar");

        if (d.edificiosComprados == null)
            throw new InvalidOperationException("Faltan los edificios comprados.");
        foreach (BuildingInventoryItemSaveData item in d.edificiosComprados)
        {
            if (item == null || string.IsNullOrEmpty(item.tipo) ||
                item.unidades < 0 || item.unidades > MaximoEdificiosComprados)
                throw new InvalidOperationException("Edificios comprados inválidos.");
        }

        if (d.esferasEnFisionador < 0 || d.misilesSinLanzar < 0 || d.misilesSinLanzar > MaximoMisiles)
            throw new InvalidOperationException("Estado del Fisionador inválido.");

        if (d.faseLunar < 0)
            throw new InvalidOperationException("Fase lunar inválida.");

        if (d.monedasPorFragmentosEnSuelo < 0)
            throw new InvalidOperationException("Monedas de fragmentos inválidas.");

        // Partidas antiguas: JsonUtility puede dejar la lista a null.
        if (d.construccionesConMonedas == null)
            d.construccionesConMonedas = new List<DemoConstruccionMonedasSaveData>();
        foreach (DemoConstruccionMonedasSaveData c in d.construccionesConMonedas)
        {
            if (c == null || string.IsNullOrEmpty(c.id) || c.nivel < 0 || c.nivel > 10 || c.monedasAportadas < 0)
                throw new InvalidOperationException("Edificio construido con monedas guardado inválido.");
        }

        if (d.piedras == null || d.piedras.Count > MaximoPiedras)
            throw new InvalidOperationException("Lista de piedras inválida.");
        foreach (DemoPiedraSaveData p in d.piedras)
        {
            if (p == null ||
                !Finito(p.positionX) || !Finito(p.positionY) || !Finito(p.positionZ) ||
                !Finito(p.rotationX) || !Finito(p.rotationY) || !Finito(p.rotationZ) || !Finito(p.rotationW) ||
                !Finito(p.scaleX) || !Finito(p.scaleY) || !Finito(p.scaleZ) ||
                p.scaleX <= 0f || p.scaleY <= 0f || p.scaleZ <= 0f ||
                !Finito(p.desgaste) || p.desgaste < 0f || p.desgaste > 1f)
                throw new InvalidOperationException("Piedra guardada inválida.");
        }
    }

    private static void ValidarConstruccion(DemoConstruccionSaveData c, string nombre)
    {
        if (c == null || !c.hasData) return;
        if (c.nivel < 1 || c.nivel > 3 || c.monedasAportadas < 0)
            throw new InvalidOperationException(nombre + " guardado inválido.");
    }

    private static bool Finito(float valor)
    {
        return !float.IsNaN(valor) && !float.IsInfinity(valor);
    }

    // =====================================================
    // RESTAURACIÓN
    // =====================================================

    /// <summary>
    /// Restaura la demo. Partidas sin datos de demo (formato 1): no cambia nada.
    /// Devuelve cuántas partes fallaron (cada fallo queda en la consola).
    /// </summary>
    public static int Restore(DemoSceneReferences r, GameSaveData save)
    {
        if (r == null) throw new ArgumentNullException(nameof(r));
        if (save == null) throw new ArgumentNullException(nameof(save));
        if (!save.hasDemoData || save.demo == null) return 0;

        DemoSaveData d = save.demo;
        Validate(d);

        int fallos = 0;

        fallos += Intentar("tanque", () =>
        {
            if (r.tanque != null) r.tanque.Restaurar(d.tanqueFraccion);
        });

        fallos += Intentar("Fusil", () =>
        {
            if (r.fusil != null && d.nivelFusil > 0) r.fusil.RestaurarNivelFusil(d.nivelFusil);
        });

        fallos += Intentar("Torbellino", () =>
        {
            if (r.torbellino != null && d.nivelCapacidadTorbellino > 0)
                r.torbellino.RestaurarNivelCapacidad(d.nivelCapacidadTorbellino);
        });

        fallos += Intentar("Lanzapiedras", () =>
        {
            if (r.lanzapiedras != null && d.lanzapiedras != null && d.lanzapiedras.hasData)
                r.lanzapiedras.RestaurarEstado(d.lanzapiedras.construida, d.lanzapiedras.nivel, d.lanzapiedras.monedasAportadas);
        });

        fallos += Intentar("Arma Lunar", () =>
        {
            if (r.armaLunar != null && d.armaLunar != null && d.armaLunar.hasData)
                r.armaLunar.RestaurarEstado(d.armaLunar.construida, d.armaLunar.nivel, d.armaLunar.monedasAportadas);
        });

        fallos += Intentar("cintas y paredes compradas", () =>
        {
            if (r.inventario == null) return;
            foreach (BuildingInventoryItemSaveData item in d.edificiosComprados)
            {
                SistemaConstruccion.TipoEdificio tipo;
                if (Enum.TryParse(item.tipo, out tipo))
                    r.inventario.RestaurarCompradasTotal(tipo, item.unidades);
                else
                    Debug.LogWarning("[CARGA] Demo: tipo de edificio desconocido '" + item.tipo + "'.");
            }
        });

        fallos += Intentar("Fisionador", () =>
        {
            if (r.fisionador != null && d.hasFisionador)
                r.fisionador.RestaurarEstado(d.fisionadorDesbloqueado, d.esferasEnFisionador, d.fisionadorCompletado, d.misilesSinLanzar);
        });

        // Después del Fisionador: si su edificio estaba construido, lo deja desbloqueado.
        fallos += Intentar("edificios construidos con monedas", () =>
        {
            foreach (DemoConstruccionMonedasSaveData c in d.construccionesConMonedas)
                ConstruccionConMonedas.RestaurarPorId(c.id, c.nivel, c.monedasAportadas);
        });

        fallos += Intentar("piedras", () =>
        {
            if (d.hasPiedras) RecrearPiedras(r, d.piedras);
        });

        // La luna la última: si la partida estaba en la fase final, empieza a caer nivel 2.
        fallos += Intentar("fase lunar", () =>
        {
            if (r.luna != null && d.hasFaseLunar) r.luna.RestaurarFase(d.faseLunar);
        });

        Debug.Log("[CARGA] Demo restaurada: " + d.piedras.Count + " piedras, tanque " +
                  (d.tanqueFraccion * 100f).ToString("0.###") + " %, Fusil " + d.nivelFusil +
                  ", Torbellino " + d.nivelCapacidadTorbellino + ", fase lunar " + d.faseLunar +
                  (fallos > 0 ? " · " + fallos + " parte(s) con avisos" : ""));
        return fallos;
    }

    private static void RecrearPiedras(DemoSceneReferences r, List<DemoPiedraSaveData> piedras)
    {
        GameObject prefab = r.lluvia != null ? r.lluvia.PrefabPiedraBase : null;
        if (prefab == null)
            throw new InvalidOperationException(
                "No hay LluviaMeteoritos con Prefab Piedra: se dejan las piedras de la escena.");

        // Fuera las piedras con las que empieza la escena.
        foreach (DesgastePiedra existente in PiedrasDeJuego())
        {
            GameObject go = existente.gameObject;
            go.SetActive(false);
            UnityEngine.Object.Destroy(go);
        }

        foreach (DemoPiedraSaveData p in piedras)
        {
            Vector3 posicion = new Vector3(p.positionX, p.positionY, p.positionZ);
            Quaternion rotacion = new Quaternion(p.rotationX, p.rotationY, p.rotationZ, p.rotationW);
            if (rotacion.x * rotacion.x + rotacion.y * rotacion.y + rotacion.z * rotacion.z + rotacion.w * rotacion.w < 0.0001f)
                rotacion = Quaternion.identity;
            else
                rotacion = Quaternion.Normalize(rotacion);

            GameObject piedra = UnityEngine.Object.Instantiate(prefab, posicion, rotacion);
            piedra.name = prefab.name;
            piedra.transform.localScale = new Vector3(p.scaleX, p.scaleY, p.scaleZ);

            MeteoritoVisual visual = piedra.GetComponent<MeteoritoVisual>();
            if (visual != null) visual.MarcarComoAterrizado();

            Rigidbody rb = piedra.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // DeformacionPiedra crea DesgastePiedra en su Awake; por si el prefab no la lleva:
            DesgastePiedra desgaste = piedra.GetComponent<DesgastePiedra>();
            if (desgaste == null) desgaste = piedra.AddComponent<DesgastePiedra>();
            desgaste.Restaurar(p.desgaste, p.esferaPremiada);
        }
    }

    private static int Intentar(string parte, Action accion)
    {
        try
        {
            accion();
            return 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[CARGA] Demo: no se pudo restaurar " + parte + ": " + ex.Message);
            return 1;
        }
    }
}
