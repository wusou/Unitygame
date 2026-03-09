using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家交互器：负责按F交互的统一入口。
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField] private LayerMask interactableLayer;

    private void OnEnable()
    {
        interactAction?.action?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action?.Disable();
    }

    private void Update()
    {
        if (!ReadInteractPressed())
        {
            return;
        }

        TryInteractNearest();
    }

    public T GetPlayerComponent<T>() where T : Component
    {
        return GetComponent<T>();
    }

    private bool ReadInteractPressed()
    {
        if (interactAction != null && interactAction.action != null)
        {
            return interactAction.action.WasPressedThisFrame();
        }

        var keyboard = Keyboard.current; return keyboard != null && keyboard.fKey.wasPressedThisFrame;
    }

    private void TryInteractNearest()
    {
        int layerMask = interactableLayer.value == 0 ? Physics2D.DefaultRaycastLayers : interactableLayer.value;
        var hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, layerMask);

        IInteractable nearest = null;
        var minDist = float.MaxValue;

        for (var i = 0; i < hits.Length; i++)
        {
            var interactable = FindInteractable(hits[i]);
            if (interactable == null || !interactable.CanInteract(this))
            {
                continue;
            }

            var dist = Vector2.Distance(transform.position, hits[i].transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = interactable;
            }
        }

        nearest?.Interact(this);
    }

    private static IInteractable FindInteractable(Collider2D col)
    {
        if (col == null)
        {
            return null;
        }

        var behaviours = col.GetComponentsInParent<MonoBehaviour>();
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

