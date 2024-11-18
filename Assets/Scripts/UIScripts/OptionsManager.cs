using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


public class OptionsManager : MonoBehaviour
{
    public GameObject optionsPanel;
    public Image fadeImage; // ���̵� ȿ���� ���� ���� �̹���
    public float fadeDuration = 1f; // ���̵� ȿ�� ���� �ð�
    private bool isFading = false; // �ߺ� ���� ����
    void Awake()
    {
        // �ʱ�ȭ: fadeImage�� ���������� �����Ͽ� �� ���� �� ���̵��� �غ�
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1); // ������ ���� ����
            fadeImage.gameObject.SetActive(true);   // Ȱ��ȭ
        }
    }

    void Start()
    {
        // �� ���� �� ���̵��� ����
        StartCoroutine(FadeIn());
    }


    void Update()
    {
        // ESC Ű�� ������ �� �ɼ� â ���
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleOptions();
        }
    }

    public void ToggleOptions()
    {
        bool isActive = !optionsPanel.activeSelf;
        optionsPanel.SetActive(isActive);

        // ���� �Ͻ� ����
        Time.timeScale = isActive ? 0 : 1;
    }

    public void OnOptionsButtonClick()
    {
        // ESC Ű�� �����ϰ� �ɼ� â ���
        ToggleOptions();
    }

    public void OnHomeButton()
    {
        // Ȩ ��ư - ���� �޴��� �̵�
        SceneManager.LoadScene("MainTitle");
    }

    public void OnResumeButton()
    {
        // Resume ��ư - �ɼ� â �ݱ�
        optionsPanel.SetActive(false);

        // ���� �簳
        Time.timeScale = 1; // �ִϸ��̼� �� ���� ���� �簳
    }

    public void OnRestartButton()
    {
        // Restart ��ư - ���� �� �ٽ� �ε�
        StartCoroutine(FadeAndLoadScene(SceneManager.GetActiveScene().name));

        // ���� �簳
        Time.timeScale = 1; // �ִϸ��̼� �� ���� ���� �簳
    }

    private IEnumerator FadeIn()
    {
        // ���̵���
        Color color = fadeImage.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration); // ���İ��� 1 -> 0����
            fadeImage.color = color;
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0); // ������ ����
        fadeImage.gameObject.SetActive(false);  // ���̵� ȿ�� ���� �� ��Ȱ��ȭ
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        isFading = true; // �ߺ� ���� ����
        fadeImage.gameObject.SetActive(true);

        // ���̵�ƿ�
        Color color = fadeImage.color;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        // �� ��ȯ
        SceneManager.LoadScene(sceneName);

        // ���̵��� (���ο� ������)
        StartCoroutine(FadeIn());
        isFading = false;
    }
}
