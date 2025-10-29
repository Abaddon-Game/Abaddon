using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAttack : MonoBehaviour
{
    [SerializeField]
    public uint damage = 1;

    [SerializeField]
    public EnemyMovement enemyMovement;

    [HideInInspector]
    public EnemySfx sfxPlayer;

    private void Awake()
    {
        sfxPlayer = GetComponent<EnemySfx>();
    }

    public virtual bool WillAttack(Vector2 position, Vector2 direction)
    {
        // we just check if the collider is the player, and if it is, we return true - direction is for if children of this need it
        RaycastHit2D[] hits = Physics2D.RaycastAll(position, direction, 1f);
        bool res = false;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                res = true;
            }
            else if (hit.collider.gameObject != this.gameObject)
            {
                res = false;
                return res;
            }
        }

        return res;
    }

    public virtual void Attack(Vector2 direction)
    {
        if (Controller.main == null)
            return;

        if (sfxPlayer.playAttackOnDamagePlayer)
        {
            sfxPlayer.PlayAttackSound();
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            transform.position,
            direction,
            1f,
            LayerMask.GetMask("Player")
        );
        if (hits.Length == 0)
        {
            return;
        }
        Controller.main.DamagePlayer(damage);
    }

    public virtual string ExtraSaveInfo()
    {
        return "{}";
    }

    public virtual void LoadSaveInfo(string payload)
    {
        return;
    }

    public virtual string GetAttackAnimationName()
    {
        return "attack";
    }
}
