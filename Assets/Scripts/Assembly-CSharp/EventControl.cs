using TMPro;
using UnityEngine;

public class EventControl : MonoBehaviour
{
	public static EventControl Instance;
	private TextMeshProUGUI route;
	public static float Route;
	public static TextMeshProUGUI piece_number;
	public static int piece_count;
	public static int scenesPassed;
	public static readonly int[] lootCount = new int[3];
	private TextMeshProUGUI[] lootNumbers = new TextMeshProUGUI[3];
	private int scene;
	public static bool isplay;
	public static int PiecesNeeded { get { return 10 + scenesPassed * 2; } }
	public static string DistanceText { get { return Route.ToString("F2") + "m"; } }

	public static bool isPaused { get; private set; }
	private static bool wasPlaying;

	private void Awake()
	{
		Instance = this;
		isPaused = false;
		isplay = false;
		wasPlaying = false;
		Time.timeScale = 1f;
		pause.menuKeys = true;
	}

	public static void PauseGame()
	{
		if (isPaused) return;
		wasPlaying = isplay;
		isPaused = true;
		isplay = false;
		Time.timeScale = 0f;
		pause.menuKeys = false;
	}

	public static void ContinueGame()
	{
		if (!isPaused) return;
		isPaused = false;
		isplay = wasPlaying;
		Time.timeScale = 1f;
		pause.menuKeys = true;
	}

	public static void ResetRun()
	{
		GameRecords.EndRun();
		isPaused = false;
		isplay = false;
		wasPlaying = false;
		Time.timeScale = 1f;
		pause.menuKeys = true;
		PlayerControl.Hp = 3;
		Route = 0f;
		piece_count = 0;
		scenesPassed = 0;
		for (int i = 0; i < lootCount.Length; i++) lootCount[i] = 0;
	}

	public static void FinishRun()
	{
		GameRecords.EndRun(true);
		isplay = false;
		if (Instance != null)
		{
			if (Instance.route != null) Instance.route.text = DistanceText;
			for (int i = 0; i < lootCount.Length; i++)
				if (Instance.lootNumbers[i] != null) Instance.lootNumbers[i].text = lootCount[i].ToString();
		}
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	private void OnApplicationQuit()
	{
		GameRecords.EndRun();
	}

	private void Start()
	{
		if (!GameRecords.IsRunning)
		{
			ResetRun();
			GameRecords.BeginRun();
		}
		piece_number = base.transform.Find("board_2").Find("piece_number").GetComponent<TextMeshProUGUI>();
		route = base.transform.Find("board_1").Find("route").GetComponent<TextMeshProUGUI>();
		scene = GroundControl.Instance.scene;
		Transform summary = PlayerControl.Instance.deathPanel.transform.Find("lootSummary");
		if (summary != null)
		{
			for (int i = 0; i < lootCount.Length; i++) lootNumbers[i] = summary.Find("stage" + i + "/count").GetComponent<TextMeshProUGUI>();
		}
		piece_count = 0;
		piece_number.text = "0/" + PiecesNeeded;
		route.text = DistanceText;
		isplay = false;
	}

	public void CollectPiece()
	{
		piece_count++;
		lootCount[scene]++;
		piece_number.text = Mathf.Min(piece_count, PiecesNeeded) + "/" + PiecesNeeded;
	}

	private void Update()
	{
		if (PlayerControl.Hp != 0 && isplay)
		{
			Route += GroundControl.Speed * Time.deltaTime * 3f;
			route.text = DistanceText;
		}
	}
}
