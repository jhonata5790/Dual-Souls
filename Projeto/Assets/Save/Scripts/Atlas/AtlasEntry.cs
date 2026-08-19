using UnityEngine;

namespace DualSouls.Atlas
{
    [CreateAssetMenu(menuName = "Dual Souls/Atlas/Atlas Entry")]
    public class AtlasEntry : ScriptableObject
    {
        public string entryTitle;
        public AtlasCategory category;

        [TextArea(4, 10)]
        public string zunnaNotes;

        public Sprite illustration;
        public int dangerLevel = 0;
        public string element;
        public string region;
    }

    public enum AtlasCategory
    {
        Biome,
        Creature,
        Plant,
        Location,
        Faction,
        Lore,
        Character
    }
}
