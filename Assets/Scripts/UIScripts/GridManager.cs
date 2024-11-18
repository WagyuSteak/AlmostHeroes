using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public float cellWidth = 1.0f;
    public float cellHeight = 1.0f;
    public float cellYOffset = 0.0f;
    public GameObject[] cellPrefabs;
    public GameObject sylphPrefab;
    public GameObject pointerPrefab;
    public Vector3 gridOrigin = Vector3.zero;
    public Color defaultColor = Color.white;
    public List<Vector2Int> allyZonePositions = new List<Vector2Int>();
    public GameObject obstaclePrefab; // 장애물 프리팹
    public List<Vector2Int> obstaclePositions = new List<Vector2Int>(); // 장애물 위치 리스트
    public Camera mainCamera;
    public LayerMask cellLayerMask;

    public Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, CharacterBase> characterPositions = new Dictionary<Vector2Int, CharacterBase>();
    public Dictionary<Vector2Int, GameObject> enemyPositions = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, bool> activatedCellsByCharacters = new Dictionary<Vector2Int, bool>();

    // 실프와 다른 캐릭터들을 관리할 리스트
    public List<CharacterBase> characters = new List<CharacterBase>();

    public Sylph sylphInstance;
    public Vector2Int sylphPosition;


    // 게임 시작 시 아군 영역 정의
    private void Start()
    {
        DefineAllyZone();
        DefineObstacles(); // 장애물 배치 메서드 호출
    }

    // 그리드를 생성하는 메서드
    public void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // 셀의 월드 좌표 계산
                Vector3 cellPosition = gridOrigin + new Vector3(x * cellWidth, cellYOffset, z * cellHeight);

                // 랜덤으로 셀 프리팹 선택 (1/3 확률로 선택)
                GameObject randomCellPrefab = cellPrefabs[Random.Range(0, cellPrefabs.Length)];

                // 셀 프리팹을 월드에 생성하고 그리드 딕셔너리에 저장
                GameObject newCell = Instantiate(randomCellPrefab, cellPosition, Quaternion.Euler(-90, 0, 0), transform);
                gridCells[new Vector2Int(x, z)] = newCell;
                newCell.name = $"GridCell_{x}_{z}";
            }
        }
    }

    // 장애물 배치 메서드
    private void DefineObstacles()
    {
        foreach (var position in obstaclePositions)
        {
            if (gridCells.ContainsKey(position) && obstaclePrefab != null)
            {
                // 장애물의 월드 위치를 얻고 y 좌표를 0.55로 설정
                Vector3 obstaclePosition = GetWorldPositionFromGrid(position);
                obstaclePosition.y = 0.55f;

                // 장애물의 회전을 x축 -90도 고정 및 y축을 0~360도 사이의 랜덤 값으로 설정
                float randomYRotation = Random.Range(0f, 360f);
                Quaternion obstacleRotation = Quaternion.Euler(-90, randomYRotation, 0);

                // 장애물 인스턴스 생성
                Instantiate(obstaclePrefab, obstaclePosition, obstacleRotation, transform);
            }
        }
    }

    // 실프 캐릭터를 생성하는 메서드
    public void SpawnSylph()
    {
        // 실프의 시작 위치 설정
        Vector3 startPosition = gridOrigin + new Vector3(0, 1.0f, 0);
        GameObject sylphObject = Instantiate(sylphPrefab, startPosition, Quaternion.identity);
        sylphInstance = sylphObject.GetComponent<Sylph>();

        // 실프에 필요한 데이터 전달 (그리드 정보와 카메라 등)
        sylphInstance.cellWidth = cellWidth;
        sylphInstance.cellHeight = cellHeight;
        sylphInstance.gridOrigin = gridOrigin;
        sylphInstance.pointerPrefab = pointerPrefab;
        sylphInstance.mainCamera = mainCamera;
        sylphInstance.cellLayerMask = cellLayerMask;
    }

    // 아군 영역을 정의하는 메서드
    private void DefineAllyZone()
    {
        foreach (var position in allyZonePositions)
        {
            if (gridCells.ContainsKey(position))
            {
                gridCells[position].GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }
    public bool IsAllyZone(Vector2Int position)
    {
        return allyZonePositions.Contains(position);
    }

    // 모든 그리드 셀의 색상을 기본 색상으로 초기화하는 메서드
    public void ResetGridColors()
    {
        foreach (var cell in gridCells.Values)
        {
            Renderer cellRenderer = cell.GetComponent<Renderer>();
            if (cellRenderer != null)
            {
                cellRenderer.material.color = defaultColor; // 기본 색상으로 초기화 (화이트)
            }
        }
        Debug.Log("GridManager: 모든 그리드 셀의 색상을 기본 색상으로 초기화했습니다.");
    }
    // 실프의 위치를 설정하는 메서드
    public void SetSylphPosition(Vector2Int position)
    {
        sylphPosition = position;
    }

    // 특정 위치가 장애물인지 확인하는 메서드
    public bool IsObstaclePosition(Vector2Int position)
    {
        return obstaclePositions.Contains(position);
    }

    // 특정 셀이 실프의 위치인지 확인하는 메서드
    public bool IsSylphPosition(Vector2Int position)
    {
        return position == sylphPosition;
    }

    // 특정 위치에 일반 캐릭터가 있는지 확인하는 메서드
    public bool IsCharacterPosition(Vector2Int position)
    {
        return characterPositions.ContainsKey(position);
    }
    // 적 위치를 추가하는 메서드
    public void AddEnemyPosition(Vector2Int position, GameObject enemy)
    {
        if (!enemyPositions.ContainsKey(position))
        {
            enemyPositions[position] = enemy;
        }
    }

    public void UpdateGridAfterEnemyDeath(EnemyBase enemy)
    {
        // 적이 삭제된 후 참조 제거
        if (enemyPositions.ContainsKey(enemy.CurrentGridPosition))
        {
            if (enemyPositions[enemy.CurrentGridPosition] == enemy.gameObject)
            {
                enemyPositions.Remove(enemy.CurrentGridPosition);
                Debug.Log($"GridManager에서 {enemy.name} 제거 완료.");
            }
        }
    }

    // 적 위치를 제거하는 메서드
    public void RemoveEnemyPosition(Vector2Int position)
    {
        if (enemyPositions.ContainsKey(position))
        {
            enemyPositions.Remove(position);
        }
    }

    // 특정 위치에 적이 있는지 확인하는 메서드
    public bool IsEnemyPosition(Vector2Int position)
    {
        return enemyPositions.ContainsKey(position);
    }

    // 특정 위치의 적 객체를 반환하는 메서드
    public EnemyBase GetEnemyAtPosition(Vector2Int position)
    {
        if (enemyPositions.ContainsKey(position))
        {
            return enemyPositions[position].GetComponent<EnemyBase>();
        }
        return null;
    }

    public CharacterBase GetCharacterAtPosition(Vector2Int position)
    {
        return characterPositions.ContainsKey(position) ? characterPositions[position] : null;
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellWidth);
        int z = Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / cellHeight);
        return new Vector2Int(x, z);
    }

    public void AddCharacterToGrid(Vector2Int position, CharacterBase character)
    {
        if (!characterPositions.ContainsKey(position))
        {
            characterPositions[position] = character;
            characters.Add(character);  // 캐릭터 리스트에 추가
        }
    }
    public void RemoveCharacterFromGrid(CharacterBase character)
    {
        Vector2Int positionToRemove = characterPositions.FirstOrDefault(x => x.Value == character).Key;
        if (positionToRemove != null)
        {
            characterPositions.Remove(positionToRemove);
            characters.Remove(character);
        }
    }

    // 그리드 좌표를 월드 좌표로 변환하는 메서드
    public Vector3 GetWorldPositionFromGrid(Vector2Int gridPosition)
    {
        float x = gridOrigin.x + (gridPosition.x * cellWidth);
        float z = gridOrigin.z + (gridPosition.y * cellHeight);
        return new Vector3(x, 0, z); // y 좌표는 0으로 고정
    }

    // 그리드 내에 위치가 있는지 확인하는 메서드
    public bool IsWithinGridBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < width &&
               gridPosition.y >= 0 && gridPosition.y < height;
    }

    // 특정 셀이 캐릭터에 의해 활성화되었는지 확인하는 메서드
    public bool IsCellActivatedByCharacter(Vector2Int gridPosition)
    {
        return activatedCellsByCharacters.ContainsKey(gridPosition) && activatedCellsByCharacters[gridPosition];
    }

    // 캐릭터에 의해 활성화된 셀을 기록하는 메서드
    public void ActivateCellForCharacter(Vector2Int gridPosition)
    {
        if (!activatedCellsByCharacters.ContainsKey(gridPosition))
        {
            activatedCellsByCharacters[gridPosition] = true;
        }
    }
    // 캐릭터에 의해 활성화된 셀을 비활성화하는 메서드
    public void DeactivateCellsForCharacter(List<Vector2Int> gridPositions)
    {
        foreach (var gridPosition in gridPositions)
        {
            if (activatedCellsByCharacters.ContainsKey(gridPosition))
            {
                activatedCellsByCharacters[gridPosition] = false; // 활성화 상태 해제

                // 셀 색상 초기화
                if (gridCells.ContainsKey(gridPosition))
                {
                    Renderer cellRenderer = gridCells[gridPosition].GetComponent<Renderer>();
                    if (cellRenderer != null)
                    {
                        cellRenderer.material.color = defaultColor; // 기본 색상으로 설정
                    }
                }
            }
        }
    }

    // GridManager에서 활성화된 셀을 모두 초기화하는 메서드
    public void ClearActivatedCells()
    {
        activatedCellsByCharacters.Clear(); // 전체 초기화
        Debug.Log("모든 활성화된 셀이 초기화되었습니다.");
    }
}
