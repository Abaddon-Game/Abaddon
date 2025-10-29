using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum UIState
{
    Pause,
    Dialogue,
    Inventory,
    Death,
    Win,
    Shop,
    Settings,
}

[Serializable]
public struct UIScreen
{
    [Tooltip("The state of the screen. This is used to identify the screen and to open/close it.")]
    public UIState state;

    [Tooltip(
        "If a screen is in this list, it will be automatically closed when this screen is opened."
    )]
    public UIState[] overrides;

    [Tooltip(
        "If a screen is in this list, it will be ignored to open while this is open but not removed if already open."
    )]
    public UIState[] inhibits;

    [Tooltip("The game object that will be enabled/disabled when the screen is opened/closed.")]
    public GameObject screenObject;

    [Tooltip(
        "The key that will be used to open/close the screen ACROSS THE WHOLE GAME. Use sparingly, instead opt for local logic that calls ToggleUIPage."
    )]
    public KeyCode defaultKey;

    [Tooltip("The events that will be called when the screen is enabled/disabled.")]
    public UnityEvent onEnable,
        onDisable;

    [Tooltip("Close the screen when ESC is pressed.")]
    public bool closeOnEsc;

    [Tooltip("If you wouldn't like the screen object to get disabled when it's not open")]
    public bool dontDisableOnClose;
}

public class UIStateManager : MonoBehaviour
{
    public static UIStateManager singleton;

    [Tooltip("The simple black background that darkens the screen when a menu pops up.")]
    [SerializeField]
    private Image darkener;

    [Tooltip("The list of screens that can be opened/closed and their settings.")]
    [SerializeField]
    private List<UIScreen> screens = new List<UIScreen>();

    [SerializeField]
    string MainMenuScene = "Main Menu";

    [SerializeField]
    string SaveFileName = "abaddon_save_data.json";

    private float _darkenerOpacity = 1;
    public float darkenerOpacity
    {
        get { return _darkenerOpacity; }
        set
        {
            _darkenerOpacity = Mathf.Clamp01(value);
            var color = darkener.color;
            color.a = value;
            darkener.color = color;
        }
    }

    public Stack<UIState> activeScreens { get; private set; } = new Stack<UIState>();
    private float lerpSpeed = 0;
    private float intendedDarkValue = 0;
    private bool isBeingAltered = false;
    public UIState? mostRecentState => activeScreens.TryPeek(out var state) ? state : null;
    public UIScreen? currentScreen =>
        mostRecentState != null ? screens.First(screen => screen.state == mostRecentState) : null;

    private void Awake()
    {
        singleton = this;
    }

    private void Start()
    {
        LoadFromFile();
    }

    private void Update()
    {
        bool ran_close_on_esc = false;
        screens.ForEach(screen =>
        {
            if (
                Input.GetKeyDown(screen.defaultKey)
                && !(ran_close_on_esc && screen.defaultKey == KeyCode.Escape) // prevent the pause screen from appearing if we closed, lets say, the inventory, with escape
            )
                ToggleUIPage(screen.state);
            else if (
                screen.closeOnEsc
                && mostRecentState == screen.state
                && Input.GetKeyDown(KeyCode.Escape)
            )
            {
                CloseUIPage(screen.state);
                ran_close_on_esc = true;
            }
        });

        if (isBeingAltered && lerpSpeed != 0)
        {
            darkenerOpacity = Mathf.Lerp(
                darkenerOpacity,
                intendedDarkValue,
                Time.deltaTime * lerpSpeed
            );
            if (Mathf.Abs(intendedDarkValue - darkenerOpacity) < 0.03f)
            {
                lerpSpeed = 0;
                darkenerOpacity = intendedDarkValue;
                if (intendedDarkValue == 0)
                    SetDarkenedBackground(false);
                intendedDarkValue = 0;
            }
        }
    }

