using System;
using UnityEngine;

[Serializable]
public class CardDataEntry
{
    [SerializeField] private int _cardId;
    [SerializeField] private string _cardName;
    [SerializeField] private int _attack;
    [SerializeField] private int _defense;
    [SerializeField] private Sprite _cardSprite;

    public int CardId => _cardId;
    public string CardName => _cardName;
    public int Attack => _attack;
    public int Defense => _defense;
    public Sprite CardSprite => _cardSprite;
}
