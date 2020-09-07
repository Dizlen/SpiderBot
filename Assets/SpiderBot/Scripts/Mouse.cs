using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mouse : MonoBehaviour
{
    [SerializeField]
    public float sensitivity = 5.0f;
    [SerializeField]
    public float smoothing = 2.0f;
    // the chacter is the capsule
    public GameObject character;
    // get the incremental value of mouse moving
    private Vector2 mouseLook;
    // smooth the mouse moving
    private Vector2 smoothV;
    private RaycastHit hit;
    private int frontBot = 1 << 27;
    private int leftBot = 1 << 28;
    private int rightBot = 1 << 29;
    private int BackBot = 1 << 30;
    private int topBot = 1 << 31;
    public SpiderBot bot;
    private bool botHit;

    // Use this for initialization
    void Start()
    {
        character = this.transform.parent.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        ShootRay();
        MouseMove();
    }

    void ShootRay()
    {
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 10, Color.yellow);

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {

            //front of bot
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 10, frontBot))
            {
                bot = hit.transform.gameObject.GetComponentInParent<SpiderBot>();
                bot.TakeDamage(Random.Range(10, 15));
                Debug.Log("Hit enemy");
            }
        }

    }

    void MouseMove()
    {
        // md is mosue delta
        var md = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        md = Vector2.Scale(md, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        // the interpolated float result between the two float values
        smoothV.x = Mathf.Lerp(smoothV.x, md.x, 1f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, md.y, 1f / smoothing);
        // incrementally add to the camera look
        mouseLook += smoothV;

        // vector3.right means the x-axis
        transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
        character.transform.localRotation = Quaternion.AngleAxis(mouseLook.x, character.transform.up);
    }
}
