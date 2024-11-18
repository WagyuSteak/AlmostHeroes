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
    public GameObject obstaclePrefab; // ��ֹ� ������
    public List<Vector2Int> obstaclePositions = new List<Vector2Int>(); // ��ֹ� ��ġ ����Ʈ
    public Camera mainCamera;
    public LayerMask cellLayerMask;

    public Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, CharacterBase> characterPositions = new Dictionary<Vector2Int, CharacterBase>();
    public Dictionary<Vector2Int, GameObject> enemyPositions = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, bool> activatedCellsByCharacters = new Dictionary<Vector2Int, bool>();

    // ������ �ٸ� ĳ���͵��� ������ ����Ʈ
    public List<CharacterBase> characters = new List<CharacterBase>();

    public Sylph sylphInstance;
    public Vector2Int sylphPosition;


    // ���� ���� �� �Ʊ� ���� ����
    private void Start()
    {
        DefineAllyZone();
        DefineObstacles(); // ��ֹ� ��ġ �޼��� ȣ��
    }

    // �׸��带 �����ϴ� �޼���
    public void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // ���� ���� ��ǥ ���
                Vector3 cellPosition = gridOrigin + new Vector3(x * cellWidth, cellYOffset, z * cellHeight);

                // �������� �� ������ ���� (1/3 Ȯ���� ����)
                GameObject randomCellPrefab = cellPrefabs[Random.Range(0, cellPrefabs.Length)];

                // �� �������� ���忡 �����ϰ� �׸��� ��ųʸ��� ����
                GameObject newCell = Instantiate(randomCellPrefab, cellPosition, Quaternion.Euler(-90, 0, 0), transform);
                gridCells[new Vector2Int(x, z)] = newCell;
                newCell.name = $"GridCell_{x}_{z}";
            }
        }
    }

    // ��ֹ� ��ġ �޼���
    private void DefineObstacles()
    {
        foreach (var position in obstaclePositions)
        {
            if (gridCells.ContainsKey(position) && obstaclePrefab != null)
            {
                // ��ֹ��� ���� ��ġ�� ��� y ��ǥ�� 0.55�� ����
                Vector3 obstaclePosition = GetWorldPositionFromGrid(position);
                obstaclePosition.y = 0.55f;

                // ��ֹ��� ȸ���� x�� -90�� ���� �� y���� 0~360�� ������ ���� ������ ����
                float randomYRotation = Random.Range(0f, 360f);
                Quaternion obstacleRotation = Quaternion.Euler(-90, randomYRotation, 0);

                // ��ֹ� �ν��Ͻ� ����
                Instantiate(obstaclePrefab, obstaclePosition, obstacleRotation, transform);
            }
        }
    }

    // ���� ĳ���͸� �����ϴ� �޼���
    public void SpawnSylph()
    {
        // ������ ���� ��ġ ����
        Vector3 startPosition = gridOrigin + new Vector3(0, 1.0f, 0);
        GameObject sylphObject = Instantiate(sylphPrefab, startPosition, Quaternion.identity);
        sylphInstance = sylphObject.GetComponent<Sylph>();

        // ������ �ʿ��� ������ ���� (�׸��� ������ ī�޶� ��)
        sylphInstance.cellWidth = cellWidth;
        sylphInstance.cellHeight = cellHeight;
        sylphInstance.gridOrigin = gridOrigin;
        sylphInstance.pointerPrefab = pointerPrefab;
        sylphInstance.mainCamera = mainCamera;
        sylphInstance.cellLayerMask = cellLayerMask;
    }

    // �Ʊ� ������ �����ϴ� �޼���
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

    // ��� �׸��� ���� ������ �⺻ �������� �ʱ�ȭ�ϴ� �޼���
    public void ResetGridColors()
    {
        foreach (var cell in gridCells.Values)
        {
            Renderer cellRenderer = cell.GetComponent<Renderer>();
            if (cellRenderer != null)
            {
                cellRenderer.material.color = defaultColor; // �⺻ �������� �ʱ�ȭ (ȭ��Ʈ)
            }
        }
        Debug.Log("GridManager: ��� �׸��� ���� ������ �⺻ �������� �ʱ�ȭ�߽��ϴ�.");
    }
    // ������ ��ġ�� �����ϴ� �޼���
    public void SetSylphPosition(Vector2Int position)
    {
        sylphPosition = position;
    }

    // Ư�� ��ġ�� ��ֹ����� Ȯ���ϴ� �޼���
    public bool IsObstaclePosition(Vector2Int position)
    {
        return obstaclePositions.Contains(position);
    }

    // Ư�� ���� ������ ��ġ���� Ȯ���ϴ� �޼���
    public bool IsSylphPosition(Vector2Int position)
    {
        return position == sylphPosition;
    }

    // Ư�� ��ġ�� �Ϲ� ĳ���Ͱ� �ִ��� Ȯ���ϴ� �޼���
    public bool IsCharacterPosition(Vector2Int position)
    {
        return characterPositions.ContainsKey(position);
    }
    // �� ��ġ�� �߰��ϴ� �޼���
    public void AddEnemyPosition(Vector2Int position, GameObject enemy)
    {
        if (!enemyPositions.ContainsKey(position))
        {
            enemyPositions[position] = enemy;
        }
    }

    public void UpdateGridAfterEnemyDeath(EnemyBase enemy)
    {
        // ���� ������ �� ���� ����
        if (enemyPositions.ContainsKey(enemy.CurrentGridPosition))
        {
            if (enemyPositions[enemy.CurrentGridPosition] == enemy.gameObject)
            {
                enemyPositions.Remove(enemy.CurrentGridPosition);
                Debug.Log($"GridManager���� {enemy.name} ���� �Ϸ�.");
            }
        }
    }

    // �� ��ġ�� �����ϴ� �޼���
    public void RemoveEnemyPosition(Vector2Int position)
    {
        if (enemyPositions.ContainsKey(position))
        {
            enemyPositions.Remove(position);
        }
    }

    // Ư�� ��ġ�� ���� �ִ��� Ȯ���ϴ� �޼���
    public bool IsEnemyPosition(Vector2Int position)
    {
        return enemyPositions.ContainsKey(position);
    }

    // Ư�� ��ġ�� �� ��ü�� ��ȯ�ϴ� �޼���
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
            characters.Add(character);  // ĳ���� ����Ʈ�� �߰�
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

    // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ�ϴ� �޼���
    public Vector3 GetWorldPositionFromGrid(Vector2Int gridPosition)
    {
        float x = gridOrigin.x + (gridPosition.x * cellWidth);
        float z = gridOrigin.z + (gridPosition.y * cellHeight);
        return new Vector3(x, 0, z); // y ��ǥ�� 0���� ����
    }

    // �׸��� ���� ��ġ�� �ִ��� Ȯ���ϴ� �޼���
    public bool IsWithinGridBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < width &&
               gridPosition.y >= 0 && gridPosition.y < height;
    }

    // Ư�� ���� ĳ���Ϳ� ���� Ȱ��ȭ�Ǿ����� Ȯ���ϴ� �޼���
    public bool IsCellActivatedByCharacter(Vector2Int gridPosition)
    {
        return activatedCellsByCharacters.ContainsKey(gridPosition) && activatedCellsByCharacters[gridPosition];
    }

    // ĳ���Ϳ� ���� Ȱ��ȭ�� ���� ����ϴ� �޼���
    public void ActivateCellForCharacter(Vector2Int gridPosition)
    {
        if (!activatedCellsByCharacters.ContainsKey(gridPosition))
        {
            activatedCellsByCharacters[gridPosition] = true;
        }
    }
    // ĳ���Ϳ� ���� Ȱ��ȭ�� ���� ��Ȱ��ȭ�ϴ� �޼���
    public void DeactivateCellsForCharacter(List<Vector2Int> gridPositions)
    {
        foreach (var gridPosition in gridPositions)
        {
            if (activatedCellsByCharacters.ContainsKey(gridPosition))
            {
                activatedCellsByCharacters[gridPosition] = false; // Ȱ��ȭ ���� ����

                // �� ���� �ʱ�ȭ
                if (gridCells.ContainsKey(gridPosition))
                {
                    Renderer cellRenderer = gridCells[gridPosition].GetComponent<Renderer>();
                    if (cellRenderer != null)
                    {
                        cellRenderer.material.color = defaultColor; // �⺻ �������� ����
                    }
                }
            }
        }
    }

    // GridManager���� Ȱ��ȭ�� ���� ��� �ʱ�ȭ�ϴ� �޼���
    public void ClearActivatedCells()
    {
        activatedCellsByCharacters.Clear(); // ��ü �ʱ�ȭ
        Debug.Log("��� Ȱ��ȭ�� ���� �ʱ�ȭ�Ǿ����ϴ�.");
    }
}
