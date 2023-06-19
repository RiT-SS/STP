using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartButton : MonoBehaviour
{
	public void StartGame()
	{
		EventControl.ResetRun();
		GameRecords.BeginRun();
		SceneManager.LoadScene(1);
	}

	public void Selection()
	{
		SceneManager.LoadScene(4);
	}

	public void returnMenu()
	{
		SceneManager.LoadScene(0);
	}

	public void exitGame()
	{
		Application.Quit();
	}
}
