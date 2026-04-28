using UnityEngine;
using System;

/// <summary>
/// Manages the Gravity Gun's charge state. Tracks current charge, applies passive recharge,
/// and validates charge spending. Fires an event whenever charge changes for UI subscription.
/// </summary>
public class GunBatterySystem : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the default cell loaded at start")]
    private GravitonCell defaultCell;

    [SerializeField]
    [Tooltip("Optional reference to the GravityGun that owns this battery; used to read TestingMode.")]
    private GravityGun gravityGun;

    private GravitonCell currentCell;
    [SerializeField] private float currentCharge;

    /// <summary>
    /// Fired whenever charge changes. Passes (currentCharge, maxCharge).
    /// </summary>
    public event Action<float, float> OnChargeChanged;

    private void Start()
    {
        if (gravityGun == null)
            gravityGun = GetComponent<GravityGun>();

        if (defaultCell != null)
        {
            SwapCell(defaultCell);
        }
        else
        {
            Debug.LogWarning("GunBatterySystem: No default cell assigned. Battery system non-functional.");
            currentCell = null;
        }
    }

    private bool IsTestingMode => gravityGun != null && gravityGun.TestingMode;

    private void Update()
    {
        if (currentCell == null)
            return;

        float previousCharge = currentCharge;

        if (IsTestingMode)
        {
            currentCharge = currentCell.MaxCharge;
        }
        else
        {
            // Apply passive recharge
            currentCharge += currentCell.PassiveRechargeRate * Time.deltaTime;
            currentCharge = Mathf.Clamp(currentCharge, 0f, currentCell.MaxCharge);
        }

        if (!Mathf.Approximately(previousCharge, currentCharge))
        {
            OnChargeChanged?.Invoke(currentCharge, currentCell.MaxCharge);
        }
    }

    /// <summary>
    /// Returns true if the gun has any charge.
    /// </summary>
    public bool HasCharge => currentCharge > 0f;

    /// <summary>
    /// Returns current charge as percentage (0-1).
    /// </summary>
    public float ChargePercent => currentCell != null ? currentCharge / currentCell.MaxCharge : 0f;

    /// <summary>
    /// Tries to spend charge. If available, deducts and returns true. Otherwise, returns false.
    /// </summary>
    public bool TrySpendCharge(float amount)
    {
        if (IsTestingMode)
            return true;

        if (currentCharge >= amount)
        {
            currentCharge -= amount;
            OnChargeChanged?.Invoke(currentCharge, currentCell.MaxCharge);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Directly adds charge (used by recharge stations, enemy drops, etc).
    /// </summary>
    public void AddCharge(float amount)
    {
        if (currentCell == null)
            return;

        currentCharge += amount;
        currentCharge = Mathf.Clamp(currentCharge, 0f, currentCell.MaxCharge);
        OnChargeChanged?.Invoke(currentCharge, currentCell.MaxCharge);
    }

    /// <summary>
    /// Swaps to a new cell type and clamps current charge to the new max if needed.
    /// </summary>
    public void SwapCell(GravitonCell newCell)
    {
        if (newCell == null)
        {
            Debug.LogWarning("GunBatterySystem: Attempted to swap to null cell.");
            return;
        }

        currentCell = newCell;
        // Clamp charge to new cell's max
        currentCharge = Mathf.Clamp(currentCharge, 0f, currentCell.MaxCharge);
        OnChargeChanged?.Invoke(currentCharge, currentCell.MaxCharge);
        Debug.Log($"Swapped to cell: {currentCell.CellName}");
    }

    /// <summary>
    /// Sets the battery charge directly and fires a charge changed event.
    /// </summary>
    public void SetCharge(float amount)
    {
        if (currentCell == null)
            return;

        currentCharge = Mathf.Clamp(amount, 0f, currentCell.MaxCharge);
        OnChargeChanged?.Invoke(currentCharge, currentCell.MaxCharge);
    }

    /// <summary>
    /// Returns the currently equipped cell.
    /// </summary>
    public GravitonCell CurrentCell => currentCell;

    /// <summary>
    /// Returns the current charge value.
    /// </summary>
    public float CurrentCharge => currentCharge;
}
