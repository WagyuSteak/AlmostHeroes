using UnityEngine;
using UnityEngine.UI;

public class StageButtonController : MonoBehaviour
{
    public Button[] stageButtons;
    public Color unlockedColor = Color.white; // 열려 있는 버튼의 기본 색상
    public Color lockedColor = Color.gray; // 잠겨 있는 버튼의 색상

    private const string StageKeyPrefix = "Stage_";

    private void Start()
    {
        // 각 버튼의 상호작용 가능 여부 및 색상 설정
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageNumber = i + 1; // 스테이지 번호 (배열 인덱스는 0부터 시작하므로 +1)
            if (PlayerPrefs.GetInt(StageKeyPrefix + stageNumber, 0) == 1)
            {
                stageButtons[i].interactable = true;
                stageButtons[i].GetComponent<Image>().color = unlockedColor; // 열려 있는 버튼 색상
            }
            else
            {
                stageButtons[i].interactable = false;
                stageButtons[i].GetComponent<Image>().color = lockedColor; // 잠겨 있는 버튼 색상
            }
        }
    }
}
