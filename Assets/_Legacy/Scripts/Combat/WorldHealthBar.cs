using System.Collections;
using UnityEngine;

public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] Vector2 barSize = new(1.4f, 0.14f);
    [SerializeField] Vector3 localOffset = new(0f, 1.35f, 0f);

    Transform barRoot;
    Transform fillTransform;
    TextMesh valueText;
    TextMesh totalDamageText;
    Health health;
    float totalDamage;

    public float TotalDamage => totalDamage;

    public void Bind(Health targetHealth)
    {
        health = targetHealth;
        health.HealthChanged += OnHealthChanged;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        OnHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    void Awake()
    {
        CreateVisuals();
    }

    void OnDestroy()
    {
        if (health == null)
            return;

        health.HealthChanged -= OnHealthChanged;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    void CreateVisuals()
    {
        barRoot = new GameObject("HealthBar").transform;
        barRoot.SetParent(transform);
        barRoot.localPosition = localOffset;

        var background = new GameObject("Background");
        background.transform.SetParent(barRoot);
        var bgRenderer = background.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CombatSpriteUtil.CreateRectSprite(8, 2, new Color(0.08f, 0.08f, 0.1f, 0.85f));
        bgRenderer.drawMode = SpriteDrawMode.Sliced;
        bgRenderer.size = barSize;
        bgRenderer.sortingOrder = 30;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(barRoot);
        fillTransform = fill.transform;
        fillTransform.localPosition = new Vector3(-barSize.x * 0.5f, 0f, 0f);
        var fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CombatSpriteUtil.CreateRectSprite(8, 2, new Color(0.35f, 0.9f, 0.45f, 0.95f));
        fillRenderer.drawMode = SpriteDrawMode.Sliced;
        fillRenderer.size = barSize;
        fillRenderer.sortingOrder = 31;

        var valueObject = new GameObject("HealthValue");
        valueObject.transform.SetParent(barRoot);
        valueObject.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        valueText = valueObject.AddComponent<TextMesh>();
        valueText.characterSize = 0.08f;
        valueText.fontSize = 48;
        valueText.anchor = TextAnchor.MiddleCenter;
        valueText.color = new Color(0.95f, 0.98f, 0.92f);
        valueText.GetComponent<MeshRenderer>().sortingOrder = 32;

        var totalObject = new GameObject("TotalDamage");
        totalObject.transform.SetParent(barRoot);
        totalObject.transform.localPosition = new Vector3(0f, -0.24f, 0f);
        totalDamageText = totalObject.AddComponent<TextMesh>();
        totalDamageText.characterSize = 0.07f;
        totalDamageText.fontSize = 40;
        totalDamageText.anchor = TextAnchor.MiddleCenter;
        totalDamageText.color = new Color(1f, 0.72f, 0.45f);
        totalDamageText.GetComponent<MeshRenderer>().sortingOrder = 32;
    }

    void LateUpdate()
    {
        if (barRoot == null)
            return;

        float sign = Mathf.Sign(transform.lossyScale.x);
        if (Mathf.Approximately(sign, 0f))
            sign = 1f;
        barRoot.localScale = new Vector3(sign, 1f, 1f);
    }

    void OnHealthChanged(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        fillTransform.localScale = new Vector3(ratio, 1f, 1f);

        var fillRenderer = fillTransform.GetComponent<SpriteRenderer>();
        if (fillRenderer != null)
        {
            fillRenderer.size = new Vector2(barSize.x * ratio, barSize.y);
            fillRenderer.color = ratio <= 0.25f
                ? new Color(0.95f, 0.35f, 0.3f, 0.95f)
                : new Color(0.35f, 0.9f, 0.45f, 0.95f);
        }

        fillTransform.localPosition = new Vector3(-barSize.x * 0.5f + barSize.x * ratio * 0.5f, 0f, 0f);
        valueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        UpdateTotalDamageLabel();
    }

    void OnDamaged(float amount)
    {
        totalDamage += amount;
        UpdateTotalDamageLabel();
        DamagePopup.Spawn(transform.position + Vector3.up * 1.1f, amount);
    }

    void OnDied()
    {
        barRoot.gameObject.SetActive(false);
    }

    public void ResetTotalDamage()
    {
        totalDamage = 0f;
        UpdateTotalDamageLabel();
        barRoot.gameObject.SetActive(true);
    }

    void UpdateTotalDamageLabel()
    {
        totalDamageText.text = $"누적 피해 {totalDamage:0.#}";
    }

    public void SetVisible(bool visible)
    {
        if (barRoot != null)
            barRoot.gameObject.SetActive(visible);
    }
}
