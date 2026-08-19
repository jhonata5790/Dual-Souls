using UnityEngine;
using TMPro;

namespace DualSouls.Dialogue
{
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("UI")]
        public GameObject dialoguePanel;
        public TMP_Text speakerText;
        public TMP_Text bodyText;

        [Header("Input")]
        public KeyCode advanceKey = KeyCode.Space;

        private DialogueData currentDialogue;
        private int currentLineIndex;
        private bool isActive;

        public bool IsActive => isActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        private void Update()
        {
            if (!isActive)
                return;

            if (Input.GetKeyDown(advanceKey) || Input.GetMouseButtonDown(0))
                Advance();
        }

        public void StartDialogue(DialogueData dialogue)
        {
            if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
                return;

            currentDialogue = dialogue;
            currentLineIndex = 0;
            isActive = true;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (speakerText != null)
                speakerText.text = currentDialogue.speakerName;

            if (bodyText != null)
                bodyText.text = currentDialogue.lines[currentLineIndex];
        }

        public void Advance()
        {
            currentLineIndex++;

            if (currentLineIndex >= currentDialogue.lines.Length)
            {
                EndDialogue();
                return;
            }

            ShowCurrentLine();
        }

        public void EndDialogue()
        {
            isActive = false;
            currentDialogue = null;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }
    }
}
