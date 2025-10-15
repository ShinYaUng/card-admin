using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;

public static class CardDataRuntimeFactory
{
    static void Set<T>(CardData cd, string field, T val)
    {
        var fi = typeof(CardData).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        fi?.SetValue(cd, val);
    }

    public static RarityType ParseRarity(string s) =>
      Enum.TryParse<RarityType>(s, true, out var r) ? r : RarityType.Common;

    public static CardActionType ParseActionType(string s) =>
      Enum.TryParse<CardActionType>(s, true, out var t) ? t : CardActionType.Attack;

    public static ActionTargetType ParseTarget(string s) =>
      Enum.TryParse<ActionTargetType>(s, true, out var t) ? t : ActionTargetType.Enemy;

    public static StatusType ParseStatus(string s) =>
      Enum.TryParse<StatusType>(s, true, out var st) ? st : StatusType.Strength;

    public static CardData CreateFrom(RemoteCardDto dto, Sprite sprite, CardData nextUpgrade = null)
    {
        var cd = ScriptableObject.CreateInstance<CardData>();
        Set(cd, "id", dto.id);
        Set(cd, "cardName", dto.cardName);
        Set(cd, "manaCost", dto.manaCost);
        Set(cd, "cardSprite", sprite);
        Set(cd, "rarity", ParseRarity(dto.rarity));
        Set(cd, "usableWithoutTarget", dto.usableWithoutTarget);
        Set(cd, "exhaustAfterPlay", dto.exhaustAfterPlay);

        var actions = new List<CardActionData>();
        foreach (var a in dto.actions)
        {
            var ad = new CardActionData();
#if !UNITY_EDITOR
            // ใช้ Reflection ตั้งค่า private
            typeof(CardActionData).GetField("cardActionType", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, ParseActionType(a.type));
            typeof(CardActionData).GetField("actionTargetType", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, ParseTarget(a.target));
            typeof(CardActionData).GetField("actionValue", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, a.value);
            typeof(CardActionData).GetField("actionDelay", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, a.delay);
#else
      ad.EditActionType(ParseActionType(a.type));
      ad.EditActionTarget(ParseTarget(a.target));
      ad.EditActionValue(a.value);
      ad.EditActionDelay(a.delay);
#endif
            actions.Add(ad);
        }
        Set(cd, "cardActionDataList", actions);

        var descs = new List<CardDescriptionData>();
        foreach (var d in dto.desc)
        {
            var dd = new CardDescriptionData();
            typeof(CardDescriptionData).GetField("descriptionText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, d.text);
            typeof(CardDescriptionData).GetField("useModifier", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, d.useModifier);
            typeof(CardDescriptionData).GetField("modifiedActionValueIndex", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, d.actionIndex);
            typeof(CardDescriptionData).GetField("modiferStats", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, ParseStatus(d.modStat));
            descs.Add(dd);
        }
        Set(cd, "cardDescriptionDataList", descs);

        if (nextUpgrade != null)
        {
            Set(cd, "nextUpgrade", nextUpgrade);
            Set(cd, "upgradeStep", 1);
        }

        cd.UpdateDescription(); // เติม MyDescription
        return cd;
    }
}