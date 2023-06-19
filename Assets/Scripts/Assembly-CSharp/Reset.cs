using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			EventControl.ResetRun();
			GameRecords.BeginRun();
			SceneManager.LoadScene(1);
		}
	}
}
