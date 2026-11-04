
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Captura, lee y valida edificios colocados
/// junto con sus conexiones.
///
/// No construye, destruye ni modifica edificios.
/// No modifica conexiones, monedas o inventario.
/// </summary>
public static class PlacedBuildingsSaveAdapter
{
    // ==========================================
    // CAPTURAR
    // ==========================================

    public static void Capture(
        SistemaConstruccion sistema,
        GameSaveData partida)
    {
        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        Scene escena = SceneManager.GetActiveScene();

        PlacedBuildingsSaveData datos =
            new PlacedBuildingsSaveData();

        EdificioConstruido[] edificios =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in edificios)
        {
            if (edificio == null ||
                edificio.gameObject.scene != escena)
            {
                continue;
            }

            // Excluir edificios en demolicion.
            if (edificio.GetComponent<
                EfectoBloopDestruccion>() != null)
            {
                continue;
            }

            // Ignorar EfectoBloop desactivados
            // que vienen dentro de algunos prefabs.
            EfectoBloop[] efectos =
                edificio.GetComponents<EfectoBloop>();

            foreach (EfectoBloop efecto in efectos)
            {
                if (efecto != null &&
                    efecto.isActiveAndEnabled)
                {
                    throw new InvalidOperationException(
                        "El edificio " + edificio.name +
                        " sigue en su animacion de construccion."
                    );
                }
            }

            if (!VarianteRegistrada(
                sistema,
                edificio.tipo,
                edificio.VariantePrefab))
            {
                throw new InvalidOperationException(
                    "Variante no encontrada: " +
                    edificio.tipo + " / " +
                    edificio.VariantePrefab
                );
            }

            Transform t = edificio.transform;

            Vector3 posicion = t.position;
            Quaternion rotacion = t.rotation;
            Vector3 escala = t.localScale;

            PlacedBuildingSaveData elemento =
                new PlacedBuildingSaveData
                {
                    idPersistente = edificio.IdPersistente,
                    tipo = edificio.tipo.ToString(),

                    variantePrefab = edificio.VariantePrefab,
                    curvaturaRampa = edificio.CurvaturaRampa,

                    positionX = posicion.x,
                    positionY = posicion.y,
                    positionZ = posicion.z,

                    rotationX = rotacion.x,
                    rotationY = rotacion.y,
                    rotationZ = rotacion.z,
                    rotationW = rotacion.w,

                    scaleX = escala.x,
                    scaleY = escala.y,
                    scaleZ = escala.z,

                    hasConnection = false,
                    connectedToBuildingId = null,
                    connectorPath = null,
                    connectorTag = null
                };

            // NUEVO: capturar el conector utilizado.
            CapturarConexion(
                edificio,
                elemento,
                escena
            );

            datos.edificios.Add(elemento);
        }

        // Orden estable por ID.
        datos.edificios.Sort(
            (a, b) => string.CompareOrdinal(
                a.idPersistente,
                b.idPersistente
            )
        );

        // Validar el guardado completo.
        Validate(datos);

        // Verificar que los propietarios de las conexiones
        // tambien forman parte de esta captura.
        ComprobarPropietarios(datos);

