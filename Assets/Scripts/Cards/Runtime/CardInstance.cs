public class CardInstance
{
    public CardDataEntry Data { get; }
    public int CurrentAttack { get; set; }
    public int CurrentDefense { get; set; }

    public CardInstance(CardDataEntry data)
    {
        Data = data;
        ResetStats();
    }

    public void ResetStats()
    {
        CurrentAttack = Data.Attack;
        CurrentDefense = Data.Defense;
    }
}
