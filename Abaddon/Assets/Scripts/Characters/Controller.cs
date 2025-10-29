using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[
    RequireComponent(typeof(PlayerSfx)),
    RequireComponent(typeof(Inventory)),
    RequireComponent(typeof(BoxCollider2D))
]
public class Controller : MonoBehaviour, ISaver
{
    #region Variables
    public static Controller main;
    public DashTrailPrefab dashTrailPrefab;

    #region Stats
    [Header("Base Stats")]
    [SerializeField, Tooltip("Constitution (maximum health)")]
    public int constitution = 10;

    [SerializeField, Tooltip("Dexterity (dodge chance)")]
    public int dexterity = 10;

    [SerializeField, Tooltip("Strength (attack damage)")]
    public int strength = 10;

    [SerializeField, Tooltip("Wisdom (ability damage)")]
    public int wisdom = 10;

    [SerializeField, Tooltip("Minimum stat roll")]
    public int minimum_stat_roll = 1;

    [SerializeField, Tooltip("Maximum stat roll")]
    public int maximum_stat_roll = 7;

    [SerializeField, Tooltip("Sum of starting stats")]
    public int sum_of_starting_stats = 40;

    [Space]
    [SerializeField]
    public int conModifier;

    [SerializeField]
    public int dexModifier;

    [SerializeField]
    public int strModifier;

    [SerializeField]
    public int wisModifier;

    [Space(5)]
    [SerializeField]
    private bool randomizeStats = false;

    [Space(10)]
    [HideInInspector]
    public int exp = 0;
    private int ticksUntilDash = 0;

    [Space]
    [SerializeField]
    private int ticksBetweenDashes;

    [SerializeField]
    Slider expBarVisual;

    [Space]
    #endregion

    #region Health
    [Header("Health")]
    [SerializeField]
    Slider healthBarVisual;

    [SerializeField]
    Transform respawnPoint;

    [Space]
    [HideInInspector]
    public Action onDie;

    [HideInInspector]
    public int max_health;
    public int health
    {
        get => _health;
        private set
        {
            _health = value;
            _health = Math.Clamp(_health, 0, max_health);
            if (healthBarVisual)
                healthBarVisual.value = MathF.Round(health / (float)max_health * 113f);
            if (_health <= 0)
            {
                onDie?.Invoke();
            }
        }
    }
    int _health;

    #endregion

    #region Movement
    [Header("Movement")]
    [SerializeField]
    LayerMask collideLayers;

    [Space]
    Vector2 current_player_direction = new Vector2(0, -1);

    float turnEndStart = Mathf.Infinity;
    #endregion

    #region Player Update System
    [HideInInspector]
    public static Action OnTick;

    [HideInInspector]
    public static Action OnMoved;

    [HideInInspector]
    public EnemyMovement[] enemies;

    [HideInInspector]
    public bool done_with_tick = true;
    int current_enemy = 0;
    #endregion

    #region Other
    [Header("Other")]
    [SerializeField]
    private TextMeshProUGUI goldIndicator;

    [SerializeField]
    public Animator animator;

    [SerializeField]
    CameraScript mainCamera;

    [SerializeField]
    GameObject tombstonePrefab;

    [SerializeField]
    Item baseRespawnSword;

    [SerializeField]
    public GameObject UIObject;

    [SerializeField]
    public GameObject textFadePrefab;

    [SerializeField]
    SpriteRenderer sprite;

    [SerializeField]
    public Vector3 damageNumberOffset = Vector3.up * 0.5f;

    [HideInInspector]
    public PlayerSfx sfxPlayer;

    [HideInInspector]
    public Inventory inventory;

    #endregion

    #region Constants
    const int KeyID = 1;
    #endregion

    #region Local Things
    private bool god_mode = false;
    private int _goldCount = 0;
    public int goldCount
    {
        get => _goldCount;
        set
        {
            goldIndicator.text = $"{value}";
            _goldCount = value;
        }
    }
    private bool god_mode_keys_prev = false;
    #endregion

    #region Movement Controls
    private class MoveState
    {
        public float start_hold_time = 0f;
        public bool held_last_frame = false;
        public float next_repeat_run = 0f;
    }

    private enum MovementDirection
    {
        Up,
        Down,
        Left,
        Right,
    }

    private Dictionary<MovementDirection, MoveState> movementStates =
        new Dictionary<MovementDirection, MoveState>();

