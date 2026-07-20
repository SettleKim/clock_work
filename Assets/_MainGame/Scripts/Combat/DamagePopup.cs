using System.Collections;
using UnityEngine;

namespace ClockWork.Game
{
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] float lifetime = 0.75f;
        [SerializeField] float riseSpeed = 1.4f;

        TextMesh textMesh;
        Color startColor;

        public static void Spawn(Vector3 worldPosition, float damage)
        {
            var popupObject = new GameObject("DamagePopup");
            popupObject.transform.position = worldPosition + (Vector3)(Random.insideUnitCircle * 0.08f);

            var popup = popupObject.AddComponent<DamagePopup>();
            popup.Initialize(damage);
        }

        void Initialize(float damage)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = $"-{damage:0.#}";
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 56;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = new Color(1f, 0.45f, 0.35f);

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.sortingOrder = 40;

            startColor = textMesh.color;
            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            float timer = 0f;
            while (timer < lifetime)
            {
                timer += Time.deltaTime;
                transform.position += Vector3.up * riseSpeed * Time.deltaTime;
                float alpha = 1f - timer / lifetime;
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
