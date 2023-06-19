using UnityEngine;
using UnityEngine.UI;

public class FillValueNumber : MonoBehaviour
{
	public Image TargetImage;

	private void Update()
	{
		float num = TargetImage.fillAmount * 100f;
		base.gameObject.GetComponent<Text>().text = num.ToString("F0");
	}
}
