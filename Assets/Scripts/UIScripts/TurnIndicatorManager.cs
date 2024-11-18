using UnityEngine;
using System.Collections;

public class TurnIndicatorManager : MonoBehaviour
{
    // 서로 다른 턴을 위한 이미지 오브젝트
    public GameObject sylphTurnIndicator;    // 실프 턴 이미지
    public GameObject characterTurnIndicator; // 캐릭터 턴 이미지
    public GameObject enemyTurnIndicator;   // 적 턴 이미지

    public float fadeDuration = 0.5f; // 페이드 인/아웃 시간
    public float displayDuration = 1f; // 유지 시간
    public float moveDistance = 500f; // 이동 거리 (왼쪽으로 이동)

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // 턴 타입 정의
    public enum TurnType
    {
        SylphTurn,    // 실프 턴
        CharacterTurn, // 캐릭터 턴
        EnemyTurn     // 적 턴
    }

    private void Start()
    {
        // 시작 시 모든 이미지 비활성화
        sylphTurnIndicator.SetActive(false);
        characterTurnIndicator.SetActive(false);
        enemyTurnIndicator.SetActive(false);
    }

    public void ShowTurnIndicator(TurnType turnType)
    {
        // 표시할 이미지 선택
        GameObject turnIndicator = null;
        switch (turnType)
        {
            case TurnType.SylphTurn:
                turnIndicator = sylphTurnIndicator;
                break;
            case TurnType.CharacterTurn:
                turnIndicator = characterTurnIndicator;
                break;
            case TurnType.EnemyTurn:
                turnIndicator = enemyTurnIndicator;
                break;
        }

        if (turnIndicator == null)
        {
            Debug.LogError("턴 이미지가 설정되지 않았습니다.");
            return;
        }

        // 애니메이션 시작
        StartCoroutine(AnimateTurnIndicator(turnIndicator));
    }

    private IEnumerator AnimateTurnIndicator(GameObject turnIndicator)
    {
        // 필요한 컴포넌트 초기화
        rectTransform = turnIndicator.GetComponent<RectTransform>();
        canvasGroup = turnIndicator.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = turnIndicator.AddComponent<CanvasGroup>();
        }

        turnIndicator.SetActive(true); // 이미지 활성화
        rectTransform.anchoredPosition = Vector2.zero; // 화면 중앙
        canvasGroup.alpha = 0; // 투명하게 시작

        // 페이드 인
        yield return Fade(0, 1, fadeDuration);

        // 유지 시간 대기
        yield return new WaitForSeconds(displayDuration);

        // 왼쪽 이동 + 페이드 아웃
        yield return MoveAndFadeOut(turnIndicator);

        // 연출 종료 후 이미지 비활성화
        turnIndicator.SetActive(false);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }

    private IEnumerator MoveAndFadeOut(GameObject turnIndicator)
    {
        float timer = 0f;
        float duration = fadeDuration;

        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(-moveDistance, 0); // 왼쪽으로 이동

        while (timer < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, timer / duration);
            canvasGroup.alpha = Mathf.Lerp(1, 0, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = startPosition; // 원래 위치로 초기화
    }
}
