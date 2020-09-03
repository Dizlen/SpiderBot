using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class PassiveBot : MonoBehaviour
{
    [Space]
    [Header("Main")]
    public int health;
    public float speed;
    public float viewDistance;
    public bool isHostile;
    public float huntTimer;
    [Space]
    [Header("Movement")]
    public int movementRadius;
    public float movementDelay;
    [Space]
    [Header("Materials and objs")]
    public Material passiveMaterial;
    public Material alertMaterial;
    public Material hostileMaterial;
    public GameObject alertObj;
    public GameObject huntObj;


    //Private
    private PassiveBotState _state;
    private NavMeshAgent agent;
    //private MeshRenderer botRenderer;
    private User player;
    private float defaultSpeed;
    private float defaultDelay;
    private float timeStart;
    private float startHuntTimer;
    private bool searchingPlayer;
    private RaycastHit hit;

    public enum PassiveBotState
    {
        Initialize,
        Explore,
        Combat,
        Death
    }

    public IEnumerator Start()
    {
        //botRenderer = GetComponent<MeshRenderer>();
        agent = GetComponent<NavMeshAgent>();
        defaultDelay = movementDelay;
        defaultSpeed = speed; 
        _state = PassiveBotState.Initialize;


        while (true)
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
        timeStart = 0;
        searchingPlayer = false;
        isHostile = false;
        movementDelay = defaultDelay;
        agent.speed = defaultSpeed;
        huntObj.SetActive(false);
        alertObj.SetActive(false);
        //botRenderer.material = passiveMaterial;
        _state = PassiveBotState.Explore;
    }

    // Agent starts timer and begins to move around the scene
    public void Explore()
    {
        if(timeStart < movementDelay && !isHostile)
        {
            timeStart += Time.deltaTime;
            if(timeStart>= movementDelay)
            {
                RandomNavmeshLocation();
                timeStart = 0;
            }
        }

        // If the bot is shot, turn to hostile
        if (isHostile)
        {
            AlertMode();
        }

        // Is used to take damage, would be replaced with a player gun in future
        if (Input.GetKeyDown(KeyCode.X))
        {
            TakeDamage(1);
        }
    }

    public void Combat()
    {
        //Debug.Log("COMBATTTTTTTTTTTTTT");
        searchingPlayer = false;
        agent.destination = player.transform.position;
        //botRenderer.material = hostileMaterial;
        alertObj.SetActive(false);
        huntObj.SetActive(true);

    }

    public void Death()
    {
        Destroy(this.gameObject);
    }

    public void AlertMode()
    {
        //Called only once to update the values for alert mode, speed increase and less movement delay
        if (!searchingPlayer)
        {
            //botRenderer.material = alertMaterial;
            alertObj.SetActive(true);
            agent.speed *= 2;
            movementDelay /= 2;
            timeStart = 0;

            //Once this value turns true, the fixedupdate will begin to search for the player
            searchingPlayer = true;
        }
    }

    public void TakeDamage(int dmg)
    {
        isHostile = true;
        health -= dmg;

        if(health <= 0)
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

        if(startHuntTimer < huntTimer)
        {
            startHuntTimer += Time.deltaTime;
            if(startHuntTimer >= huntTimer)
            {
                NoTargetFound();
                startHuntTimer = 0;
            }
        }
    }

    public void NoTargetFound()
    {
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
}
