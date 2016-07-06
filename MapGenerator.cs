using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

    private int _mapX = 100;
    private int _mapZ = 100;
    private int _expectRoomNum = 5;
    private int _roomMinRange = 4;
    private int _roomMaxRange = 7;

    private int[,] _floorInfo;
    private int[,] _mapInfo;//0:none -1:wall N:room N

	private class cCorridorExit {
		public int X;
		public int Z;
		public int deltaX;
		public int deltaZ;
	}

    private class cRoom {
		public cRoom (
			int idValue,
			int xValue,
			int zValue,
			int widthValue,
			int lengthValue
		) {
			id = idValue;
			x = xValue;
			z = zValue;
			xWidth = widthValue;
			zLength = lengthValue;
		}
        public int id;
        public int x;
        public int z;
        public int xWidth;
        public int zLength; 
    }

    private class cCorridor {
		public cCorridor (
			int idValue,
			int xValue,
			int zValue,
			int delX,
			int delZ,
			int corridorLength
		) {
			id = idValue;
			x = xValue;
			z = zValue;
			deltaX = delX;
			deltaZ = delZ;
			exitList = new List<cCorridorExit>();
		}
		public int id;
        public int x;
        public int z;
        public int deltaX;
        public int deltaZ;
		public List<cCorridorExit> exitList;
    }

	int currentRoomId;
	int currentCorridorId;
    cRoom currentRoom;
	cCorridor currentCorridor;
    List<cRoom> roomList;
	List<cCorridor> corridorList;

    void Awake() {
        Create();
        PrintLogMapInfo();
    }

    public void SetMapParams (int mapWidth, int mapLength, int roomNum, int roomMin, int roomMax) {
        _mapX = mapWidth;
        _mapZ = mapLength;
        _expectRoomNum = roomNum;
        _roomMinRange = roomMin;
        _roomMaxRange = roomMax;
    }

    public void Create() {
        InitMapInfo();
        GenerateMap();
    }

    void InitMapInfo() {
        _floorInfo = new int[_mapX, _mapZ];
        _mapInfo = new int[_mapX, _mapZ];
        for (int i=0; i<_mapX; i++) {
            for (int j=0; j<_mapZ; j++) {
                _floorInfo[i, j] = 0;
                _mapInfo[i, j] = 0;
            }
        }
    }

    void GenerateMap() {
        currentRoomId = 1;
		currentCorridorId = 1;
        roomList = new List<cRoom>();

		currentRoom = new cRoom(
			currentRoomId,
			Random.Range(_mapX / 3, _mapX / 3 * 2),
			Random.Range(_mapZ / 3, _mapZ / 3 * 2),
			Random.Range(_roomMinRange, _roomMaxRange),
			Random.Range(_roomMinRange, _roomMaxRange)
		);

        UpdateMapInfo(currentRoom);
        roomList.Add(currentRoom);

        for (int n=_expectRoomNum; n>0; n--) {

        }
    }

    void UpdateMapInfo (cRoom room) {
        for (int i=room.x; i<room.x+room.xWidth; i++) {
            for (int j=room.z; j<room.z+room.zLength; j++) {
                if (i == room.x ||
                    i == room.x+room.xWidth - 1 ||
                    j == room.z ||
                    j == room.z+room.zLength - 1)
                {
                    _mapInfo[i, j] = -1;
                }
                else
                {
                    _mapInfo[i, j] = room.id;
                }
            }
        }
    }

    void PrintLogMapInfo() {
        string debugStr;
        for (int i = 0; i < _mapX; i++)
        {
            debugStr = " ";
            for (int j = 0; j < _mapZ; j++)
            {
                debugStr += _mapInfo[i, j].ToString();
                debugStr += " ";
            }
            Debug.Log(debugStr);
        }
    }

    bool CreateCorridorRecursivly () {
        //randomly choose a room
        cRoom room = roomList[Random.Range(0, roomList.Count - 1)];
		int deltaX, deltaZ;
		int wallX, wallZ;//target wall pos
		switch (Random.Range(1,4)) {
		//randomly choose a direction (for corridor)
		case 1:
			//up
			deltaX = 1;
			deltaZ = 0;
			wallX = Random.Range (room.x + 1, room.x + room.xWidth - 2);
			wallZ = room.z + room.zLength - 1;
			break;
		case 2:
			//down
			deltaX = -1;
			deltaZ = 0;
			wallX = Random.Range (room.x + 1, room.x + room.xWidth - 2);
			wallZ = room.z;
			break;
		case 3:
			//left
			deltaX = 0;
			deltaZ = -1;
			wallX = room.x;
			wallZ = Random.Range (room.z + 1, room.z + room.zLength - 2);
			break;
		case 4:
			//right
			deltaX = 0;
			deltaZ = 1;
			wallX = room.x+room.xWidth-1;
			wallZ = Random.Range (room.z + 1, room.z + room.zLength - 2);
			break;
		default:
			deltaX = 0;
			deltaZ = 0;
			Debug.LogError ("Invalid Direction!");
			break;
		}
		currentCorridor = new cCorridor (
			currentRoomId,
			wallX + deltaX,
			wallZ + deltaZ,
			deltaX,
			deltaZ
		);


        return true;
    }

	bool CheckCorridorSpace (int startX, int startZ, int delX, int delZ, int corridorLength) {
		
	}
}
