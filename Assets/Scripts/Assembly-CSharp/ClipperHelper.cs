using System.Collections.Generic;
using ClipperLib;
using Delaunay;
using UnityEngine;

public static class ClipperHelper
{
	private static float multiplier = 1000f;

	public static List<List<Vector2>> clip(List<Vector2> boundary, Triangle piece)
	{
		List<List<IntPoint>> ppg = createPolygons(boundary);
		List<List<IntPoint>> ppg2 = createPolygons(piece);
		List<List<IntPoint>> list = new List<List<IntPoint>>();
		Clipper clipper = new Clipper();
		clipper.AddPaths(ppg2, PolyType.ptClip, closed: true);
		clipper.AddPaths(ppg, PolyType.ptSubject, closed: true);
		clipper.Execute(ClipType.ctIntersection, list, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
		List<List<Vector2>> list2 = new List<List<Vector2>>();
		foreach (List<IntPoint> item in list)
		{
			List<Vector2> list3 = new List<Vector2>();
			foreach (IntPoint item2 in item)
			{
				list3.Add(new Vector2(item2.X, item2.Y) / multiplier);
			}
			list2.Add(list3);
		}
		return list2;
	}

	public static List<List<Vector2>> clip(List<Vector2> boundary, List<Vector2> region)
	{
		List<List<IntPoint>> ppg = createPolygons(boundary);
		List<List<IntPoint>> ppg2 = createPolygons(region);
		List<List<IntPoint>> list = new List<List<IntPoint>>();
		Clipper clipper = new Clipper();
		clipper.AddPaths(ppg2, PolyType.ptClip, closed: true);
		clipper.AddPaths(ppg, PolyType.ptSubject, closed: true);
		clipper.Execute(ClipType.ctIntersection, list, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
		List<List<Vector2>> list2 = new List<List<Vector2>>();
		foreach (List<IntPoint> item in list)
		{
			List<Vector2> list3 = new List<Vector2>();
			foreach (IntPoint item2 in item)
			{
				list3.Add(new Vector2(item2.X, item2.Y) / multiplier);
			}
			list2.Add(list3);
		}
		return list2;
	}

	private static List<List<IntPoint>> createPolygons(List<Vector2> source)
	{
		List<List<IntPoint>> list = new List<List<IntPoint>>(1);
		list.Add(new List<IntPoint>(source.Count));
		foreach (Vector2 item in source)
		{
			list[0].Add(new IntPoint(item.x * multiplier, item.y * multiplier));
		}
		return list;
	}

	private static List<List<IntPoint>> createPolygons(Triangle tri)
	{
		List<List<IntPoint>> list = new List<List<IntPoint>>(1);
		list.Add(new List<IntPoint>(3));
		for (int i = 0; i < 3; i++)
		{
			list[0].Add(new IntPoint(tri.sites[i].x * multiplier, tri.sites[i].y * multiplier));
		}
		return list;
	}
}
