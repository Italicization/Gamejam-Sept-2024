using UnityEngine;

/// <summary>
/// A shell the crabbies can put on and hide in :)
/// </summary>
public class CrabShell : MonoBehaviour
{
	public bool HasContents { get; private set; } = false;
	public Vector3 PositionOffset;
	public Rigidbody ShellRigidBody { get; private set; }
	private Transform ShellParent;
	private Collider shellCollider;

	// Start is called before the first frame update
	void Start()
	{
		ShellRigidBody = GetComponent<Rigidbody>();
		shellCollider = GetComponent<Collider>();
	}

	private void Update()
	{
		if (ShellParent != null)
		{
			transform.parent = ShellParent;
			transform.SetPositionAndRotation(ShellParent.position + PositionOffset, ShellParent.rotation);
		}
	}

	public void EnterShell(Transform shellParent)
	{
		ShellParent = shellParent;
		ShellRigidBody.isKinematic = true;
		shellCollider.enabled = false;
	}

	public void ExitShell(Transform shellParent)
	{
		transform.parent = null;
		ShellRigidBody.isKinematic = false;
		shellCollider.enabled = true;
		ShellParent = null;
	}
}
