using UnityEngine;

/// <summary>
/// Anima a Botro según hable o no (BotroDice): callado = Idle (cabeza caída); al empezar a hablar,
/// LevantarCabeza y después el bucle Hablar; al callarse un rato, vuelve al Idle.
/// El Animator Controller lo crea Tools > PiedrasIncr > Botro: preparar animaciones.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimacionBotro : MonoBehaviour
{
    public const string ParamHablando = "Hablando";
    private static readonly int IdHablando = Animator.StringToHash(ParamHablando);

    [Tooltip("Segundos callado antes de volver a bajar la cabeza (entre frase y frase la mantiene alzada).")]
    public float esperaAntesDeCallar = 1.5f;

    private Animator animator;
    private float ultimaVezHablando = float.NegativeInfinity;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        BotroDice dice = BotroDice.Instancia;
        if (dice != null && dice.Hablando) ultimaVezHablando = Time.time;
        animator.SetBool(IdHablando, Time.time - ultimaVezHablando < esperaAntesDeCallar);
    }
}
