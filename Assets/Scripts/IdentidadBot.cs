using UnityEngine;

[RequireComponent(
    typeof(ConfiguracionBot)
)]
public class IdentidadBot : MonoBehaviour
{
    // =====================================================
    // DATOS
    // =====================================================

    [Header("Identidad")]

    [SerializeField]
    private int idBot =
        -1;


    public int IdBot
        => idBot;


    // =====================================================
    // REFERENCIAS
    // =====================================================

    private ConfiguracionBot configuracionBot;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        configuracionBot =
            GetComponent<ConfiguracionBot>();


        Registrar();
    }


    // =====================================================
    // REGISTRAR
    // =====================================================

    private void Registrar()
    {
        if (configuracionBot == null)
            return;


        GestorBots gestor =
            GestorBots.Instancia;


        if (gestor == null)
        {
            gestor =
                FindAnyObjectByType<
                    GestorBots
                >();
        }


        if (gestor == null)
        {
            Debug.LogError(
                name +
                ": no existe GestorBots en la escena."
            );

            return;
        }


        idBot =
            gestor.RegistrarBot(
                configuracionBot
            );
    }


    // =====================================================
    // DESTROY
    // =====================================================

    private void OnDestroy()
    {
        if (configuracionBot == null)
            return;


        if (GestorBots.Instancia == null)
            return;


        GestorBots.Instancia
            .DesregistrarBot(
                configuracionBot
            );
    }
}