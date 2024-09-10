using UnityEditor;
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
	[SerializeField] private Transform visualTransform;
	[SerializeField] private CrabShell currentShell;
	[SerializeField] private Transform shellHolder;
	

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

		TryUpdateShellParent();
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
		movement = flatCameraTransform.right * movement.x + flatCameraTransform.forward * movement.z;
		characterRigidBody.AddForce(movement, ForceMode.Force);
		//characterRigidBody.AddForce(flatCameraTransform.forward * movement.z, ForceMode.Force);
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
		// Hmmmmmm seems to be having trouble with 180deg changes
		Vector3 lerpedDirection = Vector3.LerpUnclamped(visualTransform.forward, direction.normalized, 0.2f);
		visualTransform.LookAt(lerpedDirection + visualTransform.position);
	}

	CrabShell FindClosestCrabShell()
	{
		CrabShell closestCrabShell = null;
		RaycastHit[] colliders = Physics.BoxCastAll(visualTransform.position + visualTransform.forward * 2, Vector3.one, visualTransform.forward);
		Debug.DrawLine(visualTransform.position, visualTransform.position + visualTransform.forward, Color.red,1);
		foreach (RaycastHit hit in colliders)
		{
			CrabShell shell = hit.collider.gameObject.GetComponent<CrabShell>();
			if (shell != null)
			{
				Vector3 toHit = hit.collider.gameObject.transform.position - visualTransform.position;
				float closeness = Vector3.Dot(visualTransform.forward, toHit);
				Debug.Log(hit.collider.gameObject.name);
				closestCrabShell = shell;
			}
		}
		return closestCrabShell;
	}

	void TryUpdateShellParent()
	{
        if (currentShell != null)
        {
			currentShell.gameObject.transform.parent = shellHolder;
			currentShell.gameObject.transform.position = shellHolder.position + currentShell.positionOffset;
			currentShell.gameObject.transform.rotation = shellHolder.rotation;
        }
    }

	void EnterEmptyShell(CrabShell shell)
	{
		currentShell = shell;
		shell.gameObject.GetComponent<Rigidbody>().isKinematic = true;
		shell.gameObject.GetComponent<Collider>().enabled = false;
	}

	void OnPrimaryAttack(InputAction.CallbackContext context)
    {
    }


	/// <summary>
	/// Try to enter an unoccupied shell. If already in a shell, hide within it
	/// </summary>
	void OnEnterShell(InputAction.CallbackContext context)
	{
		bool isAlreadyInShell = currentShell != null;

		if(isAlreadyInShell)
		{

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

	void OnExitShell(InputAction.CallbackContext context)
	{
		if(currentShell != null)
		{
			currentShell.gameObject.transform.parent = null;
			currentShell.gameObject.GetComponent<Rigidbody>().isKinematic = false;
			currentShell.gameObject.GetComponent<Collider>().enabled = true;
			currentShell = null;
		}
	}

    void OnJump(InputAction.CallbackContext context)
    {

    }
}
