using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private NetworkMovementComponent _playerMovement;
    [SerializeField] private Transform aimTransform;

    private void Awake()
    {
        aimTransform = transform.Find("AimedParent");
    }

    private void Update()
    {
        if (IsClient && IsLocalPlayer)
        {
            MousePositionHandler();
            float moveX = 0f;
            float moveY = 0f;
            if(Input.GetKey(KeyCode.W)) moveY = +1f;
            if(Input.GetKey(KeyCode.S)) moveY = -1f;
            if(Input.GetKey(KeyCode.A)) moveX = -1f;
            if(Input.GetKey(KeyCode.D)) moveX = +1f;

            Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;
            _playerMovement.ProcessLocalPlayerMovement(moveDirection);

        } else {
            _playerMovement.ProcessSimulatedPlayerMovement();
        }
    }

    public void MousePositionHandler() 
    {
        // Grabs the main camera and does a "screen to world point" on the mouse position
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // Sets the z axis to 0
        mouseWorldPosition.z = 0f;
        Vector3 aimDirection = (mouseWorldPosition - transform.position).normalized;
        // Convert to a Euler angle
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimTransform.eulerAngles = new Vector3(0, 0, angle);
    }
}
