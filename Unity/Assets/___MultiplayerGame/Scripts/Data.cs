using System;
using System.Collections.Generic;

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