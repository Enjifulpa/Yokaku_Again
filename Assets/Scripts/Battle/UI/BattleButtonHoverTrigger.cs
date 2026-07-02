using UnityEngine;
using UnityEngine.EventSystems;

namespace Battle.UI
{
    /// <summary>
    /// Taruh di masing2 tombol (Battle, Switch, Item, Escape).
    /// Set "Index" sesuai urutan slot di list BattlePanelHoverController:
    /// 0 = Battle, 1 = Switch, 2 = Item, 3 = Escape.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BattleButtonHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private BattlePanelHoverController controller;
        [SerializeField] private int index;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (controller != null) controller.OnButtonHoverEnter(index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (controller != null) controller.OnButtonHoverExit(index);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller != null) controller.OnButtonClicked(index);
        }
    }
}
