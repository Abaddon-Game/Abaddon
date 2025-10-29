using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Serializer : MonoBehaviour
{
    public static string Serialize(bool pretty)
    {
        List<ISaver> savers = GetAllSavers();

        return "{"
            + string.Join(
                ", " + (pretty ? "\n" : ""),
                savers.Select(saver =>
                {
                    return '"'
                        + ObjectLoader.IDToString(saver.GetID())
                        + "\": "
                        + saver.ToJson(pretty);
                })
            )
            + "}";
    }

    public static void Deserialize(string data)
    {
        var entries = ParseToStringDict(data);

        foreach (var kvp in entries)
        {
            ObjectLoader.Load(kvp.Key, kvp.Value);
        }
    }

    private static Dictionary<string, string> ParseToStringDict(string json)
    {
        // lowk shitty ass code but i don't mind
        var dict = new Dictionary<string, string>();
        foreach (var prop in JObject.Parse(json).Properties())
        {
            if (prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array)
                dict[prop.Name] = prop.Value.ToString(Formatting.None); // nested objects as JSON string
            else
                dict[prop.Name] = prop.Value.ToString(); // primitives
        }
        return dict;
    }

    static List<ISaver> GetAllSavers()
    {
        return new List<ISaver>(FindObjectsOfType<MonoBehaviour>().OfType<ISaver>());
    }
}

// this exists so its easy to find all the objects that wanna get saved
public interface ISaver
{
    public SaveableObjectID GetID();
    public string ToJson(bool pretty);
}

public enum SaveableObjectID
{
    Player,
    Inventory,
    Unknown,
}

// NOTE: whenever adding something new to be saved, ALWAYS EDIT THIS!!!
class ObjectLoader
{
    public static string IDToString(SaveableObjectID id)
    {
        switch (id)
        {
            case SaveableObjectID.Player:
                return "player";
            case SaveableObjectID.Inventory:
                return "inventory";
            case SaveableObjectID.Unknown:
            default:
                return "UNKNOWN_OBJECT";
        }
    }

    public static SaveableObjectID StringToID(string id)
    {
        switch (id)
        {
            case "player":
                return SaveableObjectID.Player;
            case "inventory":
                return SaveableObjectID.Inventory;
            default:
                return SaveableObjectID.Unknown;
        }
    }

    public static void Load(string id, string payload)
    {
        Load(StringToID(id), payload);
    }

    public static void Load(SaveableObjectID id, string payload)
    {
        switch (id)
        {
            case SaveableObjectID.Player:
            {
                Controller.main.LoadFromPayload(payload);
                return;
            }
            case SaveableObjectID.Inventory:
            {
                Controller.main.GetComponent<InvSaveAndLoad>().Load(payload);
                return;
            }
            default:
                return;
        }
    }
}
