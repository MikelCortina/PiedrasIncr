using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TODO lo que dice Botro y CUÁNDO lo dice (Resources/GuionBotro). Botro es el único que habla: el jugador no le responde.
/// Se edita en Tools > PiedrasIncr > Botro: qué dice y cuándo.
/// Cada "momento" tiene un disparador (cuándo) y unas frases (qué dice).
///  - PasoIntro: lo lanza SecuenciaIntro al empezar ese paso; la frase marcada como instrucción se queda en pantalla.
///  - El resto los lanza HitosIntermedio con BotroHabla.Lanzar(...).
/// </summary>
[CreateAssetMenu(fileName = "GuionBotro", menuName = "PiedrasIncr/Botro: qué dice y cuándo")]
public class GuionBotro : ScriptableObject
{
    public const string RutaResources = "GuionBotro";

    // Se guarda por NOMBRE en el archivo (campo 'cuando'): se pueden añadir valores en cualquier sitio.
    public enum Cuando
    {
        PasoIntro, TutorialTerminado, EsferaPerfecta, Compra, PiedraMetida, QuedanPocasPiedras, MinutoDePartida
    }

    // Qué enfoca la cámara mientras Botro dice una frase (solo en la intro). Se guarda por NOMBRE.
    public enum Enfoque
    {
        Nada, Generador, Botro, Trampilla, Manguera, PantallaGasolina, Piedra
    }

    [Serializable]
    public class Frase
    {
        [TextArea(2, 4)] public string texto;
        [Tooltip("Solo en la intro: se queda en pantalla hasta que acaba el paso.")]
        public bool esInstruccion;
        [Tooltip("Segundos de espera antes de decirla.")]
        [Min(0f)] public float retraso;
        [Tooltip("Segundos en pantalla si no se omite con G. 0 = automático según la longitud.")]
        [Min(0f)] public float duracion;
        [Tooltip("Solo en la intro: la cámara hace zoom a esto mientras la dice (nombre de GuionBotro.Enfoque).")]
        public string enfocar;

        public Enfoque Enfocar
        {
            get
            {
                Enfoque e;
                return !string.IsNullOrEmpty(enfocar) && Enum.TryParse(enfocar, out e) ? e : Enfoque.Nada;
            }
            set { enfocar = value == Enfoque.Nada ? "" : value.ToString(); }
        }

        public Frase() { }

        public Frase(string texto, bool esInstruccion = false, float retraso = 0f, float duracion = 0f)
        {
            this.texto = texto;
            this.esInstruccion = esInstruccion;
            this.retraso = retraso;
            this.duracion = duracion;
        }
    }

    [Serializable]
    public class Momento
    {
        [Tooltip("Identificador único (para 'solo una vez').")]
        public string id;
        public bool activo = true;
        [Tooltip("Nombre de GuionBotro.Cuando.")]
        public string cuando;
        [Tooltip("PasoIntro: nombre del paso (SecuenciaIntro.Paso).")]
        public string paso;
        [Tooltip("Compra: id de la mejora (EconomiaDemo.Ids). Vacío = cualquiera.")]
        public string compra;
        [Tooltip("Compra: nivel (0 = cualquiera). PiedraMetida: número de piedra. MinutoDePartida: minuto.")]
        public int numero;
        [Tooltip("Segundos de espera desde que ocurre hasta que Botro empieza a hablar.")]
        [Min(0f)] public float retraso;
        [Tooltip("Solo lo dice la primera vez que ocurre.")]
        public bool soloUnaVez = true;
        public List<Frase> frases = new List<Frase>();
        [Tooltip("Solo en la intro: si el jugador tarda, Botro repite la última instrucción cada estos segundos. 0 = no.")]
        [Min(0f)] public float repetirInstruccionCada;

        public Momento() { }

        public Momento(string id, Cuando cuando, string paso, string compra, int numero, float retraso, bool soloUnaVez,
                       float repetirInstruccionCada, params Frase[] frases)
        {
            this.id = id;
            this.cuando = cuando.ToString();
            this.paso = paso;
            this.compra = compra;
            this.numero = numero;
            this.retraso = retraso;
            this.soloUnaVez = soloUnaVez;
            this.repetirInstruccionCada = repetirInstruccionCada;
            this.frases = new List<Frase>(frases);
        }

        public Cuando Disparador
        {
            get
            {
                Cuando c;
                return Enum.TryParse(cuando, out c) ? c : Cuando.PasoIntro;
            }
            set { cuando = value.ToString(); }
        }

        public Momento Copia()
        {
            Momento copia = (Momento)MemberwiseClone();
            copia.frases = new List<Frase>();
            foreach (Frase f in frases) copia.frases.Add(new Frase(f.texto, f.esInstruccion, f.retraso, f.duracion) { enfocar = f.enfocar });
            return copia;
        }
    }

