using UnityEngine;

[CreateAssetMenu(menuName = "Game/Turn Config")]
public class TurnConfigSO : ScriptableObject
{
    [Header("Turn")]
    public int MaxTurns = 6;
    public float TurnDuration = 30f;
    public float ResolveDuration = 2f;

    [Header("Health")]
    public int StartingHp = 30;
}
