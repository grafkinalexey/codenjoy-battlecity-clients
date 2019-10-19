using System;
using System.Linq;
using System.Collections.Generic;
using WebSocket4Net;

namespace CodeBattleNetLibrary
{
    public class GameClientBattlecity
    {
        private readonly WebSocket _socket;
        private event Action OnUpdate;

        public Elements[,] Map { get; private set; }
        public int MapSize { get; private set; }
        public int PlayerX { get; private set; }
        public int PlayerY { get; private set; }

        public GameClientBattlecity(string url)
        {
            MapSize = 0;
            var _server = url.Replace("http", "ws").Replace("board/player/", "ws?user=").Replace("?code=", "&code=");

            _socket =
                new WebSocket(_server);
            _socket.MessageReceived += (s, e) => { ParseField(e.Message); };
        }

        private void ParseField(string rawField)
        {
            rawField = rawField.Substring(6);
            int size = (int)Math.Sqrt(rawField.Length);
            if (MapSize != size)
            {
                Map = new Elements[size, size];
                MapSize = size;
            }

            int rawPosition = 0;
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    Map[i, j] = CharToBlock(rawField[rawPosition]);

                    if (IsPlayerCoords(Map[i, j]))
                    {
                        PlayerX = i;
                        PlayerY = j;
                    }

                    rawPosition++;
                }
            }

