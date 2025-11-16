using Unity.VisualScripting;
using UnityEngine;

public class WeepingEyeAttack : BaseAttack
{
    [Header("References")]
    [SerializeField]
    GameObject beamObject;

    public enum AttackStage
    {
        WindUp,
        Attack,
    }

    [HideInInspector]
    public Vector2 windUpDirection = Vector2.zero;

    [HideInInspector]
    public AttackStage attackStage = AttackStage.WindUp;

    public void Start()
    {
        Destroy(gameObject);
    }

    public override bool WillAttack(Vector2 position, Vector2 direction)
    {
        // we check in a line for the weeping eye, but it's longer than the one provided
        RaycastHit2D hit = Physics2D.Raycast(
            position,
            direction,
            3f, // we check 3 tiles in front of us
            LayerMask.GetMask("Player")
        );
        return hit.collider != null;
        // RaycastHit2D[] hits = Physics2D.RaycastAll(position, direction, 3f);
        // Debug.DrawRay(position, direction * 3f, Color.green, 1f);
        // bool res = false;
        // foreach (RaycastHit2D hit in hits)
        // {
        //     if (hit.collider == null)
        //     {
        //         continue;
        //     }

        //     if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
        //     {
        //         res = true;
        //     }
        //     else if (hit.collider.gameObject != this.gameObject)
        //     {
        //         res = false;
        //         return res;
        //     }
        // }
        // // mask only returns player
        // return hits.Length > 0;
    }

    public override void Attack(Vector2 direction)
    {
        if (Controller.main == null)
            return;

        switch (attackStage)
        {
            case AttackStage.WindUp:
                enemyMovement.forceAttackNextTurn = true;
                attackStage = AttackStage.Attack;
                windUpDirection = direction;
                break;
            case AttackStage.Attack:
                // create beam no matter what
                if (WillAttack(transform.position, windUpDirection))
                {
                    // only actually damage player if we hit them
                    Debug.Log("Weeping Eye hit player");
                    Controller.main.DamagePlayer(damage);
                }
                enemyMovement.forceAttackNextTurn = false;
                attackStage = AttackStage.WindUp;
                break;
        }

        if (sfxPlayer.playAttackOnDamagePlayer)
        {
            sfxPlayer.PlayAttackSound(attackStage == AttackStage.WindUp ? 1 : 0);
        }
    }

    public void CreateBeam(Vector2 position, Vector2 direction)
    {
        // create the beam
        GameObject beam = Instantiate(beamObject, position, Quaternion.identity);
        GameObject beamChild = beam.transform.GetChild(0).gameObject;
        if (beamChild.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.sortingOrder =
                this.transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder - 1;
        }
        else
        {
            Debug.LogError("Beam child does not have a SpriteRenderer component.");
        }
        beam.transform.rotation = Quaternion.Euler(
            0,
            0,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f
        );
        // todo set rotation
    }

    public override string GetAttackAnimationName()
    {
        switch (attackStage)
        {
            case AttackStage.WindUp:
                return "winduptransition";
            case AttackStage.Attack:
                return "attack";
            default:
                return "idle";
        }
    }
}
