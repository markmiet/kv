﻿using System;
using UnityEngine;

public class PelaajaController : MonoBehaviour
{
    //private LevelManager t_LevelManager;
    public Transform m_GroundCheck1, m_GroundCheck2;
    public float groundCheckRadius1, groundCheckRadius2;
    private GameObject m_StompBox;
    public Animator m_Animator;
    private Rigidbody2D m_Rigidbody2D;
    //public CapsuleCollider2D biggerCircleCollider2D;

    public LayerMask GroundLayers;
    public GameObject Fireball;
    public Transform FirePos;
    private float waitBetweenFire = .2f;
    private float fireTime1, fireTime2;

    private float faceDirectionX;
    private float moveDirectionX;
    private float normalGravity;

    private float currentSpeedX;
    private float speedXBeforeJump;

    private float minWalkSpeedX = .28f;
    private float walkAccelerationX = .14f;
    private float runAccelerationX = .21f;
    private float releaseDecelerationX = .25f; // original: .19f;
    private float skidDecelerationX = .5f; // .38f;
    private float skidTurnaroundSpeedX = 3.5f; // 2.11;
    private float maxWalkSpeedX = 5.86f;
    private float maxRunSpeedX = 9.61f;

    private float jumpSpeedY;
    private float jumpUpGravity;
    private float jumpDownGravity;
    private float midairAccelerationX;
    private float midairDecelerationX;

    private float automaticWalkSpeedX;
    private float automaticGravity;

    public float castleWalkSpeedX = 5.86f;
    public float levelEntryWalkSpeedX = 3.05f;

    private bool isGrounded;
    private bool isDashing;
    private bool isFalling;
    private bool isJumping;
    private bool isChangingDirection;
    private bool wasDashingBeforeJump;
    private bool isShooting;
    public bool isCrouching;

    private bool jumpButtonHeld;
    private bool jumpButtonReleased;

    public bool inputFreezed;

    private SpriteRenderer[] spriteRenderer;

    // Use this for initialization
    void Start()
    {
        //t_LevelManager = FindObjectOfType<LevelManager>();
        //m_GroundCheck1 = transform.FindChild("Ground Check 1");
        //m_GroundCheck2 = transform.FindChild("Ground Check 2");
        //m_StompBox = transform.FindChild("Stomp Box").gameObject;
        //m_Animator = GetComponent<Animator>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        //m_CircleCollider2D = GetComponent<CircleCollider2D>();
        normalGravity = m_Rigidbody2D.gravityScale;
        spriteRenderer = GetComponentsInChildren<SpriteRenderer>();
        //spriteRenderer = GetComponent<SpriteRenderer>();

        // Drop Mario at spawn position
        //transform.position = FindObjectOfType<LevelManager>().FindSpawnPosition();

        // Set correct size
        UpdateSize();

        jumpButtonReleased = true;
        fireTime1 = 0;
        fireTime2 = 0;
    }


    /****************** Movement control */
    void SetJumpParams()
    {
        if (currentSpeedX < 3.75f)
        {
            jumpSpeedY = 6f;
            jumpUpGravity = .47f;
            jumpDownGravity = 1.64f;
        }
        else if (currentSpeedX < 8.67f)
        {
            jumpSpeedY = 5f;
            jumpUpGravity = .44f;
            jumpDownGravity = 1.41f;
        }
        else
        {
            jumpSpeedY = 8f;
            jumpUpGravity = .59f;
            jumpDownGravity = 2.11f;
        }
    }

    void SetMidairParams()
    {
        if (currentSpeedX < 5.86f)
        {
            midairAccelerationX = .14f;
            if (speedXBeforeJump < 6.80f)
            {
                midairDecelerationX = .14f;
            }
            else
            {
                midairDecelerationX = .19f;
            }
        }
        else
        {
            midairAccelerationX = .21f;
            midairDecelerationX = .21f;
        }
    }


