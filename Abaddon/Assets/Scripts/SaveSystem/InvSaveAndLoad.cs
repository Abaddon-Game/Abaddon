///----------------------------\\\
//  Ultimate Inventory Engine   \\
// Copyright (c) N-Studios. All Rights Reserved. \\
//      https://nikichatv.com/N-Studios.html	  \\
///-----------------------------------------------\\\
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class InvSaveAndLoad : MonoBehaviour, ISaver
{
    [SerializeField]
    ItemFinder itemFinder;

    Inventory inv;

    bool startRan = false;

    private void Awake()
    {
        startRan = true;
        inv = GetComponent<Inventory>();
    }

    public SaveableObjectID GetID()
    {
        return SaveableObjectID.Inventory;
    }

    public string ToJson(bool pretty)
    {
        Debug.Log("hiiiii");

        int items = 0;
        for (int i = 0; i < inv.slots.Length; i++)
            if (inv.slots[i].slotsItem)
                items++;
        for (int i = 0; i < inv.equipSlots.Length; i++)
            if (inv.equipSlots[i].slotsItem)
                items++;
        List<Slot> allSlots = new List<Slot>();

        foreach (Slot slot in inv.slots)
            allSlots.Add(slot);
        foreach (Slot slot in inv.equipSlots)
            allSlots.Add(slot);

        Slot[] readySlots = allSlots.ToArray();

        List<ItemLoad> itemsToLoad = new List<ItemLoad>();
        if (items >= 1)
            for (int i = 0; i < readySlots.Length; i++)
            {
                Slot z = readySlots[i];
                if (z.slotsItem)
                {
                    ItemLoad h = new(z.slotsItem.ItemID, z.slotsItem.amountInStack, i);
                    itemsToLoad.Add(h);
                }
            }
        return CustomJSON.ToJson(itemsToLoad);
    }

    public void Load(string payload)
    {
        if (!startRan)
        {
            Debug.LogError("loading save file before start run");
            return;
        }

        List<ItemLoad> itemsToLoad = CustomJSON.FromJson<ItemLoad>(payload);

        List<Slot> allSlots = new List<Slot>();

        Debug.Log(startRan);
        foreach (Slot slot in inv.slots)
            allSlots.Add(slot);
        foreach (Slot slot in inv.equipSlots)
            allSlots.Add(slot);

        Slot[] readySlots = allSlots.ToArray();

        if (itemsToLoad != null)
        {
            if (itemsToLoad.Count > 0)
            {
                // i find the mixing of snake_case and dogshitCase hilarious and will continue to use it
                foreach (ItemLoad item_load in itemsToLoad)
                {
                    Item item = Instantiate(
                            itemFinder.GetItemByID(item_load.id),
                            allSlots[item_load.slotIndex].transform
                        )
                        .GetComponent<Item>();
                    item.amountInStack = item_load.amount;

                    if (
                        allSlots[item_load.slotIndex]
                            .transform.TryGetComponent<EquipmentSlot>(out EquipmentSlot equip_slot)
                    )
                    {
                        equip_slot.curItem = item;
                        if (item.TryGetComponent(out StatModifier slotModifier))
                        {
                            equip_slot._SetStats(slotModifier);

                            // csharpier hates me
                            Controller.main.UpdateConstitutionModifier(
                                slotModifier.constitution,
                                false
                            );
                            Controller.main.UpdateDexterityModifier(slotModifier.dexterity, false);
                            Controller.main.UpdateStrengthModifier(slotModifier.strength, false);
                            Controller.main.UpdateWisdomModifier(slotModifier.wisdom, false);
                        }
                        equip_slot.pastEquippedItemID = item_load.id;
                    }

                    break;
                }
                StartCoroutine(Refresh(0.0000001f));
            }
        }
    }

    IEnumerator Refresh(float time)
    {
        inv.inventoryObject.gameObject.SetActive(true);
        yield return new WaitForSeconds(time);
        inv.inventoryObject.gameObject.SetActive(false);
    }

    public void ResetSaveFiles()
    {
        List<ItemLoad> itemsToLoad = new List<ItemLoad>();

        string json = CustomJSON.ToJson(itemsToLoad);
        File.WriteAllText(Application.persistentDataPath + transform.name, json);
    }
}

[System.Serializable]
public class ItemLoad
{
    public int id,
        amount,
        slotIndex;

    public ItemLoad(int ID, int AMOUNT, int SLOTINDEX)
    {
        id = ID;
        amount = AMOUNT;
        slotIndex = SLOTINDEX;
    }
}
