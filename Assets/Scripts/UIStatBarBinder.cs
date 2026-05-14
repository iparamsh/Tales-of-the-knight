using UnityEngine;
using UnityEngine.UIElements;

public class UIStatBarBinder : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private string healthParentName = "HealthBarBackground";
    [SerializeField] private string fpParentName = "FpBarBackground";
    [SerializeField] private string staminaParentName = "StaminaBarBackground";
    [SerializeField] private float animationDuration = 0.3f;

    private VisualElement healthBar, fpBar, staminaBar;

    private float healthFillTarget, healthTextTarget, healthTextCurrent;
    private float fpFillTarget, fpTextTarget, fpTextCurrent;
    private float staminaFillTarget, staminaTextTarget, staminaTextCurrent;
    private float healthAnimTimer, fpAnimTimer, staminaAnimTimer;

    private float currentHealthFill, currentFpFill, currentStaminaFill;

    private float prevHealth, prevFP, prevStamina;
    private float healthFlashTimer, fpFlashTimer, staminaFlashTimer;
    private Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
    private Color healFlashColor = new Color(0.3f, 1f, 0.3f, 1f);
    private float flashDuration = 0.2f;
    private bool isBound;

    void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogWarning("UIStatBarBinder: No UIDocument found on GameObject or assigned in inspector.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        var healthParent = root.Q<VisualElement>(healthParentName);
        var fpParent = root.Q<VisualElement>(fpParentName);
        var staminaParent = root.Q<VisualElement>(staminaParentName);

        healthBar = FindBarElement(healthParent, "Health");
        fpBar = FindBarElement(fpParent, "Fp");
        staminaBar = FindBarElement(staminaParent, "Stamina");

        TryBindPlayerStats();
        if (!isBound)
        {
            return;
        }
    }

    private VisualElement FindBarElement(VisualElement parent, string barName)
    {
        if (parent == null) return null;
        foreach (var child in parent.Children())
        {
            if (child.name == barName)
                return child;
            var deeper = FindBarElement(child, barName);
            if (deeper != null) return deeper;
        }
        return null;
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
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats == null)
            return;

        playerStats.OnHealthChanged += UpdateHealth;
        playerStats.OnFPChanged += UpdateFP;
        playerStats.OnStaminaChanged += UpdateStamina;

        currentHealthFill = playerStats.MaxHealth > 0f ? playerStats.CurrentHealth / playerStats.MaxHealth * 100f : 0f;
        currentFpFill = playerStats.MaxFP > 0f ? playerStats.CurrentFP / playerStats.MaxFP * 100f : 0f;
        currentStaminaFill = playerStats.MaxStamina > 0f ? playerStats.CurrentStamina / playerStats.MaxStamina * 100f : 0f;

        healthFillTarget = currentHealthFill;
        fpFillTarget = currentFpFill;
        staminaFillTarget = currentStaminaFill;

        healthTextTarget = playerStats.CurrentHealth;
        fpTextTarget = playerStats.CurrentFP;
        staminaTextTarget = playerStats.CurrentStamina;

        healthTextCurrent = healthTextTarget;
        fpTextCurrent = fpTextTarget;
        staminaTextCurrent = staminaTextTarget;

        prevHealth = playerStats.CurrentHealth;
        prevFP = playerStats.CurrentFP;
        prevStamina = playerStats.CurrentStamina;

        if (healthBar != null)
            healthBar.style.width = Length.Percent(currentHealthFill);
        if (fpBar != null)
            fpBar.style.width = Length.Percent(currentFpFill);
        if (staminaBar != null)
            staminaBar.style.width = Length.Percent(currentStaminaFill);

        isBound = true;
    }

    private void UpdateHealth(float current, float max)
    {
        healthFillTarget = max > 0f ? current / max * 100f : 0f;
        healthTextTarget = current;
        healthAnimTimer = 0f;

        if (current < prevHealth)
            healthFlashTimer = flashDuration;
        else if (current > prevHealth)
            healthFlashTimer = flashDuration;
        prevHealth = current;
    }

    private void UpdateFP(float current, float max)
    {
        fpFillTarget = max > 0f ? current / max * 100f : 0f;
        fpTextTarget = current;
        fpAnimTimer = 0f;

        if (current < prevFP)
            fpFlashTimer = flashDuration;
        else if (current > prevFP)
            fpFlashTimer = flashDuration;
        prevFP = current;
    }

    private void UpdateStamina(float current, float max)
    {
        staminaFillTarget = max > 0f ? current / max * 100f : 0f;
        staminaTextTarget = current;
        staminaAnimTimer = 0f;

        if (current < prevStamina)
            staminaFlashTimer = flashDuration;
        else if (current > prevStamina)
            staminaFlashTimer = flashDuration;
        prevStamina = current;
    }

    void Update()
    {
        if (!isBound)
            TryBindPlayerStats();

        // Animate health
        if (playerStats != null && healthAnimTimer < animationDuration)
        {
            healthAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(healthAnimTimer / animationDuration);
            currentHealthFill = Mathf.Lerp(currentHealthFill, healthFillTarget, t);
            float currentText = Mathf.Lerp(healthTextCurrent, healthTextTarget, t);
            healthTextCurrent = currentText;

            if (healthBar != null)
                healthBar.style.width = Length.Percent(currentHealthFill);
        }

        // Health flash
        if (healthFlashTimer > 0f)
        {
            healthFlashTimer -= Time.deltaTime;
            float flashAlpha = healthFlashTimer / flashDuration;
            Color flashColor = prevHealth > healthTextTarget ? damageFlashColor : healFlashColor;
            flashColor.a = flashAlpha * 0.5f;
            if (healthBar != null)
                healthBar.style.backgroundColor = flashColor;
        }
        else if (healthBar != null)
        {
            healthBar.style.backgroundColor = Color.clear;
        }

        // Animate FP
        if (playerStats != null && fpAnimTimer < animationDuration)
        {
            fpAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fpAnimTimer / animationDuration);
            currentFpFill = Mathf.Lerp(currentFpFill, fpFillTarget, t);
            float currentText = Mathf.Lerp(fpTextCurrent, fpTextTarget, t);
            fpTextCurrent = currentText;

            if (fpBar != null)
                fpBar.style.width = Length.Percent(currentFpFill);
        }

        // FP flash
        if (fpFlashTimer > 0f)
        {
            fpFlashTimer -= Time.deltaTime;
            float flashAlpha = fpFlashTimer / flashDuration;
            Color flashColor = prevFP > fpTextTarget ? damageFlashColor : healFlashColor;
            flashColor.a = flashAlpha * 0.5f;
            if (fpBar != null)
                fpBar.style.backgroundColor = flashColor;
        }
        else if (fpBar != null)
        {
            fpBar.style.backgroundColor = Color.clear;
        }

        // Animate Stamina
        if (playerStats != null && staminaAnimTimer < animationDuration)
        {
            staminaAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(staminaAnimTimer / animationDuration);
            currentStaminaFill = Mathf.Lerp(currentStaminaFill, staminaFillTarget, t);
            float currentText = Mathf.Lerp(staminaTextCurrent, staminaTextTarget, t);
            staminaTextCurrent = currentText;

            if (staminaBar != null)
                staminaBar.style.width = Length.Percent(currentStaminaFill);
        }

        // Stamina flash
        if (staminaFlashTimer > 0f)
        {
            staminaFlashTimer -= Time.deltaTime;
            float flashAlpha = staminaFlashTimer / flashDuration;
            Color flashColor = prevStamina > staminaTextTarget ? damageFlashColor : healFlashColor;
            flashColor.a = flashAlpha * 0.5f;
            if (staminaBar != null)
                staminaBar.style.backgroundColor = flashColor;
        }
        else if (staminaBar != null)
        {
            staminaBar.style.backgroundColor = Color.clear;
        }
    }
}
