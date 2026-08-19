using UnityEngine;

namespace DualSouls.Dialogue
{
    [CreateAssetMenu(menuName = "Dual Souls/Dialogue/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public string speakerName;

        [TextArea(2, 6)]
        public string[] lines;
    }
}
