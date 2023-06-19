using System.Collections.Generic;
using UnityEngine;

public class FragmentLife : MonoBehaviour
{
	private static Dictionary<Material, int> counts = new Dictionary<Material, int>();
	private Mesh mesh;
	private Material material;
	private float life;

	public void Init(Mesh value, Material mat)
	{
		mesh = value;
		material = mat;
		counts.TryGetValue(material, out int count);
		counts[material] = count + 1;
	}

	private void Update()
	{
		life += Time.deltaTime;
		if (life >= 6f || transform.position.y < -12f) Destroy(gameObject);
	}

	private void OnDestroy()
	{
		if (mesh != null) Destroy(mesh);
		if (material != null && counts.TryGetValue(material, out int count))
		{
			if (count > 1) counts[material] = count - 1;
			else
			{
				counts.Remove(material);
				Destroy(material);
			}
		}
	}
}
