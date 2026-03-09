using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("Canvas Elements")]
    [SerializeField] private Image _cardArt;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _attackText;
    [SerializeField] private TextMeshProUGUI _defenseText;

    public CardInstance CardInstance { get; private set; }
    public Transform AttackTransform => _attackText.transform;
    public Transform DefenseTransform => _defenseText.transform;

    public void Setup(CardInstance cardInstance)
    {
        CardInstance = cardInstance;

        if (_cardArt != null && cardInstance.Data.CardSprite != null)
            _cardArt.sprite = cardInstance.Data.CardSprite;

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (CardInstance == null) return;

        _nameText.text = CardInstance.Data.CardName;
        _attackText.text = CardInstance.CurrentAttack.ToString();
        _defenseText.text = CardInstance.CurrentDefense.ToString();
    }
}
