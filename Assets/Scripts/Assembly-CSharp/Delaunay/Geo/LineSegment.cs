using UnityEngine;

namespace Delaunay.Geo
{
	public sealed class LineSegment
	{
		public Vector2? p0;

		public Vector2? p1;

		public static int CompareLengths_MAX(LineSegment segment0, LineSegment segment1)
		{
			float num = Vector2.Distance(segment0.p0.Value, segment0.p1.Value);
			float num2 = Vector2.Distance(segment1.p0.Value, segment1.p1.Value);
			if (num < num2)
			{
				return 1;
			}
			if (num > num2)
			{
				return -1;
			}
			return 0;
		}

		public static int CompareLengths(LineSegment edge0, LineSegment edge1)
		{
			return -CompareLengths_MAX(edge0, edge1);
		}

		public LineSegment(Vector2? p0, Vector2? p1)
		{
			this.p0 = p0;
			this.p1 = p1;
		}
	}
}
