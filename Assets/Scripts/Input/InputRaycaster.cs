using UnityEngine;
using VContainer;

public class InputRaycaster : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private float _dragPlaneDistance = 10f;

    [Inject] private GameEventBus _eventBus;

    private const int DragSortingOrder = 100;

    private GameState _currentState = GameState.DeckBuilder;
    private CardUnit _draggedCard;
    private int _prevSortingOrder;
    private float _dragY;
    private bool _locked;

    private void OnEnable()
    {
        _eventBus.SubscribeTo<GameStateChanged>(OnGameStateChanged);
        _eventBus.SubscribeTo<LocalPlayerConfirmed>(OnLocalPlayerConfirmed);
        _eventBus.SubscribeTo<TurnStarted>(OnTurnStarted);
    }

    private void OnDisable()
    {
        _eventBus.UnsubscribeFrom<GameStateChanged>(OnGameStateChanged);
        _eventBus.UnsubscribeFrom<LocalPlayerConfirmed>(OnLocalPlayerConfirmed);
        _eventBus.UnsubscribeFrom<TurnStarted>(OnTurnStarted);
    }

    private void OnGameStateChanged(ref GameStateChanged e)
    {
        _currentState = e.State;
    }

    private void OnLocalPlayerConfirmed(ref LocalPlayerConfirmed _)
    {
        _locked = true;
        ForceDropDraggedCard();
    }

    private void ForceDropDraggedCard()
    {
        if (_draggedCard != null)
        {
            _draggedCard.Canvas.sortingOrder = _prevSortingOrder;
            _draggedCard.OnDrop();
            _draggedCard = null;
        }
    }

    private void OnTurnStarted(ref TurnStarted _)
    {
        _locked = false;
    }

    private void Update()
    {
        if (_currentState == GameState.DeckBuilder)
            HandleClick();
        else
            HandleDragDrop();
    }

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, _interactableLayer))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable) && interactable.Interactable)
            {
                interactable.OnInteract();
            }
        }
    }

    private void HandleDragDrop()
    {
        if (_locked) return;
        if (Input.GetMouseButtonDown(0))
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, _interactableLayer))
            {
                if (hit.collider.TryGetComponent<CardUnit>(out var card) && card.Interactable)
                {
                    _draggedCard = card;
                    _dragY = _draggedCard.transform.position.y;
                    _prevSortingOrder = _draggedCard.Canvas.sortingOrder;
                    _draggedCard.Canvas.sortingOrder = DragSortingOrder;
                    _draggedCard.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    _draggedCard.OnDragBegin();
                }
            }
        }
        else if (_draggedCard != null && Input.GetMouseButton(0))
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            Vector3 worldPos = ray.GetPoint(_dragPlaneDistance);
            worldPos.y = _dragY;
            _draggedCard.transform.position = worldPos;
            _draggedCard.OnDrag(worldPos);
        }
        else if (_draggedCard != null && Input.GetMouseButtonUp(0))
        {
            _draggedCard.OnDrop();
            _draggedCard = null;
        }
    }
}
