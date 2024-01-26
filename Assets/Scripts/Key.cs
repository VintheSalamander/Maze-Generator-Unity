using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    public Exit Exit;
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 90.0f;

    public float bobbingAmplitude = 0.5f; 
    public float bobbingFrequency = 1.0f; 

    void Start()
    {
    }

    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        Vector3 newPosition = new Vector3(0, Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude, 0);
        transform.position += newPosition;
    }

    private void OnTriggerEnter(Collider other){
        if (other.CompareTag("Player")){
            Exit.KeyCollected();
            Destroy(gameObject);
        }
    }
}
