using UnityEngine;

public class DarkHoleControl : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		foreach (Transform item in base.transform)
		{
			if (item.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
			{
				Object.Destroy(item.gameObject);
			}
		}
	}
}
