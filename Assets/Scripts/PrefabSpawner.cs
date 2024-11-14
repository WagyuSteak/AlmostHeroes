using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class PrefabSpawner : MonoBehaviour
{
    public GameObject[] characterPrefabs; // ������, ��ó, ������, ������Ʈ ������ �迭
    public TurnManager turnManager; // �� ������ ����ϴ� TurnManager�� ����
    public GridManager gridManager; // �׸��� ������ �� Ȱ��ȭ�� ����ϴ� GridManager�� ����
    public Button[] characterButtons; // ĳ���� ���� ��ư��
    public event Action OnCharacterPlaced; // ĳ���Ͱ� ��ġ�� �� �߻��ϴ� �̺�Ʈ
    public Vector3 defaultRotation; // ĳ���Ͱ� ������ ���� �⺻ ȸ�� ���� (x, y, z)

    private List<GameObject> tempPlacedCharacters = new List<GameObject>(); // �ӽ÷� ��ġ�� ĳ���͵��� �����ϴ� ����Ʈ
    private int charactersPlacedCount = 0;
    private GameObject lastPlacedCharacter = null; // ���������� ��ġ�� ĳ����
    private void Start()
    {
        // �� ĳ���� ���� ��ư�� �̺�Ʈ ���
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i; // ���� ������ �ε��� ���� ĸó�Ͽ� ��ư�� ����
            characterButtons[i].onClick.AddListener(() => PlaceCharacter(index)); // ��ư Ŭ�� �� �ش� �ε����� ĳ���� ����
        }

    }

    private void Update()
    {
        // ���콺 ��Ŭ�� �� ���������� ��ġ�� ĳ���͸� �ٽ� ���콺�� ���󰡰� ����
        if (Input.GetMouseButtonDown(1) && lastPlacedCharacter != null)
        {
            // charactersPlacedCount�� ���ҽ��� ���ġ ���� ���� �ݿ�
            charactersPlacedCount--;

            // FollowMouse ������Ʈ�� �̹� �ִ��� Ȯ��
            FollowMouse followMouse = lastPlacedCharacter.GetComponent<FollowMouse>();
            if (followMouse == null)
            {
                followMouse = lastPlacedCharacter.AddComponent<FollowMouse>();
            }
            followMouse.SetGridManager(gridManager);

            // ��ġ �Ϸ� �̺�Ʈ ���
            followMouse.OnPlacementCompleted += () =>
            {
                charactersPlacedCount++;
                if (charactersPlacedCount >= 4)
                {
                    ConfirmPlacement();
                }
            };

            lastPlacedCharacter = null; // �ٽ� ���󰡰� ������ �� �ʱ�ȭ
        }
    }

    // ĳ���͸� �ӽ÷� ��ġ�ϴ� �޼���
    private void PlaceCharacter(int index)
    {
        // ��� ��ư ��Ȱ��ȭ
        SetButtonsInteractable(false);

        // ���õ� ĳ���� ������ ��������
        GameObject characterPrefab = characterPrefabs[index];

        // ȭ�鿡 ���̴� ���콺 ��ġ���� ĳ������ ���� ��ġ ���
        Vector3 spawnPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
        spawnPosition.y = 0; // y ��ǥ�� 0���� ����

        // ������ �⺻ ȸ���� ����Ͽ� ĳ���� ����
        Quaternion spawnRotation = Quaternion.Euler(defaultRotation);
        GameObject character = Instantiate(characterPrefab, spawnPosition, spawnRotation);
        tempPlacedCharacters.Add(character); // �ӽ� ����Ʈ�� �߰�
        lastPlacedCharacter = character; // ���������� ��ġ�� ĳ���� ������Ʈ

        // ������ ĳ���Ϳ� GridManager�� TurnManager�� ���� ����
        CharacterBase characterBase = character.GetComponent<CharacterBase>();

        // ĳ���Ͱ� ���콺�� ���� �̵��ϵ��� ����
        FollowMouse followMouse = character.AddComponent<FollowMouse>();
        followMouse.SetGridManager(gridManager);

        // ĳ���� ��ġ �Ϸ� �� ȣ��� �̺�Ʈ ���
        followMouse.OnPlacementCompleted += () =>
        {
            // ��� ��ư ��Ȱ��ȭ
            SetButtonsInteractable(true);
            characterButtons[index].gameObject.SetActive(false); // ĳ���� ��ư ��Ȱ��ȭ
            charactersPlacedCount++;
            if (charactersPlacedCount >= 4)
            {
                ConfirmPlacement();
            }
        };

    }

    // ��� ĳ������ ��ġ�� Ȯ���ϴ� �޼���
    private void ConfirmPlacement()
    {
        foreach (GameObject character in tempPlacedCharacters)
        {
            CharacterBase characterBase = character.GetComponent<CharacterBase>();
            if (characterBase != null)
            {
                // ĳ������ �׸��� ��ġ Ȯ�� �� TurnManager�� ���
                Vector2Int gridPosition = gridManager.GetGridPosition(character.transform.position);
                characterBase.SetGridPosition(gridPosition);
                gridManager.AddCharacterToGrid(gridPosition, characterBase);
                FindObjectOfType<TurnManager>().OnCharacterPlaced(characterBase);
            }
        }

        tempPlacedCharacters.Clear(); // �ӽ� ����Ʈ ����
        lastPlacedCharacter = null; // �ٽ� ���󰡰� ������ �� �ʱ�ȭ
        Debug.Log("��� ĳ���� ��ġ�� �Ϸ�Ǿ����ϴ�.");
    }

    // ��� ĳ���� ���� ��ư�� Ȱ��ȭ ���¸� �����ϴ� �޼���
    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var button in characterButtons)
        {
            button.interactable = interactable;
        }
    }
        // ĳ���Ͱ� ���콺�� ���� �̵��ϰ� ��ġ�Ǵ� ������ ����ϴ� FollowMouse Ŭ����
        public class FollowMouse : MonoBehaviour
    {
        public event Action OnPlacementCompleted; // ĳ���� ��ġ �Ϸ� �� �߻��� �̺�Ʈ

        private bool followingMouse = true; // ĳ���Ͱ� ���콺�� ���� �̵��ϴ��� ����
        private GridManager gridManager; // �׸��� �Ŵ��� ����

        private Vector2Int currentGridPosition; // ���� �׸��� ��ġ
        private Vector2Int? previousInvalidCell = null; // ������ ���������� ǥ�õ� �߸��� �� ��ġ

        // GridManager�� �����ϴ� �޼���
        public void SetGridManager(GridManager manager)
        {
            gridManager = manager;
        }

        // �� �����Ӹ��� ����Ǵ� �޼���
        void Update()
        {
            if (followingMouse)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // ���콺 ��ġ���� ���� ����
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridManager.cellLayerMask))
                {
                    Vector2Int gridPosition = gridManager.GetGridPosition(hit.point); // ���콺 ��ġ�� �׸��� ��ǥ�� ������

                    // �׸��� ��� ���� �ִ��� Ȯ��
                    if (gridManager.IsWithinGridBounds(gridPosition))
                    {
                        Vector3 gridCellCenter = gridManager.GetWorldPositionFromGrid(gridPosition);
                        transform.position = new Vector3(gridCellCenter.x, 1.3f, gridCellCenter.z); // ĳ���� ��ġ ������Ʈ

                        // �Ʊ� ������ ��ġ�� �ְ� ���� ����ִ��� Ȯ��
                        if (gridManager.IsAllyZone(gridPosition) &&
                            !gridManager.IsSylphPosition(gridPosition) &&
                            !gridManager.IsCharacterPosition(gridPosition) &&
                            !gridManager.IsObstaclePosition(gridPosition) &&
                            !gridManager.IsEnemyPosition(gridPosition))
                        {
                            ResetPreviousInvalidCellColor();
                            if (Input.GetMouseButtonDown(0))
                            {
                                followingMouse = false;
                                OnPlacementCompleted?.Invoke();
                                Destroy(this); // FollowMouse ������Ʈ ����
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
                    // �׸��� �ܺο��� ĳ���Ͱ� ���콺�� ���󰡵��� ����
                    Vector3 mousePosition = ray.GetPoint(6f);
                    transform.position = new Vector3(mousePosition.x, 1.5f, mousePosition.z);

                    ResetPreviousInvalidCellColor();
                }
            }
        }

        // �߸��� ���� ���������� ���� ǥ���ϴ� �޼���
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
                    cellRenderer.material.color = Color.red; // �� ������ ���������� ����
                    previousInvalidCell = gridPosition; // ���� �߸��� �� ��ġ ������Ʈ
                }
            }
        }

        // ���� �߸��� ���� ������ ������� �����ϴ� �޼���
        private void ResetPreviousInvalidCellColor()
        {
            if (previousInvalidCell.HasValue && gridManager.gridCells.ContainsKey(previousInvalidCell.Value))
            {
                Renderer cellRenderer = gridManager.gridCells[previousInvalidCell.Value].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    cellRenderer.material.color = gridManager.defaultColor; // �⺻ �������� ����
                }
                previousInvalidCell = null;
            }
        }
    }
}
