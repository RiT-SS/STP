using UnityEngine;

public class ShellControl : MonoBehaviour
{
	private Transform player;
	private PlayerControl playerControl;

	private bool flag;

	private Animator ani;

	private BoxCollider2D shellBox;

	private void Start()
	{
		playerControl = PlayerControl.Instance;
		if (playerControl == null) { enabled = false; return; }
		player = playerControl.transform;
		shellBox = GetComponent<BoxCollider2D>();
		ani = GetComponent<Animator>();
	}

	private void Update()
	{
		if (!EventControl.isplay || EventControl.isPaused || player == null)
		{
			return;
		}
		Vector3 vector = new Vector3(-3.42f, 0.06f);
		float magnitude = (vector - base.transform.position).magnitude;
		if (magnitude <= 0.3f)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if (magnitude <= 2f)
		{
			float t = 1f - Mathf.Pow(1f - 0.02f, Time.deltaTime * 60f);
			Vector2 vector2 = Vector2.Lerp(base.transform.position, vector, t);
			base.transform.position = new Vector3(vector2.x, vector2.y, base.transform.position.z);
			if (flag)
			{
				base.transform.localScale = new Vector3(Mathf.MoveTowards(base.transform.localScale.x, 0f, 0.04f * 60f * Time.deltaTime), Mathf.MoveTowards(base.transform.localScale.y, 0f, 0.04f * 60f * Time.deltaTime));
			}
			else
			{
				base.transform.localScale = new Vector3(Mathf.MoveTowards(base.transform.localScale.x, 0f, 0.01f * 60f * Time.deltaTime), Mathf.MoveTowards(base.transform.localScale.y, 0f, 0.01f * 60f * Time.deltaTime));
			}
			return;
		}
		if (flag)
		{
			if (ani.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
			{
				Object.Destroy(base.gameObject);
			}
			base.transform.position = new Vector3(base.transform.position.x - GroundControl.Speed * Time.deltaTime, base.transform.position.y);
			return;
		}
		Vector3 oldPosition = transform.position;
		base.transform.position = new Vector3(base.transform.position.x - GroundControl.Speed * Time.deltaTime * 2f, base.transform.position.y);
		if (PlayerControl.Hp == 0)
		{
			return;
		}
		BoxCollider2D box = playerControl.box;
		if (!(box != null))
		{
			return;
		}
		Bounds shellBounds = GetBounds(shellBox, oldPosition);
		shellBounds.Encapsulate(GetBounds(shellBox, transform.position));
		if (shellBounds.Intersects(GetBounds(box, player.position)))
		{
			if (!PlayerControl.isRage) playerControl.Damage();
			flag = true;
			ani.enabled = true;
			base.transform.localScale = new Vector3(4f, 4f);
		}
	}

	private static Bounds GetBounds(BoxCollider2D box, Vector3 position)
	{
		Vector3 center = box.transform.TransformPoint(box.offset) + position - box.transform.position;
		Vector3 right = box.transform.TransformVector(new Vector3(box.size.x / 2f, 0f, 0f));
		Vector3 up = box.transform.TransformVector(new Vector3(0f, box.size.y / 2f, 0f));
		Vector3 size = new Vector3((Mathf.Abs(right.x) + Mathf.Abs(up.x)) * 2f, (Mathf.Abs(right.y) + Mathf.Abs(up.y)) * 2f, 1f);
		return new Bounds(center, size);
	}
}
