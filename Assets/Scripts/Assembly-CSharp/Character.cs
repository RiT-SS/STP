using UnityEngine;

public class Character : MonoBehaviour
{
	public static Character Instance;

	public static int num;

	private void Start()
	{
		if (Instance == null)
		{
			Instance = this;
			Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void Update()
	{
	}
}
