using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la UI del panel en la esquina inferior derecha:
/// - muestra Tipo de arma, Modelo, Munición, Cargador y Características
/// - botones prev / next / select
/// </summary>
public class WeaponPanelUI : MonoBehaviour
{
    [Header("Inventory reference")]
    [SerializeField] private WeaponInventoryManager inventory;

    [Header("Texts (TMP)")]
    public TMP_Text typeText;
    public TMP_Text modelText;
    public TMP_Text ammoText;
    public TMP_Text magazineText;
    public TMP_Text characteristicsText;

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button selectButton;

    [Header("Visuals")]
    public GameObject activeIndicator;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindObjectOfType<WeaponInventoryManager>();
    }

    // =========================
    // UI BUTTON CALLBACKS
    // =========================

    public void OnNextPressed()
    {
        inventory?.NextPreview();
    }

    public void OnPrevPressed()
    {
        inventory?.PrevPreview();
    }

    public void OnSelectPressed()
    {
        inventory?.SelectPreviewAsActive();
    }

    // =========================
    // PANEL UPDATE
    // =========================

    public void UpdatePanel(string displayName,
                            string weaponType,
                            string modelName,
                            string ammoName,
                            int magazineSize,
                            string characteristics,
                            bool isActive,
                            bool selectionLocked)
    {
        if (typeText != null) typeText.text = $"Tipo: {weaponType}";
        if (modelText != null) modelText.text = $"Modelo: {modelName}";
        if (ammoText != null) ammoText.text = $"Munición: {ammoName}";
        if (magazineText != null) magazineText.text = $"Cargador: {magazineSize} tiros";
        if (characteristicsText != null) characteristicsText.text = characteristics ?? "";

        bool canChange = !selectionLocked;

        if (prevButton != null) prevButton.interactable = canChange;
        if (nextButton != null) nextButton.interactable = canChange;

        if (selectButton != null)
        {
            selectButton.interactable = canChange && !isActive;
            selectButton.GetComponentInChildren<TMP_Text>()
                ?.SetText(isActive ? "Selected" : "Select");
        }

        if (activeIndicator != null)
            activeIndicator.SetActive(isActive);
    }

    public void SetEmpty()
    {
        if (typeText != null) typeText.text = "-";
        if (modelText != null) modelText.text = "-";
        if (ammoText != null) ammoText.text = "-";
        if (magazineText != null) magazineText.text = "-";
        if (characteristicsText != null) characteristicsText.text = "-";

        if (prevButton != null) prevButton.interactable = false;
        if (nextButton != null) nextButton.interactable = false;
        if (selectButton != null) selectButton.interactable = false;

        if (activeIndicator != null)
            activeIndicator.SetActive(false);
    }
}
