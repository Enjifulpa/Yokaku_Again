using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float gridSize = 1f;
    public bool snapToGridOnStart = true;

    public Sprite[] sprites;

    [Header("Encounter")]
    public string grassTag = "Grass";
    public string battleSceneName = "Battle";
    [Range(0f, 1f)] public float encounterChance = 0.1f;
    [Min(1)] public int minimumGrassStepsBeforeEncounter = 2;
    public float grassCheckRadius = 0.2f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Collider2D playerCollider;

    Vector2 move;
    Vector2 moveDirection;
    Vector2 targetPosition;

    readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    float timer;
    int frame;
    int grassSteps;

    bool isMoving;
    bool inGrass;
    bool encounterStarted;

    string direction = "down";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        gridSize = Mathf.Max(0.01f, gridSize);
        minimumGrassStepsBeforeEncounter = Mathf.Max(1, minimumGrassStepsBeforeEncounter);

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearVelocity = Vector2.zero;

            if (snapToGridOnStart)
            {
                Vector2 snappedPosition = SnapToGrid(rb.position);
                rb.position = snappedPosition;
                transform.position = new Vector3(snappedPosition.x, snappedPosition.y, transform.position.z);
            }

            // Kalau baru balik dari battle (misal abis pencet Escape), restore posisi
            // player ke titik persis sebelum encounter kejadian, bukan posisi default/spawn.
            if (EncounterState.HasPendingReturn && EncounterState.PreviousSceneName == gameObject.scene.name)
            {
                Vector2 restorePosition = EncounterState.PlayerPosition;
                rb.position = restorePosition;
                transform.position = new Vector3(restorePosition.x, restorePosition.y, transform.position.z);

                if (!string.IsNullOrEmpty(EncounterState.PlayerDirection))
                    direction = EncounterState.PlayerDirection;

                EncounterState.Consume();
            }
        }
    }

    void Update()
    {
        if (rb == null || encounterStarted)
            return;

        if (isMoving)
        {
            move = moveDirection;
        }
        else
        {
            move = ReadGridInput();

            if (move != Vector2.zero)
            {
                SetDirection(move);

                if (IsBlocked(move))
                    move = Vector2.zero;
                else
                    StartGridMove(move);
            }
        }

        Animate();
    }

    void FixedUpdate()
    {
        if (rb == null || encounterStarted)
            return;

        rb.linearVelocity = Vector2.zero;

        if (isMoving)
            ContinueGridMove();
    }


    Vector2 ReadGridInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal != 0f)
            return new Vector2(Mathf.Sign(horizontal), 0f);

        float vertical = Input.GetAxisRaw("Vertical");
        if (vertical != 0f)
            return new Vector2(0f, Mathf.Sign(vertical));

        return Vector2.zero;
    }

    void StartGridMove(Vector2 directionInput)
    {
        moveDirection = directionInput;
        targetPosition = rb.position + moveDirection * gridSize;
        isMoving = true;
        move = moveDirection;
    }

    void ContinueGridMove()
    {
        Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetPosition, speed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);

        if ((targetPosition - nextPosition).sqrMagnitude > 0.0001f)
            return;

        rb.position = targetPosition;
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        isMoving = false;
        move = Vector2.zero;

        if (IsStandingInGrass())
            TryStartEncounter();
        else
            grassSteps = 0;
    }

    bool IsBlocked(Vector2 directionInput)
    {
        if (playerCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(Physics2D.AllLayers);

        int hitCount = playerCollider.Cast(directionInput, filter, castHits, gridSize);

        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].collider != null && !castHits[i].collider.isTrigger)
                return true;
        }

        return false;
    }

    Vector2 SnapToGrid(Vector2 position)
    {
        return new Vector2(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize
        );
    }

    void SetDirection(Vector2 directionInput)
    {
        if (directionInput.x < 0f)
            direction = "left";
        else if (directionInput.x > 0f)
            direction = "right";
        else if (directionInput.y > 0f)
            direction = "up";
        else if (directionInput.y < 0f)
            direction = "down";
    }

    bool IsStandingInGrass()
    {
        if (inGrass)
            return true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, grassCheckRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (IsGrass(hits[i]))
                return true;
        }

        return false;
    }

    int GetBattleSceneBuildIndex()
    {
        string scenePath = battleSceneName.EndsWith(".unity")
            ? battleSceneName
            : string.Format("Assets/Scenes/{0}.unity", battleSceneName);

        return SceneUtility.GetBuildIndexByScenePath(scenePath);
    }


    void TryStartEncounter()
    {
        if (encounterStarted || encounterChance <= 0f)
            return;

        grassSteps++;

        if (grassSteps < minimumGrassStepsBeforeEncounter)
            return;

        if (Random.value > encounterChance)
            return;

        if (string.IsNullOrEmpty(battleSceneName))
        {
            Debug.LogWarning("Encounter terjadi, tapi battleSceneName belum diisi.");
            return;
        }

        int battleSceneBuildIndex = GetBattleSceneBuildIndex();
        if (battleSceneBuildIndex < 0 && !Application.CanStreamedLevelBeLoaded(battleSceneName))
        {
            Debug.LogWarningFormat("Encounter terjadi, tapi scene '{0}' belum ada di Build Settings.", battleSceneName);
            return;
        }

        encounterStarted = true;
        rb.linearVelocity = Vector2.zero;

        // Simpen posisi & scene sekarang SEBELUM pindah ke Battle, biar Escape bisa balikin player ke sini lagi.
        EncounterState.SaveEncounter(gameObject.scene.name, transform.position, direction);

        if (battleSceneBuildIndex >= 0)
            SceneManager.LoadScene(battleSceneBuildIndex);
        else
            SceneManager.LoadScene(battleSceneName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsGrass(other))
            inGrass = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (IsGrass(other))
            inGrass = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsGrass(other))
            return;

        inGrass = false;

        if (!IsStandingInGrass())
            grassSteps = 0;
    }

    bool IsGrass(Collider2D other)
    {
        return other != null && !string.IsNullOrEmpty(grassTag) && other.CompareTag(grassTag);
    }

    void Animate()
    {
        if (move == Vector2.zero)
        {
            if (direction == "down")
                SetSprite(1);

            if (direction == "left")
                SetSprite(4);

            if (direction == "right")
                SetSprite(7);

            if (direction == "up")
                SetSprite(10);

            return;
        }

        timer += Time.deltaTime;

        if (timer > 0.2f)
        {
            timer = 0f;
            frame++;

            if (frame > 2)
                frame = 0;
        }

        if (direction == "down")
            SetSprite(frame);

        if (direction == "left")
            SetSprite(3 + frame);

        if (direction == "right")
            SetSprite(6 + frame);

        if (direction == "up")
            SetSprite(9 + frame);
    }

    void SetSprite(int index)
    {
        if (sr == null || sprites == null || index < 0 || index >= sprites.Length)
            return;

        sr.sprite = sprites[index];
    }
}