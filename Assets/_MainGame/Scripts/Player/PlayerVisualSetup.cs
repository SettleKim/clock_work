using UnityEngine;

namespace ClockWork.Game
{
    static class PlayerVisualSetup
    {
        const string ControllerPath = "Assets/_MainGame/art/player/visual.controller";

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

            if (visualObject.GetComponent<PlayerCharacterVisual>() == null)
                visualObject.AddComponent<PlayerCharacterVisual>();

            if (visualObject.GetComponent<PlayerAttackAnimEvents>() == null)
                visualObject.AddComponent<PlayerAttackAnimEvents>();

            var spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 10;

#if UNITY_EDITOR
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controller != null)
                animator.runtimeAnimatorController = controller;
#endif
        }

        static void DisableRootSprite(GameObject player)
        {
            if (player.GetComponentInChildren<PlayerCharacterVisual>() == null)
                return;

            var rootSprite = player.GetComponent<SpriteRenderer>();
            if (rootSprite != null)
                rootSprite.enabled = false;
        }
    }
}
