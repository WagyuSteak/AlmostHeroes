using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.TextCore.Text;
using Unity.VisualScripting;
using UnityEngine.TextCore;

public class Sylph : MonoBehaviour
{
    // TurnManager와 GridManager에 대한 참조 변수
    TurnManager turnManager; // 게임의 턴 관리를 담당하는 TurnManager의 참조
    GridManager gridManager; // 그리드 관리와 셀 활성화를 담당하는 GridManager의 참조
    UIManager uiManager;

    // 실프의 최대 마나와 그리드의 셀 크기 및 원점 좌표
    public int maxMana = 10; // 실프의 최대 마나 수치
    public int currentMana; // 실프의 현재 마나 수치
    public float cellWidth;   // 그리드 셀의 너비
    public float cellHeight;  // 그리드 셀의 높이
    public Vector3 gridOrigin; // 그리드의 월드 좌표상 원점 위치

    // 실프 이동 시 나타나는 포인터, 카메라, 레이어 마스크, 프리팹 등의 참조 변수
    public GameObject pointerPrefab;    // 마우스 포인터를 따라가는 프리팹
    public GameObject circlePrefab;    // 동그란 프리팹 (경로 시작점 및 각 셀에 생성)
    public GameObject linePrefab;      // 직선 프리팹 (각 셀 사이에 연결되는 선)
    public float pathIndicatorHeight = 1.5f; // 경로 표시 오브젝트의 Y축 높이

    private Vector2Int lastGridPosition; // 마지막 위치를 저장하는 변수
    public Camera mainCamera;           // 메인 카메라 참조
    public LayerMask cellLayerMask;     // 셀을 감지하기 위한 레이어 마스크

    // 이벤트와 캐릭터 관리
    public event Action OnSylphMoved; // 실프가 이동할 때 호출되는 이벤트
    private GameObject currentPointer; // 현재 마우스 위치를 나타내는 포인터
    public Vector2Int gridPosition;   // 실프의 현재 그리드 좌표
    public List<GameObject> pathIndicators = new List<GameObject>(); // 경로 표시 오브젝트 리스트
    public List<Vector2Int> activatedCells = new List<Vector2Int>(); // 실프가 활성화한 셀들의 리스트
    public List<CharacterBase> encounteredCharacters = new List<CharacterBase>(); // 실프가 마주친 캐릭터 리스트

    private Vector2Int startGridPosition; // 시작 그리드 위치
    private List<GameObject> pathCircles = new List<GameObject>(); // 동그란 프리팹들을 저장하는 리스트
    private List<GameObject> pathLines = new List<GameObject>();   // 직선 프리팹들을 저장하는 리스트

    // 상태 변수
    private bool hasMoved = false; // 실프가 한 번이라도 이동했는지 여부를 저장
    public bool isControllable = false; // 실프가 조작 가능한 상태인지 여부
    private bool isMoving = false; // 이동 중인지 여부를 나타내는 변수

    // 초기 설정 및 컴포넌트 찾기
    private void Start()
    {
        gridManager = FindObjectOfType<GridManager>(); // GridManager를 씬에서 찾음
        turnManager = FindObjectOfType<TurnManager>(); // TurnManager를 씬에서 찾음
        uiManager = FindObjectOfType<UIManager>();
        SylphTurn();
        gridPosition = new Vector2Int(0, 0); // 그리드 시작 위치 설정
        MoveToCell(gridPosition); // 초기 위치로 이동 설정
        uiManager.negativeEndTurnButton();
        // 초기 위치를 설정할 때 Y 좌표를 1.3으로 고정하고, x축 -90도 회전 적용
        transform.position = new Vector3(transform.position.x, 1.05f, transform.position.z);
        transform.rotation = Quaternion.Euler(0, 90, transform.rotation.eulerAngles.z);

    }

    public void SetControllable(bool canControl)
    {
        isControllable = canControl; // 실프가 조작 가능한지 여부 설정
        Debug.Log($"실프 조작 가능 상태: {isControllable}"); // 조작 가능 상태 출력
    }


