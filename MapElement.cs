using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapElement : MonoBehaviour {

	public DungeonGenerator.E_MapElementType elementType;
	public int startX;
	public int startZ;
	public List<Transform> walls;
	public List<Transform> doors;
	public int exitCount;

	//room
	public Transform[,] tile;
	public DungeonGenerator.E_RoomType roomType;
	public bool[,] hasItem; 

	//corridor
	public Transform[] tileSingle;
	public int dirX;
	public int dirZ;
	public Transform removedWall;

}
