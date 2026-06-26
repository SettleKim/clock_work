using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerComboController))]
public class PlayerParryController : MonoBehaviour
{
    [SerializeField] float parryWindowDuration = 0.5f;
    [SerializeField] float slowMotionDuration = 3f;
    [SerializeField] float slowMotionScale = 0.15f;

    PlayerComboController comboController;
    InputAction blockAction;
    float parryWindowEndUnscaledTime;

    public bool IsParryWindowOpen => Time.unscaledTime <= parryWindowEndUnscaledTime;

    void Awake()
    {
        blockAction = GetComponent<PlayerInput>().actions.FindAction("Block", false);
        comboController = GetComponent<PlayerComboController>();
    }

    void Update()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen || comboController.IsComboActive)
            return;

        bool blockPressed = blockAction != null && blockAction.WasPressedThisFrame();
        if (!blockPressed && Keyboard.current != null)
            blockPressed = Keyboard.current.wKey.wasPressedThisFrame;

        if (blockPressed)
        {
            parryWindowEndUnscaledTime = Time.unscaledTime + parryWindowDuration;
            GetComponentInChildren<PlayerGearbotVisual>()?.PlayGuard();
        }
    }

    public bool TryParryIncomingDamage(float amount, GameObject source)
    {
        if (!IsParryWindowOpen || amount <= 0f)
            return false;

        parryWindowEndUnscaledTime = 0f;
        GetComponentInChildren<PlayerGearbotVisual>()?.OnGuardSucceeded();
        WorldSlowMotion.Enter(slowMotionDuration, slowMotionScale);
        comboController.BeginComboSelection();

        if (source != null && source.TryGetComponent(out BossController boss))
            boss.OnChargeParried();

        return true;
    }
}
