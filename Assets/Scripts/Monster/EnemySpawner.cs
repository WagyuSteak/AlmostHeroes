using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab; // 적 캐릭터의 프리팹
        public Vector2Int gridPosition; // 그리드 좌표
        public Vector3 rotation; // 회전 각도 (x, y, z)
        public float additionalHeight = 0.6f; // y 축의 추가 높이
    }

    public List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>(); // 스폰할 적 리스트
    public GridManager gridManager; // 그리드 매니저 참조

    void Start()
    {
        // 게임 시작 시 적 캐릭터 배치
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
            // 추가 높이 적용
            worldPosition.y += enemyData.additionalHeight;
            Quaternion worldRotation = Quaternion.Euler(enemyData.rotation); // 설정된 회전 각도로 회전
            GameObject enemyInstance = Instantiate(enemyData.enemyPrefab, worldPosition, worldRotation);
            // 적의 위치 정보를 GridManager에 전달
            gridManager.AddEnemyPosition(enemyData.gridPosition, enemyInstance);
        }
        else
        {
            Debug.LogWarning($"그리드 좌표 {enemyData.gridPosition}가 그리드 경계를 벗어났습니다.");
        }
    }
}
