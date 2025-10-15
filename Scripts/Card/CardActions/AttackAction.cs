using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Characters;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class AttackAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Attack;
        public override void DoAction(CardActionParameters actionParameters)
        {
            if (!actionParameters.TargetCharacter) return;

            var targetCharacter = actionParameters.TargetCharacter;
            var selfCharacter = actionParameters.SelfCharacter;

            // ค่าฐานโจมตีเฉพาะรอบ (ผู้เล่นได้จาก AllyBase.RunBaseAttackBonus)
            int runBaseAtk = 0;
            if (selfCharacter is AllyBase ally)
            {
                // ฝั่งผู้เล่น: ใช้ค่าที่อัดไว้ตอนเข้าด่าน (รวม base + แต้มอัปในรอบนี้แล้ว)
                runBaseAtk = ally.RunBaseAttackBonus;
            }
            else
            {
                // ฝั่งศัตรู/อื่นๆ: ใช้ค่า base จาก CharacterData ตามเดิม
                runBaseAtk = selfCharacter.CharacterData.BaseAttackPower;
            }

            var strength = selfCharacter.CharacterStats.StatusDict[StatusType.Strength].StatusValue;

            // ดาเมจรวม = ค่าการ์ด + STR + BaseAtk(ตามรอบ)
            var value = actionParameters.Value + strength + runBaseAtk;

            targetCharacter.CharacterStats.Damage(Mathf.RoundToInt(value));

            if (FxManager != null)
            {
                FxManager.PlayFx(actionParameters.TargetCharacter.transform, FxType.Attack);
                FxManager.SpawnFloatingText(actionParameters.TargetCharacter.TextSpawnRoot, value.ToString());
            }

            if (AudioManager != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}