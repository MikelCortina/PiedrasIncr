
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mochila de piezas de construccion.
/// Comprar cuesta monedas; colocar consume una unidad.
/// La restauracion no realiza compras ni construcciones.
/// </summary>
public class InventarioEdificios : MonoBehaviour
{
    [Serializable]
    public class Articulo
    {
        public SistemaConstruccion.TipoEdificio tipo;

        [Min(1)] public int precioMonedas = 1;
        [Min(1)] public int maximoEnMochila = 5;

        [SerializeField, Min(0)]
        private int unidades = 0;

        public int Unidades => unidades;

        public void AjustarValores()
        {
            precioMonedas = Mathf.Max(1, precioMonedas);
            maximoEnMochila = Mathf.Max(1, maximoEnMochila);

            // No perder compras existentes al reducir el limite.
            unidades = Mathf.Max(0, unidades);
        }

        public bool Añadir()
        {
            if (unidades >= maximoEnMochila)
                return false;

            unidades++;
            return true;
        }

        public bool Sacar()
        {
            if (unidades <= 0)
                return false;

            unidades--;
            return true;
        }

        // NUEVO: asignacion directa para restaurar guardados.
        // No realiza compras ni aplica el limite de mochila.
        internal void RestaurarUnidadesSinCompra(int cantidad)
        {
            if (cantidad < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cantidad),
                    "Las unidades no pueden ser negativas."
                );
            }

            unidades = cantidad;
        }
    }

    [Header("Cartera del jugador")]
    public Cartera cartera;

    [Header("Pruebas / tráiler")]
    [Tooltip("Se puede colocar cualquier estructura sin haberla comprado y sin gastar unidades.")]
    public bool construccionInfinita = false;

    [Header("Precio y limite INDEPENDIENTE de cada edificio")]
    [Tooltip(
        "Por defecto todos cuestan 1 moneda " +
        "y caben 5 unidades de cada tipo."
    )]
    public List<Articulo> articulos = new List<Articulo>();

    public event Action InventarioCambiado;

    private void Reset()
    {
        CrearArticulosQueFaltan();
    }

    private void Awake()
    {
        CrearArticulosQueFaltan();

        if (cartera == null)
            cartera = GetComponentInParent<Cartera>();
    }

    private void OnValidate()
    {
        CrearArticulosQueFaltan();

        foreach (Articulo a in articulos)
        {
            if (a != null)
                a.AjustarValores();
        }
    }

    private void CrearArticulosQueFaltan()
    {
        if (articulos == null)
            articulos = new List<Articulo>();

        foreach (
            SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)
            )
        )
        {
            if (Obtener(tipo) != null)
                continue;

            articulos.Add(
                new Articulo
                {
                    tipo = tipo,
                    precioMonedas = 1,
                    maximoEnMochila = 5
                }
            );
        }
    }

    public Articulo Obtener(
        SistemaConstruccion.TipoEdificio tipo)
    {
        if (articulos == null)
            return null;

        foreach (Articulo a in articulos)
        {
            if (a != null && a.tipo == tipo)
                return a;
        }

        return null;
    }

    public bool TieneUnidad(
        SistemaConstruccion.TipoEdificio tipo)
    {
        if (construccionInfinita) return true;

        Articulo a = Obtener(tipo);

        return a != null && a.Unidades > 0;
    }

    public bool ConsumirUnidad(
        SistemaConstruccion.TipoEdificio tipo)
    {
        // Con construcción infinita colocar no gasta nada.
        if (construccionInfinita) return true;

        Articulo a = Obtener(tipo);

        if (a == null || !a.Sacar())
            return false;

        InventarioCambiado?.Invoke();

        return true;
    }

    public bool ComprarUnidad(
        SistemaConstruccion.TipoEdificio tipo,
        out string mensaje)
    {
        Articulo a = Obtener(tipo);

        if (a == null)
        {
            mensaje =
                "Este edificio no esta configurado en la mochila.";

            return false;
        }

        if (a.Unidades >= a.maximoEnMochila)
        {
            mensaje =
                tipo + ": mochila llena (" +
                a.Unidades + "/" +
                a.maximoEnMochila + ").";

            return false;
        }

        if (cartera == null)
        {
            mensaje =
                "Falta asignar la Cartera del Player " +
                "en InventarioEdificios.";

            return false;
        }

        if (!cartera.GastarMonedas(a.precioMonedas))
        {
            mensaje =
                "No tienes monedas suficientes. Cuesta " +
                a.precioMonedas + ".";

            return false;
        }

        a.Añadir();

        mensaje =
            "Comprado: " + tipo +
            " (" + a.Unidades +
            "/" + a.maximoEnMochila + ")";

        InventarioCambiado?.Invoke();

        return true;
    }

    // =====================================================
    // NUEVO: RESTAURAR INVENTARIO DESDE UN GUARDADO
    // =====================================================

    public void RestaurarUnidadesGuardadas(
        Dictionary<SistemaConstruccion.TipoEdificio, int>
            cantidades)
    {
        if (cantidades == null)
        {
            throw new ArgumentNullException(
                nameof(cantidades)
            );
        }

        if (articulos == null)
        {
            throw new InvalidOperationException(
                "No existe el catalogo de edificios."
            );
        }

        Array tipos = Enum.GetValues(
            typeof(SistemaConstruccion.TipoEdificio)
        );

        if (cantidades.Count != tipos.Length)
        {
            throw new InvalidOperationException(
                "El guardado no contiene todos los tipos " +
                "de edificios."
            );
        }

        // Comprobar la configuracion del inventario.
        HashSet<SistemaConstruccion.TipoEdificio> registrados =
            new HashSet<SistemaConstruccion.TipoEdificio>();

        foreach (Articulo articulo in articulos)
        {
            if (articulo == null)
            {
                throw new InvalidOperationException(
                    "Hay un articulo nulo en el inventario."
                );
            }

            if (!Enum.IsDefined(
                typeof(SistemaConstruccion.TipoEdificio),
                articulo.tipo))
            {
                throw new InvalidOperationException(
                    "El inventario contiene un tipo desconocido."
                );
            }

            if (!registrados.Add(articulo.tipo))
            {
                throw new InvalidOperationException(
                    "Hay tipos duplicados en el inventario: " +
                    articulo.tipo
                );
            }
        }

        // VALIDAR TODO ANTES DE MODIFICAR UNA SOLA UNIDAD.
        foreach (
            SistemaConstruccion.TipoEdificio tipo in tipos
        )
        {
            int unidades;

            if (!cantidades.TryGetValue(tipo, out unidades))
            {
                throw new InvalidOperationException(
                    "Faltan las unidades del tipo " + tipo
                );
            }

            if (unidades < 0)
            {
                throw new InvalidOperationException(
                    "Cantidad negativa para " + tipo
                );
            }

            if (Obtener(tipo) == null)
            {
                throw new InvalidOperationException(
                    "No existe el articulo " + tipo +
                    " en el inventario."
                );
            }
        }

        // APLICAR LOS VALORES EXACTOS DEL GUARDADO.
        // No se llama a ComprarUnidad ni a ConsumirUnidad.
        foreach (
            SistemaConstruccion.TipoEdificio tipo in tipos
        )
        {
            Articulo articulo = Obtener(tipo);

            articulo.RestaurarUnidadesSinCompra(
                cantidades[tipo]
            );
        }

        // Notificar una unica vez para actualizar la UI.
        InventarioCambiado?.Invoke();
    }
}
