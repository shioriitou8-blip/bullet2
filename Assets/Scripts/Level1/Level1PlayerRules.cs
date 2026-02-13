using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public class Level1PlayerRules : MonoBehaviour
{
    [SerializeField] private bool disableBombs = true;
    [SerializeField] private int forcedBombCount = 0;
    [SerializeField] private bool forceFireDirection = true;
    [SerializeField] private Vector2 forcedFireDirection = Vector2.right;

    private void Start()
    {
        if (!disableBombs)
        {
            return;
        }

        PlayerController player = GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }

        int delta = forcedBombCount - player.Bombs;
        if (delta != 0)
        {
            player.AddBomb(delta);
        }

        if (!forceFireDirection)
        {
            return;
        }

        PlayerShooter shooter = GetComponent<PlayerShooter>();
        if (shooter != null)
        {
            shooter.SetBaseShotDirection(forcedFireDirection);
        }
    }
}
