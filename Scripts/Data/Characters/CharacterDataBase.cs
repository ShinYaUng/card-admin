using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Characters
{
    public abstract class CharacterDataBase : ScriptableObject
    {
        [Header("Base Info")]
        [SerializeField] protected string characterID;
        [SerializeField] protected string characterName;
        [SerializeField][TextArea] protected string characterDescription;

        [Header("Base Stats")]
        [SerializeField] protected int maxHealth = 0;
        [SerializeField] protected int baseArmor = 0;
        [SerializeField] protected int baseEnergy = 0;
        [SerializeField] protected int baseAttackPower = 0;

        [Header("Level System")]
        [SerializeField] protected int level = 1;
        [SerializeField] protected int currentExp = 0;
        [SerializeField] protected int expToNextLevel = 100;
        [SerializeField] protected int availableStatusPoints = 0;
        [SerializeField] protected int expGrowthRate = 50;


        #region Encapsulation
        public string CharacterID => characterID;
        public string CharacterName => characterName;
        public string CharacterDescription => characterDescription;

        public int MaxHealth => maxHealth;
        public int BaseArmor => baseArmor;
        public int BaseEnergy => baseEnergy;
        public int BaseAttackPower => baseAttackPower;

        public int Level => level;
        public int CurrentExp => currentExp;
        public int ExpToNextLevel => expToNextLevel;
        public int AvailableStatusPoints => availableStatusPoints;
        #endregion

        #region Methods
        public void AddExp(int amount)
        {
            currentExp += amount;
            if (currentExp >= expToNextLevel)
                LevelUp();
        }

        private void LevelUp()
        {
            currentExp -= expToNextLevel;
            level++;
            expToNextLevel += expGrowthRate;
            availableStatusPoints += 1;
        }

        public void IncreaseStat(string statType)
        {
            if (availableStatusPoints <= 0) return;

            switch (statType)
            {
                case "Health":
                    maxHealth += 10;
                    break;
                case "Armor":
                    baseArmor += 2;
                    break;
                case "Energy":
                    baseEnergy += 1;
                    break;
                case "Attack":
                    baseAttackPower += 2;
                    break;
            }

            availableStatusPoints--;
        }
        #endregion
    }
}
