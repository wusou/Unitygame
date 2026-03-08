using UnityEngine;
using UnityEngine.InputSystem;

public class InputBindingsManager : MonoBehaviour
{
    public static InputBindingsManager Instance { get; private set; }

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private const string BindingOverridesKey = "input.binding.overrides";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        LoadBindingOverrides();
    }

    public void SaveBindingOverrides()
    {
        if (inputActions == null)
        {
            return;
        }

        var json = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BindingOverridesKey, json);
        PlayerPrefs.Save();
    }

    public void LoadBindingOverrides()
    {
        if (inputActions == null)
        {
            return;
        }

        if (!PlayerPrefs.HasKey(BindingOverridesKey))
        {
            return;
        }

        var json = PlayerPrefs.GetString(BindingOverridesKey);
        if (!string.IsNullOrEmpty(json))
        {
            inputActions.LoadBindingOverridesFromJson(json);
        }
    }

    public void ResetBindingOverrides()
    {
        if (inputActions == null)
        {
            return;
        }

        inputActions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingOverridesKey);
        PlayerPrefs.Save();
    }
}
