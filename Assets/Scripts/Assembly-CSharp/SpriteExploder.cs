using System.Collections.Generic;
using Delaunay;
using UnityEngine;

public static class SpriteExploder
{
	private static Dictionary<Texture, Material> materials = new Dictionary<Texture, Material>();
	public static List<GameObject> GenerateTriangularPieces(GameObject source, int extraPoints = 0, int subshatterSteps = 0, Material mat = null)
	{
		List<GameObject> list = new List<GameObject>();
		if (mat == null)
		{
			mat = createFragmentMaterial(source);
		}
		Vector3 localScale = source.transform.localScale;
		source.transform.localScale = Vector3.one;
		Quaternion localRotation = source.transform.localRotation;
		source.transform.localRotation = Quaternion.identity;
		Vector2 velocity = source.GetComponent<Rigidbody2D>().velocity;
		PolygonCollider2D component = source.GetComponent<PolygonCollider2D>();
		BoxCollider2D component2 = source.GetComponent<BoxCollider2D>();
		List<Vector2> list2 = new List<Vector2>();
		List<Vector2> boundary = new List<Vector2>();
		if (component != null)
		{
			list2 = getPoints(component);
			boundary = getPoints(component);
		}
		else if (component2 != null)
		{
			list2 = getPoints(component2);
			boundary = getPoints(component2);
		}
		Rect rect = getRect(source);
		if (list2.Count == 3)
		{
			list2.Add((list2[0] + list2[1] + list2[2]) / 3f);
		}
		for (int i = 0; i < extraPoints; i++)
		{
			list2.Add(new Vector2(Random.Range(rect.width / -2f, rect.width / 2f), Random.Range(rect.height / -2f, rect.height / 2f)));
		}
		Voronoi voronoi = new Voronoi(list2, null, rect);
		new List<List<Vector2>>();
		foreach (Triangle item in voronoi.Triangles())
		{
			foreach (List<Vector2> item2 in ClipperHelper.clip(boundary, item))
			{
				list.Add(generateTriangularPiece(source, item2, velocity, localScale, localRotation, mat));
			}
		}
		List<GameObject> list3 = new List<GameObject>();
		if (subshatterSteps > 0)
		{
			subshatterSteps--;
			foreach (GameObject item3 in list)
			{
				list3.AddRange(GenerateTriangularPieces(item3, extraPoints, subshatterSteps, mat));
				if (Application.isPlaying) Object.Destroy(item3);
				else Object.DestroyImmediate(item3);
			}
		}
		else
		{
			list3 = list;
		}
		source.transform.localScale = localScale;
		source.transform.localRotation = localRotation;
		return list3;
	}

	private static GameObject generateTriangularPiece(GameObject source, List<Vector2> tri, Vector2 origVelocity, Vector3 origScale, Quaternion origRotation, Material mat)
	{
		GameObject gameObject = new GameObject(source.name + " piece");
		gameObject.transform.position = source.transform.position;
		gameObject.transform.rotation = source.transform.rotation;
		gameObject.transform.localScale = source.transform.localScale;
		MeshFilter meshFilter = (MeshFilter)gameObject.AddComponent(typeof(MeshFilter));
		gameObject.AddComponent(typeof(MeshRenderer));
		Mesh sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
		if (sharedMesh == null)
		{
			meshFilter.mesh = new Mesh();
			sharedMesh = meshFilter.sharedMesh;
		}
		Vector3[] array = new Vector3[3];
		int[] array2 = new int[3];
		array[0] = new Vector3(tri[0].x, tri[0].y, 0f);
		array[1] = new Vector3(tri[1].x, tri[1].y, 0f);
		array[2] = new Vector3(tri[2].x, tri[2].y, 0f);
		array2[0] = 0;
		array2[1] = 1;
		array2[2] = 2;
		sharedMesh.vertices = array;
		sharedMesh.triangles = array2;
		if (source.GetComponent<SpriteRenderer>() != null)
		{
			sharedMesh.uv = calcUV(array, source.GetComponent<SpriteRenderer>(), source.transform);
		}
		else
		{
			sharedMesh.uv = calcUV(array, source.GetComponent<MeshRenderer>(), source.transform);
		}
		gameObject.transform.localScale = origScale;
		gameObject.transform.localRotation = origRotation;
		Vector3 diff = calcPivotCenterDiff(gameObject);
		centerMeshPivot(gameObject, diff);
		sharedMesh.RecalculateBounds();
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = mat;
		meshFilter.mesh = sharedMesh;
		gameObject.AddComponent<PolygonCollider2D>().SetPath(0, new Vector2[3]
		{
			sharedMesh.vertices[0],
			sharedMesh.vertices[1],
			sharedMesh.vertices[2]
		});
		gameObject.AddComponent<Rigidbody2D>().velocity = origVelocity;
		if (Application.isPlaying) gameObject.AddComponent<FragmentLife>().Init(sharedMesh, mat);
		return gameObject;
	}

