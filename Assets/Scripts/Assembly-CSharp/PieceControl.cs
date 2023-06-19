using UnityEngine;

public class PieceControl : MonoBehaviour
{
	public float spreadTime = 0.18f;
	public float absorbTime = 0.72f;
	private float EpNow;
	private bool scattered;
	private Vector3 startPosition;
	private Vector3 spreadPosition;
	private Vector3 controlPosition;
	private Vector3 holePosition;
	private Vector3 startScale;
	private Quaternion startRotation;
	private MeshRenderer render;
	private MaterialPropertyBlock color;

	public bool IsScattered { get { return scattered; } }

	public void Scatter(Vector3 center, Vector3 hole, int index, int count)
	{
		scattered = true;
		EpNow = 0f;
		startPosition = transform.position;
		startScale = transform.localScale;
		startRotation = transform.rotation;
		holePosition = new Vector3(hole.x, hole.y, startPosition.z);
		Vector3 away = startPosition - center;
		away.z = 0f;
		if (away.sqrMagnitude < 0.001f)
		{
			float angle = index * Mathf.PI * 2f / Mathf.Max(1, count);
			away = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.15f;
		}
		// 先把原来拼在一起的碎片拉开，保留大小，再开始吸入。
		spreadPosition = startPosition + Vector3.ClampMagnitude(away * 0.18f + away.normalized * 0.025f, 0.16f);
		Vector3 direction = spreadPosition - holePosition;
		Vector3 tangent = new Vector3(-direction.y, direction.x, 0f).normalized;
		controlPosition = (spreadPosition + holePosition) * 0.5f + tangent * 0.22f;
		absorbTime += index % 4 * 0.045f;
		Rigidbody2D body = GetComponent<Rigidbody2D>();
		if (body != null) body.simulated = false;
		render = GetComponent<MeshRenderer>();
		color = new MaterialPropertyBlock();
	}

	private void Update()
	{
		if (!EventControl.isplay || EventControl.isPaused) return;
		EpNow += Time.deltaTime;
		if (scattered)
		{
			if (EpNow < spreadTime)
			{
				float t = Mathf.Clamp01(EpNow / spreadTime);
				float move = 1f - Mathf.Pow(1f - t, 3f);
				transform.position = Vector3.Lerp(startPosition, spreadPosition, move);
				transform.localScale = startScale * Mathf.Lerp(1f, 0.96f, t);
				transform.rotation = startRotation;
				return;
			}
			float progress = Mathf.Clamp01((EpNow - spreadTime - 0.03f) / absorbTime);
			float u = progress * progress;
			transform.position = (1f - u) * (1f - u) * spreadPosition + 2f * (1f - u) * u * controlPosition + u * u * holePosition;
			float shrink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, progress));
			transform.localScale = startScale * (0.96f * (1f - shrink));
			transform.rotation = startRotation;
			color.SetColor("_Color", new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(0.85f, 1f, progress)));
			render.SetPropertyBlock(color);
			if (progress >= 1f) Destroy(gameObject);
			return;
		}
		if (EpNow > 0.1f)
		{
			Vector3 center = new Vector3(-3.3f, 0.05f, transform.position.z);
			float distance = Vector2.Distance(center, transform.position);
			if (distance <= 0.3f) Destroy(gameObject);
			else if (distance <= 3f)
			{
				Rigidbody2D body = GetComponent<Rigidbody2D>();
				if (body != null) Destroy(body);
				transform.position = Vector3.Lerp(transform.position, center, 1f - Mathf.Pow(0.98f, Time.deltaTime * 60f));
			}
		}
		if (transform.position.y < -10f) Destroy(gameObject);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (scattered) return;
		if (collision.name.Contains("Die") || collision.name.Contains("player") || collision.name.Contains("coin")) Destroy(gameObject);
	}
}
