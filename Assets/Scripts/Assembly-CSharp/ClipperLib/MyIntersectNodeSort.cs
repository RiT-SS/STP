using System.Collections.Generic;

namespace ClipperLib
{
	public class MyIntersectNodeSort : IComparer<IntersectNode>
	{
		public int Compare(IntersectNode node1, IntersectNode node2)
		{
			return (int)(node2.Pt.Y - node1.Pt.Y);
		}
	}
}
