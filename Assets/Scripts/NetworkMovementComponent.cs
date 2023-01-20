using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public class NetworkMovementComponent : NetworkBehaviour
{
    // Referencing the player network script
    [SerializeField] private PlayerNetwork _pn;
    [SerializeField] private float _speed;

    //Tick rate for the server
    private int _tick = 0;
    private float _tickRate = 1f / 60f;
    private float _tickDeltaTime = 0f;
    private int _lastProcessedTick = -0;

    //Store the sent input
    private const int BUFFER_SIZE = 1024;

    //Creating a new array of sent inputs, with as many elements as the buffer size
    //The buffer size is how many stored ticks of information the array will keep stored
    private InputState[] _inputStates = new InputState[BUFFER_SIZE];

    // An array of transforms with the same buffer size
    public TransformState[] _transformStates = new TransformState[BUFFER_SIZE];

    // So the server can send this information -
    // This will be the latest transform that has been established on the server
    public NetworkVariable<TransformState> ServerTransformState = new NetworkVariable<TransformState>();
    public TransformState _previousTransformState;

    private void OnEnable() 
    {
        // Listening to the variable change, and if it does then call a function
        ServerTransformState.OnValueChanged += OnServerStateChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    //In the future, this is where server reconciliation will be established
    private void OnServerStateChanged(TransformState previousValue, TransformState serverState)
    {
        if(!IsLocalPlayer) return;

            if (_previousTransformState == null)
            {
                _previousTransformState = serverState;
            }
            TransformState calculatedState = _transformStates.First(localState => localState.Tick == serverState.Tick);
            if (calculatedState.Position != serverState.Position)
            {
                Debug.Log("Correcting client position");
                //Teleport the player to the server position
                TeleportPlayer(serverState);
                //Replay the inputs that happened after
                IEnumerable<InputState> inputs = _inputStates.Where(input => input.Tick > serverState.Tick);
                inputs = from input in inputs orderby input.Tick select input;
                
                foreach (InputState inputState in inputs)
                {
                    MovePlayer(_pn.moveDirectionPn);
                    //RotatePlayer(inputState.LookInput);

                    TransformState newTransformState = new TransformState()
                    {
                        Tick = inputState.Tick,
                        Position = transform.position,
                        //Rotation = transform.rotation,
                        HasStartedMoving = true
                    };

                    for (int i = 0; i < _transformStates.Length; i++)
                    {
                        if (_transformStates[i].Tick == inputState.Tick)
                        {
                            _transformStates[i] = newTransformState;
                            break;
                        }
                    }
                }
            }
    }

    private void TeleportPlayer(TransformState state)
    {
        _pn.enabled = false;
        transform.position = state.Position;
        //transform.rotation = state.Rotation;
        _pn.enabled = true;

        for (int i = 0; i < _transformStates.Length; i++)
        {
            if (_transformStates[i].Tick == state.Tick)
            {
                _transformStates[i] = state;
                break;
            }
        }
    }

    // Enable the PlayerNetwork script controller to call a method for actually moving
    public void ProcessLocalPlayerMovement(Vector3 movementInput)
    {
        // Used to base speed off of server tick rate, which essentially normalizes values across all clients
        _tickDeltaTime += Time.deltaTime;

        // IF we are within the tick rate
        if (_tickDeltaTime > _tickRate)
        {
            // Shows where we are in the buffer size currently
            int bufferIndex = _tick % BUFFER_SIZE;

            // If we are not the server
            if (!IsServer)
            {
                // New server RPC which takes the current tick and the input
                MovePlayerServerRpc(_tick, movementInput);
                MovePlayer(movementInput);

            } else 
            {
                MovePlayer(movementInput);

                TransformState state = new TransformState()
                {
                    Tick = _tick,
                    Position = transform.position,
                    HasStartedMoving = true
                };

                _previousTransformState = ServerTransformState.Value;
                ServerTransformState.Value = state;
            }

            InputState inputState = new InputState()
            {
                Tick = _tick,
                movementInput = movementInput,
            };

            TransformState transformState = new TransformState()
            {
                Tick = _tick,
                Position = transform.position,
                HasStartedMoving = true
            };

            _inputStates[bufferIndex] = inputState;
            _transformStates[bufferIndex] = transformState;

            _tickDeltaTime -= _tickRate;
            _tick++;


        }
    }

    public void ProcessSimulatedPlayerMovement()
    {
        _tickDeltaTime += Time.deltaTime;
        if (_tickDeltaTime > _tickRate)
        {
            if (ServerTransformState.Value.HasStartedMoving)
            {
                transform.position = ServerTransformState.Value.Position;
            }

            _tickDeltaTime -= _tickRate;
            _tick++;
        }
    }

    private void MovePlayer(Vector3 movementInput)
    {
        transform.position += movementInput * _speed * _tickRate;
    }

    // Server RPC that processes player movement
    [ServerRpc]
    private void MovePlayerServerRpc(int tick, Vector3 movementInput)
    {   
        // This will move the player
        MovePlayer(movementInput);
        // This is also where we would call functions that do other player actions, such as rotating the player

        // Set the state of the position that we arrived to, at this specific tick
        TransformState state = new TransformState()
        {
            Tick = tick,
            Position = transform.position,
            HasStartedMoving = true
        };

        //Store that state as our previous state
        _previousTransformState = ServerTransformState.Value;
        ServerTransformState.Value = state;
    }
}
