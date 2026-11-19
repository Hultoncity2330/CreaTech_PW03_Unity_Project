using UnityEngine;

/// <summary>One edible tuft of grass. It can only be consumed once.</summary>
public class GrassBehaviour : MonoBehaviour
{
    private bool isConsumed;

    public bool IsConsumed => isConsumed;

    /// <summary>Claims and removes this tuft; returns false if already eaten.</summary>
    public bool TryConsume()
    {
        if (!isConsumed)
        {
            isConsumed = true;
            Destroy(gameObject);
            return true;
        }

        return false;
    }
}
