using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O'yin holati va menyular: Bosh menyu (Play / Settings / Exit), Sozlamalar va o'yin ekrani (HUD).
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Panellar")]
    public GameObject menuPanel;
    public GameObject settingsPanel;
    public GameObject hudPanel;
    public GameObject winText;

    [Header("UI elementlari")]
    public Text scoreText;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("O'yinchi")]
    public PlayerController player;

    int totalCoins;

    void Awake()
    {
        Application.targetFrameRate = 60;

        // Har qanday tugma bosilganda "click" tovushi chalinadi (sakrash tugmasi bundan mustasno - u o'z tovushiga ega)
        foreach (Button button in FindObjectsOfType<Button>(true))
        {
            if (button.name != "JumpButton")
                button.onClick.AddListener(PlayClickSound);
        }
    }

    void Start()
    {
        totalCoins = FindObjectsOfType<Coin>().Length;
        if (player != null) player.OnScoreChanged += UpdateScore;
        UpdateScore(0);
        ShowMenu();
    }

    void PlayClickSound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    // ---------- Tugmalar shu metodlarni chaqiradi ----------

    public void ShowMenu()
    {
        menuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        hudPanel.SetActive(false);
        if (player != null) player.canMove = false;
    }

    public void OnPlay()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        hudPanel.SetActive(true);
        if (player != null) player.canMove = true;
    }

    public void OnSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);

        if (AudioManager.Instance != null)
        {
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }
    }

    public void OnBackFromSettings()
    {
        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = "Tangalar: " + score + " / " + totalCoins;

        if (winText != null)
            winText.SetActive(totalCoins > 0 && score >= totalCoins);
    }
}
