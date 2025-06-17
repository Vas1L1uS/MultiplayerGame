using UnityEngine;

public class Client
{
    public string Id;
    public ClientData Data;
    public GameObject Obj;

    public Client(string id, ClientData data, GameObject obj)
    {
        Id = id;
        Data = data;
        Data.Id = id;
        Obj = obj;
    }

    public void Tick()
    {
        Data.Position = new()
        {
            X = Obj.transform.position.x,
            Y = Obj.transform.position.y,
            Z = Obj.transform.position.z,
        };
    }
}