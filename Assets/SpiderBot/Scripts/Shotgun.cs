using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [Header("Ammo")]
    public int totalAmmo;
    public int maxAmmo;
    public int shellsLoaded;
    public int maxLoadedShells;
    [Header("Range and accuracy")]
    public int shots;
    public float accuracy;
    public float range;
    public ParticleSystem particles;
    private Animator shotgunAnim;
    private RaycastHit hit;
    private SpiderBot bot;
    int layermask = 1 << 9;

    private void Start()
    {
        shotgunAnim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            for (var i = 0; i < shots; i++)
            {
                var v3Offset = transform.up * Random.Range(0f, accuracy);
                v3Offset = Quaternion.AngleAxis(Random.Range(0f, 360f), -transform.right) * v3Offset;
                var v3Hit = -transform.right * range + v3Offset;

                Debug.DrawRay(transform.position, v3Hit, Color.red);

                if(Physics.Raycast(transform.position,v3Hit,out hit, range, layermask))
                {
                    bot = hit.transform.gameObject.GetComponent<SpiderBot>();
                    bot.TakeDamage(Random.Range(10, 15),0);
                    Debug.Log("Hit enemy");
                }

                // Position an object to test pattern
                //var tr = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                //tr.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                //tr.position = v3Hit;
            }
            particles.Play();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            shotgunAnim.SetBool("Reload", true);

        }
    }

    public void resetshotty()
    {
        shotgunAnim.SetBool("Reload", false);

    }
}
