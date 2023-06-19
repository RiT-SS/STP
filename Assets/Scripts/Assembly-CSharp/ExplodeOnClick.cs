using UnityEngine;

[RequireComponent(typeof(Explodable))]
public class ExplodeOnClick : MonoBehaviour
{
	private Explodable _explodable;

	private void Start()
	{
		_explodable = GetComponent<Explodable>();
	}

	private void OnMouseDown()
	{
		_explodable.explode();
		Object.FindObjectOfType<ExplosionForce>().doExplosion(base.transform.position);
	}
}
