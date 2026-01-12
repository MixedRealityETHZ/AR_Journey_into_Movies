using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateIcon : MonoBehaviour
{
    public float speed = 200f;

    void Update()
    {
        transform.Rotate(Vector3.back, speed * Time.deltaTime);
    }
}