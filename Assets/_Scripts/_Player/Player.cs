using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    public static Player S;

    public float moveSpeed = 6.0f;//6;       // I made this exposed. ###
    Collider2D dashTrailCollider;
    public float dashSpeed = 12.0f;//6;       // I made this exposed. ###
    public ParticleSystem dashParticleSystem;
    ParticleSystem dashStopChargeParticleSystem;
    ParticleSystem jumpParticleSystem;
    public bool isPhantasming = false;
    public bool isDownSlamming = false;

    //public float jumpHeight = 3.5f;//3.5f; // E10 Jump logic and equation! ###

    public float maxJumpHeight = 3.5f;//3.5f;
    public float minJumpHeight = 1.0f;//1.0f;
    public float timeToJumpApex = 0.4f;//.4f;

    public int airJumpsAllowed = 1; // set to 0 if can only jump when on ground.
	int currentAirJumpCount;

    public int jumpCounter = 0;
    public int baseNumberOfJumps = 0;

    float accelerationTimeAirborne = 0.2f;//.2f;
    float accelerationTimeGrounded = 0.1f;//.1f;

    public Vector2 wallJumpClimb; // (7.5, 16)
    public Vector2 wallJumpOff;   // (8.5, 7)
    public Vector2 wallLeap;      // (18,  17)

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .25f; // Fixes issue of tricky 'wallLeap' since once you start move opposite you also quickly start moving down.
    float timeToWallUnstick;

    public float gravity;
    float storeGravity;

    //float maxJumpVelocity; // E10 Jump logic and equation! ###

    float maxJumpVelocity;
    float minJumpVelocity;

    Vector3 velocity;
    float velocityXSmoothing;
    float targetVelocityX;

    public bool canMove = true;

    Controller2D controller;
    AudioSource aud;

    void Awake()
    {
        S = this;
    }

    void Start()
    {
        controller = GetComponent<Controller2D>();
        dashParticleSystem = transform.GetChild(1).GetComponent<ParticleSystem>();
        jumpParticleSystem = transform.GetChild(2).GetComponent<ParticleSystem>();
        dashStopChargeParticleSystem = transform.GetChild(4).GetComponent<ParticleSystem>();
        dashTrailCollider = transform.GetChild(5).GetComponent<Collider2D>();
        aud = GetComponent<AudioSource>();

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        storeGravity = gravity;

        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt (2 * Mathf.Abs (gravity) * minJumpHeight); // Kinematic solved for minJumpVelocity.
        print("Gravity: " + gravity + "  Jump Velocity: " + maxJumpVelocity);

        //print("Gravity: " + gravity + "  Jump Velocity: " + jumpVelocity); // Old code before variable jump height E10.
        StartCoroutine(PhantasmFlashFX(0.01f));
    }

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); // Moved to top, wall jumping code stuff.

        int wallDirX = (controller.collisions.left) ? -1 : 1; // This is going to be -1 if collide wall to left of us, and positive 1 if collide wall to right of us.

        //float targetVelocityX = input.x * moveSpeed;
        targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);

        // Wall Jumping Code -->
        // Check for the case where this is true. 
        // Need to be colliding with a wall to the left or right.
        // Needs to not be touching the ground and Also has to be moving downwards.
        bool wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;  // Weird results if we don't rest used with 'ref' above in smoothdampf line.
                velocity.x = 0;          // While we want to remain stuck the wall. This is why input.x and SmoothDampf were moved up before this.

                if (input.x != wallDirX && input.x != 0) // We are moving away from wall we're hitting, moving opposite for 'wallLeap'.
                {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    timeToWallUnstick = wallStickTime; // Reset
                }
            }
            else // If none of above is true also just reset. fix?
            {
                timeToWallUnstick = wallStickTime;
            }

        }

        //if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below) // No longer the case with wall jumping code in.
        //if (Input.GetKeyDown(KeyCode.Space))
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (wallSliding)
            {
                if (wallDirX == input.x)                        // Wall jump and moving into direction of wall.
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                else if (input.x == 0)                          // Where we just jump off the wall. On wall. 
                {
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y;
                }
                else                                            // When we have an input that is opposite to wall direction.
                {
                    velocity.x = -wallDirX * wallLeap.x;
                    velocity.y = wallLeap.y;
                }
            }
            // No longer wall sliding here

            //if (controller.collisions.below) LPSwordAnimTest.S.anim.SetBool("slashDown", false);

            //if (controller.collisions.below) // Regular jump.
            if (controller.collisions.below || currentAirJumpCount < airJumpsAllowed)
            {
                velocity.y = maxJumpVelocity;
                jumpParticleSystem.Play(); // Best fits here, where else should it go???

                // Double jump code begin
                    if (!controller.collisions.below)
                    {
                        currentAirJumpCount++;
                    }
                    else {
                        currentAirJumpCount = 0;
                    }
                // Double jump code end
            }

        } // END OF Jumping code + wall jump.

        //if (Input.GetKeyUp(KeyCode.Space)) // For variable jump height E10 @2:25 code.
        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
        {
            if (velocity.y > minJumpVelocity)
            {
                velocity.y = minJumpVelocity;
            }
        }

        // <-- Wall Jumping Code
        // velocity.x ... was here... moved for wall jump code.
        velocity.y += gravity * Time.deltaTime;
        if (canMove)
            controller.Move(velocity * Time.deltaTime, input);

        // E10 @11:55 Explains why this was moved from above Space input to below .Move call.
        // Moving platform ALSO CALLS .Move().
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }

        // Change face code
        if (input.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input.x), 1, 1);
            //transform.localScale = new Vector3(controller.facing, 1, 1);
        }
        //NOTE: using controller.facing instead of sign(input.x) will result in player flipping dir while walljumping
        // If this is desirable behaviour, use the following line instead:
        // Change face code

        // Phantasm Code
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (isPhantasming == false && input.x != 0)
                StartCoroutine(Phantasm(Mathf.Sign(input.x)));
        }

            dashStopChargeParticleSystem.gameObject.transform.Rotate(transform.forward * (transform.localScale.x)*-300.0f * Time.deltaTime);
    } // END OF Update() METHOD

    IEnumerator Phantasm(float dir)
    {
        isPhantasming = true;
        canMove = false;
        gravity = 0;
        velocity.y = 0;
        dashStopChargeParticleSystem.Play();
        yield return new WaitForSeconds(0.2f);
        dashStopChargeParticleSystem.Stop();
        dashParticleSystem.transform.localScale = transform.localScale;
        dashParticleSystem.Play();
        canMove = true;
        GlitchHandler.S.ColorDriftFXMethod(0.25f);
        aud.pitch = Random.Range(0.9f, 1.1f);
        aud.Play();
        velocity.x = transform.localScale.x * dashSpeed;
        dashTrailCollider.enabled = true;
        yield return new WaitForSeconds(0.1f);
        dashTrailCollider.enabled = false;
        canMove = false;
        yield return new WaitForSeconds(0.2f);
        dashParticleSystem.Stop();
        canMove = true;
        gravity = storeGravity;
        isPhantasming = false;
    }

    IEnumerator PhantasmFlashFX(float flashTime)
    {
        while (true)
        {
                dashStopChargeParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", Color.blue);
                dashParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", Color.red);
                yield return new WaitForSeconds(flashTime);
                dashStopChargeParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", Color.green);
                dashParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", Color.green);
                yield return new WaitForSeconds(flashTime);
                dashStopChargeParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", Color.red);
                dashParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", Color.blue);
                yield return new WaitForSeconds(flashTime);
        }
        //yield return new WaitForSeconds(0);
        //dashParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.color = Color.blue;
    }

    public void DownSlamMethod(float js) { StartCoroutine(DownSlam(js)); }
    IEnumerator DownSlam(float js)
    {
        canMove = false;
        dashStopChargeParticleSystem.Play();
        yield return new WaitForSeconds(0.15f);
        dashStopChargeParticleSystem.Stop();
        canMove = true;
        velocity.x = 0;
        velocity.y = js;
        aud.pitch = Random.Range(0.8f, 1.0f);
        aud.Play();
        dashParticleSystem.transform.localScale = transform.localScale;
        Player.S.dashParticleSystem.Play();
        GlitchHandler.S.ColorDriftFXMethod(0.25f);
        isDownSlamming = false; 
    }

    public void ExternalJump(float js)
    {
        velocity.y = js;
    }

}
