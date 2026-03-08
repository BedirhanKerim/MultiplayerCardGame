using System;
using UnityEngine;

[RequireComponent(typeof(CardView))]
public class CardUnit : MonoBehaviour, IInteractable
{
    [SerializeField] private Canvas _canvas;

    public CardView CardView { get; private set; }
    public Canvas Canvas => _canvas;
    public CardInstance CardInstance => CardView.CardInstance;
    public bool Interactable { get; set; } = true;

    public event Action<CardUnit> OnClicked;
    public event Action<CardUnit> OnDragStarted;
    public event Action<CardUnit, Vector3> OnDragging;
    public event Action<CardUnit> OnDropped;

    private void Awake()
    {
        CardView = GetComponent<CardView>();
    }

    public void OnInteract()
    {
        OnClicked?.Invoke(this);
    }

    public void OnDragBegin() => OnDragStarted?.Invoke(this);
    public void OnDrag(Vector3 worldPosition) => OnDragging?.Invoke(this, worldPosition);
    public void OnDrop() => OnDropped?.Invoke(this);
}