    void FixedUpdate()
    {
        /******** Horizontal movement on ground */
        if (isGrounded)
        {
            Debug.Log("is grounded");
            // If holding directional button, accelerate until reach max walk speed
            // If holding Dash, accelerate until reach max run speed
            if (faceDirectionX != 0)
            {
                if (currentSpeedX == 0)
                {
                    currentSpeedX = minWalkSpeedX;
                }
                else if (currentSpeedX < maxWalkSpeedX)
                {
                    currentSpeedX = IncreaseWithinBound(currentSpeedX, walkAccelerationX, maxWalkSpeedX);
                }
                else if (isDashing && currentSpeedX < maxRunSpeedX)
                {
                    currentSpeedX = IncreaseWithinBound(currentSpeedX, runAccelerationX, maxRunSpeedX);
                }
            }

            // Decelerate upon release of directional button
            else if (currentSpeedX > 0)
            {
                currentSpeedX = DecreaseWithinBound(currentSpeedX, releaseDecelerationX, 0);
            }

            // If change direction, skid until lose all momentum then turn around
            if (isChangingDirection)
            {
                if (currentSpeedX > skidTurnaroundSpeedX)
                {
                    moveDirectionX = -faceDirectionX;
                    //m_Animator.SetBool("isSkidding", true);
                    currentSpeedX = DecreaseWithinBound(currentSpeedX, skidDecelerationX, 0);
                }
                else
                {
                    moveDirectionX = faceDirectionX;
                    //m_Animator.SetBool("isSkidding", false);
                }
            }
            else
            {
                //m_Animator.SetBool("isSkidding", false);
            }

            // Freeze horizontal movement while crouching
            if (isCrouching)
            {
                currentSpeedX = 0;
            }


            /******** Horizontal movement on air */
        }
        else
        {
            SetMidairParams();

            // Holding Dash while in midair has no effect
            if (faceDirectionX != 0)
            {
                if (currentSpeedX == 0)
                {
                    currentSpeedX = minWalkSpeedX;
                }
                else if (currentSpeedX < maxWalkSpeedX)
                {
                    currentSpeedX = IncreaseWithinBound(currentSpeedX, midairAccelerationX, maxWalkSpeedX);
                }
                else if (wasDashingBeforeJump && currentSpeedX < maxRunSpeedX)
                {
                    currentSpeedX = IncreaseWithinBound(currentSpeedX, midairAccelerationX, maxRunSpeedX);
                }
            }
            else if (currentSpeedX > 0)
            {
                currentSpeedX = DecreaseWithinBound(currentSpeedX, releaseDecelerationX, 0);
            }

            // If change direction, decelerate but keep facing move direction
            if (isChangingDirection)
            {
                faceDirectionX = moveDirectionX;
                currentSpeedX = DecreaseWithinBound(currentSpeedX, midairDecelerationX, 0);
            }
        }


        /******** Vertical movement */
        if (isGrounded)
        {
            isJumping = false;
            m_Rigidbody2D.gravityScale = normalGravity;
        }

        if (!isJumping)
        {
            if (isGrounded && jumpButtonHeld && jumpButtonReleased)
            {
                SetJumpParams();
                //Debug.Log("SetJumpParams=" + jumpButtonReleased);
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, jumpSpeedY);
                isJumping = true;
                jumpButtonReleased = false;
                speedXBeforeJump = currentSpeedX;
                wasDashingBeforeJump = isDashing;
                /*
                if (t_LevelManager.marioSize == 0)
                {
                    t_LevelManager.soundSource.PlayOneShot(t_LevelManager.jumpSmallSound);
                }
                else
                {
                    t_LevelManager.soundSource.PlayOneShot(t_LevelManager.jumpSuperSound);
                }
                */
            }
        }
        else
        {
            Debug.Log("jumping");
            // lower gravity if Jump button held; increased gravity if released
            if (m_Rigidbody2D.velocity.y > 0 && jumpButtonHeld)
            {
                m_Rigidbody2D.gravityScale = normalGravity * jumpUpGravity;
            }
            else
            {
                m_Rigidbody2D.gravityScale = normalGravity * jumpDownGravity;
            }
        }


        // Disable Stomp Box if not falling down
        // Disable Circle Collider if falling down (to prevent multi collisions registered)
        /*
        if (!isFalling)
        {
            //m_StompBox.SetActive(false);
            biggerCircleCollider2D.enabled = true;
        }
        else
        {
            //m_StompBox.SetActive(true);
            biggerCircleCollider2D.enabled = false;
        }
      */


        /******** Horizontal orientation */
        /*
        if (faceDirectionX > 0)
        {
            transform.localScale = new Vector2(1, 1); // facing right
        }
        else if (faceDirectionX < 0)
        {
            transform.localScale = new Vector2(-1, 1);
        }
        */

        /******** Reset params for automatic movement */
        if (inputFreezed)
        {
            currentSpeedX = automaticWalkSpeedX;
            m_Rigidbody2D.gravityScale = automaticGravity;
        }

