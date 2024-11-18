using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundRotator : MonoBehaviour
{
    public float rotationSpeed = 10f; // Y�� ȸ�� �ӵ�

    void Update()
    {
        // ���� ���� ȸ���� ������
        Vector3 currentRotation = transform.localEulerAngles;

        // Y�ุ �����ϰ� �������� �״�� ����
        currentRotation.y += rotationSpeed * Time.deltaTime;

        // ���ŵ� ȸ���� ����
        transform.localEulerAngles = currentRotation;
    }
}
