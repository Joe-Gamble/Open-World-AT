using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;
    public Animator animator;

    [Header("Player Attributes")]

    [Range(10f, 100f)]
    public float mouseSensitivity = 70f;
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float speed = 4f;

    public float turn_smooth_time = 0.1f;
    private float turn_smooth_velocity;

    float horizontal;
    float vertical;
    Vector3 direction;


    public Transform ground;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    public float distToGround = 0.1f;
    public LayerMask groundMask;

    bool isGrounded = true;
    Vector3 velocity;
    bool jumping = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    void GroundCheck()
    {

        isGrounded = Physics.CheckSphere(ground.position, distToGround, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

    }

    void GetInputs()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            speed = runSpeed;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            speed = walkSpeed;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumping = true;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumping = false;
        }
    }

    void SetAnimatorVariables()
    {
        animator.SetFloat("Speed", speed);
        animator.SetFloat("Vertical", Mathf.Abs(direction.z));
        animator.SetFloat("Horizontal", Mathf.Abs(direction.x));
        animator.SetBool("isRunning", Input.GetKey(KeyCode.LeftShift));
        animator.SetBool("isJumping", jumping);

    }

    // Update is called once per frame
    void Update()
    {
        GroundCheck();
        GetInputs();

        SetAnimatorVariables();

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turn_smooth_velocity, turn_smooth_time);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);

        }

        if (jumping && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
