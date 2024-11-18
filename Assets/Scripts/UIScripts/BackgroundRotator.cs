using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundRotator : MonoBehaviour
{
    public float rotationSpeed = 10f; // Y축 회전 속도

    void Update()
    {
        // 현재 로컬 회전을 가져옴
        Vector3 currentRotation = transform.localEulerAngles;

        // Y축만 변경하고 나머지는 그대로 유지
        currentRotation.y += rotationSpeed * Time.deltaTime;

        // 갱신된 회전을 적용
        transform.localEulerAngles = currentRotation;
    }
}
