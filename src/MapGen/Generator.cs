using Godot;
using System;
using System.Collections.Generic;
public partial class Generator : GridMap
{
    enum Rooms : int { 
        empty = -1,
        room1 = 0,
        room2 = 1,
        room2c = 2,
        room3 = 3,
        room4 = 4,
        gatea = 5,
        gateb = 6,
        offices = 7,
        offices2 = 8,
        poffices = 9,
        toilets = 10,
        medibay = 11,
        ec = 12 //electrical center
    }
    // 90 degrees is 16 in matrix, 180 - 10, 270 - 22 and 0 is 0. 
    enum Angle : int {nul = 0, pi2 = 16, pi = 10, pi32 = 22}
    [Export]
    int width = 16, height = 16;
    [Export]
    int iterations = 64;
    class walker
    {
        public Vector3I dir;
        public Vector3I pos;

        public void RandomDirection()
        {
            Random r = new Random();
            int choice = r.Next(0, 4);
            switch (choice)
            {
                case 0:
                    dir = Vector3I.Back;
                    break;
                case 1:
                    dir = Vector3I.Left;
                    break;
                case 2:
                    dir =  Vector3I.Forward;
                    break;
                case 3:
                    dir = Vector3I.Right;
                    break;
            }
        }
    }
    List<walker> walkers = new List<walker>();
    [Export]
    int maxWalkers = 8;
    [Export(PropertyHint.Range, "0,1,0.05")]
    float walkerDirChange = 0.5f, walkerSpawn = 0.05f;
    [Export(PropertyHint.Range, "0,1,0.05")]
    float walkerDestroy = 0.05f;
    [Export(PropertyHint.Range, "0,1,0.05")]
    float percentFill = 0.2f;

    public override void _Ready()
    {
        Initialize();
        GenerateMap();
        GenerateRooms();
    }

    void Initialize()
    {
        walker newWalker = new walker();
        newWalker.RandomDirection();
        //spawn walker in the center of the map
        newWalker.pos = new Vector3I(width / 2, 0, height / 2);
        walkers.Add(newWalker);
    }

    void GenerateMap()
    {
        int iterationsCount = 0;
        do
        {
            foreach(walker w in walkers)
            {
                //fill with temporary rooms.
                SetCellItem(w.pos, (int)Rooms.room4);
            }

            Random r = new Random();
            for (int i = 0; i < walkers.Count; i++)
            {
                if (r.NextDouble() < walkerDestroy && walkers.Count > 1)
                {
                    walkers.RemoveAt(i);
                    break;
                }
            }
            for (int i = 0; i < walkers.Count; i++)
            {
                if (r.NextDouble() < walkerDirChange)
                {
                    walkers[i].RandomDirection();
                }
            }
            for (int i = 0; i < walkers.Count; i++)
            {
                if (r.NextDouble() < walkerSpawn && walkers.Count < maxWalkers)
                {
                    walker newWalker = new walker();
                    newWalker.RandomDirection();
                    newWalker.pos = walkers[i].pos;
                    walkers.Add(newWalker);
                }
            }
            // Walker movement
            for (int i = 0; i < walkers.Count; i++)
            {
                walkers[i].pos += walkers[i].dir;
            }
            for (int i = 0; i < walkers.Count; i++)
            {
                walkers[i].pos.X = Mathf.Clamp(walkers[i].pos.X, 1, width - 1);
                walkers[i].pos.Z = Mathf.Clamp(walkers[i].pos.Z, 1, height - 1);
            }

            if (GetUsedCellsByItem((int)Rooms.room4).Count / (float)(width*height) > percentFill)
            {
                break;
            }
            iterationsCount++;
        } while (iterationsCount < iterations);
    }

