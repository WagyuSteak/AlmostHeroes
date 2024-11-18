using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // �� ������ �ʿ��� ���ӽ����̽�
using TMPro;
public class GameManager : MonoBehaviour
{
    public GameObject stageClearPanel; // �������� Ŭ���� �г�
    public GameObject gameOverPanel; // ���� ���� �г�
    public TurnManager turnManager; // TurnManager ����

    public EnemySpawner enemySpawner; // EnemySpawner ����

    private int characterDeathCount = 0; // ĳ���� ��� ī��Ʈ
    private int enemyDeathCount = 0; // �� ��� ī��Ʈ

    private const int maxCharacterDeaths = 4; // ĳ���� ��� �ִ� �� (���� ���� ����)

    public GameObject dialoguePanel;   // ��ȭ �г�
    public TextMeshProUGUI dialogueText; // TextMeshPro ��ȭ �ؽ�Ʈ
    private string[] dialogues = {
                "asdasdasd.",
        "qweqw155sd22!",
        "zxcvcxvkdkdkdmf."
    };
    private int currentDialogueIndex = 0; // ���� ��ȭ �ε���

    private bool isTyping = false; // ���� Ÿ���� ������ Ȯ��
    private bool skipTyping = false; // Ÿ������ �ǳʶ��� Ȯ��

    void Start()
    {
        // �г� �ʱ� ��Ȱ��ȭ
        stageClearPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        dialoguePanel.SetActive(false); // ��ȭ �гε� ��Ȱ��ȭ
        turnManager = FindObjectOfType<TurnManager>(); // TurnManager �ʱ�ȭ
    }

    void Update()
    {
        // ���� ���� üũ
        CheckGameState();
    }

    public void IncrementCharacterDeath()
    {
        characterDeathCount++;

        // ĳ���� ��� ī��Ʈ�� 4�� �Ǹ� ���� ���� ó��
        if (characterDeathCount >= maxCharacterDeaths)
        {
            StartCoroutine(HandleGameOver());
        }
    }

    public void IncrementEnemyDeath()
    {
        enemyDeathCount++;

        // �� ��� ī��Ʈ�� ������ ���� ���� ������ �������� Ŭ���� ó��
        if (enemyDeathCount >= enemySpawner.enemiesToSpawn.Count)
        {
            StartCoroutine(HandleStageClear());
        }
    }

    private void CheckGameState()
    {
        // ���� ������ �������� Ŭ���� ���¸� �̹� ó�� ���̸� �߰� �������� ����
        if (characterDeathCount >= maxCharacterDeaths ||
            enemyDeathCount >= enemySpawner.enemiesToSpawn.Count)
        {
            return;
        }
    }

    private IEnumerator HandleStageClear()
    {
        if (turnManager != null)
        {
            turnManager.EndGame(); // TurnManager�� ���� ���� �˸�
        }

        stageClearPanel.SetActive(true); // �������� Ŭ���� �г� Ȱ��ȭ
        yield return new WaitForSeconds(1f); // 2�� ���

        // �г� ���̵� �ƿ��� �Ϸ��� ������ ���
        yield return StartCoroutine(FadeOutPanel(stageClearPanel));

        yield return new WaitForSeconds(1f); // 2�� ���

        StartDialogue(); // ��ȭ ����
    }
    private void StartDialogue()
    {
        dialoguePanel.SetActive(true); // ��ȭ �г� Ȱ��ȭ
        ShowNextDialogue();
    }
    public void ShowNextDialogue()
    {
        if (currentDialogueIndex < dialogues.Length)
        {
            StopAllCoroutines(); // ���� �ڷ�ƾ�� ���� ���̸� ����
            StartCoroutine(TypeText(dialogues[currentDialogueIndex])); // Ÿ���� ȿ�� ����
            currentDialogueIndex++;
        }
        else
        {
            EndDialogue(); // ��ȭ ����
        }
    }
    private IEnumerator TypeText(string dialogue)
    {
        isTyping = true; // Ÿ���� ���� ����
        skipTyping = false; // Ÿ���� �ǳʶٱ� �ʱ�ȭ
        dialogueText.text = ""; // �ʱ� �ؽ�Ʈ ����

        foreach (char letter in dialogue.ToCharArray())
        {
            if (skipTyping) // �ǳʶٱⰡ Ȱ��ȭ�Ǹ� ��� ��ü �ؽ�Ʈ ���
            {
                dialogueText.text = dialogue;
                break;
            }

            dialogueText.text += letter; // �� ���ھ� ���
            yield return new WaitForSeconds(0.15f); // �� ���� ��� ���� (���� ����)
        }

        isTyping = false; // Ÿ���� ���� ����
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false); // ��ȭ �г� ��Ȱ��ȭ
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // ���� �� �ٽ� �ε�
    }

    private IEnumerator HandleGameOver()
    {
        if (turnManager != null)
        {
            turnManager.EndGame(); // TurnManager�� ���� ���� �˸�
        }
        yield return StartCoroutine(FadeOutPanel(gameOverPanel)); // ���� ���� �г� Ȱ��ȭ

        yield return new WaitForSeconds(1f); // 2�� ���

        FadeOutPanel(gameOverPanel); // �г� ���̵� �ƿ�

    }

    private IEnumerator FadeOutPanel(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

        // CanvasGroup�� ������ �߰�
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        float fadeDuration = 1f; // ���̵�ƿ� ���� �ð�
        float fadeAmount = canvasGroup.alpha;

        while (fadeAmount > 0f)
        {
            fadeAmount -= Time.deltaTime / fadeDuration; // �� �����Ӹ��� ���� ����
            canvasGroup.alpha = fadeAmount;
            yield return null;
        }

        panel.SetActive(false); // ������ ���������� ��Ȱ��ȭ
    }
}
