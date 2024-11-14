using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // �������� ��ȣ���� ��� ���¸� Ȯ���ϴ� Ű
    private const string StageKeyPrefix = "Stage_";

    private void Start()
    {
        // ���� ���� �� Ʃ�丮�� �� ù ��° ���������� �������� ����
        if (!PlayerPrefs.HasKey(StageKeyPrefix + "1"))
        {
            PlayerPrefs.SetInt(StageKeyPrefix + "1", 1); // 1 = ����, 0 = ���
            PlayerPrefs.Save();
        }
    }

    public void LoadStage(int stageNumber)
    {
        // ���������� ���� �ִ��� Ȯ��
        if (PlayerPrefs.GetInt(StageKeyPrefix + stageNumber, 0) == 1)
        {
            SceneManager.LoadScene("Stage" + stageNumber);
        }
        else
        {
            Debug.Log("�� ���������� ���� ��� �ֽ��ϴ�.");
        }
    }

    public void CompleteStage(int stageNumber)
    {
        // ���� �������� Ŭ���� ó�� �� ���� �������� ����
        PlayerPrefs.SetInt(StageKeyPrefix + stageNumber, 1);
        PlayerPrefs.SetInt(StageKeyPrefix + (stageNumber + 1), 1); // ���� �������� ����
        PlayerPrefs.Save();
        Debug.Log("�������� " + stageNumber + "��(��) Ŭ����Ǿ����ϴ�!");
    }

    public void ResetGameProgress()
    {
        // ��� �������� ��� ���� �ʱ�ȭ
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("���� ���� �����Ͱ� �ʱ�ȭ�Ǿ����ϴ�.");
    }

    public void LoadStageSelectionScene()
    {
        SceneManager.LoadScene("StageSelection"); // �������� ���� �� �ε�
    }

    public void LoadMainTitle()
    {
        SceneManager.LoadScene("MainTitle");
    }

    public void LoadSynergyBookScene()
    {
        SceneManager.LoadScene("SynergyBook"); // �ó��� ���� �� �ε�
    }

    public void LoadCredits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void QuitGame()
    {
        Debug.Log("���� ����");
        Application.Quit();
    }
}
