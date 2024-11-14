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
    // TurnManager�� GridManager�� ���� ���� ����
    TurnManager turnManager; // ������ �� ������ ����ϴ� TurnManager�� ����
    GridManager gridManager; // �׸��� ������ �� Ȱ��ȭ�� ����ϴ� GridManager�� ����
    UIManager uiManager;

    // ������ �ִ� ������ �׸����� �� ũ�� �� ���� ��ǥ
    public int maxMana = 10; // ������ �ִ� ���� ��ġ
    public int currentMana; // ������ ���� ���� ��ġ
    public float cellWidth;   // �׸��� ���� �ʺ�
    public float cellHeight;  // �׸��� ���� ����
    public Vector3 gridOrigin; // �׸����� ���� ��ǥ�� ���� ��ġ

    // ���� �̵� �� ��Ÿ���� ������, ī�޶�, ���̾� ����ũ, ������ ���� ���� ����
    public GameObject pointerPrefab;    // ���콺 �����͸� ���󰡴� ������
    public GameObject circlePrefab;    // ���׶� ������ (��� ������ �� �� ���� ����)
    public GameObject linePrefab;      // ���� ������ (�� �� ���̿� ����Ǵ� ��)
    public float pathIndicatorHeight = 1.5f; // ��� ǥ�� ������Ʈ�� Y�� ����

    private Vector2Int lastGridPosition; // ������ ��ġ�� �����ϴ� ����
    public Camera mainCamera;           // ���� ī�޶� ����
    public LayerMask cellLayerMask;     // ���� �����ϱ� ���� ���̾� ����ũ

    // �̺�Ʈ�� ĳ���� ����
    public event Action OnSylphMoved; // ������ �̵��� �� ȣ��Ǵ� �̺�Ʈ
    private GameObject currentPointer; // ���� ���콺 ��ġ�� ��Ÿ���� ������
    public Vector2Int gridPosition;   // ������ ���� �׸��� ��ǥ
    public List<GameObject> pathIndicators = new List<GameObject>(); // ��� ǥ�� ������Ʈ ����Ʈ
    public List<Vector2Int> activatedCells = new List<Vector2Int>(); // ������ Ȱ��ȭ�� ������ ����Ʈ
    public List<CharacterBase> encounteredCharacters = new List<CharacterBase>(); // ������ ����ģ ĳ���� ����Ʈ

    private Vector2Int startGridPosition; // ���� �׸��� ��ġ
    private List<GameObject> pathCircles = new List<GameObject>(); // ���׶� �����յ��� �����ϴ� ����Ʈ
    private List<GameObject> pathLines = new List<GameObject>();   // ���� �����յ��� �����ϴ� ����Ʈ

    // ���� ����
    private bool hasMoved = false; // ������ �� ���̶� �̵��ߴ��� ���θ� ����
    public bool isControllable = false; // ������ ���� ������ �������� ����
    private bool isMoving = false; // �̵� ������ ���θ� ��Ÿ���� ����

    // �ʱ� ���� �� ������Ʈ ã��
    private void Start()
    {
        gridManager = FindObjectOfType<GridManager>(); // GridManager�� ������ ã��
        turnManager = FindObjectOfType<TurnManager>(); // TurnManager�� ������ ã��
        uiManager = FindObjectOfType<UIManager>();
        SylphTurn();
        gridPosition = new Vector2Int(0, 0); // �׸��� ���� ��ġ ����
        MoveToCell(gridPosition); // �ʱ� ��ġ�� �̵� ����
        uiManager.negativeEndTurnButton();
        // �ʱ� ��ġ�� ������ �� Y ��ǥ�� 1.3���� �����ϰ�, x�� -90�� ȸ�� ����
        transform.position = new Vector3(transform.position.x, 1.05f, transform.position.z);
        transform.rotation = Quaternion.Euler(0, 90, transform.rotation.eulerAngles.z);

    }

    public void SetControllable(bool canControl)
    {
        isControllable = canControl; // ������ ���� �������� ���� ����
        Debug.Log($"���� ���� ���� ����: {isControllable}"); // ���� ���� ���� ���
    }


    public void SylphTurn()
    {
        currentMana = maxMana; // ������ ���� ������ �ִ� ������ �ʱ�ȭ
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

        if (isControllable) // ������ ���� ������ ������ ����
        {
            // ���� ��ġ���� ĳ���� ����
            DetectCharacterInCell(gridPosition);

            // ���� ��ġ�� �ٸ� ĳ���Ͱ� �����Ǿ����� Ȯ��
            if (encounteredCharacters.Count == 0 || !encounteredCharacters.Exists(character => character.GetCurrentGridPosition() == gridPosition))
            {
                if (activatedCells.Count > 1 && !isMoving) // Ȱ��ȭ�� ���� ���� ��쿡�� �̵� ����
                {

                    uiManager.ActivateEndTurnButton();
                    if (Input.GetKeyDown(KeyCode.Space)) // �����̽� Ű�� ������ ��
                    {
                        uiManager.negativeEndTurnButton();
                        StartCoroutine(StartMovement()); // �̵��� �����ϴ� �ڷ�ƾ ȣ��
                    }
                }
            }
            else
            {
                uiManager.negativeEndTurnButton();
                Debug.Log("���� ��ġ�� ĳ���Ͱ� �־� ���� ������ �� �����ϴ�."); // ���� ��ġ�� ĳ���Ͱ� �־� �̵��� �� ������ ���
            }

            if (Input.GetMouseButtonDown(0)) // ���콺 ���� ��ư�� ������ ��
            {
                HandleMouseClick(); // ���콺 Ŭ�� ó��
            }

            if (Input.GetMouseButton(0) && currentPointer != null) // ���콺 ���� ��ư�� ���� ���¿��� �����Ͱ� ������ ��
            {
                HandlePointerMovement(); // ������ �̵� ó��
            }

            if (Input.GetMouseButtonUp(0)) // ���콺 ���� ��ư�� �������� ��
            {
                if (currentPointer != null) // ���� �����Ͱ� �����Ѵٸ�
                {
                    Destroy(currentPointer); // ������ ������Ʈ ����
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
                // ��� Ȱ��ȭ�� �� ����
                gridManager.DeactivateCellsForCharacter(activatedCells);
                activatedCells.Clear();

                // ��� ��ο� �ε������� ����
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
            ClearPathIndicators(); // ��� ǥ�� ������Ʈ �ʱ�ȭ
            encounteredCharacters.Clear(); // ����ģ ĳ���� ����Ʈ �ʱ�ȭ
            uiManager.negativeEndTurnButton();

            // �̵� ���� Ƚ�� �ʱ�ȭ
            currentMana = maxMana;

            gridPosition = startGridPosition;
                Debug.Log("��� Ȱ��ȭ�� ���� ����ϰ� �ʱ�ȭ�߽��ϴ�.");
            }
            else
            {
                Debug.Log("����� Ȱ��ȭ�� ���� �����ϴ�.");
            }
    }
        public void GiveMana()
    {
        TransferManaToEncounteredCharacters(); // ����ģ ĳ���͵鿡�� ������ ����
        DisplayEncounteredCharacters(); // ����ģ ĳ���͵��� ���
        turnManager.EndSylphTurn(encounteredCharacters); // ������ ���� �����ϰ� �� �Ŵ������� �˸�
    }

    public List<CharacterBase> GetEncounteredCharacters()
    {
        return new List<CharacterBase>(encounteredCharacters); // ����ģ ĳ���� ����Ʈ ��ȯ
    }

    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // ���콺 ��ġ���� ���� ����
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // ����ĳ��Ʈ�� �����ϸ�
        {
            Vector3 pointerPosition = hit.point; // ��Ʈ�� ��ġ ��������
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition); // �׸��� ��ǥ�� ��ȯ

            if (targetGridPosition == gridPosition) // ���� �׸��� ��ġ�� �����ϴٸ�
            {
                currentPointer = Instantiate(pointerPrefab, pointerPosition, Quaternion.identity); // ������ ������ ����
                if (!activatedCells.Contains(targetGridPosition))
                {
                    activatedCells.Add(targetGridPosition); // �� Ȱ��ȭ
                }
                startGridPosition = gridPosition;
            }
        }
    }

    private void HandlePointerMovement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // ���콺 ��ġ���� ���� ����
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // ����ĳ��Ʈ�� �����ϸ�
        {
            Vector3 pointerPosition = hit.point; // ��Ʈ�� ��ġ ��������
            currentPointer.transform.position = pointerPosition; // ������ ��ġ ������Ʈ
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition); // �׸��� ��ǥ�� ��ȯ

            if (IsAdjacentToCurrentPosition(targetGridPosition) && !activatedCells.Contains(targetGridPosition) && !gridManager.IsObstaclePosition(targetGridPosition)) // ������ ���̰� ���� Ȱ��ȭ���� ���� ���
            {
                if (gridManager.IsWithinGridBounds(targetGridPosition)) // �׸��� ��� ���� �ִ��� Ȯ��
                {
                    ActivateCell(targetGridPosition); // �� Ȱ��ȭ
                }
                else
                {
                    Debug.Log("�׸��� ��踦 ����־� ��θ� ������ �� �����ϴ�."); // ��� ���̶�� ��� �޽��� ���
                }
            }
        }
    }

    private Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellWidth); // ���� ��ǥ�� �׸��� x��ǥ�� ��ȯ
        int z = Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / cellHeight); // ���� ��ǥ�� �׸��� z��ǥ�� ��ȯ
        return new Vector2Int(x, z); // �׸��� ��ǥ ��ȯ
    }

    private void ActivateCell(Vector2Int targetGridPosition)
    {
        if (currentMana > 0) // ������ ����� ��쿡��
        {
            // Ȱ��ȭ�� �� ��Ͽ� �̹� �����ϴ� ��� �߰����� ����
            if (!activatedCells.Contains(targetGridPosition))
            {
                activatedCells.Add(targetGridPosition); // Ȱ��ȭ�� �� ����Ʈ�� �߰�
                currentMana--; // ���� ����
                gridPosition = targetGridPosition; // ���� ��ġ ������Ʈ

                gridManager.SetSylphPosition(gridPosition);
                DetectCharacterInCell(targetGridPosition);

                // ��� ǥ�� ������Ʈ ���� �� ���� ����
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
        DetectCharacterInCell(targetGridPosition); // �̵� �ø��� ���� ȣ��
    }

    // Sylph Ŭ�������� ĳ���͸� �����ϴ� �޼���
    private void DetectCharacterInCell(Vector2Int gridPosition)
    {
        encounteredCharacters.RemoveAll(character => character == null || character.Health <= 0); // ���� ĳ���� ����

        foreach (CharacterBase character in gridManager.characters)
        {
            if (character.GetCurrentGridPosition() == gridPosition && character.Health > 0 && !encounteredCharacters.Contains(character))
            {
                encounteredCharacters.Add(character);
                Debug.Log($"{character.name}��(��) �׸��� ��ġ {gridPosition}���� �����߽��ϴ�.");
            }
        }
    }

    private void TransferManaToEncounteredCharacters()
    {
        foreach (CharacterBase character in encounteredCharacters) // ����ģ ĳ���͵鿡�� ���� ����
        {
            character.ReceiveMana(currentMana);
        }
    }

    private void DisplayEncounteredCharacters()
    {
        if (encounteredCharacters.Count == 0) // ����ģ ĳ���Ͱ� ���� ���
        {
            Debug.Log("����� ĳ���Ͱ� �����ϴ�.");
            return;
        }

        string characterNames = "Encountered Characters ���� ���: ";
        foreach (CharacterBase character in encounteredCharacters) // ����ģ ĳ���� �̸��� ���
        {
            characterNames += $"{character.name}, ";
        }

        characterNames = characterNames.TrimEnd(',', ' '); // ���ڿ� ���� ��ǥ�� ���� ����
        Debug.Log(characterNames); // ���������� ����ģ ĳ���� �̸� ���
    }

    private bool IsAdjacentToCurrentPosition(Vector2Int targetGridPosition)
    {
        int deltaX = Mathf.Abs(targetGridPosition.x - gridPosition.x); // x�� �Ÿ� ���� ���
        int deltaY = Mathf.Abs(targetGridPosition.y - gridPosition.y); // y�� �Ÿ� ���� ���
        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1); // �� �����θ� 1ĭ ������ ��� ������ ������ �Ǵ�
    }

    public IEnumerator StartMovement()
    {
        isMoving = true;
        // ù ��° ���� �ǳʶٱ� ���� i�� 1�� ����
        for (int i = 1; i < activatedCells.Count; i++)
        {
            Vector2Int cellPosition = activatedCells[i];

            // ��ǥ ��ġ�� Y ��ǥ�� 1.3���� ����
            Vector3 targetPosition = gridOrigin + new Vector3(cellPosition.x * cellWidth, 1.3f, cellPosition.y * cellHeight);

            // ������ ����Ͽ� y�� ȸ���� ���� (x�� ȸ�� ����)
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero) // ���� ���Ͱ� 0�� �ƴ� ���� ȸ��
            {
                Quaternion targetRotation = Quaternion.Euler(0, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles.y, 0);

                while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f) // ��ǥ ȸ���� ������ ������ ȸ��
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30); // ȸ�� �ӵ� ���� ����
                    yield return null;
                }
            }

            // ĳ���Ͱ� ��ǥ ��ġ�� ������ ������ �̵� (Y ��ǥ�� 1.3���� ����)
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 7); // �ӵ� ����
                yield return null;
            }


        }
        uiManager.negativeEndTurnButton();
        ClearActivatedCells();
        ClearPathIndicators(); // ��� ǥ�� ������Ʈ �ʱ�ȭ
        Debug.Log("��� �̵� �Ϸ�"); // �̵� �Ϸ� �޽��� ���
        GiveMana(); // ���� ����
        isMoving = false;
    }

    public void ClearPathIndicators()
    {
        // ���׶� �����յ� ����
        foreach (GameObject circle in pathCircles)
        {
            Destroy(circle);
        }
        pathCircles.Clear(); // ����Ʈ �ʱ�ȭ

        // ���� �����յ� ����
        foreach (GameObject line in pathLines)
        {
            Destroy(line);
        }
        pathLines.Clear(); // ����Ʈ �ʱ�ȭ
    }

    public void ClearActivatedCells()
    {
        activatedCells.Clear(); // Ȱ��ȭ�� �� ����Ʈ �ʱ�ȭ
        Debug.Log("������ Ȱ��ȭ�� ��� ���� �ʱ�ȭ�Ǿ����ϴ�.");
    }

    public void ClearEncounteredCharacters()
    {
        encounteredCharacters.Clear(); // ����ģ ĳ���� ����Ʈ �ʱ�ȭ
        Debug.Log("������ ���� ĳ���� ����Ʈ �ʱ�ȭ��");
    }
}