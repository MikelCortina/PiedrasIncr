using UnityEngine;

public class SwordHitZone : MonoBehaviour
{
    public enum TipoZona
    {
        Filo,
        Punta,
        Cara,
        Mango
    }

    [Header("Tipo de zona")]
    [SerializeField] private TipoZona tipoZona;

    public TipoZona Tipo =>
        tipoZona;

    public bool SeClava()
    {
        return tipoZona == TipoZona.Filo ||
               tipoZona == TipoZona.Punta;
    }

    public bool Rebota()
    {
        return tipoZona == TipoZona.Cara ||
               tipoZona == TipoZona.Mango;
    }
}