	public static List<GameObject> GenerateVoronoiPieces(GameObject source, int extraPoints = 0, int subshatterSteps = 0, Material mat = null)
	{
		List<GameObject> list = new List<GameObject>();
		if (mat == null)
		{
			mat = createFragmentMaterial(source);
		}
		Vector3 localScale = source.transform.localScale;
		source.transform.localScale = Vector3.one;
		Quaternion localRotation = source.transform.localRotation;
		source.transform.localRotation = Quaternion.identity;
		Vector2 velocity = source.GetComponent<Rigidbody2D>().velocity;
		PolygonCollider2D component = source.GetComponent<PolygonCollider2D>();
		BoxCollider2D component2 = source.GetComponent<BoxCollider2D>();
		List<Vector2> list2 = new List<Vector2>();
		List<Vector2> boundary = new List<Vector2>();
		if (component != null)
		{
			list2 = getPoints(component);
			boundary = getPoints(component);
		}
		else if (component2 != null)
		{
			list2 = getPoints(component2);
			boundary = getPoints(component2);
		}
		Rect rect = getRect(source);
		for (int i = 0; i < extraPoints; i++)
		{
			list2.Add(new Vector2(Random.Range(rect.width / -2f, rect.width / 2f), Random.Range(rect.height / -2f, rect.height / 2f)));
		}
		Voronoi voronoi = new Voronoi(list2, null, rect);
		new List<List<Vector2>>();
		foreach (List<Vector2> item in voronoi.Regions())
		{
			foreach (List<Vector2> item2 in ClipperHelper.clip(boundary, item))
			{
				list.Add(generateVoronoiPiece(source, item2, velocity, localScale, localRotation, mat));
			}
		}
		List<GameObject> list3 = new List<GameObject>();
		if (subshatterSteps > 0)
		{
			subshatterSteps--;
			foreach (GameObject item3 in list)
			{
				list3.AddRange(GenerateVoronoiPieces(item3, extraPoints, subshatterSteps));
				if (Application.isPlaying) Object.Destroy(item3);
				else Object.DestroyImmediate(item3);
			}
		}
		else
		{
			list3 = list;
		}
		source.transform.localScale = localScale;
		source.transform.localRotation = localRotation;
		return list3;
	}

	private static GameObject generateVoronoiPiece(GameObject source, List<Vector2> region, Vector2 origVelocity, Vector3 origScale, Quaternion origRotation, Material mat)
	{
		GameObject gameObject = new GameObject(source.name + " piece");
		gameObject.transform.position = source.transform.position;
		gameObject.transform.rotation = source.transform.rotation;
		gameObject.transform.localScale = source.transform.localScale;
		MeshFilter meshFilter = (MeshFilter)gameObject.AddComponent(typeof(MeshFilter));
		gameObject.AddComponent(typeof(MeshRenderer));
		Mesh sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
		if (sharedMesh == null)
		{
			meshFilter.mesh = new Mesh();
			sharedMesh = meshFilter.sharedMesh;
		}
		Voronoi region2 = new Voronoi(region, null, getRect(region));
		Vector3[] vertices = calcVerts(region2);
		int[] triangles = calcTriangles(region2);
		sharedMesh.vertices = vertices;
		sharedMesh.triangles = triangles;
		if (source.GetComponent<SpriteRenderer>() != null)
		{
			sharedMesh.uv = calcUV(vertices, source.GetComponent<SpriteRenderer>(), source.transform);
		}
		else
		{
			sharedMesh.uv = calcUV(vertices, source.GetComponent<MeshRenderer>(), source.transform);
		}
		gameObject.transform.localScale = origScale;
		gameObject.transform.localRotation = origRotation;
		Vector3 vector = calcPivotCenterDiff(gameObject);
		centerMeshPivot(gameObject, vector);
		sharedMesh.RecalculateBounds();
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = mat;
		meshFilter.mesh = sharedMesh;
		PolygonCollider2D polygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();
		polygonCollider2D.SetPath(0, calcPolyColliderPoints(region, vector));
		gameObject.AddComponent<Rigidbody2D>().velocity = origVelocity;
		if (Application.isPlaying) gameObject.AddComponent<FragmentLife>().Init(sharedMesh, mat);
		polygonCollider2D.isTrigger = true;
		gameObject.AddComponent<PieceControl>();
		return gameObject;
	}

