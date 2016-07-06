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

    private class cRoom {
        public int id;
        public int x;
        public int z;
        public int xWidth;
        public int zLength;
        public int exit;   
    }

    private class cCorridor {
        public int x;
        public int z;
        public int endX;
        public int endZ;
        public int exit;
    }

    int currentRoomId = 0;
    cRoom currentRoom;
    List<cRoom> roomList;

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
        roomList = new List<cRoom>();

        currentRoom = new cRoom();
        currentRoom.x = Random.Range(_mapX / 3, _mapX / 3 * 2);
        currentRoom.z = Random.Range(_mapZ / 3, _mapZ / 3 * 2);
        currentRoom.xWidth = Random.Range(_roomMinRange, _roomMaxRange);
        currentRoom.zLength = Random.Range(_roomMinRange, _roomMaxRange);
        currentRoom.id = currentRoomId;
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
        return true;
    }
}
