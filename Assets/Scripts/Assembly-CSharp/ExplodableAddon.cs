using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Explodable))]
public abstract class ExplodableAddon : MonoBehaviour
{
	protected Explodable explodable;

	private void Start()
	{
		explodable = GetComponent<Explodable>();
	}

	public abstract void OnFragmentsGenerated(List<GameObject> fragments);
}
