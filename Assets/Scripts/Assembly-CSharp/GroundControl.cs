using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GroundControl : MonoBehaviour
{
	public static GroundControl Instance;
	public static float Speed;
	private static HashSet<int> breaking = new HashSet<int>();
	private static int breakingFrame;

	private int piece_need = 10;

	private float[,] random_height = new float[3, 3]
	{
		{ -1.4f, -1.8f, -2.2f },
		{ -1.37f, -1.77f, -2.17f },
		{ -0.95f, -1.35f, -1.75f }
	};

	private float[] i_offset = new float[3] { 2.1f, 2.9f, 3.1f };

	private string[] random_item = new string[3] { "shield", "magnet", "med" };

	private string[] start_ground = new string[3] { "AC_ground", "WW_ground", "MA_ground" };

	public Sprite[] items;

	public Sprite[] pieces;

	public int scene;

	public GameObject shell;

	public GameObject[] GroundPrefabs;

	public GameObject Portal;

	public GameObject Portal_Effect;

	public static GameObject portal;

	public static Transform portal_effect;

	public static bool new_scene;

	public GameObject PreCrash;

	public GameObject DarkHole_Effect;

	public static float nextY;

	public static float nextX;

	public GameObject crash;

	public ExplosionForce ef;

	private List<Transform> grounds = new List<Transform>();
	private Transform darkhole;
	private Animator portalAnimator;
	private SpriteRenderer portalRender;

	private void Awake()
	{
		Instance = this;
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	private void Start()
	{
		Speed = 2f;
		piece_need = EventControl.PiecesNeeded;
		foreach (Transform child in transform) SetItems(child);
		darkhole = GameObject.Find("darkhole").transform;
		Transform obj = base.transform.Find(start_ground[scene]);
		BoxCollider2D component = obj.gameObject.GetComponent<BoxCollider2D>();
		nextY = obj.position.y + component.offset.y + component.size.y / 2f;
		nextX = obj.position.x + component.offset.x - component.size.x / 2f;
		ef = GameObject.Find("GroundForce").GetComponent<ExplosionForce>();
		new_scene = false;
		IEnumerator enumerator = base.transform.GetEnumerator();
		try
		{
			if (enumerator.MoveNext())
			{
				Transform parent = (Transform)enumerator.Current;
				portal = UnityEngine.Object.Instantiate(Portal, parent);
				if (scene == 2)
				{
					portal.transform.position = new Vector3(portal.transform.position.x - 1f, portal.transform.position.y - 0.4f);
				}
				UnityEngine.Object.Destroy(portal.GetComponent<BoxCollider2D>());
				portal_effect = UnityEngine.Object.Instantiate(Portal_Effect, portal.transform).transform;
				portal_effect.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}

	private void Update()
	{
		if (PlayerControl.Hp == 0 || EventControl.isPaused || Time.deltaTime <= 0f)
		{
			return;
		}
		if (!EventControl.isplay)
		{
			if (portalAnimator == null || portalAnimator.transform != portal_effect)
			{
				portalAnimator = portal_effect.GetComponent<Animator>();
				portalRender = portal_effect.GetComponent<SpriteRenderer>();
			}
			if (!(portalAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f))
			{
				return;
			}
			portalRender.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
			if (new_scene)
			{
				int next = scene + 1;
				if (EventControl.scenesPassed >= 2) next = scene + UnityEngine.Random.Range(1, 3);
				next %= 3;
				EventControl.scenesPassed++;
				if (next == 0)
				{
					SceneManager.LoadScene("GameScene_AC");
				}
				else if (next == 1)
				{
					SceneManager.LoadScene("GameScene_WW");
				}
				else
				{
					SceneManager.LoadScene("GameScene_MA");
				}
			}
			else
			{
				PlayerControl.ani.SetBool("isIdle", value: false);
				EventControl.isplay = true;
			}
			return;
		}
		grounds.Clear();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			grounds.Add(base.transform.GetChild(i));
		}
		foreach (Transform transform in grounds)
		{
			if (transform.gameObject.name == "Crash(Clone)")
			{
				continue;
			}
			Vector3 position = transform.position;
			position.x -= Speed * Time.deltaTime;
			SpriteRenderer component = transform.gameObject.GetComponent<SpriteRenderer>();
			Rect textureRect = component.sprite.textureRect;
			if (base.transform.position.x + position.x - textureRect.width / 200f <= -3f)
			{
				if (textureRect.width <= 100f)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(GroundPrefabs[UnityEngine.Random.Range(0, GroundPrefabs.Length)], base.transform);
					BoxCollider2D component2 = gameObject.GetComponent<BoxCollider2D>();
					Transform obj = gameObject.transform;
					Vector2 vector = obj.position;
					vector.x = position.x + textureRect.width / 200f + i_offset[scene] + 3f * component2.size.x / 2f - component2.offset.x;
					vector.y = random_height[scene, UnityEngine.Random.Range(0, random_height.GetLength(1))];
					nextY = vector.y + component2.offset.y + component2.size.y / 2f;
					nextX = vector.x + component2.offset.x - component2.size.x / 2f;
					obj.position = vector;
					if (scene == 1)
					{
						Transform obj2 = UnityEngine.Object.Instantiate(shell, base.transform.parent).transform;
						obj2.position = new Vector3(obj2.position.x, UnityEngine.Random.Range(-0.8f, 0.8f));
					}
					SetItems(gameObject.transform);
					if (EventControl.piece_count >= piece_need && !new_scene)
					{
						portal = UnityEngine.Object.Instantiate(Portal, gameObject.transform);
						if (gameObject.name.Contains("AC_ground_5"))
						{
							portal.transform.position = new Vector2(portal.transform.position.x + gameObject.GetComponent<BoxCollider2D>().size.x / 2f + 0.5f, portal.transform.position.y + 0.35f);
						}
						else if (scene == 2)
						{
							portal.transform.position = new Vector2(portal.transform.position.x + gameObject.GetComponent<BoxCollider2D>().size.x / 2f + 0.5f, portal.transform.position.y - 0.4f);
						}
						else
						{
							portal.transform.position = new Vector2(portal.transform.position.x + gameObject.GetComponent<BoxCollider2D>().size.x / 2f + 0.5f, portal.transform.position.y);
						}
						new_scene = true;
					}
					BreakGround(transform.gameObject);
					break;
				}
				crash = UnityEngine.Object.Instantiate(PreCrash, base.transform);
				Texture2D texture = component.sprite.texture;
				float pixelsPerUnit = component.sprite.pixelsPerUnit;
				Sprite sprite = Sprite.Create(texture, new Rect(textureRect.x, textureRect.y, 100f, textureRect.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
				Sprite sprite2 = Sprite.Create(texture, new Rect(textureRect.x + 100f, textureRect.y, textureRect.width - 100f, textureRect.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
				crash.AddComponent<GroundSprite>().SetSprite(sprite);
				GroundSprite groundSprite = transform.GetComponent<GroundSprite>();
				if (groundSprite == null) groundSprite = transform.gameObject.AddComponent<GroundSprite>();
				groundSprite.SetSprite(sprite2);
				crash.GetComponent<SpriteRenderer>().sprite = sprite;
				crash.GetComponent<SpriteRenderer>().sortingOrder = component.sortingOrder;
				crash.transform.position = new Vector2(transform.position.x - (textureRect.width - 100f) / 200f, transform.position.y);
				transform.gameObject.GetComponent<SpriteRenderer>().sprite = sprite2;
				position.x += 0.5f;
				BoxCollider2D component3 = transform.gameObject.GetComponent<BoxCollider2D>();
				component3.size = new Vector2(component3.size.x - 1f, component3.size.y);
				foreach (Transform item2 in transform)
				{
					Vector3 position2 = item2.position;
					position2.x -= 0.5f;
					item2.position = position2;
				}
				BreakGround(crash);
			}
			transform.position = position;
			foreach (Transform item3 in transform)
			{
				Vector2 b = new Vector3(-3.42f, 0.06f);
				float num2 = Mathf.Abs(item3.position.x + Speed * Time.deltaTime - b.x);
				if ((item3.CompareTag("hinder") || item3.CompareTag("enemy")) && num2 <= 1f)
				{
					BreakObject(item3.gameObject);
					continue;
				}
				if (num2 <= 0.2f)
				{
					UnityEngine.Object.Instantiate(DarkHole_Effect, darkhole).transform.position = new Vector2(item3.position.x, item3.position.y);
					UnityEngine.Object.Destroy(item3.gameObject);
				}
				else if (num2 <= 1f)
				{
					float t = 1f - Mathf.Pow(1f - 0.02f, Time.deltaTime * 60f);
					Vector2 vector2 = Vector2.Lerp(new Vector2(item3.position.x + Speed * Time.deltaTime, item3.position.y), b, t);
					item3.position = new Vector3(vector2.x, vector2.y);
					if (item3.gameObject.name.Contains("AC_barrier_1"))
					{
						item3.localScale = new Vector3(Mathf.MoveTowards(item3.localScale.x, 0f, 0.0075f * 60f * Time.deltaTime), Mathf.MoveTowards(item3.localScale.y, 0f, 0.0075f * 60f * Time.deltaTime));
					}
					else if (item3.gameObject.name.Contains("pipe"))
					{
						item3.localScale = new Vector3(Mathf.MoveTowards(item3.localScale.x, 0f, 0.03f * 60f * Time.deltaTime), Mathf.MoveTowards(item3.localScale.y, 0f, 0.03f * 60f * Time.deltaTime));
					}
					else if (item3.gameObject.name.Contains("MA_barrier_2"))
					{
						item3.localScale = new Vector3(Mathf.MoveTowards(item3.localScale.x, 0f, 0.003f * 60f * Time.deltaTime), Mathf.MoveTowards(item3.localScale.y, 0f, 0.003f * 60f * Time.deltaTime));
					}
					else
					{
						item3.localScale = new Vector3(Mathf.MoveTowards(item3.localScale.x, 0f, 0.015f * 60f * Time.deltaTime), Mathf.MoveTowards(item3.localScale.y, 0f, 0.015f * 60f * Time.deltaTime));
					}
				}
			}
		}
	}

	private void SetItems(Transform ground)
	{
		foreach (Transform item in ground)
		{
			if (item.name.Contains("item") || Array.IndexOf(random_item, item.name) >= 0)
			{
				if (UnityEngine.Random.Range(0, 4) == 0)
				{
					int num = UnityEngine.Random.Range(0, random_item.Length);
					item.GetComponent<SpriteRenderer>().sprite = items[num];
					item.gameObject.name = random_item[num];
				}
				else
				{
					item.gameObject.SetActive(false);
					UnityEngine.Object.Destroy(item.gameObject);
				}
			}
			else if (item.name.Contains("piece"))
			{
				item.GetComponent<SpriteRenderer>().sprite = pieces[scene];
			}
		}
	}

	private void BreakGround(GameObject obj)
	{
		foreach (Transform child in obj.transform)
		{
			if (child.CompareTag("hinder") || child.CompareTag("enemy")) BreakObject(child.gameObject);
		}
		SpriteRenderer render = obj.GetComponent<SpriteRenderer>();
		Explodable piece = obj.AddComponent<Explodable>();
		obj.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
		PolygonCollider2D shape = obj.GetComponent<PolygonCollider2D>();
		if (shape == null) shape = obj.AddComponent<PolygonCollider2D>();
		Bounds bounds = render.sprite.bounds;
		shape.pathCount = 1;
		shape.SetPath(0, new Vector2[]
		{
			new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.max.x, bounds.min.y),
			new Vector2(bounds.max.x, bounds.max.y), new Vector2(bounds.min.x, bounds.max.y)
		});
		piece.shatterType = Explodable.ShatterType.Voronoi;
		piece.extraPoints = PlayerControl.isRage ? 4 : 10;
		piece.allowRuntimeFragmentation = true;
		piece.sortingLayerName = render.sortingLayerName;
		piece.orderInLayer = render.sortingOrder;
		piece.explode();
	}

	public static void BreakObject(GameObject obj)
	{
		if (obj == null) return;
		if (breakingFrame != Time.frameCount)
		{
			breaking.Clear();
			breakingFrame = Time.frameCount;
		}
		if (!breaking.Add(obj.GetInstanceID())) return;
		foreach (Collider2D box in obj.GetComponentsInChildren<Collider2D>()) box.enabled = false;
		ObstacleControl obstacle = obj.GetComponent<ObstacleControl>();
		if (obstacle != null && obstacle.type == ObstacleControl.ObstacleType.Creature)
		{
			obstacle.Disappear();
			UnityEngine.Object.Destroy(obj);
			return;
		}
		foreach (SpriteRenderer render in obj.GetComponentsInChildren<SpriteRenderer>())
		{
			if (!render.enabled || render.sprite == null) continue;
			GameObject copy = new GameObject(obj.name + " debris");
			copy.transform.position = render.transform.position;
			copy.transform.rotation = render.transform.rotation;
			copy.transform.localScale = render.transform.lossyScale;
			SpriteRenderer image = copy.AddComponent<SpriteRenderer>();
			image.sprite = render.sprite;
			image.sharedMaterial = render.sharedMaterial;
			image.color = render.color;
			image.flipX = render.flipX;
			image.flipY = render.flipY;
			BoxCollider2D shape = copy.AddComponent<BoxCollider2D>();
			shape.size = render.sprite.bounds.size;
			shape.offset = render.sprite.bounds.center;
			Explodable piece = copy.AddComponent<Explodable>();
			copy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
			piece.shatterType = Explodable.ShatterType.Voronoi;
			piece.extraPoints = 4;
			piece.allowRuntimeFragmentation = true;
			piece.sortingLayerName = render.sortingLayerName;
			piece.orderInLayer = render.sortingOrder;
			piece.explode();
		}
		UnityEngine.Object.Destroy(obj);
	}

	public bool TryGetRageHeight(float x, float halfWidth, float lookAhead, out float top)
	{
		top = float.NegativeInfinity;
		float next = float.PositiveInfinity;
		float nextTop = 0f;
		foreach (Transform child in transform)
		{
			if (child.GetComponent<Explodable>() != null) continue;
			BoxCollider2D box = child.GetComponent<BoxCollider2D>();
			if (box == null || !box.enabled || box.isTrigger) continue;
			Bounds bounds = box.bounds;
			if (bounds.max.x < x - halfWidth) continue;
			if (bounds.min.x <= x + halfWidth) top = Mathf.Max(top, bounds.max.y);
			else if (bounds.min.x < next)
			{
				next = bounds.min.x;
				nextTop = bounds.max.y;
			}
		}
		// 高台提前抬升；低台等离开当前平台后再降下，避免从地形中穿过去。
		if (!float.IsPositiveInfinity(next) && (float.IsNegativeInfinity(top) || (next <= x + halfWidth + lookAhead && nextTop > top))) top = nextTop;
		return !float.IsNegativeInfinity(top);
	}

	public bool TryGetLanding(float x, float halfWidth, float footOffset, float height, float gravity, out float top)
	{
		top = float.NegativeInfinity;
		foreach (Transform child in transform)
		{
			if (child.GetComponent<Explodable>() != null) continue;
			BoxCollider2D box = child.GetComponent<BoxCollider2D>();
			if (box == null || !box.enabled || box.isTrigger) continue;
			Bounds bounds = box.bounds;
			float fall = Mathf.Sqrt(2f * Mathf.Max(0f, height - footOffset - bounds.max.y) / Mathf.Max(0.01f, gravity));
			float margin = halfWidth + 0.15f + 2f * fall;
			if (bounds.min.x <= x - halfWidth && bounds.max.x >= x + margin) top = Mathf.Max(top, bounds.max.y);
		}
		return !float.IsNegativeInfinity(top);
	}
}
