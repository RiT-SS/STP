using System.Collections.Generic;
using UnityEngine;

namespace Delaunay.Geo
{
	public sealed class Polygon
	{
		private List<Vector2> _vertices;

		public Polygon(List<Vector2> vertices)
		{
			_vertices = vertices;
		}

		public float Area()
		{
			return Mathf.Abs(SignedDoubleArea() * 0.5f);
		}

		public Winding Winding()
		{
			float num = SignedDoubleArea();
			if (num < 0f)
			{
				return Delaunay.Geo.Winding.CLOCKWISE;
			}
			if (num > 0f)
			{
				return Delaunay.Geo.Winding.COUNTERCLOCKWISE;
			}
			return Delaunay.Geo.Winding.NONE;
		}

		private float SignedDoubleArea()
		{
			int count = _vertices.Count;
			float num = 0f;
			for (int i = 0; i < count; i++)
			{
				int index = (i + 1) % count;
				Vector2 vector = _vertices[i];
				Vector2 vector2 = _vertices[index];
				num += vector.x * vector2.y - vector2.x * vector.y;
			}
			return num;
		}
	}
}
