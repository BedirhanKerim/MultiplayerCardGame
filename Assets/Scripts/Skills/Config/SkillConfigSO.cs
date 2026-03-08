using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skill Config")]
public class SkillConfigSO : ScriptableObject
{
    [Header("Heal Player (Skill 1)")]
    public int HealAmount = 5;
    [TextArea] public string HealDescription = "Restores 5 HP.";

    [Header("Boost Attack (Skill 2)")]
    public int AttackBoost = 3;
    [TextArea] public string AttackBoostDescription = "Increases your card's ATK by 3.";

    [Header("Boost Defense (Skill 3)")]
    public int DefenseBoost = 3;
    [TextArea] public string DefenseBoostDescription = "Increases your card's DEF by 3.";

    [Header("Reduce Opponent Attack (Skill 4)")]
    public int OpponentAttackReduction = 2;
    [TextArea] public string ReduceAttackDescription = "Reduces opponent's card ATK by 2.";

    [Header("Reduce Opponent Defense (Skill 5)")]
    public int OpponentDefenseReduction = 2;
    [TextArea] public string ReduceDefenseDescription = "Reduces opponent's card DEF by 2.";

    [Header("Shield (Skill 6)")]
    public int ShieldAbsorb = 4;
    public int ShieldOpponentAttackBoost = 3;
    [TextArea] public string ShieldDescription = "Absorbs 4 damage, but boosts opponent's ATK by 3 next turn.";

    public string GetDescription(SkillType skill)
    {
        return skill switch
        {
            SkillType.HealPlayer => HealDescription,
            SkillType.BoostAttack => AttackBoostDescription,
            SkillType.BoostDefense => DefenseBoostDescription,
            SkillType.ReduceOpponentAttack => ReduceAttackDescription,
            SkillType.ReduceOpponentDefense => ReduceDefenseDescription,
            SkillType.Shield => ShieldDescription,
            _ => ""
        };
    }
}
