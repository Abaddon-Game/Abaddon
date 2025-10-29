//-----------------------------------------------\\
//			Ultimate Inventory Engine			 \\
// Copyright (c) N-Studios. All Rights Reserved. \\
//     https://nikichatv.com/N-Studios.html	     \\
//-----------------------------------------------\\

using UnityEngine;

[RequireComponent(typeof(Slot))]
public class EquipmentSlot : MonoBehaviour
{
    [Header("Type of Equipment")]
    [Tooltip(
        "Put the type of equipment that can be placed in this slot e.g., head, body, arms, etc. Then for all items that you want to be placeable in these slots make sure you have entered the matching string inside their equipment type box (head, body, arms, etc.)."
    )]
    public string equipmentType;

    [Header("Items Tab")]
    [Tooltip(
        "This is the item that is currently in the slot. If the box here is empty this means the slot is empty as well."
    )]
    public Item curItem;

    [Tooltip(
        "You need to drag and drop here every single object that may be equipped. You also need to make sure that you only reference objects that are suitable for this slot's equipment type."
    )]
    public EqippableItem[] possibleEqips;

    [SerializeField]
    public bool wasEquipped;

    [SerializeField]
    public int pastEquippedItemID;

    [HideInInspector]
    int searchID;
    private StatModifier stats;

    void Awake()
    {
        wasEquipped = isEquipped();
    }

    private void Update()
    {
        curItem = GetComponent<Slot>().slotsItem;
        if (wasEquipped && !isEquipped())
        {
            Unequip();
        }
        if (!wasEquipped && isEquipped())
        {
            Equip();
        }
        if (wasEquipped && isEquipped() && pastEquippedItemID != getCurrentEquippedItemID())
        {
            Unequip();
            Equip();
        }
        // if an error happens before here
        // e.g. in equip or unequip
        // it will not update the wasEquipped variable
        // and stats will update forever
        wasEquipped = isEquipped();
        pastEquippedItemID = getCurrentEquippedItemID();
    }

    public bool isEquipped()
    {
        return curItem != null;
    }

    public int getCurrentEquippedItemID()
    {
        if (!isEquipped())
        {
            return -1; // No item equipped
        }
        return curItem.ItemID;
    }

    public void _SetStats(StatModifier slotModifier)
    {
        stats = slotModifier;
    }

    public void Equip()
    {
        if (Controller.main == null)
            return;

        if (GetComponent<Slot>().slotsItem)
        {
            var item = GetComponent<Slot>().slotsItem;
            searchID = item.ItemID;
            if (item.TryGetComponent(out StatModifier slotModifier))
            {
                stats = slotModifier;
                Debug.Log($"Equipping item with stats: {stats}");
                Controller.main.UpdateConstitutionModifier(stats.constitution, true);
                Controller.main.UpdateDexterityModifier(stats.dexterity, true);
                Controller.main.UpdateStrengthModifier(stats.strength, true);
                Controller.main.UpdateWisdomModifier(stats.wisdom, true);
            }
            if (equipmentType == Pet.petString)
            {
                Pet.main.setPetItem(item, item.GetComponent<Animator>());
            }
            //for (int i = 0; i < possibleEqips.Length; i++)
            //{
            //if (possibleEqips[i].ItemID == searchID)
            //{
            //	assigned = possibleEqips[i];
            //	curItem = GetComponent<Slot>().slotsItem;
            //	possibleEqips[i].gameObject.SetActive(true);
            //}
            //}
            //if (assigned == null || !assigned.gameObject.activeSelf)
            //{
            //	Debug.LogError("The piece of equipment you are currently trying to equip wasn't found inside the \"Possible Eqips\" array!");
            //	return;
            //}
        }
    }

    public void Unequip()
    {
        if (Controller.main == null)
            return;

        // var item = GetComponent<Slot>().slotsItem;
        Debug.Log($"Unequipping item with stats: {stats}");
        if (stats == null)
        {
            Debug.Log("what the fuck");
        }
        Controller.main.UpdateConstitutionModifier(-stats.constitution, true);
        Controller.main.UpdateDexterityModifier(-stats.dexterity, true);
        Controller.main.UpdateStrengthModifier(-stats.strength, true);
        Controller.main.UpdateWisdomModifier(-stats.wisdom, true);
        // foreach (EqippableItem eqippableItem in possibleEqips)
        // {
        //     if (eqippableItem.ItemID == searchID)
        //     {
        //         eqippableItem.gameObject.SetActive(false);
        //         curItem = GetComponent<Slot>().slotsItem;
        //     }
        // }
        if (equipmentType == Pet.petString)
        {
            Pet.main.setPetItem(null, null);
        }
    }
}
