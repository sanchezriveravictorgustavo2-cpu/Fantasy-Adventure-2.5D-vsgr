using Game.Systems.Army;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.HUD
{
    /// <summary>
    /// Controls the three independent Army Command HUD buttons:
    /// - Button A: Atacar (Attack)
    /// - Button B: Formar Filas (FormRanks)
    /// - Button C: Esconder Ejército (HideArmy)
    /// Highlights the active order cleanly.
    /// </summary>
    [DisallowMultipleComponent]
    public class ArmyCommandButtonsUI : MonoBehaviour
    {
        [Header("System Reference")]
        [SerializeField] private ArmyCommandController commandController;

        [Header("Command Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button formRanksButton;
        [SerializeField] private Button hideArmyButton;

        [Header("Button Backgrounds (Optional Highlight)")]
        [SerializeField] private Image attackBackground;
        [SerializeField] private Image formRanksBackground;
        [SerializeField] private Image hideArmyBackground;

        [Header("Highlight Colors")]
        [SerializeField] private Color inactiveBgColor = new Color(0.10f, 0.19f, 0.28f, 0.90f);
        [SerializeField] private Color activeAttackColor = new Color(0.68f, 0.16f, 0.18f, 0.96f);
        [SerializeField] private Color activeFormColor = new Color(0.14f, 0.50f, 0.68f, 0.96f);
        [SerializeField] private Color activeHideColor = new Color(0.16f, 0.56f, 0.42f, 0.96f);

        private void Awake()
        {
            if (commandController == null)
            {
                commandController = FindFirstObjectByType<ArmyCommandController>();
            }
        }

        private void OnEnable()
        {
            if (attackButton != null) attackButton.onClick.AddListener(HandleAttackClicked);
            if (formRanksButton != null) formRanksButton.onClick.AddListener(HandleFormRanksClicked);
            if (hideArmyButton != null) hideArmyButton.onClick.AddListener(HandleHideArmyClicked);

            if (commandController != null)
            {
                commandController.OnOrderChanged += HandleOrderChanged;
                HandleOrderChanged(commandController.CurrentOrder);
            }
        }

        private void OnDisable()
        {
            if (attackButton != null) attackButton.onClick.RemoveListener(HandleAttackClicked);
            if (formRanksButton != null) formRanksButton.onClick.RemoveListener(HandleFormRanksClicked);
            if (hideArmyButton != null) hideArmyButton.onClick.RemoveListener(HandleHideArmyClicked);

            if (commandController != null)
            {
                commandController.OnOrderChanged -= HandleOrderChanged;
            }
        }

        private void HandleAttackClicked()
        {
            if (commandController != null)
            {
                commandController.OrderAttack();
            }
        }

        private void HandleFormRanksClicked()
        {
            if (commandController != null)
            {
                commandController.OrderFormRanks();
            }
        }

        private void HandleHideArmyClicked()
        {
            if (commandController != null)
            {
                commandController.OrderHideArmy();
            }
        }

        private void HandleOrderChanged(ArmyOrder order)
        {
            if (attackBackground != null)
            {
                attackBackground.color = order == ArmyOrder.Attack ? activeAttackColor : inactiveBgColor;
            }

            if (formRanksBackground != null)
            {
                formRanksBackground.color = order == ArmyOrder.FormRanks ? activeFormColor : inactiveBgColor;
            }

            if (hideArmyBackground != null)
            {
                hideArmyBackground.color = order == ArmyOrder.HideArmy ? activeHideColor : inactiveBgColor;
            }
        }

        public void ConfigureReferences(
            ArmyCommandController controller,
            Button atkBtn,
            Image atkBg,
            Button formBtn,
            Image formBg,
            Button hideBtn,
            Image hideBg)
        {
            commandController = controller;
            attackButton = atkBtn;
            attackBackground = atkBg;
            formRanksButton = formBtn;
            formRanksBackground = formBg;
            hideArmyButton = hideBtn;
            hideArmyBackground = hideBg;
        }
    }
}