    void GenerateRooms()
    {
        Random r = new Random();
        int room1Amount;
        int room2Amount;
        int room2cAmount;
        int room3Amount;
        int room4Amount;

        room1Amount=room2Amount=room2cAmount=room3Amount=room4Amount=0;
        int[,] specialRoomAngle = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool north, south, east, west;
                north = south = east = west = false;
                if (GetCellItem(new Vector3I(x,0,y)) == (int)Rooms.room4) //if it is a temporary room
                {
                    if (x > 0)
                    {
                        west = GetCellItem(new Vector3I(x-1, 0, y)) != (int)Rooms.empty;
                    }
                    if (x < width)
                    {
                        east = GetCellItem(new Vector3I(x+1, 0, y)) != (int)Rooms.empty;
                    }
                    if (y > 0)
                    {
                        north = GetCellItem(new Vector3I(x, 0, y+1)) != (int)Rooms.empty;
                    }
                    if (y < width)
                    {
                        south = GetCellItem(new Vector3I(x, 0, y-1)) != (int)Rooms.empty;
                    }
                    if (north && south)
                    {
                        if (east && west)
                        {
                            //Room4
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room4, (int)Angle.nul);
                            specialRoomAngle[x, y] = (int)Angle.nul;
                            room4Amount++;
                        }
                        else if (east && !west) //Room3, pointing east
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room3, (int)Angle.pi2);
                            specialRoomAngle[x, y] = (int)Angle.pi2;
                            room3Amount++;
                        }
                        else if (!east && west) //Room3, pointing west
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room3, (int)Angle.pi32);
                            specialRoomAngle[x, y] = (int)Angle.pi32;
                            room3Amount++;
                        }
                        else //vertical Room2
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room2, (int)Angle.nul);
                            specialRoomAngle[x, y] = (int)Angle.nul;
                            room2Amount++;
                        }
                    }
                    else if (east && west)
                    {
                        if (north && !south) //Room3, pointing north
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room3, (int)Angle.nul);
                            specialRoomAngle[x, y] = (int)Angle.nul;
                            room3Amount++;
                        }
                        else if (!north && south) //Room3, pointing south
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room3, (int)Angle.pi);
                            specialRoomAngle[x, y] = (int)Angle.pi;
                            room3Amount++;
                        }
                        else //horizontal Room2
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room2, (int)Angle.pi2);
                            specialRoomAngle[x, y] = (int)Angle.pi2;
                            room2Amount++;
                        }
                    }
                    else if (north)
                    {
                        if (east) //Room2c, north-east
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room2c, (int)Angle.nul);
                            specialRoomAngle[x, y] = (int)Angle.nul;
                            room2cAmount++;
                        }
                        else if (west) //Room2c, north-west
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room2c, (int)Angle.pi32);
                            specialRoomAngle[x, y] = (int)Angle.pi32;
                            room2cAmount++;
                        }
                        else //Room1, north
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room1, (int)Angle.nul);
                            specialRoomAngle[x, y] = (int)Angle.nul;
                            room1Amount++;
                        }
                    }
                    else if (south)
                    {
                        if (east) //Room2c, south-east
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room2c, (int)Angle.pi2);
                            specialRoomAngle[x, y] = (int)Angle.pi2;
                            room2cAmount++;
                        }
                        else if (west) //Room2c, south-west
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room2c, (int)Angle.pi);
                            specialRoomAngle[x, y] = (int)Angle.pi;
                            room2cAmount++;
                        }
                        else //Room1, south
                        {
                            SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room1, (int)Angle.pi);
                            specialRoomAngle[x, y] = (int)Angle.pi;
                            room1Amount++;
                        }
                    }
                    else if (east) //Room1, east
                    {
                        SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room1, (int)Angle.pi2);
                        specialRoomAngle[x, y] = (int)Angle.pi2;
                        room1Amount++;
                    }
                    else if (west) //Room1, west
                    {
                        SetCellItem(new Vector3I(x, 0, y), (int)Rooms.room1, (int)Angle.pi32);
                        specialRoomAngle[x, y] = (int)Angle.pi32;
                        room1Amount++;
                    }
                }
            }
        }
        int i = 0;
        int j = 0;
        int k = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                switch (GetCellItem(new Vector3I(x, 0, y)))
                {
                    case 0:
                        
                        switch (i)
                        {
                            case 0:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.gatea, specialRoomAngle[x, y]);
                                i++;
                                break;
                            case 1:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.gateb,  specialRoomAngle[x, y]);
                                i++;
                                break;
                        }
                        break;
                    case 1:
                        
                        switch (j)
                        {
                            case 0:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.offices, specialRoomAngle[x, y]);
                                j++;
                                break;
                            case 1:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.offices2, specialRoomAngle[x, y]);
                                j++;
                                break;
                            case 2:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.poffices, specialRoomAngle[x, y]);
                                j++;
                                break;
                            case 4:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.toilets, specialRoomAngle[x, y]);
                                j++;
                                break;
                            case 5:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.medibay, specialRoomAngle[x, y]);
                                j++;
                                break;
                        }
                        break;
                    case 2:
                        
                        switch (k)
                        {
                            case 0:
                                SetCellItem(new Vector3I(x, 0, y), (int)Rooms.ec, specialRoomAngle[x, y]);
                                k++;
                                break;
                        }
                        break;
                }
            }
        }
    }
}
