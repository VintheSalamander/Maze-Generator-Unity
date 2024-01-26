using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float playerSpeed;
    [SerializeField]
    private GameObject mesh;
    private float horizontalInput;
    private float verticalInput;

    void FixedUpdate()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        if(Math.Abs(horizontalInput) > Math.Abs(verticalInput)){
            Vector3 move = new Vector3(horizontalInput, 0, 0);
            gameObject.transform.position += move * playerSpeed;

            Quaternion newRotation = Quaternion.LookRotation(move.normalized, Vector3.up);
            mesh.transform.rotation = newRotation;

        }else if(Math.Abs(horizontalInput) < Math.Abs(verticalInput)){
            Vector3 move = new Vector3(0, 0, verticalInput);
            gameObject.transform.position += move * playerSpeed;

            Quaternion newRotation = Quaternion.LookRotation(move.normalized, Vector3.up);
            mesh.transform.rotation = newRotation;
        }
    }
}