    [SerializeField]
    float initialMovementDelay = 0.3f;

    [SerializeField]
    float holdDelay = 0.03f;

    #endregion

    #region Quests

    public enum Quest
    {
        Kill15Gnomes,
        SaveEmoBoy,
    }

    public class QuestState
    {
        public int Kill15Gnomes;

        public bool EmoBoySaved;
    }

    private QuestState quest_state;

    [HideInInspector]
    public List<Quest> current_quests;

    [HideInInspector]
    public List<Quest> completed_quests;

    [HideInInspector]
    public bool hasMoved = false;

    private struct MovementKeys
    {
        public bool up;
        public bool down;
        public bool left;
        public bool right;
        public bool rotate_x;
        public bool rotate_y;
    }

    private MovementKeys movementKeys;

    [HideInInspector]
    public bool hasRotated = false;

    [HideInInspector]
    public bool hasAttacked = false;

    [HideInInspector]
    public bool hasOpenedInventory = false;

    [HideInInspector]
    public bool hasPickedUp = false;

    #endregion

    #endregion

    void Awake()
    {
        main = this;
        onDie += () => UIStateManager.singleton.OpenUIPage(UIState.Death);
        OnMoved += () => ticksUntilDash--;
        onDie += () =>
        {
            var tombstone = Instantiate(
                    tombstonePrefab,
                    new Vector3(transform.position.x, transform.position.y, -5),
                    Quaternion.identity
                )
                .GetComponent<PlayerTombstone>();
            var unioned = inventory.slots.Union(inventory.equipSlots);
            Item[] items = unioned
                .Where(slot => slot.slotsItem != null)
                .Select(slot => slot.slotsItem)
                .ToArray();
            tombstone.SetItems(items);
            inventory.ClearInventory();
            Camera.main.GetComponent<CameraScript>().ResetTarget(0.5f, useSmoothMovement: false);
            Camera.main.GetComponent<CameraScript>().UpdateFOV(5f, 1f);
            print("updating FOV to 5f");
        };

        sfxPlayer = GetComponent<PlayerSfx>();
        inventory = FindObjectOfType<Inventory>();

        #region Generate Stats

        if (randomizeStats)
        {
            float minPercentage = (float)minimum_stat_roll / sum_of_starting_stats;
            float maxPercentage = (float)maximum_stat_roll / sum_of_starting_stats;

            float[] newStats = new float[4];
            for (int i = 0; i < newStats.Length; i++)
            {
                newStats[i] = UnityEngine.Random.Range(minPercentage, maxPercentage);
            }

            float total = newStats.Sum();
            for (int i = 0; i < newStats.Length; i++)
            {
                newStats[i] = newStats[i] / total;
            }

            var results = newStats
                .Select(stat => Mathf.RoundToInt(stat * sum_of_starting_stats))
                .ToArray();

            int diff = results.Sum() - sum_of_starting_stats;
            if (diff != 0)
                results[Array.IndexOf(results, diff < 0 ? results.Min() : results.Max())] -= diff; // If the rounding messed up, adjust the highest or lowest stat accordingly

            strength = results[0];
            dexterity = results[1];
            constitution = results[2];
            wisdom = results[3];
        }
        #endregion

        max_health = constitution * 2;
        health = max_health;

        if (expBarVisual)
        {
            expBarVisual.maxValue = 100;
            expBarVisual.minValue = 0;
            expBarVisual.value = exp;
        }

        if (respawnPoint == null)
        {
            respawnPoint = Instantiate(transform, transform.position, Quaternion.identity);
        }

        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        //Transform targetPosition = boss.transform;

        mainCamera.ResetTarget(null, false, null);

        // disable player movement until the camera has panned
        // done_with_tick = false;
        // StartCoroutine(AfterDelay(1f, () =>
        //     mainCamera.ChangeTarget(targetPosition, 0.5f, onComplete: () =>
        //     {
        //         mainCamera.ResetTarget(0.5f, onComplete: () =>
        //         {
        //             print("Camera reset complete");
        //             done_with_tick = true;
        //         });
        //     })
        // ));

        quest_state = new QuestState();
    }

