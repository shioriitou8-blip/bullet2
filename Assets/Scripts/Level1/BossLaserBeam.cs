using UnityEngine;

[DisallowMultipleComponent]
public class BossLaserBeam : MonoBehaviour
{
    [SerializeField, Min(0.02f)] private float damageInterval = 0.25f;

    private float nextDamageTime;

    private void OnEnable()
    {
        nextDamageTime = 0f;
    }

    public void SetDamageInterval(float interval)
    {
        damageInterval = Mathf.Max(0.02f, interval);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextDamageTime)
        {
            return;
        }

        if (!other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            return;
        }

        player.ReceiveEnemyHit();
        nextDamageTime = Time.time + damageInterval;
    }
}