        // Solo modificar la partida tras validar todo.
        partida.placedBuildings = datos;
        partida.hasPlacedBuildingsData = true;
    }

    // ==========================================
    // CAPTURAR CONEXION
    // ==========================================

    private static void CapturarConexion(
        EdificioConstruido edificio,
        PlacedBuildingSaveData datos,
        Scene escena)
    {
        Collider conector = edificio.conectorUsado;

        // Edificio sin conexion.
        if (conector == null)
            return;

        // Localizar al propietario del conector.
        EdificioConstruido propietario =
            conector.GetComponentInParent<
                EdificioConstruido>(true);

        if (propietario == null)
        {
            throw new InvalidOperationException(
                "El edificio " + edificio.name +
                " utiliza un conector externo sin " +
                "EdificioConstruido. Su conexion " +
                "todavia no puede guardarse."
            );
        }

        if (propietario == edificio)
        {
            throw new InvalidOperationException(
                "Un edificio no puede conectarse a si mismo."
            );
        }

        if (propietario.gameObject.scene != escena)
        {
            throw new InvalidOperationException(
                "El conector pertenece a otra escena."
            );
        }

        if (propietario.GetComponent<
            EfectoBloopDestruccion>() != null)
        {
            throw new InvalidOperationException(
                "El propietario del conector " +
                "esta siendo demolido."
            );
        }

        Guid id;

        if (string.IsNullOrEmpty(
                propietario.IdPersistente) ||
            !Guid.TryParseExact(
                propietario.IdPersistente,
                "N",
                out id))
        {
            throw new InvalidOperationException(
                "El propietario del conector " +
                "no tiene un ID permanente valido."
            );
        }

        // Guardar la relacion entre ambos edificios.
        datos.hasConnection = true;

        datos.connectedToBuildingId =
            propietario.IdPersistente;

        datos.connectorPath = ObtenerRutaConector(
            conector.transform,
            propietario.transform
        );

        datos.connectorTag = conector.gameObject.tag;
    }

    // ==========================================
    // RUTA DEL CONECTOR
    // ==========================================

    private static string ObtenerRutaConector(
        Transform conector,
        Transform raiz)
    {
        List<string> partes = new List<string>();

        Transform actual = conector;

        while (actual != null && actual != raiz)
        {
            partes.Add(
                actual.name + "[" +
                actual.GetSiblingIndex() + "]"
            );

            actual = actual.parent;
        }

        if (actual != raiz)
        {
            throw new InvalidOperationException(
                "El conector no pertenece a la jerarquia " +
                "del edificio propietario."
            );
        }

        partes.Reverse();

        return partes.Count == 0
            ? "(raiz)"
            : string.Join("/", partes.ToArray());
    }

    // ==========================================
    // LEER
    // ==========================================

    public static PlacedBuildingsSaveData Read(
        GameSaveData partida)
    {
        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        if (!partida.hasPlacedBuildingsData ||
            partida.placedBuildings == null)
        {
            throw new InvalidOperationException(
                "La partida no contiene edificios colocados."
            );
        }

        Validate(partida.placedBuildings);

        return partida.placedBuildings;
    }

    // ==========================================
    // VALIDAR DATOS
    // ==========================================

    public static void Validate(
        PlacedBuildingsSaveData datos)
    {
        if (datos == null || datos.edificios == null)
        {
            throw new InvalidOperationException(
                "La lista de edificios no existe."
            );
        }

        HashSet<string> ids =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (PlacedBuildingSaveData edificio
            in datos.edificios)
        {
            if (edificio == null)
            {
                throw new InvalidOperationException(
                    "Existe un edificio nulo."
                );
            }

            // IDENTIDAD

            Guid guid;

            if (string.IsNullOrEmpty(
                    edificio.idPersistente) ||
                !Guid.TryParseExact(
                    edificio.idPersistente,
                    "N",
                    out guid))
            {
                throw new InvalidOperationException(
                    "Identificador de edificio invalido."
                );
            }

            if (!ids.Add(edificio.idPersistente))
            {
                throw new InvalidOperationException(
                    "ID duplicado: " +
                    edificio.idPersistente
                );
            }

            // TIPO

            SistemaConstruccion.TipoEdificio tipo;

            if (string.IsNullOrEmpty(edificio.tipo) ||
                !Enum.TryParse(
                    edificio.tipo,
                    false,
                    out tipo) ||
                !Enum.IsDefined(
                    typeof(SistemaConstruccion.TipoEdificio),
                    tipo) ||
                tipo.ToString() != edificio.tipo)
            {
                throw new InvalidOperationException(
                    "Tipo desconocido: " + edificio.tipo
                );
            }

            // VARIANTE

            if (edificio.variantePrefab < 0)
            {
                throw new InvalidOperationException(
                    "Variante negativa: " +
                    edificio.idPersistente
                );
            }

            // CURVATURA

            ComprobarNumero(
                edificio.curvaturaRampa,
                "curvatura"
            );

            if (tipo !=
                    SistemaConstruccion.TipoEdificio.Rampa &&
                edificio.curvaturaRampa != 0f)
            {
                throw new InvalidOperationException(
                    "Un edificio que no es rampa " +
                    "tiene curvatura."
                );
            }

            // POSICION

            ComprobarNumero(edificio.positionX, "positionX");
            ComprobarNumero(edificio.positionY, "positionY");
            ComprobarNumero(edificio.positionZ, "positionZ");

            // ROTACION

            ComprobarNumero(edificio.rotationX, "rotationX");
            ComprobarNumero(edificio.rotationY, "rotationY");
            ComprobarNumero(edificio.rotationZ, "rotationZ");
            ComprobarNumero(edificio.rotationW, "rotationW");

            float magnitud =
                edificio.rotationX * edificio.rotationX +
                edificio.rotationY * edificio.rotationY +
                edificio.rotationZ * edificio.rotationZ +
                edificio.rotationW * edificio.rotationW;

            if (magnitud < 0.0001f)
            {
                throw new InvalidOperationException(
                    "Rotacion invalida: " +
                    edificio.idPersistente
                );
            }

            // ESCALA

            ComprobarNumero(edificio.scaleX, "scaleX");
            ComprobarNumero(edificio.scaleY, "scaleY");
            ComprobarNumero(edificio.scaleZ, "scaleZ");

            if (edificio.scaleX == 0f ||
                edificio.scaleY == 0f ||
                edificio.scaleZ == 0f)
            {
                throw new InvalidOperationException(
                    "Escala nula: " +
                    edificio.idPersistente
                );
            }

            // NUEVO: VALIDAR CONEXIONES

            ValidarConexion(edificio);
        }
    }

    // ==========================================
    // VALIDAR CONEXION
    // ==========================================

    private static void ValidarConexion(
        PlacedBuildingSaveData edificio)
    {
        if (!edificio.hasConnection)
        {
            // Guardados antiguos y edificios sueltos
            // pueden no tener datos de conexion.
            if (!string.IsNullOrEmpty(
                    edificio.connectedToBuildingId) ||
                !string.IsNullOrEmpty(
                    edificio.connectorPath) ||
                !string.IsNullOrEmpty(
                    edificio.connectorTag))
            {
                throw new InvalidOperationException(
                    "Hay datos de conexion sin " +
                    "hasConnection activado."
                );
            }

            return;
        }

        Guid guid;

        if (string.IsNullOrEmpty(
                edificio.connectedToBuildingId) ||
            !Guid.TryParseExact(
                edificio.connectedToBuildingId,
                "N",
                out guid))
        {
            throw new InvalidOperationException(
                "ID del edificio conectado invalido."
            );
        }

        if (edificio.connectedToBuildingId ==
            edificio.idPersistente)
        {
            throw new InvalidOperationException(
                "Un edificio intenta conectarse a si mismo."
            );
        }

        if (string.IsNullOrWhiteSpace(
                edificio.connectorPath))
        {
            throw new InvalidOperationException(
                "Falta la ruta del conector."
            );
        }

        if (string.IsNullOrWhiteSpace(
                edificio.connectorTag))
        {
            throw new InvalidOperationException(
                "Falta el tag del conector."
            );
        }

        // La existencia del conector fisico se verificara
        // al restaurar las conexiones en la escena.
    }

    // ==========================================
    // COMPROBAR PROPIETARIOS EN LA CAPTURA
    // ==========================================

    private static void ComprobarPropietarios(
        PlacedBuildingsSaveData datos)
    {
        HashSet<string> ids =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (PlacedBuildingSaveData edificio
            in datos.edificios)
        {
            ids.Add(edificio.idPersistente);
        }

        foreach (PlacedBuildingSaveData edificio
            in datos.edificios)
        {
            if (!edificio.hasConnection)
                continue;

            if (!ids.Contains(
                edificio.connectedToBuildingId))
            {
                throw new InvalidOperationException(
                    "El edificio " + edificio.idPersistente +
                    " esta conectado a otro edificio " +
                    "que no aparece en la captura: " +
                    edificio.connectedToBuildingId
                );
            }
        }
    }

    // ==========================================
    // COMPROBAR PREFAB
    // ==========================================

    private static bool VarianteRegistrada(
        SistemaConstruccion sistema,
        SistemaConstruccion.TipoEdificio tipo,
        int variante)
    {
        if (sistema.edificios == null ||
            variante < 0)
        {
            return false;
        }

        foreach (SistemaConstruccion.InfoEdificio info
            in sistema.edificios)
        {
            if (info.tipo != tipo)
                continue;

            if (info.prefabsReales == null ||
                variante >= info.prefabsReales.Length)
            {
                continue;
            }

            if (info.prefabsReales[variante] != null)
                return true;
        }

        return false;
    }

    // ==========================================
    // VALIDAR NUMEROS
    // ==========================================

    private static void ComprobarNumero(
        float valor,
        string campo)
    {
        if (float.IsNaN(valor) ||
            float.IsInfinity(valor))
        {
            throw new InvalidOperationException(
                "Valor numerico invalido: " + campo
            );
        }
    }
}