    void Update()
    {
        if (GetCurrentAnimation() != currentAnimation)
        {
            animator.Play(currentAnimation);
        }

        if (!done_with_tick)
        {
            return;
        }

        UpdateHealthBar();
        if (inventory != null && inventory.enabled != true)
        {
            UIFloatText();
        }

        bool god_mode_keys_down = Input.GetKey(KeyCode.F) && Input.GetKey(KeyCode.J);

        if (god_mode_keys_down && !god_mode_keys_prev)
        {
            if (god_mode)
            {
                print("God mode deactivated");
                god_mode = false;
                // round to nearest tile
                transform.position = new Vector3(
                    Mathf.Round(transform.position.x - 0.5f) + 0.5f,
                    Mathf.Round(transform.position.y - 0.5f) + 0.5f,
                    transform.position.z
                );
            }
            else
            {
                god_mode = true;
            }
        }
        god_mode_keys_prev = god_mode_keys_down;

        // rotation buttons
        // TODO make more advanced, look at other games that do this
        if (Input.GetKeyDown(SettingsMenu.singleton.rotateLeftKeybind.key))
        {
            SetMovementKeys(Vector2.zero);
            current_player_direction = new Vector2(
                -current_player_direction.y,
                current_player_direction.x
            ); // rotate left
            PlayAnimation("Idle", current_player_direction);
        }
        if (Input.GetKeyDown(SettingsMenu.singleton.rotateRightKeybind.key))
        {
            SetMovementKeys(Vector2.zero);
            current_player_direction = new Vector2(
                current_player_direction.y,
                -current_player_direction.x
            ); // rotate right
            PlayAnimation("Idle", current_player_direction);
        }

        enemies = FindObjectsOfType<EnemyMovement>();

        if (god_mode)
        {
            GodModeMove();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            print("Space pressed, dashing");
            if (CanMove(SendLongRaycast(current_player_direction)) && ticksUntilDash <= 0)
            {
                transform.Translate(current_player_direction * 2);
                SpawnDashTrail(current_player_direction);
                sfxPlayer.PlayDashSound();
                OnMoved?.Invoke();
                ticksUntilDash = ticksBetweenDashes;
                FinishTick();
            }
            else
            {
                print("Couldn't dash, something in the way or not enough ticks");
            }
        }

        if (Input.GetKeyDown(SettingsMenu.singleton.interactKeybind.key))
        {
#nullable enable
            GenericNPC? npc = CanStartConversation();
#nullable disable

            if (npc != null)
            {
                npc.StartConversation();
                return;
            }
        }

        Move();

        if (Input.GetKeyDown(KeyCode.J) && Input.GetKey(KeyCode.LeftShift))
            health += 1;
        if (Input.GetKeyDown(KeyCode.K) && Input.GetKey(KeyCode.LeftShift))
            DamagePlayer(10, false);
    }

    void SpawnDashTrail(Vector2 direction)
    {
        direction = direction.normalized;
        Vector3 start = transform.position - (Vector3)direction * 2f;
        Vector3 end = transform.position;
        int numTrails = 3;
        for (int i = 0; i < numTrails; i++)
        {
            float offset = 1 / (float)(numTrails + 1f);
            float t = offset + (i / (float)(numTrails + 1f));
            Vector3 position = Vector3.Lerp(start, end, t);
            Vector2 noise = UnityEngine.Random.insideUnitCircle * 0.1f;
            position += (Vector3)noise;
            var dashTrail = Instantiate(dashTrailPrefab, position, Quaternion.identity);
            dashTrail.GetComponent<SpriteRenderer>().sprite = transform
                .GetChild(0)
                .GetComponent<SpriteRenderer>()
                .sprite;
            dashTrail.transform.position = position;
            dashTrail.direction = direction;
            dashTrail.scaler = 1f;
            dashTrail.lifetime = 0.5f;
        }
    }

    void GodModeMove()
    {
        Vector2 direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        transform.Translate(direction * Time.deltaTime * 60f * .75f);
    }

    bool CanMove(GameObject[] objectsAhead)
    {
        return !objectsAhead.Any(obj => (collideLayers.value & (1 << obj.layer)) != 0);
    }

    bool playerPressingButtons()
    {
        return Input.GetKeyDown(SettingsMenu.singleton.interactKeybind.key)
            || Input.GetKey(SettingsMenu.singleton.attackKeybind.key);
    }

    void CheckMK()
    {
        if (movementKeys.up && movementKeys.down && movementKeys.left && movementKeys.right)
        {
            hasMoved = true;
        }
        if (movementKeys.rotate_x && movementKeys.rotate_y)
        {
            hasRotated = true;
        }
    }

