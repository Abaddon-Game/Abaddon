using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this is so stats actually update even when the inventory is open (controller is disabled)

[RequireComponent(typeof(Controller))]
public class StatUpdater : MonoBehaviour
{
    public enum Stat
    {
        Wisdom,
        Strength,
        Dexterity,
        Constitution,
    }

    public void AttemptToBuy(Stat stat, int xp_cost, int amount)
    {
        if (Controller.main == null)
            return;

        if (Controller.main.exp >= xp_cost)
        {
            Controller.main.exp -= xp_cost;
            AddToStat(stat, amount);
        }
    }

    public void AddToStat(Stat stat, int amount)
    {
        if (Controller.main == null)
            return;

        switch (stat)
        {
            case Stat.Wisdom:
                Controller.main.UpdateWisdomModifier(amount, true);
                break;
            case Stat.Strength:
                Controller.main.UpdateStrengthModifier(amount, true);
                break;
            case Stat.Dexterity:
                Controller.main.UpdateDexterityModifier(amount, true);
                break;
            case Stat.Constitution:
                Controller.main.UpdateConstitutionModifier(amount, true);
                break;
        }
    }

    public void IncreaseConstitution(int cost)
    {
        AttemptToBuy(Stat.Constitution, cost, 1);
    }

    public void IncreaseDexterity(int cost)
    {
        AttemptToBuy(Stat.Dexterity, cost, 1);
    }

    public void IncreaseStrength(int cost)
    {
        AttemptToBuy(Stat.Strength, cost, 1);
    }
}
