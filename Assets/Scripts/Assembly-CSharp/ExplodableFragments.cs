using System.Collections.Generic;
using UnityEngine;

public class ExplodableFragments : ExplodableAddon
{
	public override void OnFragmentsGenerated(List<GameObject> fragments)
	{
		foreach (GameObject fragment in fragments)
		{
			Explodable obj = fragment.AddComponent<Explodable>();
			obj.shatterType = explodable.shatterType;
			obj.fragmentLayer = explodable.fragmentLayer;
			obj.sortingLayerName = explodable.sortingLayerName;
			obj.orderInLayer = explodable.orderInLayer;
			fragment.layer = explodable.gameObject.layer;
			obj.fragmentInEditor();
		}
	}
}