            OnUpdate?.Invoke();
        }

        protected bool IsPlayerCoords(Elements block) => block == Elements.TANK_DOWN ||
                                                                block == Elements.TANK_LEFT ||
                                                                block == Elements.TANK_RIGHT ||
                                                                block == Elements.TANK_UP;

        protected Elements CharToBlock(char c) =>
            Enum.IsDefined(typeof(Elements), (int)c)
                ? (Elements)c
                : Elements.Unknown;

        public void Run(Action handler)
        {
            OnUpdate += handler;
            _socket.Open();
        }

        public void SendActions(string commands)
        {
            _socket.Send(commands);
        }

        public string Up() => "UP";

        public string Down() => "DOWN";

        public string Right() => "RIGHT";

        public string Left() => "LEFT";

        public string Act() => "ACT";

        public string Blank() => "";

        public Point GetPlayerTank() => new Point(PlayerX, PlayerY);

        public List<Point> GetOtherPlayersTanks() =>
            FindingCoordinatesOfElements(
                Elements.OTHER_TANK_DOWN,
                Elements.OTHER_TANK_LEFT,
                Elements.OTHER_TANK_RIGHT,
                Elements.OTHER_TANK_UP);

        public List<Point> GetBotsTanks() =>
            FindingCoordinatesOfElements(
                Elements.AI_TANK_DOWN,
                Elements.AI_TANK_LEFT,
                Elements.AI_TANK_RIGHT,
                Elements.AI_TANK_UP);


        public List<Point> GetAllOtherTanks()
        {
            List<Point> allTanks = new List<Point>();
            allTanks.AddRange(GetOtherPlayersTanks());
            allTanks.AddRange(GetBotsTanks());

            return allTanks;
        }

        public List<Point> GetNearestTanks(int depth)
        {
            var me = GetPlayerTank();
            var otherTanks = GetAllOtherTanks();

            var result = otherTanks
                .Where(tank => tank.X >= me.X - depth && tank.X <= me.X + depth)
                .Where(tank => tank.Y >= me.Y - depth && tank.Y <= me.Y + depth)
                .ToList();

            if (result == null || result.Count() == 0)
            {
                if (depth <= 9)
                {
                    return GetNearestTanks(depth + 3);
                }

                return otherTanks;
            }

            return result;
        }

        public Point GetNearestTank()
        {
            var me = GetPlayerTank();
            var otherTanks = GetNearestTanks(3); // GetAllOtherTanks();
            var deltaPoint = new Point(int.MaxValue, int.MaxValue);
            var index = 0;

            for (var i = 0; i < otherTanks.Count; i++)
            {
                if (me.X == otherTanks[i].X)
                {
                    deltaPoint.X = 0;
                    deltaPoint.Y = 0;
                    index = i;
                    break;
                }
                else if (me.Y == otherTanks[i].Y)
                {
                    deltaPoint.X = 0;
                    deltaPoint.Y = 0;
                    index = i;
                    break;
                }
                else if (Math.Abs(me.X - otherTanks[i].X) < deltaPoint.X &&
                    Math.Abs(me.Y - otherTanks[i].Y) < deltaPoint.Y)
                {
                    deltaPoint.X = Math.Abs(me.X - otherTanks[i].X);
                    deltaPoint.Y = Math.Abs(me.Y - otherTanks[i].Y);
                    index = i;
                } else
                {
                    index = i;
                }
            }

            Console.WriteLine("other: " + otherTanks[index]);
            Console.WriteLine("me: " + me);

            return otherTanks[index];
        }

        public Point GetNewPoint(Point me, Point finalPoint, out string direction)
        {
            var deltaX = Math.Abs(me.X - finalPoint.X);
            var deltaY = Math.Abs(me.Y - finalPoint.Y);
            int x = 0;
            int y = 0;

            if (deltaX == 0)
            {
                if(me.Y > finalPoint.Y)
                {
                    y = me.Y - 1;
                    direction = Up();
                }
                else
                {
                    y = me.Y + 1;
                    direction = Down();
                }
                return new Point(me.X, y);
            }

            if (deltaY == 0)
            {
                if (me.X > finalPoint.X)
                {
                    x = me.X - 1;
                    direction = Left();
                }
                else
                {
                    x = me.X + 1;
                    direction = Right();
                }
                return new Point(x, me.Y);
            }

            if (deltaX < deltaY)
            {
                if (me.Y > finalPoint.Y)
                {
                    y = me.Y - 1;
                    direction = Up();
                }
                else
                {
                    y = me.Y + 1;
                    direction = Down();
                }
                return new Point(me.X, y);
            }

            if (me.X > finalPoint.X)
            {
                x = me.X - 1;
                direction = Left();
            }
            else
            {
                x = me.X + 1;
                direction = Right();
            }
            return new Point(x, me.Y);
            
        }

        public string GetDirection(Point me, Point meNew)
        {
            var deltaX = Math.Abs(me.X - meNew.X);
            var deltaY = Math.Abs(me.Y - meNew.Y);

            if (deltaX < deltaY)
            {
                if (meNew.X >= MapSize-2)
                    return Left();

                if (meNew.X < 0)
                    return Right();

                return me.X > meNew.X ? Left() : Right();
            }

            if (meNew.Y >= MapSize-2)
                return Up();

            if (meNew.Y < 0)
                return Down();

            return me.Y > meNew.Y ? Up() : Down();
        }

        public Point GetNewPointWithoutWall(Point point)
        {
            if (!IsWallAt(point.X - 1, point.Y))
            {
                return new Point(point.X - 1, point.Y);
            }

            if (!IsWallAt(point.X + 1, point.Y))
            {
                return new Point(point.X + 1, point.Y);
            }

            if (!IsWallAt(point.X, point.Y - 1))
            {
                return new Point(point.X, point.Y - 1);
            }

            return new Point(point.X, point.Y + 1);
        }

        public bool Go()
        {
            var me = GetPlayerTank();
            var nearestTank = GetNearestTank();

            var newPoint = GetNewPoint(me, nearestTank, out string direction);
            if (IsWallAt(newPoint.X, newPoint.Y))
            {
                newPoint = GetNewPointWithoutWall(newPoint);
                direction = GetDirection(me, newPoint);
            }

            if (IsNear(me.X - 1, me.Y, Elements.OTHER_TANK_LEFT))
            {
                direction = Left();
            } else if (IsNear(me.X + 1, me.Y, Elements.OTHER_TANK_RIGHT))
            {
                direction = Right();
            } else if (IsNear(me.X, me.Y - 1, Elements.OTHER_TANK_UP))
            {
                direction = Up();
            }
            else if (IsNear(me.X, me.Y + 1, Elements.OTHER_TANK_DOWN))
            {
                direction = Down();
            }

            SendActions($"{direction},{Act()}");
            return true;
        }


        public List<Point> GetBullets() =>
            FindingCoordinatesOfElements(
                Elements.BULLET);

        public List<Point> GetConstructions() => FindingCoordinatesOfElements(Elements.CONSTRUCTION);

        public List<Point> GetDestroyedConstructions() =>
            FindingCoordinatesOfElements(
                Elements.CONSTRUCTION_DESTROYED_DOWN,
                Elements.CONSTRUCTION_DESTROYED_DOWN_LEFT,
                Elements.CONSTRUCTION_DESTROYED_DOWN_RIGHT,
                Elements.CONSTRUCTION_DESTROYED_DOWN_TWICE,
                Elements.CONSTRUCTION_DESTROYED_LEFT,
                Elements.CONSTRUCTION_DESTROYED_LEFT_RIGHT,
                Elements.CONSTRUCTION_DESTROYED_LEFT_TWICE,
                Elements.CONSTRUCTION_DESTROYED_RIGHT,
                Elements.CONSTRUCTION_DESTROYED_RIGHT_TWICE,
                Elements.CONSTRUCTION_DESTROYED_RIGHT_UP,
                Elements.CONSTRUCTION_DESTROYED_UP,
                Elements.CONSTRUCTION_DESTROYED_UP_DOWN,
                Elements.CONSTRUCTION_DESTROYED_DOWN,
                Elements.CONSTRUCTION_DESTROYED_UP_LEFT,
                Elements.CONSTRUCTION_DESTROYED_UP_TWICE);

        public List<Point> GetWalls() => FindingCoordinatesOfElements(Elements.BATTLE_WALL);

        public List<Point> GetBarriers()
        {
            List<Point> barriers = new List<Point>();
            barriers.AddRange(GetWalls());
            barriers.AddRange(GetConstructions());
            barriers.AddRange(GetDestroyedConstructions());
            barriers.AddRange(GetOtherPlayersTanks());
            barriers.AddRange(GetBotsTanks());
            return barriers;
        }

        public bool IsOutOf(int x, int y) => x >= MapSize || y >= MapSize || x < 0 || y < 0;

        public bool IsAt(int x, int y, Elements element) => IsOutOf(x, y) ? false : (Map[x, y] == element);

        public bool IsAnyOfAt(int x, int y, params Elements[] elements) => Array.Exists(elements, element => IsAt(x, y, element));

        public bool IsBarrierAt(int x, int y) => GetBarriers().Exists(barrier => IsAt(x, y, Map[barrier.X, barrier.Y]));

        public bool IsWallAt(int x, int y) => GetWalls().Exists(wall => IsAt(x, y, Map[wall.X, wall.Y]));

        public bool IsNear(int x, int y, Elements element) =>
            IsAt(x - 1, y, element) ||
            IsAt(x + 1, y, element) ||
            IsAt(x, y - 1, element) ||
            IsAt(x, y + 1, element);

        public int CountNear(int x, int y, Elements element)
        {
            int count = 0;
            if (IsAt(x - 1, y, element)) count++;
            if (IsAt(x + 1, y, element)) count++;
            if (IsAt(x, y - 1, element)) count++;
            if (IsAt(x, y + 1, element)) count++;
            return count;
        }

        private List<Point> FindingCoordinatesOfElements(params Elements[] elements)
        {
            List<Point> points = new List<Point>();

            for (int j = 0; j < MapSize; j++)
            {
                for (int i = 0; i < MapSize; i++)
                {
                    if (Array.Exists(elements, element => IsAt(i, j, element)))
                    {
                        points.Add(new Point(i, j));
                    }
                }
            }
            return points;
        }
    }
}
