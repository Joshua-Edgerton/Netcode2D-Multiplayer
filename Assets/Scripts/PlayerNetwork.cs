using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private NetworkMovementComponent _playerMovement;
    [SerializeField] private Transform aimTransform;
    public Vector3 moveDirectionPn;

    private void Awake()
    {
        aimTransform = transform.Find("AimedParent");
    }

    private void Update()
    {  
        if (IsClient && IsLocalPlayer)
        { 
            float moveX = 0f;
            float moveY = 0f;

            if(Input.GetKey(KeyCode.W)) moveY = +1f;
            if(Input.GetKey(KeyCode.S)) moveY = -1f;
            if(Input.GetKey(KeyCode.A)) moveX = -1f;
            if(Input.GetKey(KeyCode.D)) moveX = +1f;

            Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;
            moveDirectionPn = moveDirection;
            _playerMovement.ProcessLocalPlayerMovement(moveDirection);

        } else {
            _playerMovement.ProcessSimulatedPlayerMovement();
        }
    }
}