    void SetMovementKeys(Vector2 dir)
    {
        if (dir == Vector2.up)
        {
            movementKeys.up = true;
            CheckMK();
        }
        if (dir == Vector2.down)
        {
            movementKeys.down = true;
            CheckMK();
        }
        if (dir == Vector2.left)
        {
            movementKeys.left = true;
            CheckMK();
        }
        if (dir == Vector2.right)
        {
            movementKeys.right = true;
            CheckMK();
        }
        if (Input.GetKey(SettingsMenu.singleton.rotateLeftKeybind.key))
        {
            movementKeys.rotate_y = true;
            CheckMK();
        }
        if (Input.GetKey(SettingsMenu.singleton.rotateRightKeybind.key))
        {
            movementKeys.rotate_x = true;
            CheckMK();
        }
    }

    void Move()
    {
        // Debug.Log("Starting player movement");
        Vector2 direction = GetAxis();
        bool stickMoved = direction.magnitude != 0;
        if (!stickMoved && !playerPressingButtons())
            return; // if the player is not pressing a movement key, do nothing
        if (stickMoved)
        {
            current_player_direction = direction;
        }
        done_with_tick = false;
        GameObject[] objectsAhead = SendRaycast(current_player_direction);
        PlayAnimation("Idle", direction);

        bool did_something = false;
        if (stickMoved)
        {
            // If the player is trying to move and can, move them
            if (CanMove(objectsAhead))
            {
                SetMovementKeys(direction);
                transform.Translate(direction);
                sfxPlayer.PlayWalkSound();
                OnMoved?.Invoke();
                FinishTick();
                did_something = true;
            }
            // If the player is trying to move but can't, cancel the turn
            else
            {
                done_with_tick = true;
                return;
            }
        }

        if (Input.GetKeyDown(SettingsMenu.singleton.interactKeybind.key))
        {
#nullable enable
            CanBeInteractedWith? interactable = FindInteractable(objectsAhead);
#nullable disable
            if (interactable != null)
            {
                interactable.Interact();
                FinishTick();
                // TODO: do animation + sfx. maybe do that in the interactable's interact function? since we might want different sfx depending on the thing
                did_something = true;
            }
            else
            {
                done_with_tick = true;
                return;
            }
        }
        if (Input.GetKey(SettingsMenu.singleton.attackKeybind.key))
        {
            if (currentAnimation.Contains("Attack") || done_with_tick)
            {
                return;
            }
            // Debug.Log("V pressed, checking for enemies to attack");
            var weapon = Weapon.GetCurrentWeapon();
            CanBeDamaged[] enemies = weapon.GetFightablesInDamageArea(
                transform.position,
                current_player_direction
            );
            bool attackWorked = weapon.AttackEnemies(enemies, current_player_direction);
            if (attackWorked)
                hasAttacked = true;
            sfxPlayer.PlayAttackSound();
            var anim = weapon.AnimationName + "Attack";
            PlayAnimation(anim, current_player_direction);
            // animator.Play("Hammer Attack left");
            did_something = true;
        }

        if (Input.GetKeyDown(KeyCode.L))
            animator.Play("Hammer Attack left");

        if (!did_something)
            FinishTick();
    }

    public void UpdateHealthBar()
    {
        double health_percentage = (double)health / max_health;
        max_health = (constitution + conModifier) * 2;
        health = (int)(max_health * health_percentage);
        HealPlayer(0);
    }

    public void UpdateConstitutionModifier(int conDiff, bool addText)
    {
        if (conDiff == 0)
            return;
        conModifier += conDiff;
        TextTypes type = conDiff < 0 ? TextTypes.StatLoss : TextTypes.StatGain;
        if (addText)
            AddTextToQueue($"{conDiff} CON", type);
    }

    public void UpdateDexterityModifier(int dexDiff, bool addText)
    {
        if (dexDiff == 0)
            return;
        dexModifier += dexDiff;
        TextTypes type = dexDiff < 0 ? TextTypes.StatLoss : TextTypes.StatGain;
        if (addText)
            AddTextToQueue($"{dexDiff} DEX", type);
        // No need to update anything else, since dexterity is only used for dodge chance
    }

