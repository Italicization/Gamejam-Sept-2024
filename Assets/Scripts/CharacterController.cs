using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour
{
    private PlayerInput input;
    private PlayerControls controls;
    private InputAction Move;
    private Vector2 currentMovementInput;
    private Vector2 currentLookInput;
    [SerializeField] private Vector3 cameraOffset = new(1,1,1);
    private float cameraXRotation;
	private Transform cameraTransform;
	[SerializeField] private Transform flatCameraTransform;

	private InputAction Look;
    private InputAction Fire;
	private InputAction Enter;
    private InputAction Exit;
    private InputAction Jump;
    private Rigidbody characterRigidBody;
    [SerializeField, Tooltip("Horizontal force the character is moved at")] private float movementForce;
    [SerializeField] private float lookSensitivity = 10;

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
		Exit.performed += OnExitShell;

        Jump = controls.Player.Jump;
        Jump.performed += OnJump;
	}

	private void Start()
    {
        input = GetComponent<PlayerInput>();
		cameraTransform = input.camera.transform;
		characterRigidBody = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
	}

	// Update is called once per frame
	void Update()
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
		//Vector3 lookRelative = Vector3.ProjectOnPlane(input.camera.transform.forward, Vector3.up);
		Vector3 movement = new(moveDirection.x, 0, moveDirection.y);
		characterRigidBody.AddForce(flatCameraTransform.right * movement.x, ForceMode.Force);
		characterRigidBody.AddForce(flatCameraTransform.forward * movement.z, ForceMode.Force);
		RotateTowardsMoveDirection(new(moveDirection.x,0,moveDirection.y));
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

	void RotateTowardsMoveDirection(Vector3 direction)
	{
		Quaternion targetRotation = Quaternion.LookRotation(direction);	
		float rotationDifference = Quaternion.Angle(targetRotation, transform.rotation);
		characterRigidBody.rotation *= Quaternion.Euler(0,rotationDifference,0);
	}

	void OnPrimaryAttack(InputAction.CallbackContext context)
    {
    }

	void OnEnterShell(InputAction.CallbackContext context)
	{

	}

	void OnExitShell(InputAction.CallbackContext context)
	{

	}

    void OnJump(InputAction.CallbackContext context)
    {

    }
}