        /******** Shooting */
        /*
        if (isShooting && t_LevelManager.marioSize == 2)
        {
            fireTime2 = Time.time;

            if (fireTime2 - fireTime1 >= waitBetweenFire)
            {
                m_Animator.SetTrigger("isFiring");
                GameObject fireball = Instantiate(Fireball, FirePos.position, Quaternion.identity);
                fireball.GetComponent<MarioFireball>().directionX = transform.localScale.x;
                t_LevelManager.soundSource.PlayOneShot(t_LevelManager.fireballSound);
                fireTime1 = Time.time;
            }
        }
        */

        /******** Set params */
        m_Rigidbody2D.velocity = new Vector2(moveDirectionX * currentSpeedX, m_Rigidbody2D.velocity.y);

        //m_Animator.SetBool("isJumping", isJumping);
        //m_Animator.SetBool("isFallingNotFromJump", isFalling && !isJumping);
        //m_Animator.SetBool("isCrouching", isCrouching);
        //7m_Animator.SetFloat("absSpeed", Mathf.Abs(currentSpeedX));

        if (faceDirectionX != 0 && !isChangingDirection)
        {
            moveDirectionX = faceDirectionX;
        }

    }


    /****************** Automatic movement sequences */
    void Update()
    {

        faceDirectionX = Input.GetAxisRaw("Horizontal"); // > 0 for right, < 0 for left
                                                         //isDashing = Input.GetButton("Dash");
                                                         //isCrouching = Input.GetButton("Crouch");
                                                         //isShooting = Input.GetButtonDown("Dash");
        jumpButtonHeld = Input.GetButton("Jump");
        if (Input.GetButtonUp("Jump"))
        {
            jumpButtonReleased = true;
            //7Debug.Log("isGrounded=" + isGrounded);

            //Debug.Log("jumpButtonHeld=" + jumpButtonHeld);

            //Debug.Log("jumpButtonReleased=" + jumpButtonReleased);


        }


        isFalling = m_Rigidbody2D.velocity.y < 0 && !isGrounded;
        isGrounded = Physics2D.OverlapCircle(m_GroundCheck1.position, groundCheckRadius1, GroundLayers) ||
            Physics2D.OverlapCircle(m_GroundCheck2.position, groundCheckRadius2, GroundLayers);

        //isTouchingGround = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

        /*
        if (isGrounded)
        {
            Debug.Log("maassa");
        }
        */

        //isChangingDirection = currentSpeedX > 0 && faceDirectionX * moveDirectionX < 0;

        //m_Animator.SetFloat("speed", Mathf.Abs(currentSpeedX));
        m_Animator.SetBool("isGrounded", isGrounded);

        m_Animator.SetFloat("speed", Mathf.Abs(m_Rigidbody2D.velocity.x));

        Debug.Log("Mathf.Abs(m_Rigidbody2D.velocity.x)=" + Mathf.Abs(m_Rigidbody2D.velocity.x));

        if (faceDirectionX == -1)
        {
            m_Animator.SetBool("faceDirectionX", true);
        }
        else if (faceDirectionX == 1)
        {
            m_Animator.SetBool("faceDirectionX", false);

        }


        /*
        if (currentSpeedX<-0.01)
        {
            spriteRenderer.flipX = true;
        }
        else if (currentSpeedX > 0.01)
        {
            //m_Animator.SetBool("mirror", false);
        }
        */
        /*
        if (m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Ukkoidle") ||
            m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Ukkojumppaikaltaan"))
        {
            Debug.Log("ukkoidlee");
            foreach (SpriteRenderer value in spriteRenderer)
            {
                value.flipX = false;
            }
        }

        else 
        */

        if (faceDirectionX == -1)
        {
            foreach (SpriteRenderer value in spriteRenderer)
            {
                value.flipX = true;
            }

        }
        else if (faceDirectionX == 1)
        {
            foreach (SpriteRenderer value in spriteRenderer)
            {
                value.flipX = false;

            }
        }

        Debug.Log("idle=" + m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Ukkoidle"));


    }


    float IncreaseWithinBound(float val, float delta, float maxVal = Mathf.Infinity)
    {
        val += delta;
        if (val > maxVal)
        {
            val = maxVal;
        }
        return val;
    }

    float DecreaseWithinBound(float val, float delta, float minVal = 0)
    {
        val -= delta;
        if (val < minVal)
        {
            val = minVal;
        }
        return val;
    }

    public void UpdateSize()
    {
        //GetComponent<Animator>().SetInteger("marioSize", FindObjectOfType<LevelManager>().marioSize);
    }


}