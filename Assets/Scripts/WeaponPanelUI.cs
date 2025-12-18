using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponPanelUI : MonoBehaviour
{
    [SerializeField] private WeaponInventoryManager inventory;
    public TMP_Text typeText, modelText, ammoText, magazineText, characteristicsText;
    public Button prevButton, nextButton, selectButton;
    public GameObject activeIndicator;

    private void Awake() { if (inventory == null) inventory = FindObjectOfType<WeaponInventoryManager>(); }

    public void OnNextPressed() => inventory?.NextPreview();
    public void OnPrevPressed() => inventory?.PrevPreview();
    public void OnSelectPressed() => inventory?.SelectPreviewAsActive();

    public void UpdatePanel(string displayName, string weaponType, string modelName, string ammoName,
                            int magazineSize, string characteristics, bool isActive, bool selectionLocked)
    {
        if (typeText != null) typeText.text = $"Tipo: {weaponType}";
        if (modelText != null) modelText.text = $"Modelo: {modelName}";
        if (ammoText != null) ammoText.text = $"Munición: {ammoName}";
        if (magazineText != null) magazineText.text = $"Cargador: {magazineSize} tiros";
        if (characteristicsText != null) characteristicsText.text = characteristics ?? "";

        bool canChange = !selectionLocked;
        if (prevButton != null) prevButton.interactable = canChange;
        if (nextButton != null) nextButton.interactable = canChange;
        if (selectButton != null) selectButton.interactable = canChange && !isActive;
        selectButton?.GetComponentInChildren<TMP_Text>()?.SetText(isActive ? "Selected" : "Select");
        if (activeIndicator != null) activeIndicator.SetActive(isActive);
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
        if (activeIndicator != null) activeIndicator.SetActive(false);
    }
}
