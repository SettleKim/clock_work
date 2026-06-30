using UnityEngine;

namespace ClockWork.Game
{
    static class PlayerVisualSetup
    {
        const string ControllerPath = "Assets/Game/art/player/visual.controller";

        public static void EnsureAndConfigure(GameObject player)
        {
            if (player == null)
                return;

            Transform visualTransform = FindOrCreateVisual(player);
            ConfigureComponents(visualTransform);
            DisableRootSprite(player);
        }

        static Transform FindOrCreateVisual(GameObject player)
        {
            Transform visualTransform = player.transform.Find("Visual");
            if (visualTransform == null)
                visualTransform = player.transform.Find("visual");

            if (visualTransform == null)
            {
                var visualObject = new GameObject("Visual");
                visualTransform = visualObject.transform;
                visualTransform.SetParent(player.transform);
            }

            visualTransform.localPosition = Vector3.zero;
            visualTransform.localRotation = Quaternion.identity;
            visualTransform.localScale = Vector3.one;
            return visualTransform;
        }

        static void ConfigureComponents(Transform visualTransform)
        {
            var visualObject = visualTransform.gameObject;

            if (visualObject.GetComponent<SpriteRenderer>() == null)
                visualObject.AddComponent<SpriteRenderer>();

            var animator = visualObject.GetComponent<Animator>();
            if (animator == null)
                animator = visualObject.AddComponent<Animator>();

            var characterVisual = visualObject.GetComponent<PlayerCharacterVisual>();
            if (characterVisual == null)
                characterVisual = visualObject.AddComponent<PlayerCharacterVisual>();

            var spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 10;

            ResolveAssets(out RuntimeAnimatorController controller, out Sprite idleRight, out Sprite idleLeft);

            if (controller != null)
                animator.runtimeAnimatorController = controller;

            if (idleRight != null || idleLeft != null)
                characterVisual.ConfigureIdleSprites(idleRight, idleLeft);

            if (spriteRenderer.sprite == null && idleRight != null)
                spriteRenderer.sprite = idleRight;
        }

        static void DisableRootSprite(GameObject player)
        {
            if (player.GetComponentInChildren<PlayerCharacterVisual>() == null)
                return;

            var rootSprite = player.GetComponent<SpriteRenderer>();
            if (rootSprite != null)
                rootSprite.enabled = false;
        }

        static void ResolveAssets(
            out RuntimeAnimatorController controller,
            out Sprite idleRight,
            out Sprite idleLeft)
        {
            controller = null;
            idleRight = null;
            idleLeft = null;

#if UNITY_EDITOR
            controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            if (PlayerSpriteSheetResolver.TryGetIdleSprites(out Sprite left, out Sprite right))
            {
                idleLeft = left;
                idleRight = right;
            }
#endif
        }
    }
}