    public void FadeInDarkener(float speed, float endValue = 1, float startValue = 0)
    {
        isBeingAltered = true;
        SetDarkenedBackground(true);
        lerpSpeed = speed;
        intendedDarkValue = endValue;
        darkenerOpacity = startValue;
    }

    public void FadeOutDarkener(float speed, float? startValue = null)
    {
        isBeingAltered = true;
        lerpSpeed = speed;
        intendedDarkValue = 0;
        darkenerOpacity = startValue ?? darkenerOpacity;
    }

    public void ToggleUIPage(UIState newState)
    {
        if (mostRecentState == newState)
            CloseUIPage(newState);
        else
            OpenUIPage(newState);
    }

    public void OpenUIPage(UIState newState)
    {
        if (newState == mostRecentState)
            return;
        if (
            currentScreen != null
            && (
                currentScreen.Value.overrides.Contains(newState)
                || currentScreen.Value.inhibits.Contains(newState)
            )
        )
            return;
        CloseSubordinatePages(newState);

        activeScreens.Push(newState);
        foreach (var screen in screens.Where(screen => screen.state == newState))
        {
            if (screen.screenObject)
                screen.screenObject.SetActive(true);
            screen.onEnable?.Invoke();
        }
    }

    public void ForceDarkenerTo(float newOpacity)
    {
        isBeingAltered = false;
        darkenerOpacity = newOpacity;
        SetDarkenedBackground(newOpacity != 0);
    }

    public void CloseUIPage(UIState newState)
    {
        if (newState != mostRecentState)
            return;
        activeScreens.Pop();

        foreach (var screen in screens.Where(screen => screen.state == newState))
        {
            screen.onDisable?.Invoke();
            if (screen.screenObject && !screen.dontDisableOnClose)
                screen.screenObject.SetActive(false);
        }
    }

    public void CloseOpenUIPages()
    {
        if (activeScreens == null)
            return;

        while (activeScreens.Count > 0)
            CloseUIPage(activeScreens.Peek());
    }

    UIScreen GetScreen(UIState state) => screens.First(screen => screen.state == state);

    void CloseSubordinatePages(UIState newState)
    {
        if (activeScreens == null)
            return;

        foreach (
            var screen in screens.Where(screen =>
                GetScreen(newState).overrides.Contains(screen.state)
            )
        )
            CloseUIPage(screen.state);
    }

    public void SetDarkenedBackground(bool shouldDarken) => darkener.enabled = shouldDarken;

    public void ToggleDarkenedBackground() => darkener.enabled = !darkener.enabled;

    public void Quit()
    {
        SceneManager.LoadScene(MainMenuScene);
    }

    public void OpenSettingsPage()
    {
        CloseOpenUIPages();
        OpenUIPage(UIState.Settings);
    }

    public void SaveToFile()
    {
        string path = Path.Join(Application.persistentDataPath, SaveFileName);
        SaveToFile(path);
    }

    public void SaveToFile(string path)
    {
        // TODO: prevent saving while not players turn (so we don't have to worry about weird shenanigans)

        Debug.Log("Saving to: " + path + "...");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string data = Serializer.Serialize(true);

            using FileStream stream = new(path, FileMode.Create);
            using StreamWriter writer = new(stream);
            writer.Write(data);
        }
        catch (Exception E)
        {
            Debug.LogError("Error occured while trying to save file: " + path + '\n' + E);
        }

        Debug.Log("Done saving!");
    }

    public bool LoadFromFile()
    {
        string path = Path.Join(Application.persistentDataPath, SaveFileName);
        return LoadFromFile(path);
    }

    public bool LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using FileStream stream = new(path, FileMode.Open);
            using StreamReader reader = new(stream);
            string data = reader.ReadToEnd();
            Serializer.Deserialize(data);
        }
        catch (Exception E)
        {
            Debug.LogError("Could not read file: " + path + '\n' + E);
            return false;
        }

        return true;
    }
}
