using UnityEngine;

public class WorldPersistentObject : MonoBehaviour
{
    [SerializeField]
    private string persistentId = string.Empty;

    [SerializeField]
    private PersistentObjectKind kind = PersistentObjectKind.GravityProp;

    public string PersistentId => persistentId;

    public PersistentObjectKind Kind => kind;

    public void Initialize(string id, PersistentObjectKind objectKind)
    {
        persistentId = id;
        kind = objectKind;
    }
}
