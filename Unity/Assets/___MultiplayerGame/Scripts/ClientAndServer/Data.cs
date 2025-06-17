using System;
using UnityEngine;

[Serializable]
public class RequestData
{
    public string Type;
    public object Body;
}

[Serializable]
public class ClientData
{
    public string Id;
    public Vec3 Position;
}

[Serializable]
public class ResponseData
{
    public string Type;
    public object Body;
}

[Serializable]
public struct Vec3
{
    public float X;
    public float Y;
    public float Z;
}

public class SClient
{
    public string Id;
    public ClientData Data;
    public GameObject Obj;

    public SClient(string id, ClientData data, GameObject obj)
    {
        Id = id;
        Data = data;
        Data.Id = id;
        Obj = obj;
    }

    public void Tick()
    {
        if (Obj.transform.position.y < -10)
        {
            Obj.transform.position = Vector3.up * 5;
        }

        Data.Position = new()
        {
            X = Obj.transform.position.x,
            Y = Obj.transform.position.y,
            Z = Obj.transform.position.z,
        };
    }
}