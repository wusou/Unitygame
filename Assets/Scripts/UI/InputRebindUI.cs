using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputRebindUI : MonoBehaviour
{
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private int bindingIndex;
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private TMP_Text bindingDisplayText;
    [SerializeField] private TMP_Text statusText;

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private void OnEnable()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        DisposeRebindOperation();
    }

    public void StartRebind()
    {
        var action = actionReference?.action;
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            SetStatus("绑定配置无效");
            return;
        }

        DisposeRebindOperation();
        action.Disable();

        SetStatus("请按下新按键...");
        rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnCancel(op =>
            {
                action.Enable();
                SetStatus("已取消");
                RefreshDisplay();
                DisposeRebindOperation();
            })
            .OnComplete(op =>
            {
                action.Enable();
                SetStatus("绑定成功");
                RefreshDisplay();
                InputBindingsManager.Instance?.SaveBindingOverrides();
                DisposeRebindOperation();
            });

        rebindingOperation.Start();
    }

    public void ResetRebind()
    {
        var action = actionReference?.action;
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return;
        }

        action.RemoveBindingOverride(bindingIndex);
        InputBindingsManager.Instance?.SaveBindingOverrides();
        SetStatus("已恢复默认");
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        var action = actionReference?.action;
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            if (actionNameText != null)
            {
                actionNameText.text = "N/A";
            }

            if (bindingDisplayText != null)
            {
                bindingDisplayText.text = "-";
            }

            return;
        }

        if (actionNameText != null)
        {
            actionNameText.text = action.name;
        }

        if (bindingDisplayText != null)
        {
            bindingDisplayText.text = action.GetBindingDisplayString(bindingIndex);
        }
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    private void DisposeRebindOperation()
    {
        if (rebindingOperation == null)
        {
            return;
        }

        rebindingOperation.Dispose();
        rebindingOperation = null;
    }
}