    public void SylphTurn()
    {
        currentMana = maxMana; // 실프의 현재 마나를 최대 마나로 초기화
    }
    private void Update()
    {
        if (currentMana < maxMana)
        {
            uiManager.ActivateCancelButton();
        }
        else
        {
            uiManager.DeactivateCancelButton();
        }

        if (isControllable) // 실프가 조작 가능한 상태일 때만
        {
            // 현재 위치에서 캐릭터 감지
            DetectCharacterInCell(gridPosition);

            // 현재 위치에 다른 캐릭터가 감지되었는지 확인
            if (encounteredCharacters.Count == 0 || !encounteredCharacters.Exists(character => character.GetCurrentGridPosition() == gridPosition))
            {
                if (activatedCells.Count > 1 && !isMoving) // 활성화된 셀이 있을 경우에만 이동 시작
                {

                    uiManager.ActivateEndTurnButton();
                    if (Input.GetKeyDown(KeyCode.Space)) // 스페이스 키가 눌렸을 때
                    {
                        uiManager.negativeEndTurnButton();
                        StartCoroutine(StartMovement()); // 이동을 시작하는 코루틴 호출
                    }
                }
            }
            else
            {
                uiManager.negativeEndTurnButton();
                Debug.Log("현재 위치에 캐릭터가 있어 턴을 종료할 수 없습니다."); // 현재 위치에 캐릭터가 있어 이동할 수 없음을 출력
            }

            if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼이 눌렸을 때
            {
                HandleMouseClick(); // 마우스 클릭 처리
            }

            if (Input.GetMouseButton(0) && currentPointer != null) // 마우스 왼쪽 버튼이 눌린 상태에서 포인터가 존재할 때
            {
                HandlePointerMovement(); // 포인터 이동 처리
            }

            if (Input.GetMouseButtonUp(0)) // 마우스 왼쪽 버튼이 떼어졌을 때
            {
                if (currentPointer != null) // 현재 포인터가 존재한다면
                {
                    Destroy(currentPointer); // 포인터 오브젝트 삭제
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                UndoAllActivatedCells();
            }
        }
    }
    public void UndoAllActivatedCells()
    {
            if (activatedCells.Count > 0)
            {
                // 모든 활성화된 셀 제거
                gridManager.DeactivateCellsForCharacter(activatedCells);
                activatedCells.Clear();

                // 모든 경로와 인디케이터 제거
                foreach (var circle in pathCircles)
                {
                    Destroy(circle);
                }
                pathCircles.Clear();

                foreach (var line in pathLines)
                {
                    Destroy(line);
                }
                pathLines.Clear();

            ClearActivatedCells();
            ClearPathIndicators(); // 경로 표시 오브젝트 초기화
            encounteredCharacters.Clear(); // 마주친 캐릭터 리스트 초기화
            uiManager.negativeEndTurnButton();

            // 이동 가능 횟수 초기화
            currentMana = maxMana;

            gridPosition = startGridPosition;
                Debug.Log("모든 활성화된 셀을 취소하고 초기화했습니다.");
            }
            else
            {
                Debug.Log("취소할 활성화된 셀이 없습니다.");
            }
    }
        public void GiveMana()
    {
        TransferManaToEncounteredCharacters(); // 마주친 캐릭터들에게 마나를 전달
        DisplayEncounteredCharacters(); // 마주친 캐릭터들을 출력
        turnManager.EndSylphTurn(encounteredCharacters); // 실프의 턴을 종료하고 턴 매니저에게 알림
    }

    public List<CharacterBase> GetEncounteredCharacters()
    {
        return new List<CharacterBase>(encounteredCharacters); // 마주친 캐릭터 리스트 반환
    }

    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 레이 생성
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // 레이캐스트가 성공하면
        {
            Vector3 pointerPosition = hit.point; // 히트된 위치 가져오기
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition); // 그리드 좌표로 변환

            if (targetGridPosition == gridPosition) // 현재 그리드 위치와 동일하다면
            {
                currentPointer = Instantiate(pointerPrefab, pointerPosition, Quaternion.identity); // 포인터 프리팹 생성
                if (!activatedCells.Contains(targetGridPosition))
                {
                    activatedCells.Add(targetGridPosition); // 셀 활성화
                }
                startGridPosition = gridPosition;
            }
        }
    }

    private void HandlePointerMovement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 레이 생성
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // 레이캐스트가 성공하면
        {
            Vector3 pointerPosition = hit.point; // 히트된 위치 가져오기
            currentPointer.transform.position = pointerPosition; // 포인터 위치 업데이트
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition); // 그리드 좌표로 변환

            if (IsAdjacentToCurrentPosition(targetGridPosition) && !activatedCells.Contains(targetGridPosition) && !gridManager.IsObstaclePosition(targetGridPosition)) // 인접한 셀이고 아직 활성화되지 않은 경우
            {
                if (gridManager.IsWithinGridBounds(targetGridPosition)) // 그리드 경계 내에 있는지 확인
                {
                    ActivateCell(targetGridPosition); // 셀 활성화
                }
                else
                {
                    Debug.Log("그리드 경계를 벗어나있어 경로를 설정할 수 없습니다."); // 경계 밖이라면 경고 메시지 출력
                }
            }
        }
    }

    private Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellWidth); // 월드 좌표를 그리드 x좌표로 변환
        int z = Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / cellHeight); // 월드 좌표를 그리드 z좌표로 변환
        return new Vector2Int(x, z); // 그리드 좌표 반환
    }

    private void ActivateCell(Vector2Int targetGridPosition)
    {
        if (currentMana > 0) // 마나가 충분한 경우에만
        {
            // 활성화된 셀 목록에 이미 존재하는 경우 추가하지 않음
            if (!activatedCells.Contains(targetGridPosition))
            {
                activatedCells.Add(targetGridPosition); // 활성화된 셀 리스트에 추가
                currentMana--; // 마나 감소
                gridPosition = targetGridPosition; // 현재 위치 업데이트

                gridManager.SetSylphPosition(gridPosition);
                DetectCharacterInCell(targetGridPosition);

                // 경로 표시 오브젝트 생성 및 방향 설정
                if (circlePrefab != null && linePrefab != null)
                {
                    Vector3 circlePosition = gridOrigin + new Vector3(targetGridPosition.x * cellWidth, pathIndicatorHeight, targetGridPosition.y * cellHeight);

                    if (activatedCells.Count == 1)
                    {
                        Vector3 startPosition = new Vector3(transform.position.x, pathIndicatorHeight, transform.position.z);
                        GameObject startCircleIndicator = Instantiate(circlePrefab, startPosition, Quaternion.identity);
                        pathCircles.Add(startCircleIndicator);

                        if (targetGridPosition != GetGridPosition(startPosition))
                        {
                            Vector3 direction = (circlePosition - startPosition).normalized;
                            float distance = Vector3.Distance(circlePosition, startPosition);

                            GameObject lineIndicator = Instantiate(linePrefab, (circlePosition + startPosition) / 2, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)));
                            lineIndicator.transform.localScale = new Vector3(lineIndicator.transform.localScale.x, lineIndicator.transform.localScale.y, distance);

                            pathLines.Add(lineIndicator);
                        }
                    }
                    else
                    {
                        Vector2Int previousPosition = activatedCells[activatedCells.Count - 2];
                        Vector3 previousCirclePosition = gridOrigin + new Vector3(previousPosition.x * cellWidth, pathIndicatorHeight, previousPosition.y * cellHeight);

                        Vector3 direction = (circlePosition - previousCirclePosition).normalized;
                        float distance = Vector3.Distance(circlePosition, previousCirclePosition);

                        GameObject lineIndicator = Instantiate(linePrefab, (circlePosition + previousCirclePosition) / 2, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)));
                        lineIndicator.transform.localScale = new Vector3(lineIndicator.transform.localScale.x, lineIndicator.transform.localScale.y, distance);

                        pathLines.Add(lineIndicator);
                    }

                    GameObject circleIndicator = Instantiate(circlePrefab, circlePosition, Quaternion.identity);
                    pathCircles.Add(circleIndicator);
                }

                DetectCharacterInCell(targetGridPosition);
                Debug.Log($"Mana: {currentMana}/{maxMana}");
            }
        }
    }
    private void MoveToCell(Vector2Int targetGridPosition)
    {
        gridPosition = targetGridPosition;
        gridManager.SetSylphPosition(gridPosition);
        DetectCharacterInCell(targetGridPosition); // 이동 시마다 감지 호출
    }

    // Sylph 클래스에서 캐릭터를 감지하는 메서드
    private void DetectCharacterInCell(Vector2Int gridPosition)
    {
        encounteredCharacters.RemoveAll(character => character == null || character.Health <= 0); // 죽은 캐릭터 제거

        foreach (CharacterBase character in gridManager.characters)
        {
            if (character.GetCurrentGridPosition() == gridPosition && character.Health > 0 && !encounteredCharacters.Contains(character))
            {
                encounteredCharacters.Add(character);
                Debug.Log($"{character.name}을(를) 그리드 위치 {gridPosition}에서 감지했습니다.");
            }
        }
    }

    private void TransferManaToEncounteredCharacters()
    {
        foreach (CharacterBase character in encounteredCharacters) // 마주친 캐릭터들에게 마나 전달
        {
            character.ReceiveMana(currentMana);
        }
    }

    private void DisplayEncounteredCharacters()
    {
        if (encounteredCharacters.Count == 0) // 마주친 캐릭터가 없을 경우
        {
            Debug.Log("저장된 캐릭터가 없습니다.");
            return;
        }

        string characterNames = "Encountered Characters 순서 출력: ";
        foreach (CharacterBase character in encounteredCharacters) // 마주친 캐릭터 이름들 출력
        {
            characterNames += $"{character.name}, ";
        }

        characterNames = characterNames.TrimEnd(',', ' '); // 문자열 끝의 쉼표와 공백 제거
        Debug.Log(characterNames); // 최종적으로 마주친 캐릭터 이름 출력
    }

    private bool IsAdjacentToCurrentPosition(Vector2Int targetGridPosition)
    {
        int deltaX = Mathf.Abs(targetGridPosition.x - gridPosition.x); // x축 거리 차이 계산
        int deltaY = Mathf.Abs(targetGridPosition.y - gridPosition.y); // y축 거리 차이 계산
        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1); // 한 축으로만 1칸 떨어진 경우 인접한 것으로 판단
    }

    public IEnumerator StartMovement()
    {
        isMoving = true;
        // 첫 번째 셀을 건너뛰기 위해 i를 1로 설정
        for (int i = 1; i < activatedCells.Count; i++)
        {
            Vector2Int cellPosition = activatedCells[i];

            // 목표 위치의 Y 좌표를 1.3으로 고정
            Vector3 targetPosition = gridOrigin + new Vector3(cellPosition.x * cellWidth, 1.3f, cellPosition.y * cellHeight);

            // 방향을 계산하여 y축 회전만 설정 (x축 회전 유지)
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero) // 방향 벡터가 0이 아닐 때만 회전
            {
                Quaternion targetRotation = Quaternion.Euler(0, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles.y, 0);

                while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f) // 목표 회전에 도달할 때까지 회전
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30); // 회전 속도 조정 가능
                    yield return null;
                }
            }

            // 캐릭터가 목표 위치에 도달할 때까지 이동 (Y 좌표는 1.3으로 고정)
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 7); // 속도 조정
                yield return null;
            }


        }
        uiManager.negativeEndTurnButton();
        ClearActivatedCells();
        ClearPathIndicators(); // 경로 표시 오브젝트 초기화
        Debug.Log("경로 이동 완료"); // 이동 완료 메시지 출력
        GiveMana(); // 마나 전달
        isMoving = false;
    }

    public void ClearPathIndicators()
    {
        // 동그란 프리팹들 삭제
        foreach (GameObject circle in pathCircles)
        {
            Destroy(circle);
        }
        pathCircles.Clear(); // 리스트 초기화

        // 직선 프리팹들 삭제
        foreach (GameObject line in pathLines)
        {
            Destroy(line);
        }
        pathLines.Clear(); // 리스트 초기화
    }

    public void ClearActivatedCells()
    {
        activatedCells.Clear(); // 활성화된 셀 리스트 초기화
        Debug.Log("실프가 활성화한 모든 셀이 초기화되었습니다.");
    }

    public void ClearEncounteredCharacters()
    {
        encounteredCharacters.Clear(); // 마주친 캐릭터 리스트 초기화
        Debug.Log("실프가 만난 캐릭터 리스트 초기화됨");
    }
}