using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class BlockAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Block;
        public override void DoAction(CardActionParameters actionParameters)
        {
            var newTarget = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;

            if (!newTarget) return;

            // ค่าบล็อกพื้นฐานเดิม (ค่าในการ์ด + Dexterity)
            var baseBlock = actionParameters.Value +
                            actionParameters.SelfCharacter.CharacterStats.StatusDict[StatusType.Dexterity].StatusValue;

            // ดึงตัวคูณจาก Relic (เช่น DoubleBlock = x2) ที่เราเก็บไว้ใน PersistentGameplayData
            var mul = GameManager.Instance.PersistentGameplayData.GetBlockGainMultiplier();

            var finalBlock = Mathf.RoundToInt(baseBlock * mul);

            newTarget.CharacterStats.ApplyStatus(StatusType.Block, finalBlock);

            if (FxManager != null)
                FxManager.PlayFx(newTarget.transform, FxType.Block);

            if (AudioManager != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }

    }
}