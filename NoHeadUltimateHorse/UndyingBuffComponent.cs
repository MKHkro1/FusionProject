using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoHeadUltimateHorse.BepInEx
{
    public class UndyingBuffComponent : MonoBehaviour
    {
        public Zombie? targetZombie = null;

        public float undyingTimer = 0f;

        public bool isUndying = false;

        private readonly List<SpriteRenderer> cachedRenderers = new List<SpriteRenderer>();
        private readonly List<Color> originalColors = new List<Color>();
        private bool renderersInitialized = false;

        public void SetUndying(float duration)
        {
            this.undyingTimer = duration;
            this.isUndying = true;

            if (this.targetZombie == null)
            {
                this.targetZombie = base.gameObject.GetComponent<Zombie>();
            }

            if (this.targetZombie != null)
            {
                this.InitializeRenderers(this.targetZombie);
            }
        }

        public void ApplyGreenTint()
        {
            try
            {
                if (this.targetZombie != null)
                {
                    this.InitializeRenderers(this.targetZombie);
                }

                for (int i = 0; i < this.cachedRenderers.Count; i++)
                {
                    if (this.cachedRenderers[i] != null)
                    {
                        this.cachedRenderers[i].color = new Color(0f, 1f, 0f, 1f);
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogWarning($"无头终极马僵尸插件: ApplyGreenTint失败: {ex.Message}");
            }
        }

        public void RestoreOriginalColors()
        {
            try
            {
                for (int i = 0; i < this.cachedRenderers.Count; i++)
                {
                    if (this.cachedRenderers[i] != null)
                    {
                        Color original = (i < this.originalColors.Count) ? this.originalColors[i] : Color.white;
                        this.cachedRenderers[i].color = original;
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogWarning($"无头终极马僵尸插件: RestoreOriginalColors失败: {ex.Message}");
            }
        }

        private void InitializeRenderers(Zombie zombie)
        {
            if (this.renderersInitialized || zombie == null || zombie.gameObject == null)
                return;

            this.cachedRenderers.Clear();
            this.originalColors.Clear();

            SpriteRenderer[] renderers = zombie.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    this.cachedRenderers.Add(renderer);
                    this.originalColors.Add(renderer.color);
                }
            }

            this.renderersInitialized = true;
        }

        public void Update()
        {
            if (this.targetZombie == null)
            {
                this.targetZombie = base.gameObject.GetComponent<Zombie>();
            }

            if (this.isUndying && this.undyingTimer > 0f)
            {
                this.undyingTimer -= Time.deltaTime;

                if (this.targetZombie != null)
                {
                    this.ApplyGreenTint();
                }

                if (this.undyingTimer <= 0f)
                {
                    this.isUndying = false;
                    this.undyingTimer = 0f;
                    this.RestoreOriginalColors();
                }
            }
        }

        private void OnDisable()
        {
            if (this.isUndying)
            {
                this.RestoreOriginalColors();
            }
        }

        private void OnDestroy()
        {
            this.RestoreOriginalColors();
        }
    }
}
