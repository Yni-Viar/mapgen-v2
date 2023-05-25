using Godot;
using System;
using System.Collections.Generic;
public partial class Generator : Node
{
    [Export]
    static ulong seed = 0;
    static RandomNumberGenerator rng = new RandomNumberGenerator();
    public Generator() //should assign seed in constructor.
    {
        rng.Seed = seed;
    }
    enum RoomTypes : int { 
        empty = -1,
        room1 = 0,
        room2 = 1,
        room2c = 2,
        room3 = 3,
        room4 = 4
    }
    [Export]
    int width = 12, height = 12;
    [Export]
    int iterations = 32;
    struct room
    {
        public RoomTypes type;
        public float angle;
    }
    class walker
    {
        public Vector3I dir;
        public Vector3I pos;

        public void RandomDirection()
        {
            int choice = rng.RandiRange(0, 3);
            GD.Print(choice);
            switch (choice)
            {
                case 0:
                    dir = Vector3I.Back;
                    break;
                case 1:
                    dir = Vector3I.Left;
                    break;
                case 2:
                    dir = Vector3I.Forward;
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

    int[,] mapGen; //array, containing room points.
    int roomsQuantity = 0;
    public override void _Ready()
    {
        Initialize();
        GenerateMap();
        GenerateRooms();
    }

    void Initialize()
    {
        mapGen = new int[width, height];
        for (int x = 0; x < width; x++) //by default, there is no room.
        {
            for (int y = 0; y < height; y++)
            {
                mapGen[x, y] = 0;
            }
        }
        walker newWalker = new walker();
        newWalker.RandomDirection();
        //spawn walker in the center of the map
        newWalker.pos = new Vector3I(width / 2, 0, height / 2);
        walkers.Add(newWalker);
    }

    void GenerateMap()
    {
        int iterationsCount = 0;
        while (iterationsCount < iterations)
        {
            foreach(walker w in walkers)
            {
                //fill with temporary rooms.
                mapGen[w.pos.X, w.pos.Z] = 1;
                roomsQuantity++;
            }
            for (int i = 0; i < walkers.Count; i++)
            {
                if (rng.Randf() < walkerDestroy && walkers.Count > 1)
                {
                    walkers.RemoveAt(i);
                    break;
                }
            }
            for (int i = 0; i < walkers.Count; i++)
            {
                if (rng.Randf() < walkerDirChange)
                {
                    walkers[i].RandomDirection();
                }
            }
            for (int i = 0; i < walkers.Count; i++)
            {
                if (rng.Randf() < walkerSpawn && walkers.Count < maxWalkers)
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
            if (roomsQuantity / (float)(width*height) > percentFill)
            {
                break;
            }
            iterationsCount++;
        }
        //Debug
        /*for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GD.Print(mapGen[y, x]);
            }
            GD.Print(" ");
        }*/
    }

    void GenerateRooms()
    {
        room[,] rooms = new room[width, height];
        for (int x = 0; x < width; x++) //default values
        {
            for (int y = 0; y < height; y++)
            {
                rooms[x, y].type = RoomTypes.empty;
                rooms[x, y].angle = 0;
            }
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool north, south, east, west;
                north = south = east = west = false;
                if (mapGen[x, y] == 1) //if it is a temporary room
                {
                    if (x > 0)
                    {
                        west = mapGen[x-1, y] != 0;
                    }
                    if (x < width - 1)
                    {
                        east = mapGen[x+1, y] != 0;
                    }
                    if (y < height - 1)
                    {
                        north = mapGen[x, y+1] != 0;
                    }
                    if (y > 0)
                    {
                        south = mapGen[x, y-1] != 0;
                    }
                    if (north && south)
                    {
                        if (east && west) //Room4
                        {
                            float[] avAngle = new float[] {0, 90, 180, 270};
                            rooms[x, y].type = RoomTypes.room4;
                            rooms[x, y].angle = avAngle[rng.RandiRange(0, 3)];
                        }
                        else if (east && !west) //Room3, pointing east
                        {
                            rooms[x, y].type = RoomTypes.room3;
                            rooms[x, y].angle = 90;
                        }
                        else if (!east && west) //Room3, pointing west
                        {
                            rooms[x, y].type = RoomTypes.room3;
                            rooms[x, y].angle = 270;
                        }
                        else //vertical Room2
                        {
                            float[] avAngle = new float[] {0, 180};
                            rooms[x, y].type = RoomTypes.room2;
                            rooms[x, y].angle = avAngle[rng.RandiRange(0, 1)];
                        }
                    }
                    else if (east && west)
                    {
                        if (north && !south) //Room3, pointing north
                        {
                            rooms[x, y].type = RoomTypes.room3;
                            rooms[x, y].angle = 0;
                        }
                        else if (!north && south) //Room3, pointing south
                        {
                            rooms[x, y].type = RoomTypes.room3;
                            rooms[x, y].angle = 180;
                        }
                        else //horizontal Room2
                        {
                            float[] avAngle = new float[] {90, 270};
                            rooms[x, y].type = RoomTypes.room2;
                            rooms[x, y].angle = avAngle[rng.RandiRange(0, 1)];
                        }
                    }
                    else if (north)
                    {
                        if (east) //Room2c, north-east
                        {
                            rooms[x, y].type = RoomTypes.room2c;
                            rooms[x, y].angle = 0;
                        }
                        else if (west) //Room2c, north-west
                        {
                            rooms[x, y].type = RoomTypes.room2c;
                            rooms[x, y].angle = 270;
                        }
                        else //Room1, north
                        {
                            rooms[x, y].type = RoomTypes.room1;
                            rooms[x, y].angle = 0;
                        }
                    }
                    else if (south)
                    {
                        if (east) //Room2c, south-east
                        {
                            rooms[x, y].type = RoomTypes.room2c;
                            rooms[x, y].angle = 90;
                        }
                        else if (west) //Room2c, south-west
                        {
                            rooms[x, y].type = RoomTypes.room2c;
                            rooms[x, y].angle = 180;
                        }
                        else //Room1, south
                        {
                            rooms[x, y].type = RoomTypes.room1;
                            rooms[x, y].angle = 180;
                        }
                    }
                    else if (east) //Room1, east
                    {
                        rooms[x, y].type = RoomTypes.room1;
                        rooms[x, y].angle = 90;
                    }
                    else if (west) //Room1, west
                    {
                        rooms[x, y].type = RoomTypes.room1;
                        rooms[x, y].angle = 270;
                    }
                }
            }
        }

        int currRoom1 = 0;
        int currRoom2 = 0;
        int currRoom2c = 0;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                StaticBody3D rm;
                switch (rooms[x, y].type)
                {
                    case RoomTypes.room1:
                        
                        switch (currRoom1)
                        {
                            case 0:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room1/rz_exit_1_gatea.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom1++;
                                break;
                            case 1:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room1/rz_exit_1_gateb.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom1++;
                                break;
                            default:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room1/rz_room_1_endroom.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                break;
                        }
                        break;
                    case RoomTypes.room2:
                        
                        switch (currRoom2)
                        {
                            case 0:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2/rz_room_2_offices.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom2++;
                                break;
                            case 1:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2/rz_room_2_offices_2.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom2++;
                                break;
                            case 2:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2/rz_room_2_poffices.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom2++;
                                break;
                            case 4:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2/rz_room_2_toilets.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom2++;
                                break;
                            case 5:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2/rz_room_2_medibay.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom2++;
                                break;
                            default:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2/rz_room_2.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                break;
                        }
                        break;
                    case RoomTypes.room2c:
                        switch (currRoom2c)
                        {
                            case 0:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2c/rz_room_2c_ec.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                currRoom2c++;
                                break;
                            default:
                                rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room2c/rz_room_2c.tscn").Instantiate();
                                rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                                rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                                AddChild(rm);
                                break;
                        }
                        break;
                    case RoomTypes.room3:
                        rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room3/rz_room_3.tscn").Instantiate();
                        rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                        rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                        AddChild(rm);
                        break;
                    case RoomTypes.room4:
                        rm = (StaticBody3D)ResourceLoader.Load<PackedScene>("res://Assets/Rooms/room4/rz_room_4.tscn").Instantiate();
                        rm.Position = new Vector3(x * 20.48f, 0, y*20.48f);
                        rm.RotationDegrees = new Vector3(0, rooms[x, y].angle, 0);
                        AddChild(rm);
                        break;
                }
            }
        }
    }
}
