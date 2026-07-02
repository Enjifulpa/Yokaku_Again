using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace Battle.UI
{
    /// <summary>
    /// Taruh script ini di GameObject "Battle_Panel" (parent dari tombol: Battle, Switch, Item, Escape).
    ///
    /// Arrow TIDAK perlu di-reparent dari "Battle" ke tempat lain di Hierarchy.
    /// Kita cukup gerakin anchoredPosition-nya secara runtime supaya nongol di atas
    /// tombol mana pun yang lagi di-hover.
    /// </summary>
    public class BattlePanelHoverController : MonoBehaviour
    {
        [System.Serializable]
        public class BattleButtonSlot
        {
            public string label; // buat gampang baca di Inspector: "Battle","Switch","Item","Escape"
            public RectTransform buttonRect;

            [HideInInspector] public Vector2 restingPos; // posisi normal (turun)
            [HideInInspector] public Vector2 hoveredPos; // posisi pas naik
        }

        [Header("Urutan HARUS sama dengan index di BattleButtonHoverTrigger")]
        [SerializeField] private List<BattleButtonSlot> buttons = new List<BattleButtonSlot>();

        [Header("Arrow Indicator (boleh tetap child dari 'Battle')")]
        [SerializeField] private RectTransform arrow;
        [SerializeField] private Vector2 arrowOffset = new Vector2(0f, 20f); // jarak arrow di atas tombol

        [Header("Animasi")]
        [SerializeField] private float hoverRiseAmount = 15f; // naik berapa px
        [SerializeField] private float moveSpeed = 12f;       // makin besar makin snappy

        [Header("Escape")]
        [Tooltip("Index tombol Escape sesuai urutan di list 'buttons' (default 3).")]
        [SerializeField] private int escapeIndex = 3;
        [Tooltip("Dipakai kalau battle scene di-test langsung (bukan lewat encounter di Town), jadi EncounterState kosong.")]
        [SerializeField] private string fallbackOverworldSceneName = "Town";

        [Header("Aksi Tombol Lain (opsional, buat nanti)")]
        [SerializeField] private UnityEvent onBattleClicked;
        [SerializeField] private UnityEvent onSwitchClicked;
        [SerializeField] private UnityEvent onItemClicked;

        private int currentHoverIndex = -1;
        private Coroutine moveRoutine;

        private void Awake()
        {
            foreach (var slot in buttons)
            {
                slot.restingPos = slot.buttonRect.anchoredPosition;
                slot.hoveredPos = slot.restingPos + new Vector2(0f, hoverRiseAmount);
            }

            if (arrow != null)
                arrow.gameObject.SetActive(false);
        }

        public void OnButtonHoverEnter(int index)
        {
            if (index < 0 || index >= buttons.Count) return;

            currentHoverIndex = index;

            if (arrow != null)
            {
                arrow.gameObject.SetActive(true);
                PositionArrowAbove(buttons[index].buttonRect);
            }

            RestartMoveRoutine();
        }

        public void OnButtonHoverExit(int index)
        {
            // hanya reset kalau yang exit itu tombol yang lagi aktif,
            // biar gak flicker waktu pointer pindah cepat antar tombol
            if (currentHoverIndex != index) return;

            currentHoverIndex = -1;

            if (arrow != null)
                arrow.gameObject.SetActive(false);

            RestartMoveRoutine();
        }

        public void OnButtonClicked(int index)
        {
            if (index == escapeIndex)
            {
                HandleEscape();
                return;
            }

            if (index == 0) onBattleClicked?.Invoke();
            else if (index == 1) onSwitchClicked?.Invoke();
            else if (index == 2) onItemClicked?.Invoke();
        }

        private void HandleEscape()
        {
            // Kalau ada data encounter (player kabur dari battle beneran), balik ke scene & posisi asalnya.
            // Kalau gak ada (misal battle scene di-Play langsung buat testing), fallback ke overworld default.
            string targetScene = EncounterState.HasPendingReturn
                ? EncounterState.PreviousSceneName
                : fallbackOverworldSceneName;

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("Escape ditekan, tapi gak ada scene tujuan (EncounterState kosong & fallbackOverworldSceneName kosong).");
                return;
            }

            SceneManager.LoadScene(targetScene);
        }

        private void PositionArrowAbove(RectTransform target)
        {
            arrow.anchoredPosition = target.anchoredPosition + arrowOffset;
        }

        private void RestartMoveRoutine()
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveButtonsRoutine());
        }

        private IEnumerator MoveButtonsRoutine()
        {
            bool stillMoving = true;
            while (stillMoving)
            {
                stillMoving = false;

                for (int i = 0; i < buttons.Count; i++)
                {
                    var slot = buttons[i];
                    Vector2 target = (i == currentHoverIndex) ? slot.hoveredPos : slot.restingPos;
                    Vector2 current = slot.buttonRect.anchoredPosition;

                    if ((current - target).sqrMagnitude > 0.01f)
                    {
                        slot.buttonRect.anchoredPosition = Vector2.Lerp(current, target, Time.deltaTime * moveSpeed);
                        stillMoving = true;
                    }
                    else
                    {
                        slot.buttonRect.anchoredPosition = target;
                    }

                    // arrow ikut ngikutin real-time selama tombol yang di-hover masih bergerak naik
                    if (i == currentHoverIndex && arrow != null)
                        PositionArrowAbove(slot.buttonRect);
                }

                yield return null;
            }
        }
    }
}
