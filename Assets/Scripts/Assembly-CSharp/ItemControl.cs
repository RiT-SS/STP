using UnityEngine;
using UnityEngine.UI;

public class ItemControl : MonoBehaviour
{
	public float MaTime = 20f;

	public float ShTime = 20f;

	private bool collected;
	private Image lt_1;

	private Image lt_2;

	private SpriteRenderer Shield;

	private SpriteRenderer Magnet;

	public Transform player_tran;

	public BoxCollider2D player_collider;

	public Rigidbody2D player_rbody;

	private void Start()
	{
		player_tran = PlayerControl.Instance.transform;
		player_collider = player_tran.GetComponent<PlayerControl>().box_1;
		Shield = player_tran.Find("shield").GetComponent<SpriteRenderer>();
		Magnet = player_tran.Find("magnet").GetComponent<SpriteRenderer>();

	}

	private void Update()
	{
		if (PlayerControl.Hp != 0 && EventControl.isplay && PlayerControl.isMagnet && player_tran != null && !collected)
		{
			float magnitude = (player_tran.position - base.transform.position).magnitude;
			if (magnitude <= 0.05f)
			{
				OnTriggerEnter2D(player_collider);
			}
			else if (magnitude <= 3f)
			{
				float t = 1f - Mathf.Pow(1f - 0.04f, Time.deltaTime * 60f);
				Vector2 vector = Vector2.Lerp(base.transform.position, player_tran.position, t);
				base.transform.position = new Vector3(vector.x, vector.y, base.transform.position.z);
			}
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!collected && PlayerControl.Hp > 0 && !EventControl.isPaused && collision.CompareTag("Player"))
		{
			collected = true;
			lt_1 = SkillControl.Instance.shieldTime;
			lt_2 = SkillControl.Instance.magnetTime;
			AudioManager.Instance.Play("金币");
			if (base.gameObject.name.Contains("shield"))
			{
				lt_1.fillAmount = 0f;
				PlayerControl.isShield = true;
				Shield.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			}
			else if (base.gameObject.name.Contains("magnet"))
			{
				lt_2.fillAmount = 0f;
				PlayerControl.isMagnet = true;
				Magnet.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			}
			else if (base.gameObject.name.Contains("piece"))
			{
				EventControl.Instance.CollectPiece();
			}
			else if (base.gameObject.name.Contains("med") && PlayerControl.Hp < 3)
			{
				PlayerControl.Hp++;
			}
			Object.Destroy(base.gameObject);
		}
	}
}
