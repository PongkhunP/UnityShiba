using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    public GameObject dialoguePanel;      // ตัวกล่องข้อความทั้งหมด (Panel)
    public Image portraitImage;           // รูป NPC ด้านซ้าย
    public TextMeshProUGUI nameText;      // ชื่อ NPC
    public TextMeshProUGUI dialogueText;  // ข้อความพูด

    [Header("Settings")]
    public float typeSpeed = 0.02f;       // ความเร็วตัวหนังสือวิ่ง

    private Queue<DialogueLine> sentences;
    private bool isTyping = false;
    private string currentFullText = "";

    // เอาไว้บอกคนอื่นว่าคุยอยู่ (Player จะได้ขยับไม่ได้)
    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        sentences = new Queue<DialogueLine>();
        if (dialoguePanel) dialoguePanel.SetActive(false);
    }

    public void StartDialogue(DialogueSO dialogue)
    {
        IsDialogueActive = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);

        // ตั้งชื่อ NPC
        if (nameText) nameText.text = dialogue.npcName;

        // เคลียร์ประโยคเก่า แล้วใส่ประโยคใหม่เข้าไป
        sentences.Clear();
        foreach (var line in dialogue.lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        // ถ้ากำลังพิมพ์อยู่ ให้กดข้ามไปแสดงผลให้จบทันที
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullText;
            isTyping = false;
            return;
        }

        // ถ้าประโยคหมดแล้ว ให้จบการสนทนา
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = sentences.Dequeue();

        // 1. เปลี่ยนรูป Portrait
        if (portraitImage != null)
        {
            if (line.portrait != null)
            {
                portraitImage.sprite = line.portrait;
                portraitImage.gameObject.SetActive(true);
            }
            // ถ้าไม่มีรูปในประโยคนี้ จะให้ซ่อน หรือใช้รูปเดิมก็ได้ (ในที่นี้เลือกใช้รูปเดิมถ้าไม่ได้ใส่)
            // else portraitImage.gameObject.SetActive(false); 
        }

        // 2. พิมพ์ข้อความ (Typewriter Effect)
        currentFullText = line.text;
        StopAllCoroutines();
        StartCoroutine(TypeSentence(line.text));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
    }
}