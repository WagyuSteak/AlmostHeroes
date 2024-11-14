using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 스테이지 번호별로 잠금 상태를 확인하는 키
    private const string StageKeyPrefix = "Stage_";

    private void Start()
    {
        // 최초 실행 시 튜토리얼 및 첫 번째 스테이지만 열리도록 설정
        if (!PlayerPrefs.HasKey(StageKeyPrefix + "1"))
        {
            PlayerPrefs.SetInt(StageKeyPrefix + "1", 1); // 1 = 열림, 0 = 잠김
            PlayerPrefs.Save();
        }
    }

    public void LoadStage(int stageNumber)
    {
        // 스테이지가 열려 있는지 확인
        if (PlayerPrefs.GetInt(StageKeyPrefix + stageNumber, 0) == 1)
        {
            SceneManager.LoadScene("Stage" + stageNumber);
        }
        else
        {
            Debug.Log("이 스테이지는 아직 잠겨 있습니다.");
        }
    }

    public void CompleteStage(int stageNumber)
    {
        // 현재 스테이지 클리어 처리 및 다음 스테이지 열기
        PlayerPrefs.SetInt(StageKeyPrefix + stageNumber, 1);
        PlayerPrefs.SetInt(StageKeyPrefix + (stageNumber + 1), 1); // 다음 스테이지 열기
        PlayerPrefs.Save();
        Debug.Log("스테이지 " + stageNumber + "이(가) 클리어되었습니다!");
    }

    public void ResetGameProgress()
    {
        // 모든 스테이지 잠금 상태 초기화
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("게임 진행 데이터가 초기화되었습니다.");
    }

    public void LoadStageSelectionScene()
    {
        SceneManager.LoadScene("StageSelection"); // 스테이지 선택 씬 로드
    }

    public void LoadMainTitle()
    {
        SceneManager.LoadScene("MainTitle");
    }

    public void LoadSynergyBookScene()
    {
        SceneManager.LoadScene("SynergyBook"); // 시너지 도감 씬 로드
    }

    public void LoadCredits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}