    public void UpdateStrengthModifier(int strDiff, bool addText)
    {
        if (strDiff == 0)
            return;
        strModifier += strDiff;
        TextTypes type = strDiff < 0 ? TextTypes.StatLoss : TextTypes.StatGain;
        if (addText)
            AddTextToQueue($"{strDiff} STR", type);
        // No need to update anything else, since strength is only used for damage
    }

    public void UpdateWisdomModifier(int wisDiff, bool addText)
    {
        if (wisDiff == 0)
            return;
        wisModifier += wisDiff;
        TextTypes type = wisDiff < 0 ? TextTypes.StatLoss : TextTypes.StatGain;
        if (addText)
            AddTextToQueue($"{wisDiff} WIS", type);
        // No need to update anything else, since wisdom is only used for ability damage
    }

    [Serializable]
    public enum TextTypes
    {
        StatLoss,
        StatGain,
        Other,
    }

    [Serializable]
    public readonly struct TextPopupData
    {
        public TextPopupData(string text, TextTypes type)
        {
            this.text = text;
            this.type = type;
        }

        public string text { get; }
        public TextTypes type { get; }
    }

    #region Text Popup Stuff
    [SerializeField]
    [Header("Text Popup")]
    public List<TextPopupData> textQueue = new List<TextPopupData>();
#nullable enable
    private UITextFadeUp? lastTextFadeUp;
#nullable disable
    #endregion

    public void AddTextToQueue(string text, TextTypes type)
    {
        if (textQueue.Count > 0 && textQueue[textQueue.Count - 1].text == text)
            return; // don't add the same text twice in a row
        textQueue.Add(new TextPopupData(text, type));
    }

    public void UIFloatText()
    {
        if (textQueue.Count == 0)
            return;
        if (lastTextFadeUp != null)
        {
            if (
                lastTextFadeUp.transform.localPosition.y
                < lastTextFadeUp.textObject.mesh.bounds.size.y * 1.1f
            )
            {
                return;
            }
        }
        TextPopupData popupData = textQueue[0];
        if (popupData.text == null || popupData.text == "")
        {
            textQueue.RemoveAt(0);
            return;
        }

        string popupText = popupData.text;

        Color color = Color.red;
        bool failedToParse = false;
        switch (popupData.type)
        {
            case TextTypes.StatLoss:
                failedToParse = !UnityEngine.ColorUtility.TryParseHtmlString("#f54929", out color);
                // popupText = $"-{popupText}";
                break;
            case TextTypes.StatGain:
                failedToParse = !UnityEngine.ColorUtility.TryParseHtmlString("#c8c8e0", out color);
                popupText = $"+{popupText}";
                break;
            case TextTypes.Other:
                failedToParse = !UnityEngine.ColorUtility.TryParseHtmlString("#ff1515", out color);
                break;
        }
        if (failedToParse)
        {
            Debug.LogError($"Failed to parse color for text: {popupData.text}");
            color = Color.red; // fallback color
        }
        UITextFadeUp floatText = Instantiate(textFadePrefab, UIObject.transform)
            .GetComponent<UITextFadeUp>();
        floatText.transform.localPosition = new Vector3(-350, 0, 50);
        floatText.gameObject.layer = LayerMask.NameToLayer("Walls");
        floatText.SetText(popupText, color, Color.white, 0.4f);
        floatText.SetFontSize(24);
        lastTextFadeUp = floatText;
        textQueue.RemoveAt(0);
    }

    public int GetDamageModifier()
    {
        return (strength + strModifier - 10) / 2;
    }

    public void FinishTick()
    {
        Debug.Log("Tick finished");
        current_enemy = 0;
        turnEndStart = Time.time;
        OnTick?.Invoke();
        NextEnemy();
    }

    public void NextEnemy()
    {
        if (current_enemy >= enemies.Length)
        {
            done_with_tick = true;
            return;
        }
        current_enemy++;
        // Debug.Log(
        //     $"Current enemy: {current_enemy} Name: {enemies[current_enemy - 1]?.name} Position: {enemies[current_enemy - 1]?.transform.position}"
        // );
        if (enemies[current_enemy - 1] == null)
        {
            NextEnemy();
            return;
        }
        enemies[current_enemy - 1].MakeDecision();
    }

    public void KilledEnemy(EnemyType enemy)
    {
        switch (enemy)
        {
            case EnemyType.Gnome:
            {
                if (current_quests.Contains(Quest.Kill15Gnomes))
                {
                    quest_state.Kill15Gnomes += 1;
                    if (quest_state.Kill15Gnomes >= 15)
                    {
                        current_quests.Remove(Quest.Kill15Gnomes);
                        completed_quests.Add(Quest.Kill15Gnomes);
                    }
                }
                break;
            }
            default:
                break;
        }
    }

