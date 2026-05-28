using UnityEngine;
using UnityEngine.UIElements;

public class UIStatBarBinder : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private float animationDuration = 0.3f;

    private VisualElement healthBar, fpBar, staminaBar;
    private VisualElement healthBg, fpBg, staminaBg;
    private Label healCountLabel;
    private Label fpFlaskCountLabel;
    private VisualElement fpFlaskRow;
    private VisualElement flaskSelector;
    private PlayerController playerController;

    private float healthFillTarget, fpFillTarget, staminaFillTarget;
    private float currentHealthFill, currentFpFill, currentStaminaFill;
    private float healthAnimTimer, fpAnimTimer, staminaAnimTimer;

    private float prevHealth, prevFP, prevStamina;
    private float healthFlashTimer, fpFlashTimer;
    private float flashDuration = 0.2f;

    private Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
    private Color healFlashColor = new Color(0.3f, 1f, 0.3f, 1f);
    private Color darkTint = new Color(0.2f, 0.2f, 0.2f, 1f);

    private bool isBound;

    // Base stat values — bar is at max width when stat equals these
    private const float HEALTH_BASE = 100f;
    private const float FP_BASE = 100f;
    private const float STAMINA_BASE = 100f;

    // Max width at base stat value
    private const float HEALTH_MAX_WIDTH = 360f;
    private const float FP_MAX_WIDTH = 350f;
    private const float STAMINA_MAX_WIDTH = 350f;

    void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        healthBar = root.Q<VisualElement>("Health");
        fpBar = root.Q<VisualElement>("Fp");
        staminaBar = root.Q<VisualElement>("Stamina");
        healCountLabel = root.Q<Label>("HealCount");
        healCountLabel = root.Q<Label>("HealCount");
        fpFlaskCountLabel = root.Q<Label>("FpFlaskCount");
        fpFlaskRow = root.Q<VisualElement>("FpFlaskRow");
        flaskSelector = root.Q<VisualElement>("FlaskSelector");
        if (flaskSelector != null)
        {
            flaskSelector.style.position = Position.Absolute;
            flaskSelector.style.left = 10f;
            flaskSelector.style.bottom = 60f;
        }
        playerController = FindFirstObjectByType<PlayerController>();

        healthBg = root.Q<VisualElement>("HealthBackground");
        fpBg = root.Q<VisualElement>("FpBackground");
        staminaBg = root.Q<VisualElement>("StaminaBackground");

        Debug.Log("healthBar: " + (healthBar != null ? "found" : "NULL"));
        Debug.Log("fpBar: " + (fpBar != null ? "found" : "NULL"));
        Debug.Log("staminaBar: " + (staminaBar != null ? "found" : "NULL"));

        // Set backgrounds to full width with dark tint — never changes
        SetupBackground(healthBg, HEALTH_MAX_WIDTH);
        SetupBackground(fpBg, FP_MAX_WIDTH);
        SetupBackground(staminaBg, STAMINA_MAX_WIDTH);

        root.schedule.Execute(() => {
            TryBindPlayerStats();
        }).StartingIn(200);
    }

    void SetupBackground(VisualElement bg, float maxWidth)
    {
        if (bg == null) return;
        bg.style.width = maxWidth;
        bg.style.unityBackgroundImageTintColor = darkTint;
    }

    void OnDestroy()
    {
        if (playerStats == null) return;
        playerStats.OnHealthChanged -= UpdateHealth;
        playerStats.OnFPChanged -= UpdateFP;
        playerStats.OnStaminaChanged -= UpdateStamina;
    }

    private void TryBindPlayerStats()
    {
        if (isBound) return;

        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();

        if (playerStats == null) return;

        playerStats.OnHealthChanged += UpdateHealth;
        playerStats.OnFPChanged += UpdateFP;
        playerStats.OnStaminaChanged += UpdateStamina;

        currentHealthFill = playerStats.MaxHealth > 0f
            ? playerStats.CurrentHealth / playerStats.MaxHealth : 1f;
        currentFpFill = playerStats.MaxFP > 0f
            ? playerStats.CurrentFP / playerStats.MaxFP : 1f;
        currentStaminaFill = playerStats.MaxStamina > 0f
            ? playerStats.CurrentStamina / playerStats.MaxStamina : 1f;

        healthFillTarget = currentHealthFill;
        fpFillTarget = currentFpFill;
        staminaFillTarget = currentStaminaFill;

        prevHealth = playerStats.CurrentHealth;
        prevFP = playerStats.CurrentFP;
        prevStamina = playerStats.CurrentStamina;

        SetHealthFill(currentHealthFill);
        SetFpFill(currentFpFill);
        SetStaminaFill(currentStaminaFill);

        isBound = true;
    }

    void SetHealthFill(float fill)
    {
        if (healthBar == null || playerStats == null) return;
        float scaledMax = HEALTH_MAX_WIDTH * (playerStats.MaxHealth / HEALTH_BASE);
        healthBar.style.width = scaledMax * fill;
        if (healthBg != null) healthBg.style.width = scaledMax;
    }

    void SetFpFill(float fill)
    {
        if (fpBar == null || playerStats == null) return;
        float scaledMax = FP_MAX_WIDTH * (playerStats.MaxFP / FP_BASE);
        fpBar.style.width = scaledMax * fill;
        if (fpBg != null) fpBg.style.width = scaledMax;
    }

    void SetStaminaFill(float fill)
    {
        if (staminaBar == null || playerStats == null) return;
        float scaledMax = STAMINA_MAX_WIDTH * (playerStats.MaxStamina / STAMINA_BASE);
        staminaBar.style.width = scaledMax * fill;
        if (staminaBg != null) staminaBg.style.width = scaledMax;
    }
    private void UpdateHealth(float current, float max)
    {
        healthFillTarget = max > 0f ? current / max : 0f;
        healthAnimTimer = 0f;
        healthFlashTimer = flashDuration;
        prevHealth = current;
    }

    private void UpdateFP(float current, float max)
    {
        fpFillTarget = max > 0f ? current / max : 0f;
        fpAnimTimer = 0f;
        fpFlashTimer = flashDuration;
        prevFP = current;
    }

    private void UpdateStamina(float current, float max)
    {
        staminaFillTarget = max > 0f ? current / max : 0f;
        staminaAnimTimer = 0f;
        prevStamina = current;
    }

    void Update()
    {
        if (!isBound) TryBindPlayerStats();

        // Health
        if (healthAnimTimer < animationDuration)
        {
            healthAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(healthAnimTimer / animationDuration);
            currentHealthFill = Mathf.Lerp(currentHealthFill, healthFillTarget, t);
            SetHealthFill(currentHealthFill);
        }

        if (healthFlashTimer > 0f)
        {
            healthFlashTimer -= Time.deltaTime;
            if (healthBar != null)
                healthBar.style.unityBackgroundImageTintColor = GetFlashColor(
                    healthFlashTimer, prevHealth, healthFillTarget);
        }
        else if (healthBar != null)
            healthBar.style.unityBackgroundImageTintColor = Color.white;

        // FP
        if (fpAnimTimer < animationDuration)
        {
            fpAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fpAnimTimer / animationDuration);
            currentFpFill = Mathf.Lerp(currentFpFill, fpFillTarget, t);
            SetFpFill(currentFpFill);
        }

        if (fpFlashTimer > 0f)
        {
            fpFlashTimer -= Time.deltaTime;
            if (fpBar != null)
                fpBar.style.unityBackgroundImageTintColor = GetFlashColor(
                    fpFlashTimer, prevFP, fpFillTarget);
        }
        else if (fpBar != null)
            fpBar.style.unityBackgroundImageTintColor = Color.white;

        // Stamina
        if (staminaAnimTimer < animationDuration)
        {
            staminaAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(staminaAnimTimer / animationDuration);
            currentStaminaFill = Mathf.Lerp(currentStaminaFill, staminaFillTarget, t);
            SetStaminaFill(currentStaminaFill);
        }
        // Update heal count
        if (playerController != null)
        {
            // Heal count
            if (healCountLabel != null)
                healCountLabel.text = "x " + playerController.CurrentHeals;

            // FP flask count — show row only if player has FP flasks
            if (fpFlaskRow != null)
                fpFlaskRow.style.display = playerController.MaxFpFlasks > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (fpFlaskCountLabel != null)
                fpFlaskCountLabel.text = "x " + playerController.CurrentFpFlasks;

            // Move selector to indicate selected flask
            if (flaskSelector != null)
            {
                VisualElement healRow = flaskSelector.parent?.Q<VisualElement>("HealFlaskRow");
                VisualElement fpRow = flaskSelector.parent?.Q<VisualElement>("FpFlaskRow");
                VisualElement selectedRow = playerController.HealSelected ? healRow : fpRow;
                if (selectedRow != null)
                    flaskSelector.style.top = selectedRow.layout.y;
            }
        }
    }

    Color GetFlashColor(float timer, float prev, float current)
    {
        float alpha = timer / flashDuration;
        Color flash = prev > current ? damageFlashColor : healFlashColor;
        return Color.Lerp(Color.white, flash, alpha);
    }
}
