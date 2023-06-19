using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
	public static PlayerControl Instance;

	public GameObject deathPanel;

	public GameObject Portal_Effect;

	public RuntimeAnimatorController[] character;

	public static int Hp = 3;

	private float blinkTime = 0.1f;

	private int blinks = 3;

	public SpriteRenderer[] Heart;

	public Sprite[] Heart_icon;

	public static float player_x;

	public static float player_y;

	private Rigidbody2D rbody;

	public static Animator ani;

	private Animator _animator;

	public BoxCollider2D box_1;

	public BoxCollider2D box_2;

	public BoxCollider2D box;

	private HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
	private HashSet<Collider2D> stopContacts = new HashSet<Collider2D>();
	private bool isInvincible;
	private Animator brokeAnimator;
	private Animator rebirthAnimator;
	private Image shieldTime;
	private SpriteRenderer shieldRender;
	private SpriteRenderer[] childRenderers;

	private bool isStop;

	public static bool isGround;

	private bool isSlide;

	private float SlideTime;

	private bool isDoubleJump;

	public static bool isRage;

	public static bool isShield;

	public static bool isMagnet;

	public GameObject broke;

	public GameObject rebirth;

	public SpriteRenderer myRender;

	private bool reflag;
	private RigidbodyConstraints2D rebirthConstraints;
	private Vector3 rebirthStart;
	private Vector3 rebirthTarget;
	private SpriteRenderer rebirthRender;

	public static bool isTrap;

	private float trapTime;
	private RigidbodyConstraints2D rageConstraints;
	private float rageGravity;
	private Vector3 ragePosition;
	private float rageVelocity;

	private Transform Trap;
	private SpriteRenderer trapRender;
	private int trapOrder;
	private int trapLayer;

	private void Awake()
	{
		Instance = this;
		rbody = GetComponent<Rigidbody2D>();
		ani = GetComponent<Animator>();
		myRender = GetComponent<SpriteRenderer>();
		shieldRender = transform.Find("shield").GetComponent<SpriteRenderer>();
		childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
	}

	private void OnDisable()
	{
		ReleaseTrap();
		StopAllCoroutines();
		isInvincible = false;
		if (myRender != null) myRender.enabled = true;
		groundContacts.Clear();
		stopContacts.Clear();
		if (Instance == this)
		{
			isGround = false;
			if (reflag && rbody != null) rbody.constraints = rebirthConstraints;
			if (isRage && rbody != null)
			{
				rbody.constraints = rageConstraints;
				rbody.gravityScale = rageGravity;
			}
			isRage = false;
		}
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	private void Start()
	{
		isGround = false;
		isSlide = false;
		isRage = false;
		isShield = false;
		isMagnet = false;
		reflag = false;
		isTrap = false;
		player_x = base.transform.position.x + 0.5f;
		ani.runtimeAnimatorController = character[角色选择界面.当前选择的角色序号];
		Color color = myRender.color;
		color.a = 0f;
		myRender.color = color;
		SkillControl.skill = 角色选择界面.当前选择的角色序号;
		box = box_1;
	}

	private void Update()
	{
		if (EventControl.isPaused || Time.deltaTime <= 0f) return;
		if (!EventControl.isplay && pause.menuKeys)
		{
			if (GroundControl.new_scene)
			{
				if (myRender.color.a >= 0f)
				{
					Color color = myRender.color;
					color.a = Mathf.Max(0f, color.a - 0.4f * Time.deltaTime);
					myRender.color = color;
					foreach (SpriteRenderer item in childRenderers)
					{
						if (item == null || item == myRender) continue;
						Color color2 = item.color;
						color2.a = Mathf.Max(0f, color2.a - 0.4f * Time.deltaTime);
						item.color = color2;
					}
				}
			}
			else if (myRender.color.a <= 1f)
			{
				Color color3 = myRender.color;
				color3.a = Mathf.Min(1f, color3.a + 0.4f * Time.deltaTime);
				myRender.color = color3;
			}
		}
		if (isRage)
		{
			float top;
			if (EventControl.isplay && GroundControl.Instance != null && GroundControl.Instance.TryGetRageHeight(player_x, box.size.x / 2f, GroundControl.Speed * 0.2f, out top))
			{
				float height = top + box.size.y / 2f - box.offset.y + 0.05f;
				ragePosition.y = Mathf.SmoothDamp(ragePosition.y, height, ref rageVelocity, 0.06f, 8f, Time.deltaTime);
			}
			else rageVelocity = 0f;
			transform.position = ragePosition;
			rbody.velocity = Vector2.zero;
		}
		Vector3 vector = new Vector3(-3.22f, 0.06f);
		float num = base.transform.position.x - vector.x;
		if (num <= 1f && !reflag && !isRage)
		{
			if (Hp > 0 && SkillControl.skill == 0 && SkillControl.isEffect)
			{
				BeginRebirth();
			}
			else
			{
				if (Hp > 0 || !deathPanel.activeSelf)
				{
					Hp = 0;
					AudioManager.Instance.Play("Boss死了");
					ani.SetBool("isDie", value: true);
					CharacterDeath();
				}
				if (rbody != null) Object.Destroy(rbody);
				if (num <= 0.5f)
				{
					Object.Destroy(base.gameObject);
					return;
				}
				float t = 1f - Mathf.Pow(1f - 0.02f, Time.deltaTime * 60f);
				Vector2 vector2 = Vector2.Lerp(base.transform.position, vector, t);
				base.transform.position = new Vector3(vector2.x, vector2.y, base.transform.position.z);
				base.transform.localScale = new Vector3(Mathf.MoveTowards(base.transform.localScale.x, 0f, 0.02f * 60f * Time.deltaTime), Mathf.MoveTowards(base.transform.localScale.y, 0f, 0.02f * 60f * Time.deltaTime));
			}
		}
		if (brokeAnimator != null && brokeAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
		{
			Object.Destroy(brokeAnimator.gameObject);
		}
		if (reflag && rebirthAnimator != null)
		{
			float progress = rebirthAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
			transform.position = Vector3.Lerp(rebirthStart, rebirthTarget, Mathf.SmoothStep(0f, 1f, progress));
			if (progress >= 1f)
			{
				if (shieldTime == null) shieldTime = SkillControl.Instance.shieldTime;
				if (shieldTime.fillAmount > 0.5f) shieldTime.fillAmount = 0.5f;
				isShield = true;
				shieldRender.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
				box_1.enabled = true;
				box_2.enabled = false;
				box = box_1;
				rbody.constraints = rebirthConstraints;
				rbody.velocity = Vector2.zero;
				rbody.WakeUp();
				reflag = false;
				ani.SetInteger("action", 0);
				ani.SetBool("isIdle", value: false);
				Hp = 1;
				Object.Destroy(rebirthAnimator.gameObject);
			}
		}
		for (int i = 0; i < 3; i++)
		{
			if (i < Hp)
			{
				Heart[i].sprite = Heart_icon[0];
				Heart[i].color = Color.white;
			}
			else
			{
				Heart[i].sprite = Heart_icon[1];
				Heart[i].color = new Color(0f, 0f, 0f);
			}
		}
		if (Hp == 0 || !EventControl.isplay)
		{
			return;
		}
		if (isTrap)
		{
			trapTime += Time.deltaTime;
			if (trapTime >= 0.5f)
			{
				ani.SetBool("isIdle", value: false);
				ani.SetInteger("action", 0);
				Trap.GetComponent<Animator>().SetBool("isOpen", value: true);
				Trap.GetComponent<BoxCollider2D>().enabled = false;
				ReleaseTrap();
			}
			else
			{
				base.transform.position = new Vector3(base.transform.position.x - GroundControl.Speed * Time.deltaTime, base.transform.position.y);
			}
		}
		if (!isGround && rbody.velocity.y < 0f && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) && SkillControl.skill == 1)
		{
			rbody.velocity = new Vector2(rbody.velocity.x, -0.2f);
		}
		if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
		{
			Jump();
		}
		else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
		{
			Slide();
		}
		if (isSlide && isGround)
		{
			if (SlideTime < 1f)
			{
				SlideTime += Time.deltaTime;
			}
			else
			{
				box_1.enabled = true;
				box_2.enabled = false;
				box = box_1;
				isSlide = false;
				ani.SetInteger("action", 0);
			}
		}
		if (!isStop && isGround)
		{
			Vector3 position2 = base.transform.position;
			position2.x = Mathf.MoveTowards(position2.x, player_x, 0.24f * Time.deltaTime);
			base.transform.position = position2;
		}
	}

	private void LateUpdate()
	{
		if (reflag && rebirthRender != null) AlignRebirth();
	}

	private void AlignRebirth()
	{
		Vector3 position = rebirthRender.transform.position;
		position.x += myRender.bounds.center.x - rebirthRender.bounds.center.x;
		rebirthRender.transform.position = position;
	}

	private void ReleaseTrap()
	{
		isTrap = false;
		if (trapRender != null)
		{
			trapRender.sortingLayerID = trapLayer;
			trapRender.sortingOrder = trapOrder;
			trapRender = null;
		}
	}

	private void BeginRebirth()
	{
		Hp = 0;
		SkillControl.cd.fillAmount = 1f;
		SkillControl.isEffect = false;
		reflag = true;
		ani.SetBool("isIdle", value: true);
		StopAllCoroutines();
		isInvincible = false;
		myRender.enabled = true;
		isSlide = false;
		isDoubleJump = false;
		ReleaseTrap();
		groundContacts.Clear();
		stopContacts.Clear();
		isGround = false;
		isStop = false;
		box_1.enabled = false;
		box_2.enabled = false;
		box = null;
		rebirthConstraints = rbody.constraints;
		rbody.constraints |= RigidbodyConstraints2D.FreezePosition;
		rbody.velocity = Vector2.zero;
		rbody.Sleep();
		// 从死亡位置随特效移动，结束后再恢复下落。
		rebirthStart = transform.position;
		rebirthTarget = new Vector3(player_x, 1f, transform.position.z);
		GameObject effect = Object.Instantiate(rebirth, transform);
		effect.name = "rebirth";
		rebirthAnimator = effect.GetComponent<Animator>();
		rebirthRender = effect.GetComponent<SpriteRenderer>();
		rebirthRender.maskInteraction = SpriteMaskInteraction.None;
		AlignRebirth();
	}

	public void BeginRage()
	{
		if (isRage || Hp <= 0) return;
		rageConstraints = rbody.constraints;
		rageGravity = rbody.gravityScale;
		ragePosition = new Vector3(player_x, transform.position.y, transform.position.z);
		rageVelocity = 0f;
		box_1.enabled = true;
		box_2.enabled = false;
		box = box_1;
		isSlide = false;
		isRage = true;
		rbody.velocity = Vector2.zero;
		rbody.gravityScale = 0f;
		rbody.constraints |= RigidbodyConstraints2D.FreezePosition;
		transform.position = ragePosition;
	}

	public bool EndRage()
	{
		if (!isRage) return true;
		float footOffset = box.size.y / 2f - box.offset.y;
		float top;
		if (GroundControl.Instance == null || !GroundControl.Instance.TryGetLanding(player_x, box.size.x / 2f, footOffset, transform.position.y, Mathf.Abs(Physics2D.gravity.y * rageGravity), out top)) return false;
		// 高度跟随到位后再恢复重力，结束时不直接改坐标。
		float height = top + footOffset;
		if (transform.position.y < height || transform.position.y > height + 0.08f) return false;
		isRage = false;
		rbody.constraints = rageConstraints;
		rbody.gravityScale = rageGravity;
		rbody.velocity = Vector2.zero;
		rbody.WakeUp();
		return true;
	}

	public void Jump()
	{
		if (EventControl.isplay && !EventControl.isPaused && Hp > 0 && !isRage && !isTrap)
		{
			if (isSlide)
			{
				SlideTime = 1f;
			}
			if (isGround)
			{
				rbody.velocity = new Vector2(rbody.velocity.x, 6.5f);
				AudioManager.Instance.Play("跳");
			}
			else if (!isDoubleJump)
			{
				isDoubleJump = true;
				rbody.velocity = new Vector2(rbody.velocity.x, 6.5f);
				AudioManager.Instance.Play("跳");
			}
		}
	}

	public void Slide()
	{
		if (EventControl.isplay && !EventControl.isPaused && Hp > 0 && !isRage && !isTrap)
		{
			if (isGround)
			{
				box_1.enabled = false;
				box_2.enabled = true;
				box = box_2;
				SlideTime = 0f;
				isSlide = true;
				ani.SetInteger("action", 2);
				AudioManager.Instance.Play("跳");
			}
			else
			{
				rbody.velocity = new Vector2(rbody.velocity.x, -12f);
				box_1.enabled = false;
				box_2.enabled = true;
				box = box_2;
				SlideTime = 0f;
				isSlide = true;
				ani.SetInteger("action", 2);
				AudioManager.Instance.Play("跳");
			}
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (Hp <= 0) return;
		if (isRage && collision.collider.CompareTag("Die")) return;
		if (isRage && (collision.collider.CompareTag("hinder") || collision.collider.CompareTag("enemy")))
		{
			GroundControl.BreakObject(collision.gameObject);
			return;
		}
		if (collision.collider.tag == "hinder" || collision.collider.tag == "enemy" || collision.collider.tag == "Ground")
		{
			bool flag = UpdateContacts(collision);
			if (collision.collider.tag == "enemy" && !isRage)
			{
				if (collision.collider.name.Contains("MA_enemy_3"))
				{
					if (flag)
					{
						Damage();
					}
				}
				else
				{
					Damage();
				}
			}
			if (Hp == 0) return;
			if (!flag)
			{
				isStop = true;
				if (isRage && collision.collider.tag != "Ground")
				{
					if (brokeAnimator != null)
					{
						Object.Destroy(brokeAnimator.gameObject);
					}
					GameObject effect = Object.Instantiate(broke, base.transform);
					effect.name = "broke";
					brokeAnimator = effect.GetComponent<Animator>();
					GroundControl.BreakObject(collision.gameObject);
					base.transform.position = new Vector3(base.transform.position.x + 0.1f, base.transform.position.y);
					return;
				}
			}
		}
		if (collision.collider.tag == "Die")
		{
			if (SkillControl.skill == 0 && SkillControl.isEffect)
			{
				BeginRebirth();
			}
			else if (Hp != 0)
			{
				AudioManager.Instance.Play("Boss死了");
				ani.SetBool("isDie", value: true);
				Hp = 0;
				CharacterDeath();
			}
		}
		if (collision.collider.name == "portal(Clone)")
		{
			ani.SetBool("isIdle", value: true);
			EventControl.isplay = false;
			GroundControl.portal_effect = Object.Instantiate(Portal_Effect, GroundControl.portal.transform).transform;
			GroundControl.portal_effect.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
		}
		if (collision.collider.name.Contains("MA_enemy_4") && !isRage)
		{
			ani.SetBool("isIdle", value: true);
			ReleaseTrap();
			Trap = collision.collider.transform;
			trapRender = Trap.GetComponent<SpriteRenderer>();
			trapLayer = trapRender.sortingLayerID;
			trapOrder = trapRender.sortingOrder;
			trapRender.sortingLayerID = myRender.sortingLayerID;
			trapRender.sortingOrder = myRender.sortingOrder + 1;
			Trap.GetComponent<Animator>().SetBool("isTrap", value: true);
			isTrap = true;
			trapTime = 0f;
		}
	}

	private bool UpdateContacts(Collision2D collision)
	{
		bool flag = false;
		for (int i = 0; i < collision.contactCount; i++)
		{
			if (collision.GetContact(i).normal.y >= 0.9f)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			groundContacts.Add(collision.collider);
			stopContacts.Remove(collision.collider);
			isDoubleJump = false;
			if (!isSlide) ani.SetInteger("action", 0);
		}
		else
		{
			groundContacts.Remove(collision.collider);
			stopContacts.Add(collision.collider);
		}
		isGround = groundContacts.Count > 0;
		isStop = stopContacts.Count > 0;
		return flag;
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		if (Hp <= 0) return;
		if (collision.collider.CompareTag("hinder") || collision.collider.CompareTag("enemy") || collision.collider.CompareTag("Ground"))
		{
			UpdateContacts(collision);
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		groundContacts.Remove(collision.collider);
		stopContacts.Remove(collision.collider);
		isGround = groundContacts.Count > 0;
		isStop = stopContacts.Count > 0;
		if (!isGround && !isSlide) ani.SetInteger("action", 1);
	}

	private void BlinkPlayer(int numBlinks, float seconds)
	{
		isInvincible = true;
		StartCoroutine(DoBlinks(numBlinks, seconds));
	}

	private IEnumerator DoBlinks(int numBlinks, float seconds)
	{
		for (int i = 0; i < numBlinks * 2; i++)
		{
			float startTime = Time.time;
			while (Time.time - startTime < seconds)
			{
				yield return null;
			}
			myRender.enabled = !myRender.enabled;
			yield return null;
		}
		myRender.enabled = true;
		isInvincible = false;
	}

	private void CharacterDeath()
	{
		EventControl.FinishRun();
		deathPanel.transform.Find("score").GetComponent<TextMeshProUGUI>().text = EventControl.DistanceText;
		deathPanel.SetActive(value: true);
	}

	public void Damage()
	{
		if (Hp <= 0 || isShield || isInvincible || isRage || EventControl.isPaused)
		{
			return;
		}
		Hp--;
		if (Hp == 0)
		{
			if (SkillControl.skill == 0 && SkillControl.isEffect)
			{
				BeginRebirth();
			}
			else
			{
				AudioManager.Instance.Play("Boss死了");
				ani.SetBool("isDie", value: true);
				CharacterDeath();
			}
		}
		else
		{
			BlinkPlayer(blinks, blinkTime);
		}
	}
}
