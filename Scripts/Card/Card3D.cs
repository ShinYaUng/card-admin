using System.Collections;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card
{
    public class Card3D : CardBase
    {
        [Header("3D Settings")]
        [SerializeField] private Canvas canvas;

        public override void SetCard(CardData targetProfile, bool isPlayable)
        {
            base.SetCard(targetProfile, isPlayable);

            if (!canvas) return;

            // พยายามใช้กล้องจาก HandController ถ้ามี (เช่นในฉากต่อสู้)
            var handCtrl = CollectionManager?.HandController; // อาจเป็น null ได้ใน SceneMap
            if (handCtrl != null && handCtrl.cam != null)
            {
                canvas.worldCamera = handCtrl.cam;
            }
            else
            {
                // Fallback: ใช้กล้องหลักของ UI ปัจจุบัน
                // - ถ้า UI ของคุณมี "UICamera" เฉพาะ ให้เปลี่ยนมาอ้างถึงตัวนั้นแทน
                // - ถ้าใช้ Screen Space - Overlay จริง ๆ บรรทัดนี้ไม่จำเป็น
                var uiCam = Camera.main;
                if (uiCam != null) canvas.worldCamera = uiCam;
                // ถ้าอยากกัน NRE แบบชัวร์สุด จะไม่ตั้งอะไรเลยก็ได้ในกรณีที่ uiCam เป็น null
            }
            // หลังตั้งกล้องเสร็จ
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas && canvas != parentCanvas)
            {
                canvas.renderMode = parentCanvas.renderMode;
                canvas.worldCamera = parentCanvas.worldCamera;
                canvas.sortingLayerID = parentCanvas.sortingLayerID;
                canvas.overrideSorting = true;
                canvas.sortingOrder = parentCanvas.sortingOrder + 1; // ให้อยู่เหนือพื้นของ slot
            }
        }

        public override void SetInactiveMaterialState(bool isInactive)
        {
            base.SetInactiveMaterialState(isInactive);
        }
    }
}