using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls character movement and camera input
/// </summary>
public class CharacterController : MonoBehaviour
{
    [SerializeField] private CrabShell currentShell;

    [Header("Character attributes")]
    [SerializeField, Tooltip("Where is the camera positioned relative to the character's position?")] private Vector3 cameraOffset = new(1, 1, 1);
    [SerializeField, Tooltip("How much vertical velocity will the basic jump add?")] private float jumpVelocity;
    [SerializeField, Tooltip("How much velocity will the shell jump add?")] private float maxShellJumpVelocity;
    [SerializeField, Tooltip("Minimum speed the shell jump gives you")] private float minShellJumpVelocity;
    [SerializeField, Tooltip("Amount of seconds the character has to charge to perform a shell jump")] private float shellJumpChargeTime;
    [SerializeField, Tooltip("Horizontal force the character is moved at")] private float movementForce;
    [SerializeField] private float maxDrag;
    [SerializeField, Tooltip("Mouse sensitivity")] private float lookSensitivity = 10;
    [SerializeField, Tooltip("The range of how far the character can interact with objects, such as attacking and eating")] private float interactionRange = 1;
    [SerializeField, Tooltip("Amount of seconds the jump input will remain active for while in the air")] float jumpBufferDuration = 0.4f;

    [Header("Required fields")]
    [SerializeField, Tooltip("Flattened transform that follows the camera")] private Transform flatCameraTransform;
    [SerializeField, Tooltip("Transform of the character model")] private Transform visualTransform;
    [SerializeField, Tooltip("Parent transform where shells will be placed")] private Transform shellHolder;

    private Vector2 currentMovementInput;
    private Vector2 currentLookInput;
    private float cameraXRotation;
    private Transform cameraTransform;
    private Rigidbody characterRigidBody;
    private bool isGrounded;
    private Collider characterCollider;
    Coroutine shellEjectCharger;


    private PlayerInput input;
    private PlayerControls controls;
    private InputAction Move;
    private InputAction Look;
    private InputAction Fire;
    private InputAction Enter;
    private InputAction Exit;
    private InputAction Jump;

    private bool IsGrounded
    {
        get
        {
            Collider[] hitColliders = Physics.OverlapSphere(characterCollider.bounds.min, characterCollider.bounds.extents.x);
            foreach (Collider collider in hitColliders)
                if (collider != characterCollider)
                    return true;

            return false;
        }
    }

    private void Awake()
    {
        controls = new();
        controls.Enable();
        Move = controls.Player.Move;
        Look = controls.Player.Look;

        Fire = controls.Player.Fire;
        Fire.performed += OnPrimaryAttack;

        Enter = controls.Player.Enter;
        Enter.performed += OnEnterShell;
        Exit = controls.Player.Exit;
        Exit.performed += OnExitPressed;

        Jump = controls.Player.Jump;
        Jump.performed += OnJump;
    }

