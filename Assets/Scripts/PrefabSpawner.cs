using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class PrefabSpawner : MonoBehaviour
{
    public GameObject[] characterPrefabs; // 워리어, 아처, 메이지, 프리스트 프리팹 배열
    public TurnManager turnManager; // 턴 관리를 담당하는 TurnManager의 참조
    public GridManager gridManager; // 그리드 관리와 셀 활성화를 담당하는 GridManager의 참조
    public Button[] characterButtons; // 캐릭터 생성 버튼들
    public event Action OnCharacterPlaced; // 캐릭터가 배치될 때 발생하는 이벤트
    public Vector3 defaultRotation; // 캐릭터가 생성될 때의 기본 회전 각도 (x, y, z)

    private List<GameObject> tempPlacedCharacters = new List<GameObject>(); // 임시로 배치된 캐릭터들을 저장하는 리스트
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>(); // 임시 배치된 셀 정보
    private int charactersPlacedCount = 0;

    private bool isRepositioning = false; // 현재 배치 취소 중인지 여부
    private GameObject currentlyRepositioningCharacter = null; // 재배치 중인 캐릭터 참조

    private void Start()
    {
        // 각 캐릭터 생성 버튼에 이벤트 등록
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i; // 로컬 변수로 인덱스 값을 캡처하여 버튼에 전달
            characterButtons[i].onClick.AddListener(() => PlaceCharacter(index)); // 버튼 클릭 시 해당 인덱스의 캐릭터 생성
        }

    }
    private void Update()
    {
        HandleRepositionInput(); // 배치 취소 처리
    }

    // 캐릭터를 임시로 배치하는 메서드
    private void PlaceCharacter(int index)
    {
        // 모든 버튼 비활성화
        SetButtonsInteractable(false);

        // 선택된 캐릭터 프리팹 가져오기
        GameObject characterPrefab = characterPrefabs[index];

        // 화면에 보이는 마우스 위치에서 캐릭터의 월드 위치 계산
        Vector3 spawnPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
        spawnPosition.y = 0; // y 좌표를 0으로 고정

        // 지정된 기본 회전을 사용하여 캐릭터 생성
        Quaternion spawnRotation = Quaternion.Euler(defaultRotation);
        GameObject character = Instantiate(characterPrefab, spawnPosition, spawnRotation);
        tempPlacedCharacters.Add(character); // 임시 리스트에 추가

        // 생성된 캐릭터에 GridManager와 TurnManager의 참조 설정
        CharacterBase characterBase = character.GetComponent<CharacterBase>();

        // 캐릭터가 마우스를 따라 이동하도록 설정
        FollowMouse followMouse = character.AddComponent<FollowMouse>();
        followMouse.SetGridManager(gridManager);
        followMouse.SetOccupiedCells(occupiedCells); // occupiedCells 전달

        // 캐릭터 배치 완료 시 호출될 이벤트 등록
        followMouse.OnPlacementCompleted += () =>
        {
            // 모든 버튼 비활성화
            SetButtonsInteractable(true);
            characterButtons[index].gameObject.SetActive(false); // 캐릭터 버튼 비활성화
            charactersPlacedCount++;
            if (charactersPlacedCount >= 4)
            {
                ConfirmPlacement();
            }
        };

    }

    // 모든 캐릭터의 배치를 확정하는 메서드
    private void ConfirmPlacement()
    {
        foreach (GameObject character in tempPlacedCharacters)
        {
            CharacterBase characterBase = character.GetComponent<CharacterBase>();
            if (characterBase != null)
            {
                // 캐릭터의 그리드 위치 확정 및 TurnManager에 등록
                Vector2Int gridPosition = gridManager.GetGridPosition(character.transform.position);
                characterBase.SetGridPosition(gridPosition);
                gridManager.AddCharacterToGrid(gridPosition, characterBase);
                FindObjectOfType<TurnManager>().OnCharacterPlaced(characterBase);
            }
        }

        tempPlacedCharacters.Clear(); // 임시 리스트 비우기
        occupiedCells.Clear(); // 임시 셀 정보 초기화
        Debug.Log("모든 캐릭터 배치가 완료되었습니다.");
    }
    private void HandleRepositionInput()
    {
        if (Input.GetMouseButtonDown(1)) // 우클릭으로 재배치
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject clickedObject = hit.collider.gameObject;

                // 클릭된 객체가 임시 배치된 캐릭터인지 확인
                if (tempPlacedCharacters.Contains(clickedObject) && !isRepositioning)
                {
                    StartReposition(clickedObject);
                }
            }
        }
    }

    private void StartReposition(GameObject character)
    {
        isRepositioning = true; // 재배치 중 플래그 활성화
        currentlyRepositioningCharacter = character; // 현재 재배치 중인 캐릭터 설정

        // 캐릭터의 현재 그리드 위치를 occupiedCells에서 제거
        Vector2Int currentPosition = gridManager.GetGridPosition(character.transform.position);
        occupiedCells.Remove(currentPosition);

        // FollowMouse 추가 및 초기화
        FollowMouse followMouse = character.GetComponent<FollowMouse>();
        if (followMouse == null)
        {
            followMouse = character.AddComponent<FollowMouse>();
        }
        followMouse.SetGridManager(gridManager);
        followMouse.SetOccupiedCells(occupiedCells);

        // 버튼 비활성화
        SetButtonsInteractable(false);

        followMouse.OnPlacementCompleted += () =>
        {
            isRepositioning = false; // 재배치 완료
            currentlyRepositioningCharacter = null;
            SetButtonsInteractable(true);
        };
    }

    // 모든 캐릭터 생성 버튼의 활성화 상태를 설정하는 메서드
    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var button in characterButtons)
        {
            button.interactable = interactable;
        }
    }
}

