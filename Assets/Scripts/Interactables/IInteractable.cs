/// <summary>
/// 所有可交互对象统一接口（门、NPC、武器拾取等）。
/// </summary>
public interface IInteractable
{
    string InteractionTitle { get; }
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
}
