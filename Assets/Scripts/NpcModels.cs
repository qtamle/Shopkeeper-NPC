using System;

// Data classes for persona.json and inventory.json. Field names must match the JSON keys exactly,
// because JsonUtility maps by name and silently leaves unmatched fields empty.

[Serializable]
public class Persona
{
    public string name;
    public string role;
    public string personality;
    public string speaking_style;
    public string[] rules;
}

[Serializable]
public class Inventory
{
    public string currency;
    public Item[] items;
}

[Serializable]
public class Item
{
    public string id;
    public string name;
    public string description;
    public int price;
    public int stock;
}
