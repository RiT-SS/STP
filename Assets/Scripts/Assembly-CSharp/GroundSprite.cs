using UnityEngine;

public class GroundSprite : MonoBehaviour
{
	private Sprite sprite;

	public void SetSprite(Sprite value)
	{
		if (sprite != null) Destroy(sprite);
		sprite = value;
	}

	private void OnDestroy()
	{
		if (sprite != null) Destroy(sprite);
	}
}
