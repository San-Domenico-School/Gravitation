/// <summary>
/// Implemented by a MonoBehaviour that wants to persist arbitrary state across saves.
/// Pair it with a <see cref="SaveableEntity"/> on the same GameObject — the entity
/// owns the unique GUID and routes save/load calls.
///
/// Both methods must be deterministic (no side effects on save) and idempotent (safe
/// to call multiple times with the same blob on load).
///
/// The blob is a string — usually JSON produced by JsonUtility.ToJson. Keep it small.
/// </summary>
public interface ISaveable
{
    /// <summary>Capture this component's state into a string. Empty/null is allowed.</summary>
    string SaveState();

    /// <summary>Restore this component's state from the string previously returned by SaveState().</summary>
    void LoadState(string state);
}
