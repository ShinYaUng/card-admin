// Scripts/Net/RemoteCardDto.cs
using System;
using System.Collections.Generic;

namespace NueGames.NueDeck.Scripts.Net
{
    [Serializable]
    public class RemoteCardDto
    {
        public string id;
        public string cardName;
        public int manaCost;
        public string rarity;              // "Common","Rare","Epic","Legendary"
        public string spriteUrl;           // URL ของรูปการ์ด (ถ้าไม่ใช้ ให้ปล่อยว่างได้)
        public bool usableWithoutTarget;
        public bool exhaustAfterPlay;

        [Serializable] public class ActionDto { public string type; public string target; public float value; public float delay; }
        [Serializable] public class DescDto { public string text; public bool useModifier; public int actionIndex; public string modStat; }

        public List<ActionDto> actions = new();
        public List<DescDto> desc = new();

        [Serializable] public class UpgradeDto { public string nextId; }
        public UpgradeDto upgrade; // optional
    }

    [Serializable]
    public class RemoteCardList
    {
        public List<RemoteCardDto> cards = new();
    }
}
