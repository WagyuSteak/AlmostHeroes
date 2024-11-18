using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 씬 관리에 필요한 네임스페이스
using TMPro;
public class GameManager : MonoBehaviour
{
    public GameObject stageClearPanel; // 스테이지 클리어 패널
    public GameObject gameOverPanel; // 게임 오버 패널
    public TurnManager turnManager; // TurnManager 참조

    public EnemySpawner enemySpawner; // EnemySpawner 참조

    private int characterDeathCount = 0; // 캐릭터 사망 카운트
    private int enemyDeathCount = 0; // 적 사망 카운트

    private const int maxCharacterDeaths = 4; // 캐릭터 사망 최대 수 (게임 오버 조건)

    public GameObject dialoguePanel;   // 대화 패널
    public TextMeshProUGUI dialogueText; // TextMeshPro 대화 텍스트
    private string[] dialogues = {
                "asdasdasd.",
        "qweqw155sd22!",
        "zxcvcxvkdkdkdmf."
    };
    private int currentDialogueIndex = 0; // 현재 대화 인덱스

    private bool isTyping = false; // 현재 타이핑 중인지 확인
    private bool skipTyping = false; // 타이핑을 건너뛸지 확인

    void Start()
    {
        // 패널 초기 비활성화
        stageClearPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        dialoguePanel.SetActive(false); // 대화 패널도 비활성화
        turnManager = FindObjectOfType<TurnManager>(); // TurnManager 초기화
    }

    void Update()
    {
        // 게임 상태 체크
        CheckGameState();
    }

    public void IncrementCharacterDeath()
    {
        characterDeathCount++;

        // 캐릭터 사망 카운트가 4가 되면 게임 오버 처리
        if (characterDeathCount >= maxCharacterDeaths)
        {
            StartCoroutine(HandleGameOver());
        }
    }

    public void IncrementEnemyDeath()
    {
        enemyDeathCount++;

        // 적 사망 카운트가 스폰된 적의 수와 같으면 스테이지 클리어 처리
        if (enemyDeathCount >= enemySpawner.enemiesToSpawn.Count)
        {
            StartCoroutine(HandleStageClear());
        }
    }

    private void CheckGameState()
    {
        // 게임 오버와 스테이지 클리어 상태를 이미 처리 중이면 추가 동작하지 않음
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
            turnManager.EndGame(); // TurnManager에 게임 종료 알림
        }

        stageClearPanel.SetActive(true); // 스테이지 클리어 패널 활성화
        yield return new WaitForSeconds(1f); // 2초 대기

        // 패널 페이드 아웃을 완료할 때까지 대기
        yield return StartCoroutine(FadeOutPanel(stageClearPanel));

        yield return new WaitForSeconds(1f); // 2초 대기

        StartDialogue(); // 대화 시작
    }
    private void StartDialogue()
    {
        dialoguePanel.SetActive(true); // 대화 패널 활성화
        ShowNextDialogue();
    }
    public void ShowNextDialogue()
    {
        if (currentDialogueIndex < dialogues.Length)
        {
            StopAllCoroutines(); // 이전 코루틴이 실행 중이면 중지
            StartCoroutine(TypeText(dialogues[currentDialogueIndex])); // 타이핑 효과 실행
            currentDialogueIndex++;
        }
        else
        {
            EndDialogue(); // 대화 종료
        }
    }
    private IEnumerator TypeText(string dialogue)
    {
        isTyping = true; // 타이핑 상태 시작
        skipTyping = false; // 타이핑 건너뛰기 초기화
        dialogueText.text = ""; // 초기 텍스트 비우기

        foreach (char letter in dialogue.ToCharArray())
        {
            if (skipTyping) // 건너뛰기가 활성화되면 즉시 전체 텍스트 출력
            {
                dialogueText.text = dialogue;
                break;
            }

            dialogueText.text += letter; // 한 글자씩 출력
            yield return new WaitForSeconds(0.15f); // 각 글자 출력 간격 (조정 가능)
        }

        isTyping = false; // 타이핑 상태 종료
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false); // 대화 패널 비활성화
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 현재 씬 다시 로드
    }

    private IEnumerator HandleGameOver()
    {
        if (turnManager != null)
        {
            turnManager.EndGame(); // TurnManager에 게임 종료 알림
        }
        yield return StartCoroutine(FadeOutPanel(gameOverPanel)); // 게임 오버 패널 활성화

        yield return new WaitForSeconds(1f); // 2초 대기

        FadeOutPanel(gameOverPanel); // 패널 페이드 아웃

    }

    private IEnumerator FadeOutPanel(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

        // CanvasGroup이 없으면 추가
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        float fadeDuration = 1f; // 페이드아웃 지속 시간
        float fadeAmount = canvasGroup.alpha;

        while (fadeAmount > 0f)
        {
            fadeAmount -= Time.deltaTime / fadeDuration; // 매 프레임마다 투명도 감소
            canvasGroup.alpha = fadeAmount;
            yield return null;
        }

        panel.SetActive(false); // 완전히 투명해지면 비활성화
    }
}
