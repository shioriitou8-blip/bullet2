using UnityEngine;

[DisallowMultipleComponent]
public class PooledObject : MonoBehaviour
{
    public string PoolKey { get; private set; }
    public ObjectPool Owner { get; private set; }

    public void Bind(ObjectPool owner, string poolKey)
    {
        Owner = owner;
        PoolKey = poolKey;
    }

    public void ReturnToPool()
    {
        if (Owner == null)
        {
            gameObject.SetActive(false);
            return;
        }

        Owner.Despawn(gameObject);
    }
}
