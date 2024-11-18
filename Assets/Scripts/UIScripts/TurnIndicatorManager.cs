using UnityEngine;
using System.Collections;

public class TurnIndicatorManager : MonoBehaviour
{
    // ���� �ٸ� ���� ���� �̹��� ������Ʈ
    public GameObject sylphTurnIndicator;    // ���� �� �̹���
    public GameObject characterTurnIndicator; // ĳ���� �� �̹���
    public GameObject enemyTurnIndicator;   // �� �� �̹���

    public float fadeDuration = 0.5f; // ���̵� ��/�ƿ� �ð�
    public float displayDuration = 1f; // ���� �ð�
    public float moveDistance = 500f; // �̵� �Ÿ� (�������� �̵�)

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // �� Ÿ�� ����
    public enum TurnType
    {
        SylphTurn,    // ���� ��
        CharacterTurn, // ĳ���� ��
        EnemyTurn     // �� ��
    }

    private void Start()
    {
        // ���� �� ��� �̹��� ��Ȱ��ȭ
        sylphTurnIndicator.SetActive(false);
        characterTurnIndicator.SetActive(false);
        enemyTurnIndicator.SetActive(false);
    }

    public void ShowTurnIndicator(TurnType turnType)
    {
        // ǥ���� �̹��� ����
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
            Debug.LogError("�� �̹����� �������� �ʾҽ��ϴ�.");
            return;
        }

        // �ִϸ��̼� ����
        StartCoroutine(AnimateTurnIndicator(turnIndicator));
    }

    private IEnumerator AnimateTurnIndicator(GameObject turnIndicator)
    {
        // �ʿ��� ������Ʈ �ʱ�ȭ
        rectTransform = turnIndicator.GetComponent<RectTransform>();
        canvasGroup = turnIndicator.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = turnIndicator.AddComponent<CanvasGroup>();
        }

        turnIndicator.SetActive(true); // �̹��� Ȱ��ȭ
        rectTransform.anchoredPosition = Vector2.zero; // ȭ�� �߾�
        canvasGroup.alpha = 0; // �����ϰ� ����

        // ���̵� ��
        yield return Fade(0, 1, fadeDuration);

        // ���� �ð� ���
        yield return new WaitForSeconds(displayDuration);

        // ���� �̵� + ���̵� �ƿ�
        yield return MoveAndFadeOut(turnIndicator);

        // ���� ���� �� �̹��� ��Ȱ��ȭ
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
        Vector2 endPosition = startPosition + new Vector2(-moveDistance, 0); // �������� �̵�

        while (timer < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, timer / duration);
            canvasGroup.alpha = Mathf.Lerp(1, 0, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = startPosition; // ���� ��ġ�� �ʱ�ȭ
    }
}
