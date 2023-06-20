using UnityEngine;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-100)]
public class WindowAspect : MonoBehaviour
{
	private const float aspect = 2.5f;
	private Camera view;
	private Camera background;
	private int width, height;

	private void Awake()
	{
		view = GetComponent<Camera>();
		background = new GameObject("BlackBars").AddComponent<Camera>();
		background.transform.SetParent(transform, false);
		background.clearFlags = CameraClearFlags.SolidColor;
		background.backgroundColor = Color.black;
		background.cullingMask = 0;
		background.depth = view.depth - 1f;
		background.targetDisplay = view.targetDisplay;
		background.orthographic = true;
		background.allowHDR = false;
		background.allowMSAA = false;
	}

	private void OnEnable()
	{
		if (background != null) background.enabled = true;
		Apply();
	}

	private void Update()
	{
		if (width != Screen.width || height != Screen.height) Apply();
	}

	private void Apply()
	{
		if (view == null || Screen.width <= 0 || Screen.height <= 0) return;
		width = Screen.width;
		height = Screen.height;
		float ratio = (float)width / height;
		if (ratio > aspect)
		{
			float size = aspect / ratio;
			view.rect = new Rect((1f - size) / 2f, 0f, size, 1f);
		}
		else
		{
			float size = ratio / aspect;
			view.rect = new Rect(0f, (1f - size) / 2f, 1f, size);
		}
		view.aspect = aspect;
	}

	private void OnDisable()
	{
		if (background != null) background.enabled = false;
		if (view == null) return;
		view.rect = new Rect(0f, 0f, 1f, 1f);
		view.ResetAspect();
	}

	private void OnDestroy()
	{
		if (background != null) Destroy(background.gameObject);
	}
}
