using UnityEngine;

/// <summary>
/// Defines a Graviton Cell battery tier. ScriptableObject allows designers to create new tiers
/// in the Unity editor without modifying code.
/// </summary>
[CreateAssetMenu(fileName = "GravitonCell_", menuName = "Gravitas/Graviton Cell")]
public class GravitonCell : ScriptableObject
{
    [SerializeField]
    [Tooltip("Name of this cell tier (e.g., 'Salvage Cell')")]
    private string cellName = "Unnamed Cell";

    [SerializeField]
    [Tooltip("Maximum charge capacity for this cell")]
    private float maxCharge = 100f;

    [SerializeField]
    [Tooltip("Passive recharge rate in charge per second")]
    private float passiveRechargeRate = 2f;

    [SerializeField]
    [Tooltip("Icon displayed in UI")]
    private Sprite icon;

    [SerializeField]
    [Range(1, 4)]
    [Tooltip("Tier level (1-4)")]
    private int tier = 1;

    public string CellName => cellName;
    public float MaxCharge => maxCharge;
    public float PassiveRechargeRate => passiveRechargeRate;
    public Sprite Icon => icon;
    public int Tier => tier;
}
