using UnityEngine;

public class InputRaycaster : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _interactableLayer;

    private void Update()
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
}
