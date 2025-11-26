using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class MongoPlayer
{
    public ObjectId Id { get; set; }
    public string username;
    public string password;
    public ObjectId inventoryID;

    public MongoPlayer() 
    {
        username = "moi";
        password = "";
    }

    public MongoPlayer(ObjectId id) 
    {
        username = "moi";
        password = "";
        inventoryID = id;
    }

    public MongoPlayer(string user, string pass, ObjectId id)
    {
        username = user;
        password = pass;
        inventoryID = id;
    }
}
