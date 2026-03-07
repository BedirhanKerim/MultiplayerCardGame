using System;
using UnityEngine;

[RequireComponent(typeof(CardView))]
public class CardUnit : MonoBehaviour, IInteractable
{
    public CardView CardView { get; private set; }
    public CardInstance CardInstance => CardView.CardInstance;
    public bool Interactable { get; set; } = true;

    public event Action<CardUnit> OnClicked;

    private void Awake()
    {
        CardView = GetComponent<CardView>();
    }

    public void OnInteract()
    {
        OnClicked?.Invoke(this);
    }
}
