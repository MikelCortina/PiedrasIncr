using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mochila de piezas de construccion. Cada tipo tiene su precio, capacidad
/// y unidades propias. Comprar cuesta monedas; colocar consume una unidad.
/// </summary>
public class InventarioEdificios : MonoBehaviour
{
    [Serializable]
    public class Articulo
    {
        public SistemaConstruccion.TipoEdificio tipo;
        [Min(1)] public int precioMonedas = 1;
        [Min(1)] public int maximoEnMochila = 5;
        [SerializeField, Min(0)] private int unidades = 0;

        public int Unidades => unidades;

        public void AjustarValores()
        {
            precioMonedas = Mathf.Max(1, precioMonedas);
            maximoEnMochila = Mathf.Max(1, maximoEnMochila);
            // No borrar compras existentes si se reduce el maximo desde el Inspector.
            unidades = Mathf.Max(0, unidades);
        }

        public bool Añadir()
        {
            if (unidades >= maximoEnMochila) return false;
            unidades++;
            return true;
        }

        public bool Sacar()
        {
            if (unidades <= 0) return false;
            unidades--;
            return true;
        }
    }

    [Header("Cartera del jugador")]
    public Cartera cartera;

    [Header("Precio y limite INDEPENDIENTE de cada edificio")]
    [Tooltip("Por defecto todos cuestan 1 moneda y caben 5 unidades de cada tipo.")]
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
            if (a != null) a.AjustarValores();
    }

    private void CrearArticulosQueFaltan()
    {
        if (articulos == null) articulos = new List<Articulo>();
        foreach (SistemaConstruccion.TipoEdificio tipo in Enum.GetValues(typeof(SistemaConstruccion.TipoEdificio)))
        {
            if (Obtener(tipo) != null) continue;
            articulos.Add(new Articulo { tipo = tipo, precioMonedas = 1, maximoEnMochila = 5 });
        }
    }

    public Articulo Obtener(SistemaConstruccion.TipoEdificio tipo)
    {
        if (articulos == null) return null;
        foreach (Articulo a in articulos)
            if (a != null && a.tipo == tipo) return a;
        return null;
    }

    public bool TieneUnidad(SistemaConstruccion.TipoEdificio tipo)
    {
        Articulo a = Obtener(tipo);
        return a != null && a.Unidades > 0;
    }

    public bool ConsumirUnidad(SistemaConstruccion.TipoEdificio tipo)
    {
        Articulo a = Obtener(tipo);
        if (a == null || !a.Sacar()) return false;
        InventarioCambiado?.Invoke();
        return true;
    }

    public bool ComprarUnidad(SistemaConstruccion.TipoEdificio tipo, out string mensaje)
    {
        Articulo a = Obtener(tipo);
        if (a == null)
        {
            mensaje = "Este edificio no esta configurado en la mochila.";
            return false;
        }

        if (a.Unidades >= a.maximoEnMochila)
        {
            mensaje = tipo + ": mochila llena (" + a.Unidades + "/" + a.maximoEnMochila + ").";
            return false;
        }

        if (cartera == null)
        {
            mensaje = "Falta asignar la Cartera del Player en InventarioEdificios.";
            return false;
        }

        // GastarMonedas devuelve false si el jugador no tiene saldo.
        // Solo se incrementa el inventario cuando la compra tuvo exito.
        if (!cartera.GastarMonedas(a.precioMonedas))
        {
            mensaje = "No tienes monedas suficientes. Cuesta " + a.precioMonedas + ".";
            return false;
        }

        a.Añadir();
        mensaje = "Comprado: " + tipo + " (" + a.Unidades + "/" + a.maximoEnMochila + ")";
        InventarioCambiado?.Invoke();
        return true;
    }
}
