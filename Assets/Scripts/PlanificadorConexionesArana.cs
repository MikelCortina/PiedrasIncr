using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Calcula un camino de varias islas NavMesh conectadas mediante ConexionEscalada.
/// Devuelve SOLO el primer tramo de escalada. El bot vuelve a planificar tras
/// cruzarlo para aprovechar sus animaciones de subida y descenso existentes.
/// </summary>
public static class PlanificadorConexionesArana
{
    private struct Nodo
    {
        public Vector3 posicion;
        public ConexionEscalada conexion;
        public bool superior;
    }

    public static bool BuscarPrimeraConexion(
        Vector3 inicio,
        Vector3 destino,
        int areaMask,
        out ConexionEscalada conexionElegida,
        out bool subir,
        out Vector3 puntoEntrada,
        out NavMeshPath caminoHastaEntrada,
        int limiteConexiones = 32)
    {
        conexionElegida = null;
        subir = true;
        puntoEntrada = Vector3.zero;
        caminoHastaEntrada = new NavMeshPath();

        IReadOnlyList<ConexionEscalada> activas = ConexionEscalada.ConexionesActivas;
        if (activas == null || activas.Count == 0) return false;

        List<Nodo> nodos = new List<Nodo>();
        nodos.Add(new Nodo { posicion = inicio });          // 0: inicio
        nodos.Add(new Nodo { posicion = destino });         // 1: meta

        int incluidas = 0;
        foreach (ConexionEscalada conexion in activas)
        {
            if (incluidas >= Mathf.Max(1, limiteConexiones)) break;
            if (conexion == null || !conexion.isActiveAndEnabled) continue;

            NavMeshHit inferior, superior;
            if (!conexion.IntentarObtenerPuntosNavMesh(
                    areaMask, out inferior, out superior)) continue;

            nodos.Add(new Nodo
            {
                posicion = inferior.position,
                conexion = conexion,
                superior = false
            });
            nodos.Add(new Nodo
            {
                posicion = superior.position,
                conexion = conexion,
                superior = true
            });
            incluidas++;
        }

        int n = nodos.Count;
        if (n <= 2) return false;

        // Aristas de navegación normal (solo si el NavMesh las conecta).
        // Las demás aristas son las conexiones físicas de escalada.
        float[,] coste = new float[n, n];
        bool[,] enlaceEscalada = new bool[n, n];
        for (int a = 0; a < n; a++)
            for (int b = 0; b < n; b++)
                coste[a, b] = a == b ? 0f : float.PositiveInfinity;

        for (int a = 0; a < n; a++)
        {
            for (int b = 0; b < n; b++)
            {
                if (a == b) continue;
                NavMeshPath ruta = new NavMeshPath();
                if (!NavMesh.CalculatePath(nodos[a].posicion,
                    nodos[b].posicion, areaMask, ruta) ||
                    ruta.status != NavMeshPathStatus.PathComplete) continue;

                float distancia = 0f;
                Vector3[] esquinas = ruta.corners;
                for (int k = 1; k < esquinas.Length; k++)
                    distancia += Vector3.Distance(esquinas[k - 1], esquinas[k]);

                // Respetar el sentido de los NavMeshLink unidireccionales.
                if (esquinas.Length < 2)
                    distancia = Vector3.Distance(nodos[a].posicion, nodos[b].posicion);

                coste[a, b] = distancia;
            }
        }

        for (int a = 2; a < n; a += 2)
        {
            ConexionEscalada conexion = nodos[a].conexion;
            float longitud = Vector3.Distance(nodos[a].posicion, nodos[a + 1].posicion);
            // Una pequeña penalización evita que elija escalar si existe
            // una ruta de suelo prácticamente equivalente.
            float costeCruce = longitud + 0.75f;
            coste[a, a + 1] = costeCruce;
            enlaceEscalada[a, a + 1] = true;
            if (conexion.bidireccional && conexion.TieneDatosDescenso)
            {
                coste[a + 1, a] = costeCruce;
                enlaceEscalada[a + 1, a] = true;
            }
        }

        // Dijkstra: normalmente habrá pocos puntos de escalada por mapa.
        float[] distanciaMinima = new float[n];
        int[] previo = new int[n];
        bool[] visitado = new bool[n];
        for (int i = 0; i < n; i++)
        {
            distanciaMinima[i] = float.PositiveInfinity;
            previo[i] = -1;
        }
        distanciaMinima[0] = 0f;

        for (int vuelta = 0; vuelta < n; vuelta++)
        {
            int actual = -1;
            for (int i = 0; i < n; i++)
                if (!visitado[i] && (actual == -1 ||
                    distanciaMinima[i] < distanciaMinima[actual])) actual = i;

            if (actual < 0 || float.IsInfinity(distanciaMinima[actual])) break;
            if (actual == 1) break;
            visitado[actual] = true;

            for (int vecino = 0; vecino < n; vecino++)
            {
                if (visitado[vecino] || float.IsInfinity(coste[actual, vecino])) continue;
                float candidato = distanciaMinima[actual] + coste[actual, vecino];
                if (candidato < distanciaMinima[vecino])
                {
                    distanciaMinima[vecino] = candidato;
                    previo[vecino] = actual;
                }
            }
        }
        if (previo[1] < 0) return false;

        // Reconstruir el recorrido e identificar su PRIMER cruce de pared.
        List<int> caminoNodos = new List<int>();
        int cursor = 1;
        for (int seguridad = 0; cursor >= 0 && seguridad <= n; seguridad++)
        {
            caminoNodos.Add(cursor);
            if (cursor == 0) break;
            cursor = previo[cursor];
        }
        if (caminoNodos[caminoNodos.Count - 1] != 0) return false;
        caminoNodos.Reverse();

        for (int i = 1; i < caminoNodos.Count; i++)
        {
            int desde = caminoNodos[i - 1];
            int hasta = caminoNodos[i];
            if (!enlaceEscalada[desde, hasta]) continue;

            conexionElegida = nodos[desde].conexion;
            subir = !nodos[desde].superior;
            puntoEntrada = nodos[desde].posicion;
            if (NavMesh.CalculatePath(inicio, puntoEntrada,
                    areaMask, caminoHastaEntrada) &&
                caminoHastaEntrada.status == NavMeshPathStatus.PathComplete)
                return true;

            conexionElegida = null;
            return false;
        }
        return false;
    }
}