// 캐릭터가 마우스를 따라 이동하고 배치되는 동작을 담당하는 FollowMouse 클래스
public class FollowMouse : MonoBehaviour
{
    public event Action OnPlacementCompleted; // 캐릭터 배치 완료 시 발생할 이벤트

    private bool followingMouse = true; // 캐릭터가 마우스를 따라 이동하는지 여부
    private GridManager gridManager; // 그리드 매니저 참조
    private HashSet<Vector2Int> occupiedCells; // 참조할 임시 배치 셀 정보
    private Vector2Int currentGridPosition; // 현재 그리드 위치
    private Vector2Int? previousInvalidCell = null; // 이전에 빨간색으로 표시된 잘못된 셀 위치

    // GridManager를 설정하는 메서드
    public void SetGridManager(GridManager manager)
    {
        gridManager = manager;
    }

    public void SetOccupiedCells(HashSet<Vector2Int> cells)
    {
        occupiedCells = cells;
    }

    // 매 프레임마다 실행되는 메서드
    void Update()
    {
        if (followingMouse)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 레이 생성
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridManager.cellLayerMask))
            {
                Vector2Int gridPosition = gridManager.GetGridPosition(hit.point); // 마우스 위치의 그리드 좌표를 가져옴

                // 그리드 경계 내에 있는지 확인
                if (gridManager.IsWithinGridBounds(gridPosition))
                {
                    Vector3 gridCellCenter = gridManager.GetWorldPositionFromGrid(gridPosition);
                    transform.position = new Vector3(gridCellCenter.x, 0.55f, gridCellCenter.z); // 캐릭터 위치 업데이트

                    // 아군 영역에 위치해 있고 셀이 비어있는지 확인
                    if (gridManager.IsAllyZone(gridPosition) &&
                        !gridManager.IsSylphPosition(gridPosition) &&
                         !occupiedCells.Contains(gridPosition) &&
                        !gridManager.IsCharacterPosition(gridPosition) &&
                        !gridManager.IsObstaclePosition(gridPosition) &&
                        !gridManager.IsEnemyPosition(gridPosition))
                    {
                        ResetPreviousInvalidCellColor();
                        // 셀 색상을 초록색으로 설정
                        HighlightValidCell(gridPosition);
                        if (Input.GetMouseButtonDown(0))
                        {
                            followingMouse = false;
                            occupiedCells.Add(gridPosition); // 임시 배치된 셀 추가
                            OnPlacementCompleted?.Invoke();
                            Destroy(this); // FollowMouse 컴포넌트 제거
                        }
                    }
                    else
                    {
                        HighlightInvalidCell(gridPosition);
                    }
                }
            }
            else
            {
                // 그리드 외부에서 캐릭터가 마우스를 따라가도록 설정
                Vector3 mousePosition = ray.GetPoint(6f);
                transform.position = new Vector3(mousePosition.x, 1.5f, mousePosition.z);

                ResetPreviousInvalidCellColor();
            }
        }
    }

    private void HighlightValidCell(Vector2Int gridPosition)
    {
        if (gridManager.gridCells.ContainsKey(gridPosition))
        {
            Renderer cellRenderer = gridManager.gridCells[gridPosition].GetComponent<Renderer>();
            if (cellRenderer != null)
            {
                cellRenderer.material.color = Color.green; // 초록색으로 표시
                currentGridPosition = gridPosition;
            }
        }
    }

    // 잘못된 셀을 빨간색으로 강조 표시하는 메서드
    private void HighlightInvalidCell(Vector2Int gridPosition)
    {
        if (previousInvalidCell.HasValue && previousInvalidCell.Value != gridPosition)
        {
            ResetPreviousInvalidCellColor();
        }

        if (gridManager.gridCells.ContainsKey(gridPosition))
        {
            Renderer cellRenderer = gridManager.gridCells[gridPosition].GetComponent<Renderer>();
            if (cellRenderer != null)
            {
                cellRenderer.material.color = Color.red; // 셀 색상을 빨간색으로 설정
                previousInvalidCell = gridPosition; // 이전 잘못된 셀 위치 업데이트
            }
        }
    }

    // 이전 잘못된 셀의 색상을 원래대로 복원하는 메서드
    private void ResetPreviousInvalidCellColor()
    {
        if (previousInvalidCell.HasValue && gridManager.gridCells.ContainsKey(previousInvalidCell.Value))
        {
            Renderer cellRenderer = gridManager.gridCells[previousInvalidCell.Value].GetComponent<Renderer>();
            if (cellRenderer != null)
            {
                // 이전 셀이 아군 지역이면 초록색 복구, 아니면 기본 색상 복구
                if (gridManager.IsAllyZone(previousInvalidCell.Value))
                {
                    cellRenderer.material.color = Color.green;
                }
                else
                {
                    cellRenderer.material.color = gridManager.defaultColor; // 기본 색상으로 복구
                }
            }
            previousInvalidCell = null;
        }
    }
}
