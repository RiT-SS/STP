using TMPro;
using UnityEngine;

public class 角色选择界面 : MonoBehaviour
{
	public static int 当前选择的角色序号;

	public Quaternion[] 每个角色的旋转位置设置;

	private Transform child1Transform1;

	private Transform child1Transform2;

	private Transform child1Transform3;

	private Transform CharacterName;

	private Animator 动画控制器1;

	private Animator 动画控制器2;

	private Animator 动画控制器3;

	private SpriteRenderer 颜色控制器1;

	private SpriteRenderer 颜色控制器2;

	private SpriteRenderer 颜色控制器3;

	private TextMeshProUGUI 名字;

	private Vector3 selectedScale = new Vector3(1f, 1f, 1f);

	private Vector3 selectedPosition = new Vector3(0f, 0f, 0f);

	private Vector3 unSelectedScale1 = new Vector3(0.75f, 0.75f, 1f);

	private Vector3 unSelectedPosition1 = new Vector3(-7f, 1.5f, 0f);

	private Vector3 unSelectedScale2 = new Vector3(0.75f, 0.75f, 1f);

	private Vector3 unSelectedPosition2 = new Vector3(7f, 1.5f, 0f);

	public float duration = 0.05f;

	private void Start()
	{
		当前选择的角色序号 = 0;
		child1Transform1 = base.transform.Find("NinjaGirlRun");
		child1Transform2 = base.transform.Find("adventureGirlRun");
		child1Transform3 = base.transform.Find("RobotRun");
		动画控制器1 = child1Transform1.GetComponent<Animator>();
		动画控制器2 = child1Transform2.GetComponent<Animator>();
		动画控制器3 = child1Transform3.GetComponent<Animator>();
		颜色控制器1 = child1Transform1.GetComponent<SpriteRenderer>();
		颜色控制器2 = child1Transform2.GetComponent<SpriteRenderer>();
		颜色控制器3 = child1Transform3.GetComponent<SpriteRenderer>();
		名字 = GameObject.Find("CharacterName").GetComponent<TextMeshProUGUI>();
		Debug.Log(名字);
	}

	public void 设置当前显示的角色(int 序号)
	{
		当前选择的角色序号 += 序号;
		if (当前选择的角色序号 >= 每个角色的旋转位置设置.Length)
		{
			当前选择的角色序号 = 0;
		}
		if (当前选择的角色序号 < 0)
		{
			当前选择的角色序号 = 每个角色的旋转位置设置.Length - 1;
		}
	}

	public void 切换到下一个角色()
	{
		当前选择的角色序号++;
		if (当前选择的角色序号 >= 3)
		{
			当前选择的角色序号 = 0;
		}
		Debug.Log(当前选择的角色序号);
	}

	public void 切换到上一个角色()
	{
		当前选择的角色序号--;
		if (当前选择的角色序号 < 0)
		{
			当前选择的角色序号 = 2;
		}
		Debug.Log(当前选择的角色序号);
	}

	private void Update()
	{
		float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(duration), Time.deltaTime * 60f);
		if (当前选择的角色序号 == 0)
		{
			child1Transform2.localPosition = Vector3.Lerp(child1Transform2.localPosition, selectedPosition, t);
			child1Transform2.localScale = Vector3.Lerp(child1Transform2.localScale, selectedScale, t);
			动画控制器2.speed = 1f;
			颜色控制器2.color = Color.white;
			child1Transform1.localPosition = Vector3.Lerp(child1Transform1.localPosition, unSelectedPosition1, t);
			child1Transform1.localScale = Vector3.Lerp(child1Transform1.localScale, unSelectedScale1, t);
			动画控制器1.speed = 0f;
			颜色控制器1.color = Color.black;
			child1Transform3.localPosition = Vector3.Lerp(child1Transform3.localPosition, unSelectedPosition2, t);
			child1Transform3.localScale = Vector3.Lerp(child1Transform3.localScale, unSelectedScale2, t);
			动画控制器3.speed = 0f;
			颜色控制器3.color = Color.black;
			名字.SetText(GameRecords.GetCharacterName(0));
			Character.num = 0;
		}
		if (当前选择的角色序号 == 2)
		{
			child1Transform3.localPosition = Vector3.Lerp(child1Transform3.localPosition, new Vector3(0.3f, 0f, 0f), t);
			child1Transform3.localScale = Vector3.Lerp(child1Transform3.localScale, selectedScale, t);
			动画控制器3.speed = 1f;
			颜色控制器3.color = Color.white;
			child1Transform2.localPosition = Vector3.Lerp(child1Transform2.localPosition, unSelectedPosition1, t);
			child1Transform2.localScale = Vector3.Lerp(child1Transform2.localScale, unSelectedScale1, t);
			动画控制器2.speed = 0f;
			颜色控制器2.color = Color.black;
			child1Transform1.localPosition = Vector3.Lerp(child1Transform1.localPosition, unSelectedPosition2, t);
			child1Transform1.localScale = Vector3.Lerp(child1Transform1.localScale, unSelectedScale2, t);
			动画控制器1.speed = 0f;
			颜色控制器1.color = Color.black;
			名字.SetText(GameRecords.GetCharacterName(2));
			Character.num = 2;
		}
		if (当前选择的角色序号 == 1)
		{
			child1Transform1.localPosition = Vector3.Lerp(child1Transform1.localPosition, selectedPosition, t);
			child1Transform1.localScale = Vector3.Lerp(child1Transform1.localScale, selectedScale, t);
			动画控制器1.speed = 1f;
			颜色控制器1.color = Color.white;
			child1Transform3.localPosition = Vector3.Lerp(child1Transform3.localPosition, unSelectedPosition1, t);
			child1Transform3.localScale = Vector3.Lerp(child1Transform3.localScale, unSelectedScale1, t);
			动画控制器3.speed = 0f;
			颜色控制器3.color = Color.black;
			child1Transform2.localPosition = Vector3.Lerp(child1Transform2.localPosition, unSelectedPosition2, t);
			child1Transform2.localScale = Vector3.Lerp(child1Transform2.localScale, unSelectedScale2, t);
			动画控制器2.speed = 0f;
			颜色控制器2.color = Color.black;
			名字.SetText(GameRecords.GetCharacterName(1));
			Character.num = 1;
		}
	}
}
