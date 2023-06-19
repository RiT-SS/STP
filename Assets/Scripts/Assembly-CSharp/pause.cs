using UnityEngine;
using UnityEngine.SceneManagement;

public class pause : MonoBehaviour
{
	public GameObject menu;
	public GameObject pauseButton;
	[SerializeField]
	public static bool menuKeys = true;

	private void Start()
	{
		menuKeys = true;
	}

	public void pauseBtn()
	{
		if (PlayerControl.Hp <= 0 || menu.activeSelf) return;
		EventControl.PauseGame();
		menu.SetActive(true);
		pauseButton.SetActive(false);
	}

	public void returnBtn()
	{
		if (!menu.activeSelf) return;
		EventControl.ContinueGame();
		menu.SetActive(false);
		menuKeys = true;
		pauseButton.SetActive(true);
	}

	public void restart()
	{
		EventControl.ResetRun();
		GameRecords.BeginRun();
		SceneManager.LoadScene(1);
	}

	public void returnToMenu()
	{
		EventControl.ResetRun();
		SceneManager.LoadScene(0);
	}

	private void Update()
	{
		if (PlayerControl.Hp > 0 && Input.GetKeyDown(KeyCode.Escape))
		{
			if (menu.activeSelf) returnBtn();
			else pauseBtn();
		}
	}
}