	private static List<Vector2> getPoints(BoxCollider2D collider)
	{
		List<Vector2> list = new List<Vector2>();
		Vector2 offset = collider.offset;
		Vector2 size = collider.size;
		list.Add(new Vector2(offset.x - size.x / 2f, offset.y - size.y / 2f));
		list.Add(new Vector2(offset.x - size.x / 2f, offset.y + size.y / 2f));
		list.Add(new Vector2(offset.x + size.x / 2f, offset.y + size.y / 2f));
		list.Add(new Vector2(offset.x + size.x / 2f, offset.y - size.y / 2f));
		return list;
	}

	private static List<Vector2> getPoints(PolygonCollider2D collider)
	{
		List<Vector2> list = new List<Vector2>();
		Vector2[] path = collider.GetPath(0);
		foreach (Vector2 item in path)
		{
			list.Add(item);
		}
		return list;
	}

	private static List<Vector2> getRendererPoints(GameObject source)
	{
		List<Vector2> list = new List<Vector2>();
		Bounds bounds = source.GetComponent<Renderer>().bounds;
		list.Add(new Vector2(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y) - (Vector2)source.transform.position);
		list.Add(new Vector2(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y) - (Vector2)source.transform.position);
		list.Add(new Vector2(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y) - (Vector2)source.transform.position);
		list.Add(new Vector2(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y) - (Vector2)source.transform.position);
		return list;
	}

	private static Rect getRect(GameObject source)
	{
		Bounds bounds = source.GetComponent<Renderer>().bounds;
		return new Rect(bounds.extents.x * -1f, bounds.extents.y * -1f, bounds.size.x, bounds.size.y);
	}

	private static Rect getRect(List<Vector2> region)
	{
		Vector2 position = default;
		float x = region[0].x;
		float num = x;
		float y = region[0].y;
		float num2 = y;
		foreach (Vector2 item in region)
		{
			position += item;
			if (item.x < x)
			{
				x = item.x;
			}
			if (item.x > num)
			{
				num = item.x;
			}
			if (item.y < y)
			{
				y = item.y;
			}
			if (item.y > num2)
			{
				num2 = item.y;
			}
		}
		position /= (float)region.Count;
		return new Rect(size: new Vector2(num - x, num2 - y), position: position);
	}

	private static Vector2[] calcUV(Vector3[] vertices, SpriteRenderer sRend, Transform sTransform)
	{
		float num = sRend.bounds.extents.y * 2f / sTransform.localScale.y;
		float num2 = sRend.bounds.extents.x * 2f / sTransform.localScale.x;
		Vector3 vector = sTransform.InverseTransformPoint(new Vector3(sRend.bounds.center.x - sRend.bounds.extents.x, sRend.bounds.center.y - sRend.bounds.extents.y, 0f));
		Vector2[] array = new Vector2[vertices.Length];
		Vector2[] uv = sRend.sprite.uv;
		getUVRange(out var min, out var max, uv);
		for (int i = 0; i < vertices.Length; i++)
		{
			float target = (vertices[i].x - vector.x) / num2;
			target = scaleRange(target, 0f, 1f, min.x, max.x);
			float target2 = (vertices[i].y - vector.y) / num;
			target2 = scaleRange(target2, 0f, 1f, min.y, max.y);
			array[i] = new Vector2(target, target2);
		}
		return array;
	}

