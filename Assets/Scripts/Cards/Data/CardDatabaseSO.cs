using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Card Database")]
public class CardDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CardDataEntry> _cards = new();

    public IReadOnlyList<CardDataEntry> Cards => _cards;
}
