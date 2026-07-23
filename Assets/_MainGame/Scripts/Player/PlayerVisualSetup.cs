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
            EnsureBackWeaponSlot(player);
        }

        static void EnsureBackWeaponSlot(GameObject player)
        {
            Transform backTransform = player.transform.Find("BackWeaponSlot");
            if (backTransform == null)
            {
                var backObject = new GameObject("BackWeaponSlot");
                backTransform = backObject.transform;
                backTransform.SetParent(player.transform);
                backTransform.localPosition = new Vector3(0f, 2.4f, 0f);
                backTransform.localRotation = Quaternion.identity;
                backTransform.localScale = Vector3.one;
            }

            var backObject2 = backTransform.gameObject;
            if (backObject2.GetComponent<SpriteRenderer>() == null)
                backObject2.AddComponent<SpriteRenderer>();
            if (backObject2.GetComponent<PlayerBackWeaponVisual>() == null)
                backObject2.AddComponent<PlayerBackWeaponVisual>();

            var backSprite = backObject2.GetComponent<SpriteRenderer>();
            backSprite.sortingOrder = 9;
            backSprite.enabled = false;
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