    public List<Momento> momentos = new List<Momento>();

    // Guion por defecto. Vacío: todo lo que dice Botro se configura en la ventana.
    public static List<Momento> PorDefecto()
    {
        return new List<Momento>
        {

        };
    }

    // ---------- Textos para la ventana del editor ----------

    public static string NombreCuando(Cuando c)
    {
        switch (c)
        {
            case Cuando.PasoIntro: return "En un paso de la intro";
            case Cuando.TutorialTerminado: return "Al terminar el tutorial";
            case Cuando.EsferaPerfecta: return "Al conseguir una esfera perfecta";
            case Cuando.Compra: return "Al comprar una mejora";
            case Cuando.PiedraMetida: return "Al meter la piedra número…";
            case Cuando.QuedanPocasPiedras: return "Cuando quedan pocas piedras";
            case Cuando.MinutoDePartida: return "En el minuto… de partida";
            default: return c.ToString();
        }
    }

    // Cuándo empieza y termina cada paso de la intro (lo decide SecuenciaIntro).
    public static string DescripcionPaso(SecuenciaIntro.Paso paso)
    {
        switch (paso)
        {
            case SecuenciaIntro.Paso.EsperandoLlegada: return "Durante la cinemática de llegada. Termina al aterrizar.";
            case SecuenciaIntro.Paso.AbrirPuerta: return "Dentro de la nave. Termina al bajar la palanca.";
            case SecuenciaIntro.Paso.PatearPiedra: return "Cuando la piedra ha caído delante de la nave. Termina con la primera patada.";
            case SecuenciaIntro.Paso.LlevarPiedraABotro: return "Termina cuando la piedra y tú estáis junto a Botro.";
            case SecuenciaIntro.Paso.BotroExplica: return "Termina cuando Botro acaba de hablar.";
            case SecuenciaIntro.Paso.LlevarATrampilla: return "Llevar la piedra de vuelta a la gasolinera desgastándola. Termina cuando se acerca a la trampilla.";
            case SecuenciaIntro.Paso.Manguera: return "A la vez que el anterior: tras la primera patada de vuelta, si la manguera no está enchufada. Marca la manguera. Termina al enchufarla.";
            case SecuenciaIntro.Paso.MeterPiedra: return "Termina cuando la trampilla se traga la piedra.";
            case SecuenciaIntro.Paso.ZoomGasolina: return "Solo si el zoom a la gasolina está activado.";
            case SecuenciaIntro.Paso.RecogerRestos: return "Después de repostar. Marca la moneda. Termina al recogerla.";
            case SecuenciaIntro.Paso.GeneradorReconstruido: return "Empieza justo al reparar el generador. Marca la diana. Termina al golpearla.";
            case SecuenciaIntro.Paso.ReconstruirGenerador: return "Marca el generador de meteoritos (destruido). Sin fusil, 3 = moneda en la mano y clic para lanzarla. Termina al reconstruirlo (lo que deba decir DESPUÉS va en 'Generador reconstruido').";
            case SecuenciaIntro.Paso.Terminado: return "Fin del tutorial.";
            default: return "";
        }
    }

    /// <summary>Pasos sin acción del jugador: terminan cuando Botro acaba de hablar.</summary>
    public static bool TerminaAlHablar(SecuenciaIntro.Paso paso)
    {
        return paso == SecuenciaIntro.Paso.BotroExplica;
    }

    // ---------- Acceso ----------

    private static GuionBotro actual;
    private static List<Momento> porDefectoCache;

    public static GuionBotro Actual
    {
        get
        {
            if (actual == null) actual = Resources.Load<GuionBotro>(RutaResources);
            return actual;
        }
    }

    /// <summary>Los momentos del archivo o, si no hay archivo, los de por defecto.</summary>
    public static List<Momento> Momentos()
    {
        if (Actual != null) return Actual.momentos;
        if (porDefectoCache == null) porDefectoCache = PorDefecto();
        return porDefectoCache;
    }

    /// <summary>El momento activo de un paso de la intro (el primero de la lista).</summary>
    public static Momento ParaPaso(SecuenciaIntro.Paso paso)
    {
        string nombre = paso.ToString();
        foreach (Momento m in Momentos())
            if (m != null && m.activo && m.Disparador == Cuando.PasoIntro && m.paso == nombre)
                return m;
        return null;
    }

    public Momento Buscar(string id)
    {
        foreach (Momento m in momentos)
            if (m != null && m.id == id) return m;
        return null;
    }

    public static Momento BuscarPorDefecto(string id)
    {
        foreach (Momento m in PorDefecto())
            if (m.id == id) return m;
        return null;
    }
}
