using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Se coloca en el GameObject de la zona trigger. Abre un panel con cinco
/// botones de compra SOLO si el jugador tiene el martillo desbloqueado.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TiendaEdificiosTrigger : MonoBehaviour
{
    [Serializable]
    public class FilaCompra
    {
        public SistemaConstruccion.TipoEdificio tipo;
        public Button botonComprar;
        [Tooltip("TMP que indica nombre, coste y unidades disponibles")]
        public TMP_Text textoBoton;
    }

    [Header("Juego: referencias")]
    public GestorEquipamiento gestorEquipamiento;
    public SistemaConstruccion sistemaConstruccion;
    public InventarioEdificios inventario;

    [Header("Interfaz")]
    [Tooltip("El aviso sale en el texto de interacción común (el de la MesaTrabajo).")]
    public string mensajeAviso = "Pulsa E para abrir la tienda de edificios";
    [Tooltip("Panel entero, oculto al iniciar. Debe estar dentro de un Canvas activo.")]
    public GameObject panelTienda;
    public TMP_Text textoResultado;
    public FilaCompra[] filas = new FilaCompra[]
    {
        new FilaCompra { tipo = SistemaConstruccion.TipoEdificio.Rampa },
        new FilaCompra { tipo = SistemaConstruccion.TipoEdificio.Pared },
        new FilaCompra { tipo = SistemaConstruccion.TipoEdificio.Libre },
        new FilaCompra { tipo = SistemaConstruccion.TipoEdificio.Procesadora },
        new FilaCompra { tipo = SistemaConstruccion.TipoEdificio.Ventilador }
    };

    [Header("Controles")]
    public KeyCode teclaAbrirCerrar = KeyCode.E;
    public KeyCode teclaCancelar = KeyCode.Escape;

    private readonly HashSet<Collider> collidersJugador = new HashSet<Collider>();
    private bool abierta;
    private bool avisoVisible;

    private bool JugadorEnZona => collidersJugador.Count > 0;

    private void Start()
    {
        if (sistemaConstruccion == null)
            sistemaConstruccion = FindFirstObjectByType<SistemaConstruccion>();
        if (inventario == null && sistemaConstruccion != null)
            inventario = sistemaConstruccion.inventarioEdificios;

        if (panelTienda != null) panelTienda.SetActive(false);

        if (filas != null)
        {
            foreach (FilaCompra fila in filas)
            {
                if (fila == null || fila.botonComprar == null) continue;
                SistemaConstruccion.TipoEdificio tipoLocal = fila.tipo;
                fila.botonComprar.onClick.AddListener(() => Comprar(tipoLocal));
            }
        }
        if (inventario != null)
            inventario.InventarioCambiado += ActualizarFilas;

        Collider zona = GetComponent<Collider>();
        if (!zona.isTrigger)
            Debug.LogWarning(name + ": activa 'Is Trigger' en el collider de la tienda.", this);
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;
        collidersJugador.Add(otro);
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!collidersJugador.Remove(otro)) return;
        if (!JugadorEnZona) CerrarTienda();
    }

    private bool EsJugador(Collider otro)
    {
        if (otro == null) return false;
        GestorEquipamiento gestorEncontrado = otro.GetComponentInParent<GestorEquipamiento>();
        if (gestorEquipamiento != null)
            return gestorEncontrado == gestorEquipamiento ||
                   otro.transform == gestorEquipamiento.transform ||
                   otro.transform.IsChildOf(gestorEquipamiento.transform) ||
                   gestorEquipamiento.transform.IsChildOf(otro.transform) ||
                   otro.CompareTag("Player");

        if (gestorEncontrado != null)
        {
            gestorEquipamiento = gestorEncontrado;
            return true;
        }
        return false;
    }

    private bool PuedeUsarTienda()
    {
        return JugadorEnZona && gestorEquipamiento != null &&
               gestorEquipamiento.MartilloDesbloqueado();
    }

    private void Update()
    {
        // Si pierde el desbloqueo o abandona la zona, no queda la UI colgada.
        if (!PuedeUsarTienda() && abierta) CerrarTienda();
        bool mostrarAviso = PuedeUsarTienda() && !abierta;
        if (mostrarAviso != avisoVisible)
        {
            avisoVisible = mostrarAviso;
            if (mostrarAviso) MenuTienda.MostrarAviso(this, mensajeAviso);
            else MenuTienda.OcultarAviso(this);
        }

        if (abierta)
        {
            if (Input.GetKeyDown(teclaCancelar) || Input.GetKeyDown(teclaAbrirCerrar))
                CerrarTienda();
        }
        else if (PuedeUsarTienda() && Input.GetKeyDown(teclaAbrirCerrar))
        {
            AbrirTienda();
        }
    }

    public void AbrirTienda()
    {
        if (!PuedeUsarTienda() || abierta || panelTienda == null || inventario == null)
            return;

        abierta = true;
        MenuTienda.Abrir();
        if (sistemaConstruccion != null) sistemaConstruccion.BloquearPorTienda(true);
        panelTienda.SetActive(true);
        if (textoResultado != null) textoResultado.text = "Compra unidades para colocarlas con el martillo.";
        ActualizarFilas();
    }

    public void CerrarTienda()
    {
        if (!abierta) return;
        abierta = false;
        if (panelTienda != null) panelTienda.SetActive(false);
        if (sistemaConstruccion != null) sistemaConstruccion.BloquearPorTienda(false);
        MenuTienda.Cerrar();
    }

    private void Comprar(SistemaConstruccion.TipoEdificio tipo)
    {
        if (!abierta || inventario == null || !PuedeUsarTienda()) return;
        if (!TipoPermitido(tipo)) return;
        bool exito = inventario.ComprarUnidad(tipo, out string mensaje);
        if (textoResultado != null) textoResultado.text = mensaje;
        if (!exito) Debug.Log("Tienda edificios: " + mensaje, this);
        ActualizarFilas();
    }

    private void ActualizarFilas()
    {
        if (inventario == null || filas == null) return;
        foreach (FilaCompra fila in filas)
        {
            if (fila == null) continue;

            // Demo: las filas de tipos recortados se ocultan.
            bool permitido = TipoPermitido(fila.tipo);
            if (fila.botonComprar != null && fila.botonComprar.gameObject.activeSelf != permitido)
                fila.botonComprar.gameObject.SetActive(permitido);
            if (!permitido) continue;

            InventarioEdificios.Articulo articulo = inventario.Obtener(fila.tipo);
            if (articulo == null)
            {
                if (fila.botonComprar != null) fila.botonComprar.interactable = false;
                if (fila.textoBoton != null) fila.textoBoton.text = fila.tipo + ": no configurado";
                continue;
            }

            bool lleno = articulo.Unidades >= articulo.maximoEnMochila;
            int precio = inventario.PrecioSiguiente(fila.tipo);
            bool agotado = precio < 0;
            if (fila.botonComprar != null) fila.botonComprar.interactable = !lleno && !agotado;
            if (fila.textoBoton != null)
            {
                string textoPrecio = agotado ? "AGOTADO" : precio + (precio == 1 ? " moneda" : " monedas");
                fila.textoBoton.text = fila.tipo + "  |  " + textoPrecio +
                    "  |  " + articulo.Unidades + "/" + articulo.maximoEnMochila +
                    (lleno ? "  (LLENO)" : "");
            }
        }
    }

    private bool TipoPermitido(SistemaConstruccion.TipoEdificio tipo)
    {
        return sistemaConstruccion == null || sistemaConstruccion.EsTipoPermitido(tipo);
    }

    private void OnDisable()
    {
        CerrarTienda();
        collidersJugador.Clear();
        avisoVisible = false;
        MenuTienda.OcultarAviso(this);
    }

    private void OnDestroy()
    {
        if (inventario != null)
            inventario.InventarioCambiado -= ActualizarFilas;
    }
}
