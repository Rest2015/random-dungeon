using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour {

	public enum E_RoomType {
		normal = 0,//square
		entry,
		exit,
	}
	public enum E_TileType {
		empty = 0,
		floor,
		corner_LeftDown,
		corner_LeftUp,
		corner_RightDown,
		corner_RightUp,
		wall_left,
		wall_up,
		wall_right,
		wall_down,
		corridor_Vertical,
		corridor_Horizontal,
	}
	public enum E_TileMaterialType {
		dirt,
	}
	public enum E_Direction {
		Unset,
		Up,
		Down,
		Left,
		Right,
	}
	public enum E_MapElementType {
		Room,
		Corridor,
	}
	E_TileType[,] layerTile;
	public int dungeonWidth;
	public int dungeonLength;
	public int tileWidth;
	public int minCorridorLength;
	public int maxCorridorLength;
	public int minRoomLength;
	public int maxRoomLength;
	public int minRoomWidth;
	public int maxRoomWidth;

	public int playerStartPosX;
	public int playerStartPosZ;

	#region tile prefabs
	//normal room
	public Transform[] tile_dirt_floor;
	public Transform[] tile_dirt_wall;
	#endregion

	public Transform[] foodPrefabs;
	public Transform exitPrefab;

	int layerMinRoomSum;//地牢创建第一步至少创建的房间数目
	List<Transform> layerRooms;//当前地牢中所有房间
	List<Transform> layerCorridors;
	
	Transform currentElement; //current creating element

	public void SetDungeonLayer (int minRoomSum) {
		layerMinRoomSum = minRoomSum;
	}

	public void CreateDungeonLayer () {
		layerRooms = new List<Transform> ();
		layerCorridors = new List<Transform> ();
		layerTile = new E_TileType[dungeonWidth,dungeonLength];
		//建立初始房间
		CreateRoom (dungeonWidth/2, dungeonLength/2, 5, 5, E_RoomType.entry, E_TileMaterialType.dirt);
		CreateSurroundingCorridor ();

		playerStartPosX = (dungeonWidth / 2 + 2) * tileWidth;
		playerStartPosZ = (dungeonLength / 2 + 2) * tileWidth;

		//loop generating demanded rooms
		for (int i=0; i<layerMinRoomSum; i++) {
			for (int j=0; j<100; j++) {
				int tryCorridorIndex = Random.Range(0,layerCorridors.Count-1);
				if (CreateRoomFromMapElement(layerCorridors[tryCorridorIndex], 
				                             RandomizeRoomType(), 
				                             E_TileMaterialType.dirt,
				                             layerCorridors[tryCorridorIndex].GetComponent<MapElement>().walls.Count-Random.Range(1,3))) {
					break;
				}
			}

			CreateSurroundingCorridor ();
		}

		//
		for (int x=0; x<100; x++) {
			//只在通道尽头一格三个方向生成，但禁止十字路口
			int tryCorridorIndex = Random.Range(0,layerCorridors.Count-1);
			int tryCorridorExitCount = layerCorridors[tryCorridorIndex].GetComponent<MapElement>().exitCount;
			if (tryCorridorExitCount < 3) {//禁止十字路口
				if (CreateRoomFromMapElement(layerCorridors[tryCorridorIndex], 
				                             E_RoomType.exit, 
				                             E_TileMaterialType.dirt,
				                             layerCorridors[tryCorridorIndex].GetComponent<MapElement>().walls.Count-Random.Range(1,4-tryCorridorExitCount))) {
					break;
				}
			}
		}
		CreateSurroundingCorridor ();

		//loop generating room at corridor end
		for (int i=0; i<layerCorridors.Count; i++) {
			if (layerCorridors[i].GetComponent<MapElement>().exitCount < 2) {
				int count3 = 20;
				while (!CreateRoomFromMapElement(layerCorridors[i], 
				                                 RandomizeRoomType(), 
				                                 E_TileMaterialType.dirt, 
				                                 layerCorridors[i].GetComponent<MapElement>().walls.Count-Random.Range(1,3))) {
					Debug.Log("fail 1 time...");
					count3--;
					if (count3<0) {
						break;
					}
				}
			}
		}

		//删除剩余空过道
		for (int i=0; i<layerCorridors.Count; i++) {
			if (layerCorridors[i].GetComponent<MapElement>().exitCount < 2) {
				GameObject deleteTarget = layerCorridors[i].gameObject;
				CancelRecentMapElement(E_MapElementType.Corridor, deleteTarget);
			}
		}

		SetLayerRooms ();
	}

	//在房间内配置细节
	void SetLayerRooms () {
		for (int i=0; i<layerRooms.Count; i++) {
			switch(layerRooms[i].GetComponent<MapElement>().roomType) {
			case E_RoomType.normal:
				if (Random.Range(0,5)==1) {
					CreateItem(foodPrefabs[Random.Range(0,foodPrefabs.Length)], layerRooms[i], false);
				}
				break;
			case E_RoomType.entry:
				break;
			case E_RoomType.exit:
				CreateItem (exitPrefab, layerRooms[i], true);
				break;
			default:
				break;
			}
		}
	}

	//在房间内生成道具（药水，卷轴，入口，出口等）
	bool CreateItem (Transform itemPrefab, Transform room, bool removeFloor) {
		int targetX = Random.Range (0, room.GetComponent<MapElement>().tile.GetLength (0));
		int targetZ = Random.Range (0, room.GetComponent<MapElement>().tile.GetLength (1));

		for (int i=0; i<20; i++) {
			if (room.GetComponent<MapElement>().hasItem[targetX, targetZ]) {
				targetX = Random.Range (0, room.GetComponent<MapElement>().tile.GetLength (0));
				targetZ = Random.Range (0, room.GetComponent<MapElement>().tile.GetLength (1));
			}
			else {
				break;
			}
			if (i>=19) {
				return false;
			}
		}
		Transform item = Instantiate(itemPrefab, 
		                             room.GetComponent<MapElement>().tile[targetX, targetZ].position, 
		                             Quaternion.identity) as Transform;
		item.parent = room;
		room.GetComponent<MapElement>().hasItem [targetX, targetZ] = true;
		if (removeFloor) {
			room.GetComponent<MapElement>().tile[targetX, targetZ].gameObject.SetActive(false);
		}
		return true;
	}

	//随机确定房间类型
	E_RoomType RandomizeRoomType () {
		return E_RoomType.normal;
	}

	//为最新创建的房间建立相连的过道
	void CreateSurroundingCorridor () {
		int count = Random.Range(1,3);
		while (count>0) {
			for (int j=0; j<100; j++) {
				if (CreateCorridorFromMapElement(layerRooms[layerRooms.Count-1], E_TileMaterialType.dirt)) {
					break;
				}
			}
			count--;
		}
	}

	//generate a room from a wall on its dir
	bool CreateRoomFromMapElement (Transform mapElem, E_RoomType roomType, E_TileMaterialType tileMaterialType, int index = -1) {

		List<Transform> chosenWalls = mapElem.GetComponent<MapElement> ().walls;
		Transform testedWall;
		if (index == -1) {
			testedWall = chosenWalls[Random.Range(0,chosenWalls.Count-1)];
		}
		else {
			testedWall = chosenWalls[index];
		}

		int wallX = Mathf.RoundToInt (testedWall.position.x)/tileWidth;
		int wallZ = Mathf.RoundToInt (testedWall.position.z)/tileWidth;
		int deltaX, deltaZ;
		E_Direction WallDir = CheckWallDirection (testedWall);
		//corridor direction
		if (WallDir == E_Direction.Unset) {
			return false;
		}
		switch (WallDir) {
		case E_Direction.Up:
			deltaX = 0;
			deltaZ = 1;
			break;
		case E_Direction.Down:
			deltaX = 0;
			deltaZ = -1;
			break;
		case E_Direction.Left:
			deltaX = -1;
			deltaZ = 0;
			break;
		case E_Direction.Right:
			deltaX = 1;
			deltaZ = 0;
			break;
		default:
			deltaX = 0;
			deltaZ = 0;
			return false;
		}

		int roomWidth = Random.Range(minRoomWidth, maxRoomWidth);
		int roomLength = Random.Range(minRoomLength, maxRoomLength);
		int roomStartX, roomStartZ, doorDistance;
		//calculate room startpos and doorpos 根据门离两边的距离算出房间的起始位置
		if (deltaX == 0) {
			doorDistance = Random.Range (1, roomWidth-2);//避开房间拐角
			roomStartX = wallX-doorDistance;
			if (deltaZ == 1) {
				roomStartZ = wallZ+deltaZ;
			}
			else {
				roomStartZ = wallZ+deltaZ*roomLength;
			}
		}
		else {
			doorDistance = Random.Range (1, roomLength-2);
			roomStartZ = wallZ - doorDistance;
			if (deltaX == 1) {
				roomStartX = wallX+deltaX;
			}
			else {
				roomStartX = wallX+deltaX*roomWidth;
			}
		}
		//calculate a small entrance corridor space and offset roompos
		//random corridor length
		int corridorEntranceLength = Random.Range(1,7);
		int corridorStartX = wallX + deltaX;
		int corridorStartZ = wallZ + deltaZ;
		//offset room
		roomStartX += deltaX * corridorEntranceLength;
		roomStartZ += deltaZ * corridorEntranceLength;

		//material type
		Transform[] floorTiles, wallTiles;
		SetTileMaterial (tileMaterialType, out floorTiles, out wallTiles);

		//create room entrance corridor
		if (CreateCorridor(corridorStartX, corridorStartZ, corridorEntranceLength, WallDir, tileMaterialType)) {
			layerCorridors[layerCorridors.Count-1].GetComponent<MapElement>().removedWall = 
				RemoveWallFromMapElement(wallX+deltaX*corridorEntranceLength, 
				                         wallZ+deltaZ*corridorEntranceLength, 
			                             deltaX, 
			                             deltaZ,
				                         layerCorridors[layerCorridors.Count-1].GetComponent<MapElement>());
		}
		else {
			return false;
		}

		//为了使房间能顺利建立将入口处layerTile临时替换为empty
		E_TileType tmpTileType = layerTile [wallX + deltaX * corridorEntranceLength, wallZ + deltaZ * corridorEntranceLength];
		layerTile [wallX + deltaX * corridorEntranceLength, wallZ + deltaZ * corridorEntranceLength] = E_TileType.empty;
		
		//create room
		if (CreateRoom (roomStartX, roomStartZ, roomWidth, roomLength, roomType, tileMaterialType)) {
			RemoveWallFromMapElement(wallX+deltaX*(corridorEntranceLength+1), 
			                         wallZ+deltaZ*(corridorEntranceLength+1), 
			                         -deltaX, 
			                         -deltaZ, 
			                         layerRooms[layerRooms.Count-1].GetComponent<MapElement>());
		}
		else {
			GameObject recentCorridorGO = layerCorridors[layerCorridors.Count-1].gameObject;
			CancelRecentMapElement(E_MapElementType.Corridor, recentCorridorGO);
			return false;
		}
		//入口处恢复
		layerTile [wallX + deltaX * corridorEntranceLength, wallZ + deltaZ * corridorEntranceLength] = tmpTileType;

		mapElem.GetComponent<MapElement>().exitCount++;
		chosenWalls.Remove(testedWall);
		Destroy(testedWall.gameObject);
		return true;
	}

	bool CreateCorridorFromMapElement (Transform mapElem, E_TileMaterialType tileMaterialType, int index = -1) {

		List<Transform> chosenWalls = mapElem.GetComponent<MapElement> ().walls;
		Transform testedWall;
		if (index == -1) {
			testedWall = chosenWalls[Random.Range(0,chosenWalls.Count-1)];
		}
		else {
			testedWall = chosenWalls[index];
		}

		int wallX = Mathf.RoundToInt(testedWall.position.x)/tileWidth;
		int wallZ = Mathf.RoundToInt(testedWall.position.z)/tileWidth;
		int corridorStartX, corridorStartZ;

		//如果处于房间拐角则不能创建
		if (layerTile[wallX,wallZ] == E_TileType.corner_LeftDown ||
		    layerTile[wallX,wallZ] == E_TileType.corner_LeftUp ||
		    layerTile[wallX,wallZ] == E_TileType.corner_RightDown ||
		    layerTile[wallX,wallZ] == E_TileType.corner_RightUp) {
			return false;
		}

		E_Direction WallDir = CheckWallDirection (testedWall);
		//corridor direction
		if (WallDir == E_Direction.Unset) {
			return false;
		}
		switch (WallDir) {
		case E_Direction.Up:
			corridorStartX = wallX;
			corridorStartZ = wallZ+1;
			break;
		case E_Direction.Down:
			corridorStartX = wallX;
			corridorStartZ = wallZ-1;
			break;
		case E_Direction.Left:
			corridorStartX = wallX-1;
			corridorStartZ = wallZ;
			break;
		case E_Direction.Right:
			corridorStartX = wallX+1;
			corridorStartZ = wallZ;
			break;
		default:
			corridorStartX = wallX;
			corridorStartZ = wallZ;
			return false;
		}
		//random corridor length
		int corridorLength = Random.Range(minCorridorLength,maxCorridorLength);

		if (CreateCorridor (corridorStartX, corridorStartZ, corridorLength, WallDir, tileMaterialType)) {
			mapElem.GetComponent<MapElement>().exitCount++;
			chosenWalls.Remove(testedWall);
			//新建立的corridor将保存被隐藏的wall
			layerCorridors[layerCorridors.Count-1].GetComponent<MapElement>().removedWall = testedWall;
			testedWall.gameObject.SetActive(false);
			return true;
		}
		else {
			return false;
		}
	}

	//create a corridor from a startpoint and dir
	bool CreateCorridor (int corridorStartX, int corridorStartZ, int corridorLength, E_Direction corridorDir, E_TileMaterialType tileMaterialType) {
		int corridorDirDeltaX, corridorDirDeltaZ;
		if (corridorDir == E_Direction.Unset) {
			return false;
		}
		switch (corridorDir) {
		case E_Direction.Up:
			corridorDirDeltaX = 0;
			corridorDirDeltaZ = 1;
			break;
		case E_Direction.Down:
			corridorDirDeltaX = 0;
			corridorDirDeltaZ = -1;
			break;
		case E_Direction.Left:
			corridorDirDeltaX = -1;
			corridorDirDeltaZ = 0;
			break;
		case E_Direction.Right:
			corridorDirDeltaX = 1;
			corridorDirDeltaZ = 0;
			break;
		default:
			corridorDirDeltaX = 0;
			corridorDirDeltaZ = 0;
			return false;
		}
		
		for (int i=0; i<corridorLength+1; i++) {
			//check for enough space
			switch (corridorDir) {
			case E_Direction.Up:
				if (corridorStartX-1<0 || 
				    corridorStartX+1>layerTile.GetLength(0)-1 ||
				    corridorStartZ+i<0 ||
				    corridorStartZ+i>layerTile.GetLength(1)-1) {
					return false;
				}
				if (layerTile[corridorStartX, corridorStartZ+i] != E_TileType.empty ||
				    layerTile[corridorStartX+1, corridorStartZ+i] != E_TileType.empty ||
				    layerTile[corridorStartX-1, corridorStartZ+i] != E_TileType.empty) {
					return false;
				}
				break;
			case E_Direction.Down:
				if (corridorStartX-1<0 || 
				    corridorStartX+1>layerTile.GetLength(0)-1 ||
				    corridorStartZ-i<0 ||
				    corridorStartZ-i>layerTile.GetLength(1)-1) {
					return false;
				}
				if (layerTile[corridorStartX, corridorStartZ-i] != E_TileType.empty ||
				    layerTile[corridorStartX+1, corridorStartZ-i] != E_TileType.empty ||
				    layerTile[corridorStartX-1, corridorStartZ-i] != E_TileType.empty) {
					return false;
				}
				break;
			case E_Direction.Left:
				if (corridorStartX-i<0 || 
				    corridorStartX-i>layerTile.GetLength(0)-1 ||
				    corridorStartZ-1<0 ||
				    corridorStartZ+1>layerTile.GetLength(1)-1) {
					return false;
				}
				if (layerTile[corridorStartX-i,corridorStartZ] != E_TileType.empty ||
				    layerTile[corridorStartX-i,corridorStartZ+1] != E_TileType.empty ||
				    layerTile[corridorStartX-i,corridorStartZ-1] != E_TileType.empty) {
					return false;
				}
				break;
			case E_Direction.Right:
				if (corridorStartX+i<0 || 
				    corridorStartX+i>layerTile.GetLength(0)-1 ||
				    corridorStartZ-1<0 ||
				    corridorStartZ+1>layerTile.GetLength(1)-1) {
					return false;
				}
				if (layerTile[corridorStartX+i,corridorStartZ] != E_TileType.empty ||
				    layerTile[corridorStartX+i,corridorStartZ+1] != E_TileType.empty ||
				    layerTile[corridorStartX+i,corridorStartZ-1] != E_TileType.empty) {
					return false;
				}
				break;
			default:
				break;
			}
		}
		
		//material type
		Transform[] floorTiles, wallTiles;
		SetTileMaterial (tileMaterialType, out floorTiles, out wallTiles);
		
		//generate corridor root
		MapElement currentCorridorScript = GenerateElementRoot (E_MapElementType.Corridor, corridorStartX, corridorStartZ);

		currentCorridorScript.startX = corridorStartX;
		currentCorridorScript.startZ = corridorStartZ;
		currentCorridorScript.dirX = corridorDirDeltaX;
		currentCorridorScript.dirZ = corridorDirDeltaZ;

		//generate tiles
		currentCorridorScript.tileSingle = new Transform[corridorLength];
		currentCorridorScript.walls = new List<Transform> ();
		for (int i=0; i<corridorLength; i++) {
			currentCorridorScript.tileSingle[i] = RandomlyCreateTile(corridorDirDeltaX*i,corridorDirDeltaZ*i,floorTiles);
			if (corridorDirDeltaX == 0) {
				layerTile[corridorStartX, corridorStartZ+corridorDirDeltaZ*i] = E_TileType.corridor_Vertical;
				RandomlyCreateWall(corridorDirDeltaX*i,corridorDirDeltaZ*i,wallTiles, E_Direction.Left, currentCorridorScript.walls);
				RandomlyCreateWall(corridorDirDeltaX*i,corridorDirDeltaZ*i,wallTiles, E_Direction.Right, currentCorridorScript.walls);
			}
			else {
				layerTile[corridorStartX+corridorDirDeltaX*i, corridorStartZ] = E_TileType.corridor_Horizontal;
				RandomlyCreateWall(corridorDirDeltaX*i,corridorDirDeltaZ*i,wallTiles, E_Direction.Up, currentCorridorScript.walls);
				RandomlyCreateWall(corridorDirDeltaX*i,corridorDirDeltaZ*i,wallTiles, E_Direction.Down, currentCorridorScript.walls);
			}
			//corridor end wall
			if (i == corridorLength-1) {
				RandomlyCreateWall(corridorDirDeltaX*i,corridorDirDeltaZ*i,wallTiles, corridorDir, currentCorridorScript.walls);
			}
		}
		//corridor is created with a exit (beginning)
		currentCorridorScript.exitCount = 1;
		return true;
	}

	//create a room from a startpoint(leftdown), width, and length
	bool CreateRoom (int startX, 
	                 int startZ, 
	                 int extendX, 
	                 int extendZ, 
	                 E_RoomType roomType, 
	                 E_TileMaterialType tileMaterialType) {

		if (extendX<=1 || extendZ<=1) {//room has wall so it need enough size
			return false;
		}
		if (startX+extendX>dungeonWidth || startZ+extendZ>dungeonLength || startX<0 || startZ<0) {
			return false;// out of the dungeon bound
		}
		//enough space in case of wall thickness
		for (int i=-1; i<extendX+1; i++) {
			for (int j=-1; j<extendZ+1; j++) {
				if (startX+i<0 ||
				    startX+i>layerTile.GetLength(0)-1 ||
				    startZ+j<0 ||
				    startZ+j>layerTile.GetLength(1)-1) {
					return false;//防止数组越界
				}
				if (layerTile[startX+i,startZ+j] != E_TileType.empty) {
					return false;//not enough space for room
				}
			}
		}

		//material type
		Transform[] floorTiles, wallTiles;
		SetTileMaterial (tileMaterialType, out floorTiles, out wallTiles);

		//generate room root
		MapElement currentRoomScript = GenerateElementRoot (E_MapElementType.Room, startX, startZ);

		currentRoomScript.tile = new Transform[extendX,extendZ];
		currentRoomScript.hasItem = new bool[extendX, extendZ];
		currentRoomScript.walls = new List<Transform>();
		for (int i=0; i<extendX; i++) {
			for (int j=0; j<extendZ; j++) {
				currentRoomScript.tile[i,j] = RandomlyCreateTile(i,j,floorTiles);
				currentRoomScript.hasItem[i,j] = false;
				if (i+j==0) {
					//corner leftdown
					layerTile[startX+i,startZ+j] = E_TileType.corner_LeftDown;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Left, currentRoomScript.walls);
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Down, currentRoomScript.walls);
				}
				else if (i+j==extendX+extendZ-2) {
					//corner rightup
					layerTile[startX+i,startZ+j] = E_TileType.corner_RightUp;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Right, currentRoomScript.walls);
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Up, currentRoomScript.walls);
				}
				else if (i-j==extendX-1) {
					//corner rightdown
					layerTile[startX+i,startZ+j] = E_TileType.corner_RightDown;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Right, currentRoomScript.walls);
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Down, currentRoomScript.walls);
				}
				else if (j-i==extendZ-1) {
					//corner leftup
					layerTile[startX+i,startZ+j] = E_TileType.corner_LeftUp;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Left, currentRoomScript.walls);
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Up, currentRoomScript.walls);
				}
				else if (i==0) {
					//wall left
					layerTile[startX+i,startZ+j] = E_TileType.wall_left;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Left, currentRoomScript.walls);
				}
				else if (j==0) {
					//wall down
					layerTile[startX+i,startZ+j] = E_TileType.wall_down;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Down, currentRoomScript.walls);
				}
				else if (i==extendX-1) {
					//wall right
					layerTile[startX+i,startZ+j] = E_TileType.wall_right;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Right, currentRoomScript.walls);
				}
				else if (j==extendZ-1) {
					//wall up
					layerTile[startX+i,startZ+j] = E_TileType.wall_up;
					RandomlyCreateWall(i,j,wallTiles, E_Direction.Up, currentRoomScript.walls);
				}
				else {
					//floor
					layerTile[startX+i,startZ+j] = E_TileType.floor;
				}
			}
		}
		currentRoomScript.exitCount = 0;
		currentRoomScript.roomType = roomType;