    private bool ShouldMove(MovementDirection dir, bool isHeld)
    {
        if (!movementStates.ContainsKey(dir))
            movementStates[dir] = new MoveState();

        MoveState state = movementStates[dir];

        if (isHeld)
        {
            if (!state.held_last_frame)
            {
                state.held_last_frame = true;
                state.start_hold_time = Time.time + initialMovementDelay;
                return true;
            }
            else if (state.start_hold_time <= Time.time)
            {
                //print(state.next_repeat_run + " " + holdDelay);
                if (state.next_repeat_run <= Time.time)
                {
                    state.next_repeat_run = Time.time + holdDelay;
                    return true;
                }
            }
        }
        else
        {
            state.held_last_frame = false;
            state.start_hold_time = float.PositiveInfinity;
        }

        return false;
    }

    Vector2 GetAxis()
    {
        Func<bool, int> BoolToInt = boolValue => boolValue ? 1 : 0;

        // todo: allow people to rebind movement keys

        bool up = Input.GetKey(SettingsMenu.singleton.moveUpwardKeybind.key);
        bool down = Input.GetKey(SettingsMenu.singleton.moveDownwardKeybind.key);
        bool left = Input.GetKey(SettingsMenu.singleton.moveLeftKeybind.key);
        bool right = Input.GetKey(SettingsMenu.singleton.moveRightKeybind.key);

        up = ShouldMove(MovementDirection.Up, up);
        down = ShouldMove(MovementDirection.Down, down);
        left = ShouldMove(MovementDirection.Left, left);
        right = ShouldMove(MovementDirection.Right, right);

        int i_up = BoolToInt(up);
        int i_down = BoolToInt(down);
        int i_left = BoolToInt(left);
        int i_right = BoolToInt(right);

        float horizontal = i_right - i_left;
        float vertical = i_up - i_down;

        return new Vector2(horizontal, vertical);
    }

    // (if it hit something, what it hit)
    GameObject[] SendRaycast(Vector2 direction)
    {
        Vector2 world_position = (Vector2)transform.position + (direction * .51f); // convert direction to relative position (ex: up = (0, 1)), then add it to the current position
        Debug.DrawRay(world_position, direction, Color.green, 0.2f, false);
        return Physics2D
            .RaycastAll(world_position, direction, .5f)
            .Select(hit => hit.collider.gameObject)
            .ToArray();
    }

    GameObject[] SendLongRaycast(Vector2 direction)
    {
        Vector2 world_position = (Vector2)transform.position + (direction * .51f); // convert direction to relative position (ex: up = (0, 1)), then add it to the current position
        Debug.DrawRay(world_position, direction, Color.green, 0.2f, false);
        return Physics2D
            .RaycastAll(world_position, direction, 1.5f)
            .Select(hit => hit.collider.gameObject)
            .ToArray();
    }

    string DirectionToAnimationLabel(Vector2 direction)
    {
        if (direction == Vector2.down)
        {
            return "Down";
        }
        else if (direction == Vector2.up)
        {
            return "Up";
        }
        else if (
            direction == Vector2.left
            || direction == new Vector2(-1, -1)
            || // diagonal left down
            direction == new Vector2(-1, 1)
        ) // diagonal left up
        {
            return "Left";
        }
        else if (
            direction == Vector2.right
            || direction == new Vector2(1, -1)
            || // diagonal right down
            direction == new Vector2(1, 1)
        ) // diagonal right up
        {
            return "Right";
        }

        throw new Exception("Invalid direction " + direction);
    }

    public string currentAnimation = "";

    public void PlayAnimation(string action, Vector2? facingDirection = null)
    {
        if (facingDirection == null || facingDirection == Vector2.zero)
            facingDirection = current_player_direction;

        if (action == "Hurt")
        {
            int color = 0xf54929;
            int blue = color & 0xff;
            int green = color & 0xff00 >> 0b10000;
            int red = (color & 0xff0000) >> 0b10000;
            sprite.color = new Color(red / 255f, green / 255f, blue / 255f);
        }
        else
        {
            sprite.color = Color.white;
        }

        string animation = $"Player{DirectionToAnimationLabel((Vector2)facingDirection)}{action}"; // Changing this line
        currentAnimation = animation;
        // print($"Playing animation: {animation}");
        animator.Play(animation);
    }

