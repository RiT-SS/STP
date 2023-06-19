using UnityEngine;

public class BgControl : MonoBehaviour
{
	public static float Speed;

	public static int Crash_Width = 50;

	public GameObject PreCrash;

	public GameObject[] PreBg;

	private int PreBg_num;
	private Sprite[] bgSprites;
	private SpriteRenderer[] bgRenderers;

	private float[] Bg_width = new float[3] { 5.8f, 6.144f, 7.68f };

	private int[] Bg_num = new int[3] { 3, 3, 2 };

	public GameObject Crash;

	public ExplosionForce ef;

	public int scene;

	private void Start()
	{
		Speed = 0.2f;
		ef = GameObject.Find("BgForce").GetComponent<ExplosionForce>();
		PreBg_num = 0;
		bgSprites = new Sprite[PreBg.Length];
		for (int i = 0; i < PreBg.Length; i++) bgSprites[i] = PreBg[i].GetComponent<SpriteRenderer>().sprite;
		bgRenderers = GetComponentsInChildren<SpriteRenderer>();
	}

	private void Update()
	{
		if (PlayerControl.Hp == 0 || !EventControl.isplay)
		{
			return;
		}
		foreach (SpriteRenderer renderer in bgRenderers)
		
		{
			Transform transform = renderer.transform;
			if (!(transform.gameObject.name == "Crash(Clone)"))
			{
				Vector3 position = transform.position;
				position.x -= Speed * Time.deltaTime;
				if (position.x < -1f - Bg_width[scene])
				{
					position.x += Bg_width[scene] * (float)Bg_num[scene];
					renderer.sprite = bgSprites[PreBg_num];
					PreBg_num = (PreBg_num + 1) % PreBg.Length;
				}
				transform.position = position;
			}
		}
	}

}