    private void Start()
    {
        input = GetComponent<PlayerInput>();
        cameraTransform = input.camera.transform;
        characterRigidBody = GetComponent<Rigidbody>();
        characterCollider = GetComponent<Collider>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    private void Update()
    {
        currentMovementInput = Move.ReadValue<Vector2>();
        currentLookInput = Look.ReadValue<Vector2>();

        CharacterMove(currentMovementInput);
        CameraLook(currentLookInput);
    }

    private void LateUpdate()
    {
        UpdateCameraOffset();
    }

    private void CharacterMove(Vector2 moveDirection)
    {
        float moveMultiplier = Time.deltaTime * movementForce;
        moveDirection *= moveMultiplier;

        //Make direction camera relative
        Vector3 movement = new(moveDirection.x, 0, moveDirection.y);
        movement = flatCameraTransform.right * movement.x + flatCameraTransform.forward * movement.z;
        characterRigidBody.AddForce(movement, ForceMode.Force);
        RotateTowardsMoveDirection(movement);
    }

    private void CameraLook(Vector2 lookDirection)
    {
        float multiplier = lookSensitivity * Time.deltaTime;
        float horizontalDirection = lookDirection.x * multiplier;
        float verticalDirection = -lookDirection.y * multiplier;
        cameraXRotation = Mathf.Clamp(cameraXRotation + verticalDirection, -90, 90);

        cameraTransform.eulerAngles += Vector3.up * horizontalDirection + Vector3.right * verticalDirection;
        cameraTransform.eulerAngles = new(cameraXRotation, cameraTransform.eulerAngles.y, cameraTransform.eulerAngles.z);
    }

    private void UpdateCameraOffset()
    {
        //TODO: IGNORE SHELLS IN RAYCAST
        Vector3 idealPosition = transform.position + cameraTransform.right * cameraOffset.x + cameraTransform.up * cameraOffset.y + cameraTransform.forward * cameraOffset.z;
        // Ray from target to gameObject's ideal position
        Debug.DrawRay(transform.position, (idealPosition - transform.position), Color.cyan);

        if (Physics.Raycast(new Ray(transform.position, (idealPosition - transform.position)), out RaycastHit hit, cameraOffset.magnitude))
            cameraTransform.position = Vector3.Lerp(transform.position, idealPosition, hit.distance / (idealPosition - transform.position).magnitude);
        else
            cameraTransform.position = idealPosition;

        flatCameraTransform.position = cameraTransform.position;
        flatCameraTransform.localEulerAngles = new(0, cameraTransform.localEulerAngles.y, cameraTransform.localEulerAngles.z);
    }

    private void RotateTowardsMoveDirection(Vector3 direction)
    {
        // Hmmmmmm seems to be having trouble with 180deg changes
        Vector3 lerpedDirection = Vector3.LerpUnclamped(visualTransform.forward, direction.normalized, 0.2f);
        visualTransform.LookAt(lerpedDirection + visualTransform.position);
    }

    CrabShell FindClosestCrabShell()
    {
        CrabShell closestCrabShell = null;
        Physics.OverlapBox(visualTransform.position, Vector3.one);
        RaycastHit[] colliders = Physics.BoxCastAll(visualTransform.position + visualTransform.forward * 2, Vector3.one, visualTransform.forward);
        Debug.DrawLine(visualTransform.position, visualTransform.position + visualTransform.forward, Color.red, 1);
        foreach (RaycastHit hit in colliders)
        {
            float closeness = interactionRange;
            if (hit.collider.gameObject.TryGetComponent(out CrabShell shell))
            {

                Vector3 toHit = hit.collider.gameObject.transform.position - visualTransform.position;
                float currentCloseness = Vector3.Dot(visualTransform.forward, toHit);
                if (currentCloseness < closeness && 0 <= currentCloseness)
                {
                    closestCrabShell = shell;
                }
                closeness = currentCloseness;
                Debug.Log(hit.collider.gameObject.name);
                closestCrabShell = shell;
            }
        }
        return closestCrabShell;
    }

    private void EnterEmptyShell(CrabShell shell)
    {
        currentShell = shell;
        currentShell.EnterShell(shellHolder);
    }

    private void OnPrimaryAttack(InputAction.CallbackContext context)
    {
    }

    /// <summary>
    /// Try to enter an unoccupied shell. If already in a shell, hide within it
    /// </summary>
    private void OnEnterShell(InputAction.CallbackContext context)
    {
        bool isAlreadyInShell = currentShell != null;

        if (isAlreadyInShell)
        {
            //Do some sort of crouch things here?
        }
        else
        {
            currentShell = FindClosestCrabShell();
            if (currentShell != null)
            {
                if (!currentShell.HasContents)
                    EnterEmptyShell(currentShell);
            }
        }
    }
    
    private void ShellEjectJump()
    {
        characterRigidBody.drag = 0;
        float verticalComponent;
        verticalComponent = Input.GetKey(KeyCode.Space) ? 1 : 0;
        characterRigidBody.velocity = new(characterRigidBody.velocity.x, 0, characterRigidBody.velocity.z);
        // Dependent on character's visual forward instead of input forward. Gross.
        characterRigidBody.velocity = Vector3.zero;
        characterRigidBody.AddForce(launchSpeed * (visualTransform.forward + Vector3.up * verticalComponent).normalized, ForceMode.VelocityChange);
    }

    private void ReleaseExitShellJump(InputAction.CallbackContext context)
    {
        ShellEjectJump();
        currentShell.ExitShell(shellHolder);
        currentShell = null;
        Exit.canceled -= ReleaseExitShellJump;
        if (shellEjectCharger != null)
        {
            StopCoroutine(shellEjectCharger);
            shellEjectCharger = null;
            launchSpeed = 0;
            characterRigidBody.drag = 0;
        }
    }

    private void OnExitPressed(InputAction.CallbackContext context)
    {
        if (currentShell != null)
        {
            Exit.canceled += ReleaseExitShellJump;
            shellEjectCharger = StartCoroutine(ChargeLaunchSpeed());
        }
    }

    float launchSpeed = 0;
    private IEnumerator ChargeLaunchSpeed()
    {
        float timeElapsed = 0;
        while (timeElapsed <= shellJumpChargeTime)
        {
            timeElapsed += Time.deltaTime;
            launchSpeed = Mathf.Lerp(minShellJumpVelocity, maxShellJumpVelocity, timeElapsed / shellJumpChargeTime);
            characterRigidBody.drag = maxDrag * timeElapsed / shellJumpChargeTime;
            yield return null;
        }
    }

    Coroutine jumpCoroutine;
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!TryJump())
        {
            if (jumpCoroutine != null)
                StopCoroutine(jumpCoroutine);

            jumpCoroutine = StartCoroutine(JumpBuffer());
        }
    }

    private bool TryJump()
    {
        isGrounded = IsGrounded;
        if (isGrounded)
        {
            Vector3 startingVelocity = characterRigidBody.velocity;
            characterRigidBody.velocity = new(startingVelocity.x, jumpVelocity, startingVelocity.z);
        }

        return isGrounded;
    }

    private IEnumerator JumpBuffer()
    {
        for (float i = 0; i < jumpBufferDuration; i += Time.deltaTime)
        {
            if (!TryJump())
                yield return null;
            else
                break;
        }
    }
}
