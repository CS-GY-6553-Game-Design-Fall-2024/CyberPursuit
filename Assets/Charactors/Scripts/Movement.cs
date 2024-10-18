using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Movement : MonoBehaviour
{
    [Header("=== Character Controls ===")]
    [Tooltip("The expected keyboard input for jumping")]                    public KeyCode jumpKey = KeyCode.Space;
    [Tooltip("The expected keyboard input for sliding")]                    public KeyCode slideKey = KeyCode.S;
    [Tooltip("The expected keyboard input for wall running")]               public KeyCode wallRunKey = KeyCode.LeftShift;
    [Tooltip("The expected keyboard input for reset position")]             public KeyCode resetPositionKey = KeyCode.R;

    [Header("=== Movement Parameters ===")]
    [SerializeField, Tooltip("The player's Rigidbody")]                     private Rigidbody2D m_Rigidbody2D;
    [SerializeField, Tooltip("The transform where ground check occurs")]    private Transform m_GroundCheck;
    [Tooltip("The max horizontal movement speed")]                          public float maxSpeed = 10f;
    [Tooltip("The hor. movement acceleration")]                             public float accelerations = 8f;
    [Tooltip("The amount of upward force exerted by jumping")]              public float jumpForce = 250f;
    [Tooltip("How long to wait until checking ground - avoids dbl-jumps")]  private float m_NextGroundCheckLag = 0.1f;
    [Space]
    [Tooltip("Is the player allowed to move in midair?")]                   public bool canAirControl = false;         
    [Tooltip("What layers are considered ground?")]                         public LayerMask groundMask;
    //[Tooltip("The radius to check if the player is grounded")]              public float groundedRadius = 0.01f;
    [Tooltip("The size to check if the player is grounded")]                public Vector2 groundedCheckSize = new Vector2(0.35f, 0.05f);
    [Tooltip("The minimum hor. speed required to be able to slide")]        public float minimumSlideStartSpeed = 2f;
    [Tooltip("The minimum time needed to be in slide mode")]                public float minimumSlideTime = 0.8f;
    [Tooltip("The minimum hor. speed required to be able to wall run")]     public float wallRunningSpeedThreshold = 1f;
    [Tooltip("The maximum amount of time that the player can wall run")]    public float maximumWallRunTime = 1.5f;
    [Tooltip("The curve that controls movement during wall run")]           public AnimationCurve wallRunMovementCurve;
    [Tooltip("The force player got hit")]                                   public Vector2 injuryForce = new Vector2(50, 50);
    [Tooltip("Hurt cool down")]                                             public float hurtCD = 0.5f;
    [Tooltip("Player max health point")]                                    public int maxhp = 5;
    public RectTransform hpBar;
    
    [Header("=== Movement Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The input value for horizontal movement")]    private float horizontalInput;
    [SerializeField, Tooltip("The input value for jumping")]                private bool jumpInput;
    [SerializeField, Tooltip("The input value for sliding")]                private bool slideInput;
    [SerializeField, Tooltip("The input value for wall running")]           private bool wallRunningInput;
    [Space]
    [SerializeField, Tooltip("The current Rigidbody hor. speed")]           private float currentSpeed = 0f;
    [SerializeField, Tooltip("Is the player grounded?")]                    private bool m_Grounded;
    [SerializeField, Tooltip("The time until we can check ground")]         private float m_NextGroundCheckTime; 
    [SerializeField, Tooltip("Is the player facing rightward?")]            private bool m_FacingRight = true;
    [SerializeField, Tooltip("Is the player sliding?")]                     private bool m_isSliding = false;
    public bool isSliding => m_isSliding;
    [SerializeField, Tooltip("The time until which we can exit sliding")]   private float slideEndTime;
    [SerializeField, Tooltip("Is the player able to wall run?")]            private bool m_canWallRun = false;
    [SerializeField, Tooltip("Is the player wall running?")]                private bool isWallRunning = false;
    [SerializeField, Tooltip("Has the wall run been started?")]             private bool m_initiatedWallRun = false;
    [SerializeField, Tooltip("The time until which we must stop wall run")] private float m_wallRunEndTime;
    [SerializeField, Tooltip("Has the wall jump been started?")]            private bool m_initiatedWallJump;
    [SerializeField, Tooltip("Going to start a wall jump?")]                private bool m_readyforWallJump;
    
    [SerializeField, Tooltip("Ready to reset position?")]                   private bool m_initiateReset;
    [SerializeField, Tooltip("Position of last check point")]               private Vector3 checkpoint;
    [SerializeField, Tooltip("State of getting hurt")]                      private bool IsInjuried = false;
    [SerializeField, Tooltip("Next get hurt time")]                         private float nextHurtTime;
    [SerializeField, Tooltip("Player current health point")]                private int hp;
    private float hpBarMaxWidth;
    
    [Header("=== Animation Settings ===")]
    [SerializeField, Tooltip("The player's Animator component")]            private Animator animator;
    private float animationMaxSpeed = 1.2f;         // dynamic animation speed for running
    private float animationMinSpeed = 0.2f;

    [Header("=== Events ===")]
    [Space]
    public UnityEvent OnLandEvent;
    public UnityEvent OnAirEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameObject levelCompleteCanvas;

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (m_GroundCheck == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(m_GroundCheck.position, groundedCheckSize);
    }
#endif

    private void Awake() {
        if (m_Rigidbody2D == null) 
                m_Rigidbody2D = GetComponent<Rigidbody2D>();
            if (animator == null)
                animator = GetComponent<Animator>();
            if (OnLandEvent == null)
                OnLandEvent = new UnityEvent();
            if (OnAirEvent == null)
                OnAirEvent = new UnityEvent();
            currentSpeed = 0;
            checkpoint = transform.position;
    }

    private void Start() {
        audioManager = AudioManager.current;
        if (audioManager != null) {
            UnityEngine.Debug.Log("AudioManager successfully loaded.");
        }
        else {
            UnityEngine.Debug.LogError("AudioManager not found! Make sure there is an object with the tag 'Audio' in the scene.");
        }

        if (levelCompleteCanvas != null)
        {
            levelCompleteCanvas.SetActive(false); // Ensure it's hidden at start
        }
        else
        {
            UnityEngine.Debug.LogError("Level Complete Canvas not assigned in the Inspector.");
        }

        hp = maxhp;
        hpBarMaxWidth = hpBar.sizeDelta.x;
    }

    private void Update() {
        // cannot move if get injured
        if (IsInjuried)
        {
            return;
        }
        
        // Get the user's input, which consists of the horizontal, jump, slide, and wall run
        horizontalInput = Input.GetAxis("Horizontal");
        jumpInput = Input.GetKey(jumpKey);
        slideInput = Input.GetKey(slideKey);
        wallRunningInput = Input.GetKey(wallRunKey);
        m_initiateReset = m_initiateReset || Input.GetKeyDown(resetPositionKey);
        //jumpInput = Input.GetButton("Jump");
        //slideInput = Input.GetKey(KeyCode.S);

        // Get the rigidybody's current horizontal speed, in absolute value
        float absCurrentSpeed = Mathf.Abs(m_Rigidbody2D.velocity.x);

        // We check the state of being grounded as well as if we're allowed to wall run.
        // We pair these together because while wall running, we can also technically jump as long as we're in the motion
        m_Grounded = CheckIfGrounded();
        m_initiatedWallRun = CheckIfWallRun();

        // reset the wall jump when grounded
        if (m_Grounded)
        {
            m_initiatedWallJump = false;
        }
        
        // Sliding is dependent on some factors: 1) if the player is on the ground, if they're not sliding, and if the person is moving at a sufficiently large speed
        if (m_Grounded && slideInput && !m_isSliding && absCurrentSpeed > minimumSlideStartSpeed) {
            slideEndTime = Time.time + minimumSlideTime;    // We require the player to, if nothing else, slide for a set minimum threshold
            m_isSliding = true;                               // Boolean flag to let the system know we should be sliding.

            audioManager.PlaySFX(audioManager.slide);
        }
        // We end sliding if either 1) we reached the minimum slide time, 2) we're in the air and no longer grounded, or 3) we are moving too slow.
        if ((Time.time > slideEndTime && !slideInput) || !m_Grounded || absCurrentSpeed < minimumSlideStartSpeed) {
            m_isSliding = false;
        }
        // if sliding, automatically slow down
        if (m_isSliding)  {
            // if no friction, uses below
            // horizontalInput = (m_FacingRight? -1 : 1) * slidingFriction;
            
            // if having friction. uses below
            horizontalInput = 0;
        }
        

        // The player's ability to wall run is completely dictated by the ontriggerenter2d and ontriggerexit2d with billboards
        //      However, just because the player CAN wall run doesn't mean they are always allowed.
        //      It is very much likely that we want to allow the player to wall run only once while in a jump
        //      In other words, they must hit the ground before their ability to wall-run is triggered again.
        // There are also logistical factors as well.
        //      
        
        isWallRunning = !m_Grounded && m_initiatedWallRun && Time.time < m_wallRunEndTime && !m_initiatedWallJump;
        if (isWallRunning && jumpInput)
        {
            m_readyforWallJump = true;
            isWallRunning = false;
            // jumpInput = false;
            //keep m_initiatedWallRun as true
        }

        /*
        // Now can accelerate in the air
        if (!m_Grounded && !isWallRunning) {    // don't accelerate in the air
            if (m_FacingRight) {
                horizontalInput = horizontalInput > 0 ? 0 : horizontalInput;
            }
            else {
                horizontalInput = horizontalInput < 0 ? 0 : horizontalInput;
            }
        }
        */
        
    
        //set variables for animator
        animator.SetFloat("HorizontalSpeed", absCurrentSpeed);
        animator.SetBool("OnGround", m_Grounded);
        animator.SetFloat("VerticalSpeed", m_Rigidbody2D.velocity.y);
        animator.SetBool("IsSliding", m_isSliding);
        animator.SetBool("WallRunning", isWallRunning);
        AdjustAnimationSpeed();  // set animation speed
    }

    public bool CheckIfWallRun() {
        // This function, in the Update loop, updates whether we can actually perform a wall jump
        // `m_canWallJump` becomes `TRUE` by default the moment we enter the trigger area of a wall jumpable interactable.
        //      Likewise, it becomes `FALSE` when the player exits the wall run interactable trigger box.
        //      We shouldn't control this variable manually. Let's leave it alone.
        // What we SHOULD pay attention to is `m_initiatedWallRun`, which can only be set to `TRUE` if the player:
        //      1. can wall run, and
        //      2. is pressed or holding the wall run input
        // The unique thing about `m_initatedWallRun` is that the moment it's set to `TRUE`, we cannot set it back to `FALSE`
        //      until the player hits the ground. Only then, can we initated another wall run under the conditions above.

        // The first check is to update the wall run end time.
        // This is only updated if the player can wall run and they haven't initiated a wall run.
        if (m_canWallRun && !m_initiatedWallRun) {
            m_wallRunEndTime = Time.time + maximumWallRunTime;
        }

        // The second check is setting `m_initiatedWallRun`, which we do upon return in the normal Update loop.
        if (m_Grounded) return false;
        return m_initiatedWallRun || (wallRunningInput && m_canWallRun);
    }

    public bool CheckIfGrounded() {
        // If our time to calculate the next jump is not over the lag end, then we can't change anything
        if (!m_Grounded && Time.time < m_NextGroundCheckTime) return m_Grounded;

        // Conduct a physics overlap check for any ground underneath the player
        //Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, groundedRadius, groundMask);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(m_GroundCheck.position, groundedCheckSize, 0f, groundMask);
        // If the colliders returns empty, exit early. This is because we're in the air.
        // If we were previously grounded, the lack of colliders means we're in the air. So we must invoke any listeners on the OnAirEvent.
        // We must also update the next time to check the grounded time.
        if (colliders.Length == 0) {
            if (m_Grounded) OnAirEvent?.Invoke();
            m_NextGroundCheckTime = Time.time + m_NextGroundCheckLag;
            return false;
        }

        // Because of the ground mask, we can safely declare that as long as there is ONE collider, then we can safely declare that we are grounded
        // However, if we were previously not grounded, then we have to execute the onlanded event, if there are any listeners
        if (!m_Grounded) OnLandEvent?.Invoke();
        return true;
    }


    private void FixedUpdate() {
        
        // reset position if ResetKey pressed
        if (m_initiateReset)
        {
            ResetPosition();
            m_initiateReset = false;
            return;
        }
        if (IsInjuried) 
            return;
        
        // Update the current speed based on fixed delta time
        // We also restrict the current speed to the max speed
        currentSpeed = m_Rigidbody2D.velocity.x;
        currentSpeed += accelerations * Time.fixedDeltaTime * horizontalInput;
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // make it easier to stop
        if (Mathf.Abs(currentSpeed) < 0.1f && Mathf.Abs(horizontalInput) < 0.1f) {
            currentSpeed = 0f;
        }
        
        // Initialize movement via Move() public function
        Move(currentSpeed);
    }


    // set charactor's movement
    public void Move(float speed)
    {
        // control wall running
        float verticalSpeed = m_Rigidbody2D.velocity.y;
        if (isWallRunning) {
            // The vertical speed of the wall run is dependent on the amount of remaining time between the current time and the expected end time.
            // We use this value to determine the vertical speed 
            float wallCurveTime = 1f-(m_wallRunEndTime-Time.time)/maximumWallRunTime;
            verticalSpeed = wallRunMovementCurve.Evaluate(wallCurveTime);
            //Debug.Log($"{wallCurveTime} = {verticalSpeed.ToString()}");
        }

        
        // control horizontal movement
        //Debug.Log($"Time: {Time.time} | Grounded? {m_Grounded.ToString()} | Can Control In Air? {canAirControl.ToString()} | Hor. Input: {horizontalInput} | Current Speed: {currentSpeed.ToString()} | Speed: {speed.ToString()} | Vertical Speed: {verticalSpeed.ToString()}");
        if (m_Grounded || canAirControl) {

            // set horizontal velocity
            m_Rigidbody2D.velocity = new Vector2(speed, verticalSpeed);

            // flip the character if facing wrong direction
            if (speed > 0 && !m_FacingRight)
            {
                Flip();
            }
            else if (speed < 0 && m_FacingRight)
            {
                Flip();
            }
        }

        // control jump
        if (m_Grounded && jumpInput) {
            m_Grounded = false;
            // jumpInput = false;
            m_NextGroundCheckTime = Time.time + m_NextGroundCheckLag;
            m_Rigidbody2D.velocity = new Vector2(speed, 0);
            m_Rigidbody2D.AddForce(new Vector2(0f, jumpForce));

            audioManager.PlaySFX(audioManager.jump);
        }
        
        //control wall jump
        if (!m_initiatedWallJump && m_readyforWallJump)
        {
            m_initiatedWallJump = true;
            m_readyforWallJump = false;
            //reset the vertical speed to 0
            m_Rigidbody2D.velocity = new Vector2(speed, 0);
            m_Rigidbody2D.AddForce(new Vector2(0f, jumpForce));

            audioManager.PlaySFX(audioManager.wall_jump);
        }

    }

    // reset the position, velocity and flags
    private void ResetPosition()
    {
        hp = maxhp;
        setHealthBar();
        m_Rigidbody2D.velocity = new Vector2(0, 0);
        transform.position = checkpoint;
        m_isSliding = false;
        isWallRunning = false;
        m_initiatedWallRun = false;
        m_Grounded = true;
    }


    // switch right and left
    private void Flip()
    {
        m_FacingRight = !m_FacingRight;
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(-1, 1, 1));
    }

    private void AdjustAnimationSpeed()
    {
        animator.speed = 1;
        AnimatorClipInfo[] currentClipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (currentClipInfo.Length > 0)
        {
            string clipName = currentClipInfo[0].clip.name;
            if (clipName == "Run" || clipName == "RunOnWall")   // adjust animation speed with running speed
            {
                animator.speed = Mathf.Max(Mathf.Sin(Mathf.PI * MathF.Abs(currentSpeed) / 2 / maxSpeed) * animationMaxSpeed, animationMinSpeed);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // This gets called once only. When this occurs, we must check which kind of trigger collision this is.
        if (other.gameObject.tag == "WallRun" && !m_canWallRun) {
            m_canWallRun = true;
            m_initiatedWallRun = false;
            m_wallRunEndTime = Time.time + maximumWallRunTime;
        }

        if (other.gameObject.tag == "Reset")
        {
            UnityEngine.Debug.Log("checkpoint");
            this.checkpoint = transform.position;
        }

        if (other.gameObject.tag == "DeadZone")
        {
            m_initiateReset = true;
        }

        if (other.gameObject.tag == "Fire")
        {
            // Avoid sustaining injuries
            if (Time.time < nextHurtTime)
                return;
            nextHurtTime = Time.time + hurtCD; 
            
            // reduce health
            hp--;
            setHealthBar();
            if (hp <= 0)
            {
                ResetPosition();
                return;
            }
            audioManager.PlaySFX(audioManager.hurt);
            // m_Rigidbody2D.velocity = Vector2.zero;
            m_Rigidbody2D.velocity = new Vector2(0, 0);

            horizontalInput = 0;
            jumpInput = false;

            // RESETTT????
            // m_initiateReset = true;
            
            // hit a little bit back
            IsInjuried = true;
            m_Rigidbody2D.AddForce(m_FacingRight ? injuryForce * new Vector2(-1, 1) : injuryForce);
            animator.SetBool("IsHurt", true);
            animator.SetBool("Hurting", true);
            StartCoroutine(ResetInjuredAnim());
            //UnityEngine.Debug.Log("Player touched fire!");
            
        }

        if (other.gameObject.tag == "Transmission")
        {
            // todo
        }

        if (other.CompareTag("Car"))
        {
            if (levelCompleteCanvas != null)
            {
                levelCompleteCanvas.SetActive(true);
                UnityEngine.Debug.Log("Level Complete: Canvas displayed.");

                // Hide the player by disabling the SpriteRenderer (if it's a 2D game)
                GetComponent<SpriteRenderer>().enabled = false;

                m_Rigidbody2D.velocity = new Vector2(0, 0);

                // Alternatively, deactivate the entire player GameObject
                // gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        // This gets called once only. When this occurs, we must check which kind of trigger exit this is
        UnityEngine.Debug.Log("Trigger Exit");
        if (other.gameObject.tag == "WallRun") {
            m_initiatedWallRun = false;
            m_initiatedWallJump = false;
            m_canWallRun = false;
        }
    }

    private IEnumerator ResetInjuredAnim()
    {
        yield return new WaitForSeconds(0.05f);
        animator.SetBool("IsHurt", false);
        yield return new WaitForSeconds(0.5f);
        while (m_Rigidbody2D.velocity.x > 0.00001f)
        {
            yield return null; // wait for next frame
        }
        m_Rigidbody2D.velocity = new Vector2(0, 0);
        IsInjuried = false;
        animator.SetBool("Hurting", false);
    }

    private void setHealthBar()
    {
        
        hpBar.sizeDelta = new Vector2((float)hp / (float)maxhp * hpBarMaxWidth, hpBar.sizeDelta.y);
        Debug.Log(hpBar.sizeDelta);
    }


}