using UnityEngine;

public class EnemyControl : MonoBehaviour
{
	private Animator ani;

	public Transform player;

	private void Start()
	{
		player = GameObject.Find("player").transform;
		ani = GetComponent<Animator>();
	}

	private void Update()
	{
		if (PlayerControl.Hp != 0 && EventControl.isplay)
		{
			if (Vector3.Distance(player.position, base.transform.position) <= 3f)
			{
				ani.SetBool("isAttack", value: true);
			}
			else
			{
				ani.SetBool("isAttack", value: false);
			}
		}
	}
}