    public string GetCurrentAnimation()
    {
        // https://discussions.unity.com/t/how-to-get-current-animation-name/69393/5
        return animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
    }

    public void DamagePlayer(uint damage, bool dodgeable = true)
    {
        string text = "";
        if (dodgeable && DodgedAttack())
        {
            sfxPlayer.PlayDodgeSound();
            text = "dodged";
        }
        else
        {
            health -= (int)damage;
            sfxPlayer.PlayHurtSound();
            PlayAnimation("Hurt");
            text = damage.ToString();
        }
        Helpers.singleton.SpawnHurtText(text, transform.position + damageNumberOffset);
    }

    bool DodgedAttack()
    {
        return UnityEngine.Random.value < DexterityToPercent();
    }

    float DexterityToPercent()
    {
        return (dexterity + dexModifier - 8) / 20f;
    }

    public void Respawn()
    {
        health = max_health;
        transform.position = respawnPoint.position;
        UIStateManager.singleton.CloseUIPage(UIState.Death);
        var newSword = Instantiate(baseRespawnSword);
        inventory.AddItemAtIndex(newSword, 3, isEquipmentSlot: true);
    }

    // returns overflow health
    public int HealPlayer(int heal)
    {
        int overflowHealth = health + heal - max_health;
        health += heal;
        return overflowHealth;
    }

    public void AttackAnimationFinishHandler()
    {
        print("Attack animation finished, resetting sorting layer");
        animator.GetComponent<Renderer>().sortingLayerID = SortingLayer.NameToID("Characters");
        FinishTick();
        PlayAnimation("Idle", current_player_direction);
    }

    public void add_exp(int exp)
    {
        this.exp += exp;
    }

#nullable enable
    private GenericNPC? CanStartConversation()
    {
        // do initial check for nearness
        Collider2D npc = Physics2D
            .Raycast(transform.position, current_player_direction, 1f, collideLayers)
            .collider;
        if (npc == null)
            return null;
        return npc.GetComponent<GenericNPC>();
    }

#nullable disable

#nullable enable
    private CanBeInteractedWith? FindInteractable(GameObject[] objectsAhead)
    {
        foreach (GameObject obj in objectsAhead)
        {
            if (obj.TryGetComponent(out CanBeInteractedWith interactable))
                return interactable;
        }
        return null;
    }

#nullable disable

    public bool ShouldShowInteractionButton()
    {
        bool can_talk_to_npc = CanStartConversation() != null;

        GameObject[] objectsAhead = SendRaycast(current_player_direction);
        bool interactable_ahead = FindInteractable(objectsAhead) != null;

        return can_talk_to_npc || interactable_ahead;
    }

    public QuestState GetQuestState()
    {
        return quest_state;
    }

    public SaveableObjectID GetID()
    {
        return SaveableObjectID.Player;
    }

    public string ToJson(bool pretty)
    {
        PlayerJsonData data = new()
        {
            Position = transform.position,
            Direction = current_player_direction,
            Health = health,
            GoldCount = goldCount,
            Strength = strength,
            Wisdom = wisdom,
            Constitution = constitution,
            Dexterity = dexterity,
        };

        Debug.Log(data.Direction);

        var o = JsonUtility.ToJson(data, pretty);

        Debug.Log(o);

        return o;
    }

    public void LoadFromPayload(string payload)
    {
        Debug.Log(payload);
        PlayerJsonData data = JsonUtility.FromJson<PlayerJsonData>(payload);

        Debug.Log(data.Direction);

        this.transform.position = data.Position;
        current_player_direction = data.Direction;
        PlayAnimation("Idle", current_player_direction);
        this.health = data.Health;
        this.goldCount = data.GoldCount;
        this.strength = data.Strength;
        this.wisdom = data.Wisdom;
        this.constitution = data.Constitution;
        this.dexterity = data.Dexterity;
    }
}

[System.Serializable]
struct PlayerJsonData
{
    public Vector3 Position;
    public Vector2 Direction;
    public int Health;
    public int GoldCount;

    public int Strength;
    public int Constitution;
    public int Wisdom;
    public int Dexterity;
}

// we boldly go where many men have gone before.
// not because it is difficult, but because it is easy.
// 1000 lines hell yeah
