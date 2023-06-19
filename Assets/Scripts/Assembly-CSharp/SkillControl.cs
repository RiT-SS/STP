using UnityEngine;
using UnityEngine.UI;

public class SkillControl : MonoBehaviour
{
	public static SkillControl Instance;
	public static int skill;
	private Rigidbody2D player_rbody;
	public Image shieldTime { get { return lt_1; } }
	public Image magnetTime { get { return lt_2; } }

	public Sprite[] skill_icon;

	private bool isHot;

	private bool isCold;

	public static bool isEffect;

	public float ltTime = 3f;

	public float cdTime = 40f;

	public float MaTime = 5f;

	public float ShTime = 5f;

	public float r_cdTime = 60f;

	public float addtionalTime;

	public bool ltFlag;

	public Transform player_tran;

	public static Image lt;

	public static Image cd;

	private SpriteRenderer rage;

	private SpriteRenderer raged;

	private Image lt_1;

	private Image lt_2;

	private SpriteRenderer Shield;

	private SpriteRenderer Magnet;

	private Transform ground;

	private SpriteRenderer rush;

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
		skill = 角色选择界面.当前选择的角色序号;
		isHot = false;
		isCold = false;
		isEffect = true;
		player_tran = PlayerControl.Instance.transform;
		player_rbody = player_tran.GetComponent<Rigidbody2D>();
		lt = base.transform.Find("Skill_1").Find("raged").Find("lt")
			.GetComponent<Image>();
		cd = base.transform.Find("Skill_1").Find("rage").Find("cd")
			.GetComponent<Image>();
		raged = base.transform.Find("Skill_1").Find("raged").GetComponent<SpriteRenderer>();
		rage = base.transform.Find("Skill_1").Find("rage").GetComponent<SpriteRenderer>();
		lt_1 = base.transform.Find("Skill_2").Find("shield").Find("lt")
			.GetComponent<Image>();
		lt_2 = base.transform.Find("Skill_3").Find("magnet").Find("lt")
			.GetComponent<Image>();
		Shield = player_tran.Find("shield").GetComponent<SpriteRenderer>();
		Magnet = player_tran.Find("magnet").GetComponent<SpriteRenderer>();
		ground = GameObject.Find("GroundControl").transform;
		rush = player_tran.Find("rush").GetComponent<SpriteRenderer>();
		rage.sprite = skill_icon[skill];
	}

	public void Rage()
	{
		if (skill != 2 || isHot || isCold || PlayerControl.isTrap || PlayerControl.Hp <= 0 || !EventControl.isplay || EventControl.isPaused) return;
		PlayerControl.Instance.BeginRage();
		addtionalTime = 0f;
		ltFlag = false;
		rush.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
		BgControl.Speed = 1f;
		GroundControl.Speed = 10f;
		raged.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
		rage.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
		lt.fillAmount = 1f;
		isHot = true;
	}

	private void Update()
	{
		if (PlayerControl.Hp == 0 || !EventControl.isplay)
		{
			return;
		}
		if (skill == 2)
		{
			if (Input.GetKeyDown(KeyCode.A) && !isHot && !isCold && !PlayerControl.isTrap)
			{
				Rage();
			}
			else if (isHot)
			{
				lt.fillAmount = Mathf.Max(0f, lt.fillAmount - Time.deltaTime / ltTime);
				if (lt.fillAmount <= 0f && PlayerControl.Instance.EndRage())
				{
					rush.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
					BgControl.Speed = 0.2f;
					GroundControl.Speed = 2f;
					raged.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
					rage.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
					lt.fillAmount = 0f;
					cd.fillAmount = 1f;
					isHot = false;
					isCold = true;
				}
			}
			else if (isCold)
			{
				cd.fillAmount -= 1f / cdTime * Time.deltaTime;
				if (cd.fillAmount <= 0.02f)
				{
					cd.fillAmount = 0f;
					isCold = false;
				}
			}
		}
		else if (skill == 0 && !isEffect)
		{
			cd.fillAmount -= 1f / r_cdTime * Time.deltaTime;
			if (cd.fillAmount <= 0.02f)
			{
				cd.fillAmount = 0f;
				isEffect = true;
			}
		}
		if (PlayerControl.isShield)
		{
			lt_1.fillAmount += 1f / ShTime * Time.deltaTime;
			if (lt_1.fillAmount >= 0.98f)
			{
				PlayerControl.isShield = false;
				Shield.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
				lt_1.fillAmount = 1f;
			}
		}
		if (PlayerControl.isMagnet)
		{
			lt_2.fillAmount += 1f / MaTime * Time.deltaTime;
			if (lt_2.fillAmount >= 0.98f)
			{
				PlayerControl.isMagnet = false;
				Magnet.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
				lt_2.fillAmount = 1f;
			}
		}
	}
}
