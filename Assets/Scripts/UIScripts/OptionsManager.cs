using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


public class OptionsManager : MonoBehaviour
{
    public GameObject optionsPanel;
    public Image fadeImage; // 페이드 효과를 위한 검은 이미지
    public float fadeDuration = 1f; // 페이드 효과 지속 시간
    private bool isFading = false; // 중복 실행 방지
    void Awake()
    {
        // 초기화: fadeImage를 검은색으로 설정하여 씬 시작 시 페이드인 준비
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1); // 완전히 검은 상태
            fadeImage.gameObject.SetActive(true);   // 활성화
        }
    }

    void Start()
    {
        // 씬 시작 시 페이드인 실행
        StartCoroutine(FadeIn());
    }


    void Update()
    {
        // ESC 키를 눌렀을 때 옵션 창 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleOptions();
        }
    }

    public void ToggleOptions()
    {
        bool isActive = !optionsPanel.activeSelf;
        optionsPanel.SetActive(isActive);

        // 게임 일시 정지
        Time.timeScale = isActive ? 0 : 1;
    }

    public void OnOptionsButtonClick()
    {
        // ESC 키와 동일하게 옵션 창 토글
        ToggleOptions();
    }

    public void OnHomeButton()
    {
        // 홈 버튼 - 메인 메뉴로 이동
        SceneManager.LoadScene("MainTitle");
    }

    public void OnResumeButton()
    {
        // Resume 버튼 - 옵션 창 닫기
        optionsPanel.SetActive(false);

        // 게임 재개
        Time.timeScale = 1; // 애니메이션 및 게임 진행 재개
    }

    public void OnRestartButton()
    {
        // Restart 버튼 - 현재 씬 다시 로드
        StartCoroutine(FadeAndLoadScene(SceneManager.GetActiveScene().name));

        // 게임 재개
        Time.timeScale = 1; // 애니메이션 및 게임 진행 재개
    }

    private IEnumerator FadeIn()
    {
        // 페이드인
        Color color = fadeImage.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration); // 알파값을 1 -> 0으로
            fadeImage.color = color;
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0); // 완전히 투명
        fadeImage.gameObject.SetActive(false);  // 페이드 효과 종료 후 비활성화
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        isFading = true; // 중복 실행 방지
        fadeImage.gameObject.SetActive(true);

        // 페이드아웃
        Color color = fadeImage.color;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        // 씬 전환
        SceneManager.LoadScene(sceneName);

        // 페이드인 (새로운 씬에서)
        StartCoroutine(FadeIn());
        isFading = false;
    }
}
