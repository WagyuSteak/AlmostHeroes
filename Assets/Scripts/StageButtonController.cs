using UnityEngine;
using UnityEngine.UI;

public class StageButtonController : MonoBehaviour
{
    public Button[] stageButtons;
    public Color unlockedColor = Color.white; // ���� �ִ� ��ư�� �⺻ ����
    public Color lockedColor = Color.gray; // ��� �ִ� ��ư�� ����

    private const string StageKeyPrefix = "Stage_";

    private void Start()
    {
        // �� ��ư�� ��ȣ�ۿ� ���� ���� �� ���� ����
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageNumber = i + 1; // �������� ��ȣ (�迭 �ε����� 0���� �����ϹǷ� +1)
            if (PlayerPrefs.GetInt(StageKeyPrefix + stageNumber, 0) == 1)
            {
                stageButtons[i].interactable = true;
                stageButtons[i].GetComponent<Image>().color = unlockedColor; // ���� �ִ� ��ư ����
            }
            else
            {
                stageButtons[i].interactable = false;
                stageButtons[i].GetComponent<Image>().color = lockedColor; // ��� �ִ� ��ư ����
            }
        }
    }
}
