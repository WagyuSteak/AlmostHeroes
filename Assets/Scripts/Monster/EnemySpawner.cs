using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab; // �� ĳ������ ������
        public Vector2Int gridPosition; // �׸��� ��ǥ
        public Vector3 rotation; // ȸ�� ���� (x, y, z)
        public float additionalHeight = 0.6f; // y ���� �߰� ����
    }

    public List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>(); // ������ �� ����Ʈ
    public GridManager gridManager; // �׸��� �Ŵ��� ����

    void Start()
    {
        // ���� ���� �� �� ĳ���� ��ġ
        foreach (EnemySpawnData enemyData in enemiesToSpawn)
        {
            SpawnEnemy(enemyData);
        }
    }
    private void SpawnEnemy(EnemySpawnData enemyData)
    {
        if (gridManager != null && gridManager.IsWithinGridBounds(enemyData.gridPosition))
        {
            Vector3 worldPosition = gridManager.GetWorldPositionFromGrid(enemyData.gridPosition);
            // �߰� ���� ����
            worldPosition.y += enemyData.additionalHeight;
            Quaternion worldRotation = Quaternion.Euler(enemyData.rotation); // ������ ȸ�� ������ ȸ��
            GameObject enemyInstance = Instantiate(enemyData.enemyPrefab, worldPosition, worldRotation);
            // ���� ��ġ ������ GridManager�� ����
            gridManager.AddEnemyPosition(enemyData.gridPosition, enemyInstance);
        }
        else
        {
            Debug.LogWarning($"�׸��� ��ǥ {enemyData.gridPosition}�� �׸��� ��踦 ������ϴ�.");
        }
    }
}
