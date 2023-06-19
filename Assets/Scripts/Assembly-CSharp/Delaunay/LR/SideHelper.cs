namespace Delaunay.LR
{
	public class SideHelper
	{
		public static Side Other(Side leftRight)
		{
			if (leftRight != Side.LEFT)
			{
				return Side.LEFT;
			}
			return Side.RIGHT;
		}
	}
}