	private static Vector2[] calcUV(Vector3[] vertices, MeshRenderer mRend, Transform sTransform)
	{
		float num = mRend.bounds.extents.y * 2f / sTransform.localScale.y;
		float num2 = mRend.bounds.extents.x * 2f / sTransform.localScale.x;
		Vector3 vector = sTransform.InverseTransformPoint(new Vector3(mRend.bounds.center.x - mRend.bounds.extents.x, mRend.bounds.center.y - mRend.bounds.extents.y, 0f));
		Vector2[] array = new Vector2[vertices.Length];
		Vector2[] uv = sTransform.GetComponent<MeshFilter>().sharedMesh.uv;
		getUVRange(out var min, out var max, uv);
		for (int i = 0; i < vertices.Length; i++)
		{
			float target = (vertices[i].x - vector.x) / num2;
			target = scaleRange(target, 0f, 1f, min.x, max.x);
			float target2 = (vertices[i].y - vector.y) / num;
			target2 = scaleRange(target2, 0f, 1f, min.y, max.y);
			array[i] = new Vector2(target, target2);
		}
		return array;
	}

	private static void getUVRange(out Vector2 min, out Vector2 max, Vector2[] uv)
	{
		min = uv[0];
		max = uv[0];
		for (int i = 0; i < uv.Length; i++)
		{
			Vector2 vector = uv[i];
			if (vector.x < min.x)
			{
				min.x = vector.x;
			}
			if (vector.x > max.x)
			{
				max.x = vector.x;
			}
			if (vector.y < min.y)
			{
				min.y = vector.y;
			}
			if (vector.y > max.y)
			{
				max.y = vector.y;
			}
		}
	}

	private static float scaleRange(float target, float oldMin, float oldMax, float newMin, float newMax)
	{
		return target / ((oldMax - oldMin) / (newMax - newMin)) + newMin;
	}

	private static Vector3[] calcVerts(Voronoi region)
	{
		List<Site> sites = region.Sites()._sites;
		Vector3[] array = new Vector3[sites.Count];
		int num = 0;
		foreach (Site item in sites)
		{
			array[num++] = new Vector3(item.x, item.y, 0f);
		}
		return array;
	}

	private static int[] calcTriangles(Voronoi region)
	{
		int[] array = new int[region.Triangles().Count * 3];
		List<Site> sites = region.Sites()._sites;
		int num = 0;
		foreach (Triangle item in region.Triangles())
		{
			array[num++] = sites.IndexOf(item.sites[0]);
			array[num++] = sites.IndexOf(item.sites[1]);
			array[num++] = sites.IndexOf(item.sites[2]);
		}
		return array;
	}

	private static Vector2[] calcPolyColliderPoints(List<Vector2> points, Vector2 offset)
	{
		Vector2[] array = new Vector2[points.Count];
		for (int i = 0; i < points.Count; i++)
		{
			array[i] = points[i] + offset;
		}
		return array;
	}

	private static Vector3 calcPivotCenterDiff(GameObject target)
	{
		Vector3[] vertices = target.GetComponent<MeshFilter>().sharedMesh.vertices;
		Vector3 vector = default;
		for (int i = 0; i < vertices.Length; i++)
		{
			vector += vertices[i];
		}
		Vector3 vector2 = vector / vertices.Length;
		return target.transform.InverseTransformPoint(target.transform.position) - vector2;
	}

	private static void centerMeshPivot(GameObject target, Vector3 diff)
	{
		Mesh sharedMesh = target.GetComponent<MeshFilter>().sharedMesh;
		Vector3[] vertices = sharedMesh.vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] += diff;
		}
		sharedMesh.vertices = vertices;
		Vector3 vector = target.transform.InverseTransformPoint(target.transform.position);
		target.transform.localPosition = target.transform.TransformPoint(vector - diff);
	}

	private static void setFragmentMaterial(GameObject newSprite, GameObject source)
	{
		Material material = new Material(Shader.Find("Sprites/Default"));
		SpriteRenderer component = source.GetComponent<SpriteRenderer>();
		if (component != null)
		{
			material.SetTexture("_MainTex", component.sprite.texture);
			material.color = component.color;
		}
		else
		{
			material = source.GetComponent<MeshRenderer>().sharedMaterial;
		}
		newSprite.GetComponent<MeshRenderer>().sharedMaterial = material;
	}

	private static Material createFragmentMaterial(GameObject source)
	{
		SpriteRenderer component = source.GetComponent<SpriteRenderer>();
		if (component != null)
		{
			Texture texture = component.sprite.texture;
			if (!materials.TryGetValue(texture, out Material material) || material == null)
			{
				material = new Material(Shader.Find("Sprites/Default"));
				material.SetTexture("_MainTex", texture);
				materials[texture] = material;
			}
			return material;
		}
		return source.GetComponent<MeshRenderer>().sharedMaterial;
	}
}
