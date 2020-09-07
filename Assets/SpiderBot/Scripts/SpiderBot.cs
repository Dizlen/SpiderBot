using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class SpiderBot : MonoBehaviour
{
    [Header("---Main---")]
    public int health;
    public float speed;
    public float viewDistance;
    public bool isHostile;
    public float huntTimer;

    [Header("---Movement---")]
    public int movementRadius;
    public float movementDelay;

    [Header("---Animator and sfx---")]
    public Animator botAnim;
    public AudioSource botAudioSource;
    public AudioClip[] botWalkSFX;
    public AudioClip[] botDeathSFX;
    public AudioClip[] botHitSFX;
    public AudioClip[] botAlertSFX;
    public AudioClip[] botIdleSFX;
    public AudioClip[] botAttackSFX;

    //States and references
    private PassiveBotState _state;
    private NavMeshAgent agent;
    private User player;

    //default values are used to reset the bots speed and movement delay if the bot doesn't spot the player
    private float defaultSpeed;
    private float defaultDelay;

    //floats that track time for hunting and attacking
    private float timeStart;
    private float startHuntTimer;
    private float attackStart;

    //floats for stun duration when attacked
    private float stunTimerStart;
    private float stunTimerEnd = 0.5f;

    //Bools and raycast
    private bool stunned;
    private bool searchingPlayer;
    private bool inRangeToAttack;
    private bool dead;
    private RaycastHit hit;
    private Vector3 target;

    public enum PassiveBotState
    {
        Initialize,
        Explore,
        Combat,
        Death
    }

    public IEnumerator Start()
    {
        agent = GetComponent<NavMeshAgent>();
        botAnim = GetComponent<Animator>();
        defaultDelay = movementDelay;
        defaultSpeed = speed;
        _state = PassiveBotState.Initialize;

        //As long as the agent is alive, run the state machine
        while (true && !dead)
        {
            switch (_state)
            {
                case PassiveBotState.Initialize:
                    Initialize();
                    break;
                case PassiveBotState.Explore:
                    Explore();
                    break;
                case PassiveBotState.Combat:
                    Combat();
                    break;
                case PassiveBotState.Death:
                    Death();
                    break;
            }
            yield return 0;
        }
    }


    private void FixedUpdate()
    {
        // Set bot anim speed to velocity
        botAnim.SetFloat("Speed", agent.speed);

        // Turns on when the bot is attacked.
        // The bot is stunned for a short duration and cannot move.
        if (stunned)
        {
            agent.isStopped = true;
            botAnim.SetBool("Walking", false);
            botAnim.SetBool("Attacking", false);

            if (stunTimerStart <= stunTimerEnd)
            {
                stunTimerStart += Time.deltaTime;

                if (stunTimerStart >= stunTimerEnd)
                {
                    botAnim.SetBool("hitLeft", false);
                    botAnim.SetBool("hitRight", false);
                    botAnim.SetBool("hitBack", false);
                    botAnim.SetBool("hitFront", false);
                    agent.isStopped = false;
                    stunned = false;
                    stunTimerStart = 0;
                }
            }
        }

        // Searching for player is set to true when the bot has been attacked.
        // It will attempt to move towards the area where the gun was shot from.
        if (searchingPlayer)
        {
            AgentHunt();
        }
    }

    // Reset and initialize values
    public void Initialize()
    {
        Debug.Log("Spider bot initialize");
        timeStart = 0;
        searchingPlayer = false;
        isHostile = false;
        movementDelay = defaultDelay;
        agent.speed = defaultSpeed;
        botAnim.SetBool("Walking", false);
        _state = PassiveBotState.Explore;
    }

    // Agent starts timer and begins to move around the scene
    public void Explore()
    {
        Debug.Log("Exploring");
        if (timeStart < movementDelay && !isHostile)
        {
            timeStart += Time.deltaTime;
            if (timeStart >= movementDelay)
            {
                RandomNavmeshLocation();
                timeStart = 0;
            }
        }

        // If the bot is moving, enable animation
        if (agent.velocity != Vector3.zero)
        {
            botAnim.SetBool("Walking", true);
        }

        if (agent.remainingDistance <= 0.1f)
        {
            botAnim.SetBool("Walking", false);
        }

        // If the bot is shot, turn to hostile
        if (isHostile)
        {
            AlertMode();
        }
    }

    public void Combat()
    {
        if (!stunned)
        {
            searchingPlayer = false;
            agent.destination = player.transform.position;

            if (agent.remainingDistance <= 0.8f)
            {
                inRangeToAttack = true;
                Vector3 playerpos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
                transform.LookAt(playerpos);

                if (inRangeToAttack)
                {
                    agent.isStopped = true;
                    botAnim.SetBool("Attacking", true);
                    botAnim.SetBool("Walking", false);
                }
            }
            else if (!inRangeToAttack)
            {
                agent.isStopped = false;
                botAnim.SetBool("Attacking", false);
                botAnim.SetBool("Walking", true);
            }
        }    
    }

    // Called at the end of the attack animation to break the condition above.
    public void ResetAttack()
    {
        inRangeToAttack = false;
    }

    public void Death()
    {
        if (!dead)
        {
            Debug.Log("Dead");
            agent.ResetPath();
            agent.isStopped = true;
            dead = true;
            isHostile = false;
            botAnim.SetBool("Attacking", false);
            botAnim.SetBool("Walking", false);
            botAnim.SetBool("Dead", true);
        }
    }

    public void AlertMode()
    {
        //Called only once to update the values for alert mode, speed increase and less movement delay
        if (!searchingPlayer)
        {
            Debug.Log("Looking for the player");
            AlertSFX();
            agent.speed *= 2;
            movementDelay /= 2;
            agent.ResetPath();
            timeStart = movementDelay - 1;

            //Once this value turns true, the fixedupdate will begin to search for the player
            searchingPlayer = true;
        }
    }

    public void TakeDamage(int dmg, int side)
    {
        if(health > 0)
        {
            health -= dmg;
            isHostile = true;
            stunned = true;
            BulletHitSFX();

            // What side of the bot was hit?
            if(side == 0)
            {
                botAnim.SetBool("hitFront", true);
            }
            if (side == 1)
            {
                botAnim.SetBool("hitBack", true);
            }
            if (side == 2)
            {
                botAnim.SetBool("hitLeft", true);
            }
            if (side == 3)
            {
                botAnim.SetBool("hitRight", true);
            }
        }

        if (health <= 0)
        {
            _state = PassiveBotState.Death;
        }
    }

    public void AgentHunt()
    {
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * viewDistance, Color.yellow);
        int layermask = 1 << 10;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, viewDistance, layermask))
        {
            player = hit.transform.gameObject.GetComponent<User>();
            Debug.Log("Player spotted! Moving to attack.");
            _state = PassiveBotState.Combat;
        }

        if (timeStart < movementDelay)
        {
            timeStart += Time.deltaTime;
            if (timeStart >= movementDelay)
            {
                GameObject player = FindObjectOfType<User>().gameObject;
                Vector3 playerArea = Random.insideUnitSphere * 3;
                playerArea += player.transform.position;
                NavMeshHit hit;
                Vector3 playerPosGuess = Vector3.zero;

                if (NavMesh.SamplePosition(playerArea, out hit, 3, 1))
                {
                    playerPosGuess = hit.position;
                }

                agent.destination = playerPosGuess;
                timeStart = 0;
            }
        }

        if (startHuntTimer < huntTimer)
        {
            startHuntTimer += Time.deltaTime;
            if (startHuntTimer >= huntTimer)
            {
                NoTargetFound();
                startHuntTimer = 0;
            }
        }
    }

    public void NoTargetFound()
    {
        Debug.Log("No Target found...");
        _state = PassiveBotState.Initialize;
    }

    public Vector3 RandomNavmeshLocation()
    {
        Vector3 randomDirection = Random.insideUnitSphere * movementRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, movementRadius, 1))
        {
            finalPosition = hit.position;
        }
        agent.destination = finalPosition;
        return finalPosition;
    }

    public void SpiderBotFootstep()
    {
        botAudioSource.PlayOneShot(botWalkSFX[Random.Range(0, botWalkSFX.Length)]);
    }

    public void SpiderBotDeath()
    {
        botAudioSource.PlayOneShot(botDeathSFX[Random.Range(0, botDeathSFX.Length)]);
    }

    public void AlertSFX()
    {
        botAudioSource.PlayOneShot(botAlertSFX[Random.Range(0, botAlertSFX.Length)]);
    }

    public void BulletHitSFX()
    {
        botAudioSource.PlayOneShot(botHitSFX[Random.Range(0, botHitSFX.Length)]);
    }
    public void AttackSwipeSFX()
    {
        botAudioSource.PlayOneShot(botAttackSFX[Random.Range(0, botAttackSFX.Length)]);
    }
    public void IdleSFX()
    {
        botAudioSource.PlayOneShot(botIdleSFX[Random.Range(0, botIdleSFX.Length)]);
    }
}
