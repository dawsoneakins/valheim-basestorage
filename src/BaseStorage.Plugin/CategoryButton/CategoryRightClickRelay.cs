namespace BaseStorage.Plugin.CategoryButton;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The right-click relay of the Category button (contract S-E004-004 section 4.2, DESIGN-SYSTEM component 3). A vanilla Button only
/// answers the left button, so this component sits beside it on the clone and forwards a right click while the button is enabled.
/// It has no Unity frame method.
/// </summary>
internal sealed class CategoryRightClickRelay : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Forwards a right click to the cycle-back handler when the sibling button is interactable.</summary>
    /// <param name="eventData">The click event.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        Button button = GetComponent<Button>();
        if (button == null || !button.interactable)
        {
            return;
        }

        ChestCategoryButton.OnRightClick();
    }
}