//		switch (roomType) {
//		case E_RoomType.normal:
//
//			break;
//		default:
//			break;
//		}

		return true;
	}

	MapElement GenerateElementRoot (E_MapElementType elementType, int startX, int startZ) {
		switch(elementType) {
		case E_MapElementType.Room:
			currentElement = new GameObject("Room").transform;
			currentElement.parent = transform;
			currentElement.position = new Vector3 (startX*tileWidth, 0, startZ*tileWidth);
			layerRooms.Add (currentElement);
			currentElement.name = "Room_" + layerRooms.Count.ToString ();
			break;
		case E_MapElementType.Corridor:
			currentElement = new GameObject("Corridor").transform;
			currentElement.parent = transform;
			currentElement.position = new Vector3 (startX*tileWidth, 0, startZ*tileWidth);
			layerCorridors.Add (currentElement);
			currentElement.name = "Corridor_" + layerCorridors.Count.ToString();
			break;
		default:
			return null;
		}

		MapElement currentMapElementScript = currentElement.gameObject.AddComponent<MapElement> ();
		currentMapElementScript.startX = startX;
		currentMapElementScript.startZ = startZ;

		return currentMapElementScript;
	}

	Transform RandomlyCreateTile (int x, int z, Transform[] tilePrefab) {
		Transform newTile = Instantiate (tilePrefab[Random.Range(0,tilePrefab.Length)], 
		                                 new Vector3(currentElement.position.x + x*tileWidth, 0, currentElement.position.z + z*tileWidth), 
		                                 Quaternion.identity) as Transform;
		newTile.parent = currentElement;
		return newTile;
	}

	void RandomlyCreateWall (int x, int z, Transform[] tilePrefab, E_Direction wallDir, List<Transform> targetList) {
		Transform wall = RandomlyCreateTile (x, z, tilePrefab);
		switch (wallDir) {
		case E_Direction.Down:
			wall.Rotate(new Vector3(0,270f,0));
			break;
		case E_Direction.Left:
			break;
		case E_Direction.Right:
			wall.Rotate(new Vector3(0,180f,0));
			break;
		case E_Direction.Up:
			wall.Rotate(new Vector3(0,90f,0));
			break;
		default:
			break;
		}
		targetList.Add (wall);
	}

	E_Direction CheckWallDirection (Transform testedWall) {
		switch (Mathf.RoundToInt(testedWall.localEulerAngles.y)) {
		case 0:
			//left
			return E_Direction.Left;
		case 90:
			//up
			return E_Direction.Up;
		case 180:
			//right
			return E_Direction.Right;
		case 270:
			//down
			return E_Direction.Down;
		default:
			Debug.LogError("CreateCorridorFromWall with wrong angles");
			return E_Direction.Unset;
		}
	}

	void SetTileMaterial (E_TileMaterialType tileMaterialType, out Transform[] floorTiles, out Transform[] wallTiles) {
		switch (tileMaterialType) {
		case E_TileMaterialType.dirt:
			floorTiles = tile_dirt_floor;
			wallTiles = tile_dirt_wall;
			break;
		default:
			floorTiles = tile_dirt_floor;
			wallTiles = tile_dirt_wall;
			break;
		}
	}

	Transform RemoveWallFromMapElement (int targetX, int targetZ, int dirX, int dirZ, MapElement targetElem) {
		Transform removedWall = null;
		//remove a wall from mapelement on its pos and dir
		for (int i=0; i<targetElem.walls.Count; i++) {
			if (Mathf.RoundToInt(targetElem.walls[i].position.x)/tileWidth == targetX && 
			    Mathf.RoundToInt(targetElem.walls[i].position.z)/tileWidth == targetZ) {
				if (dirX == 0) {
					if (dirZ == 1) {
						if (Mathf.RoundToInt(targetElem.walls[i].localEulerAngles.y) == 90) {
							removedWall = targetElem.walls[i];
							removedWall.gameObject.SetActive(false);
							targetElem.walls.RemoveAt(i);
						}
					}
					else {
						if (Mathf.RoundToInt(targetElem.walls[i].localEulerAngles.y) == 270) {
							removedWall = targetElem.walls[i];
							removedWall.gameObject.SetActive(false);
							targetElem.walls.RemoveAt(i);
						}
					}
				}
				else {
					if (dirX == 1) {
						if (Mathf.RoundToInt(targetElem.walls[i].localEulerAngles.y) == 180) {
							removedWall = targetElem.walls[i];
							removedWall.gameObject.SetActive(false);
							targetElem.walls.RemoveAt(i);
						}
					}
					else {
						if (Mathf.RoundToInt(targetElem.walls[i].localEulerAngles.y) == 0) {
							removedWall = targetElem.walls[i];
							removedWall.gameObject.SetActive(false);
							targetElem.walls.RemoveAt(i);
						}
					}
				}
			}
		}
		//remove a wall increase an exit
		targetElem.exitCount++;
		return removedWall;
	}

	void CancelRecentMapElement (E_MapElementType targetType, GameObject target) {
		//GameObject target;
		switch (targetType) {
		case E_MapElementType.Corridor:
			//target = layerCorridors[layerCorridors.Count-1].gameObject;
			MapElement mapElemCorridor = target.GetComponent<MapElement>();
			for (int i=0; i<mapElemCorridor.tileSingle.Length; i++) {
				layerTile[mapElemCorridor.startX + mapElemCorridor.dirX * i,mapElemCorridor.startZ + mapElemCorridor.dirZ * i] = E_TileType.empty;
			}
			//补上被删除通道所连接的mapElem的缺口
			mapElemCorridor.removedWall.gameObject.SetActive(true);
			//layerCorridors.RemoveAt(layerCorridors.Count-1);
			layerCorridors.Remove(target.transform);
			Destroy(target);
			break;
		case E_MapElementType.Room:
			//target = layerRooms[layerRooms.Count-1].gameObject;
			MapElement mapElemRoom = target.GetComponent<MapElement>();
			for (int i=0; i<mapElemRoom.tile.GetLength(0); i++) {
				for (int j=0; j<mapElemRoom.tile.GetLength(1); j++) {
					layerTile[mapElemRoom.startX+i, mapElemRoom.startZ+j] = E_TileType.empty;
				}
			}
			layerRooms.Remove(target.transform);
			Destroy(target);
			break;
		default:
			break;
		}
	}
}
