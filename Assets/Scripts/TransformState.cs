using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TransformState : INetworkSerializable
{
    public int Tick;
    public Vector3 Position;
    //public Quaternion Rotation;
    public bool HasStartedMoving;

    // This is called whenever you want to send or recieve a value
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // IF we are recieving a value from the network (rather than sending, which would be "serializer.IsWriter" instead)
        // We are the READER
        if (serializer.IsReader)
        {
            //  Grab the recieved value and store it as a variable named "reader"
            var reader = serializer.GetFastBufferReader();

            // The order in which you READ, is also the same order in which you need to WRITE
            // So we read the value and get the Tick, then Position, then the bool "HasStartedMoving"
            reader.ReadValueSafe(out Tick);
            reader.ReadValueSafe(out Position);
            reader.ReadValueSafe(out HasStartedMoving);
        } else // Else, meaning if you are the WRITER instead
        {
            var writer = serializer.GetFastBufferWriter();
            // Write our new values
            writer.WriteValueSafe(Tick);
            writer.WriteValueSafe(Position);
            writer.WriteValueSafe(HasStartedMoving);

        }
    }
}
