using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GroundVisual : MonoBehaviour
{
    [SerializeField] Color groundColor = new(0.45f, 0.38f, 0.28f);

    void Awake()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = CombatSpriteUtil.CreateGroundSprite(64, 8);
        spriteRenderer.color = groundColor;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = new Vector2(30f, 1f);
    }
}
