using UnityEngine;

public class ObstacleControl : MonoBehaviour
{
	public enum ObstacleType
	{
		Terrain = 0,
		Obstacle = 1,
		Creature = 2
	}

	public ObstacleType type = ObstacleType.Obstacle;

	private bool disappearing;
	private bool nearHole;
	private float elapsed;
	private Vector3 startPosition;
	private Vector3 holePosition;
	private SpriteRenderer[] images;
	private Color[] colors;

	public void Disappear()
	{
		// 生物保留完整轮廓，只缩小淡出，不生成身体碎片。
		GameObject whole = new GameObject(name + " whole");
		whole.transform.position = transform.position;
		foreach (SpriteRenderer render in GetComponentsInChildren<SpriteRenderer>())
		{
			if (!render.enabled || render.sprite == null) continue;
			GameObject copy = new GameObject(render.name);
			copy.transform.position = render.transform.position;
			copy.transform.rotation = render.transform.rotation;
			copy.transform.localScale = render.transform.lossyScale;
			copy.transform.SetParent(whole.transform, true);
			SpriteRenderer image = copy.AddComponent<SpriteRenderer>();
			image.sprite = render.sprite;
			image.sharedMaterial = render.sharedMaterial;
			image.color = render.color;
			image.flipX = render.flipX;
			image.flipY = render.flipY;
			image.sortingLayerID = render.sortingLayerID;
			image.sortingOrder = render.sortingOrder;
			image.maskInteraction = render.maskInteraction;
		}
		ObstacleControl effect = whole.AddComponent<ObstacleControl>();
		effect.type = ObstacleType.Creature;
		effect.disappearing = true;
		effect.startPosition = whole.transform.position;
		GameObject hole = GameObject.Find("darkhole");
		effect.nearHole = hole != null && Mathf.Abs(transform.position.x - hole.transform.position.x) <= 1.2f;
		if (hole != null) effect.holePosition = new Vector3(hole.transform.position.x, hole.transform.position.y, transform.position.z);
		effect.images = whole.GetComponentsInChildren<SpriteRenderer>();
		effect.colors = new Color[effect.images.Length];
		for (int i = 0; i < effect.images.Length; i++) effect.colors[i] = effect.images[i].color;
	}

	private void Update()
	{
		if (!disappearing || !EventControl.isplay || EventControl.isPaused) return;
		elapsed += Time.deltaTime;
		float t = Mathf.Clamp01(elapsed / 0.45f);
		if (nearHole)
		{
			transform.position = Vector3.Lerp(startPosition, holePosition, t * t);
			transform.localScale = Vector3.one * (1f - t * t);
		}
		else transform.position += Vector3.left * GroundControl.Speed * Time.deltaTime;
		for (int i = 0; i < images.Length; i++)
		{
			Color color = colors[i];
			color.a *= 1f - t;
			images[i].color = color;
		}
		if (t >= 1f) Destroy(gameObject);
	}
}
