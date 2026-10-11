using System.Diagnostics;
using System.Drawing;

namespace Snake;

public class SnakeGame
{
    private const int BOARD_SIZE = 10;
    private const int TARGET_LENGTH = 50;
    private const int MOVE_INTERVAL_MILLISECONDS = 250;
    private const int DOOR_INTERVAL_MILLISECONDS = 6000;
    private const int DOOR_WARNING_MILLISECONDS = 2000;

    private List<Point> _snake = new List<Point>();
    private Dictionary<Point, GameObjectType> _objects = new Dictionary<Point, GameObjectType>();
    private HashSet<Point> _walls = new HashSet<Point>();
    private List<Point> _safePath = new List<Point>();
    private Stopwatch _clock = new Stopwatch();
    private Random _random = new Random();
    private long? _testElapsedMilliseconds;
    private Point[] _doors = new Point[2];
    private bool[] _doorsOpen = new bool[2];
    private bool[] _doorsPendingClose = new bool[2];
    private int _level;
    private int _statusLevel;
    private int _snakeLength;
    private int _shieldCount;
    private int _shieldPickupsSpawned;
    private int _shieldPickupsCollected;
    private int _lastCompletedLevel;
    private int _lastCompletedLength;
    private int _failedLevel;
    private int _finalLength;
    private int _lengthBeforeDeath;
    private long _nextMoveAtMilliseconds;
    private long _nextDoorAtMilliseconds;
    private int _scheduledClosingDoor;
    private int _warningDoorIndex;
    private bool _doorWarningTriggered;
    private bool _failedByFire;
    private Direction _direction;
    private Direction? _queuedDirection;
    private GamePhase _phase;
    private DeathCause _lastDeathCause;

    public SnakeGame()
    {
        _phase = GamePhase.Uninitialized;
        _warningDoorIndex = -1;
    }

    internal int Level => _level;
    internal int StatusLevel => _statusLevel;
    internal int SnakeLength => _snakeLength;
    internal int ShieldCount => _shieldCount;
    internal int TargetLength => _level <= 2 ? 20 : TARGET_LENGTH;
    internal int ProgressPercent => Math.Clamp((int)Math.Floor(_snakeLength * 100.0 / TargetLength), 0, 100);
    internal GamePhase Phase => _phase;
    internal Direction CurrentDirection => _direction;
    internal IReadOnlyList<Point> Snake => _snake;
    internal IReadOnlyDictionary<Point, GameObjectType> Objects => _objects;
    internal IReadOnlyCollection<Point> Walls => _walls;
    internal IReadOnlyList<Point> Doors => _doors;
    internal int LastCompletedLevel => _lastCompletedLevel;
    internal int LastCompletedLength => _lastCompletedLength;
    internal int FailedLevel => _failedLevel;
    internal int FinalLength => _finalLength;
    internal bool FailedByFire => _failedByFire;
    internal DeathCause LastDeathCause => _lastDeathCause;
    internal bool HasPreviousLevelResult => _lastCompletedLevel == _level - 1 && _lastCompletedLevel > 0;
    internal bool IsDoorOpen(int doorIndex) => doorIndex >= 0 && doorIndex < _doorsOpen.Length && _doorsOpen[doorIndex];
    internal bool IsDoorWarning(int doorIndex) => doorIndex >= 0 && doorIndex < _doors.Length && (_warningDoorIndex == doorIndex || _doorsPendingClose[doorIndex]);
    internal long NextMoveAtMilliseconds => _nextMoveAtMilliseconds;
    internal long NextDoorAtMilliseconds => _nextDoorAtMilliseconds;
    internal IReadOnlyList<Point> SafePath => _safePath;

    internal void SetTestElapsedMilliseconds(long milliseconds)
    {
        _testElapsedMilliseconds = milliseconds;
    }

    internal void SetTestRandom(Random random)
    {
        _random = random;
    }

    internal void SetTestScenario(
        int level,
        GamePhase phase,
        int snakeLength = 3,
        List<Point>? snake = null,
        Dictionary<Point, GameObjectType>? objects = null,
        int shieldCount = 0,
        int shieldPickupsSpawned = 0,
        int shieldPickupsCollected = 0,
        Direction direction = Direction.Right,
        bool firstDoorOpen = true,
        bool secondDoorOpen = false,
        bool firstDoorPendingClose = false,
        bool secondDoorPendingClose = false,
        int? lastCompletedLevel = null,
        int lastCompletedLength = 20,
        bool failedByFire = false,
        DeathCause deathCause = DeathCause.BodyCollision,
        long nextMoveAtMilliseconds = 250,
        long nextDoorAtMilliseconds = 6000)
    {
        PrepareLevel(level);
        _testElapsedMilliseconds = 0;
        _phase = phase;
        if (snake is null)
        {
            SetSnakeFromSafePath(snakeLength);
            _direction = direction;
        }
        else
        {
            _snake = new List<Point>(snake);
            _snakeLength = snakeLength;
            _direction = direction;
        }

        _objects.Clear();
        if (objects is not null)
        {
            foreach (KeyValuePair<Point, GameObjectType> entry in objects)
            {
                _objects.Add(entry.Key, entry.Value);
            }
        }

        _shieldCount = shieldCount;
        _shieldPickupsSpawned = shieldPickupsSpawned;
        _shieldPickupsCollected = shieldPickupsCollected;
        _doorsOpen = new bool[] { firstDoorOpen, secondDoorOpen };
        _doorsPendingClose = new bool[] { firstDoorPendingClose, secondDoorPendingClose };
        _lastCompletedLevel = lastCompletedLevel ?? (phase == GamePhase.Ready ? level - 1 : 0);
        _lastCompletedLength = _lastCompletedLevel > 0 ? lastCompletedLength : 0;
        _nextMoveAtMilliseconds = nextMoveAtMilliseconds;
        _nextDoorAtMilliseconds = nextDoorAtMilliseconds;
        _lengthBeforeDeath = snakeLength;
        _lastDeathCause = deathCause;
        _failedByFire = failedByFire;
        _failedLevel = phase == GamePhase.GameOver || phase == GamePhase.DeathPending ? level : 0;
        _finalLength = phase == GamePhase.GameOver || phase == GamePhase.DeathPending ? Math.Max(0, snakeLength) : 0;
        _clock.Stop();
        _clock.Reset();
        if (phase == GamePhase.Playing || phase == GamePhase.DeathPending)
        {
            _clock.Start();
        }
    }

    public void ShowFirstLevelReadyScreen()
    {
        if (_phase != GamePhase.Uninitialized)
        {
            throw CreatePhaseViolation(nameof(ShowFirstLevelReadyScreen), "新一輪遊戲尚未進入第一關遊玩");
        }

        ClearRunHistory();
        PrepareLevel(1);
        Verify(nameof(ShowFirstLevelReadyScreen), _phase == GamePhase.Ready && _level == 1 && _snakeLength == 3 && _direction == Direction.Right && _objects.Count == 0, "第一關待開始、3 格蛇朝右且沒有物件");
    }

    public void StartFirstLevel()
    {
        if (_level != 1 || _phase != GamePhase.Ready)
        {
            return;
        }

        BeginLevelPlay();
        MaintainLevelObjects();
        Verify(nameof(StartFirstLevel), _level == 1 && _phase == GamePhase.Playing && _snakeLength == 3 && ContainsObject(GameObjectType.Strawberry), "第一關已開始，蛇長為 3 且草莓已出現在合法空格");
    }

    public void SetDirection(Direction direction)
    {
        if (!Enum.IsDefined(direction))
        {
            throw CreateInputViolation(nameof(SetDirection), "方向只接受上、下、左或右", direction.ToString());
        }

        if (_phase != GamePhase.Playing)
        {
            return;
        }

        Point oldHead = _snake.Count > 0 ? _snake[0] : Point.Empty;
        int oldLength = _snakeLength;
        Direction oldDirection = _direction;
        if (!IsDirectReverse(_direction, direction))
        {
            _queuedDirection = direction;
        }

        Verify(nameof(SetDirection), _snakeLength == oldLength && _direction == oldDirection && (_snake.Count == 0 || _snake[0] == oldHead), "方向輸入只記錄後續方向，不直接移動蛇或改變蛇長");
    }

    public void AdvanceFirstLevelStep()
    {
        if (_level != 1 || _phase != GamePhase.Playing || GetElapsedMilliseconds() < _nextMoveAtMilliseconds)
        {
            return;
        }

        _nextMoveAtMilliseconds += MOVE_INTERVAL_MILLISECONDS;
        AdvanceOneCell();
        Verify(nameof(AdvanceFirstLevelStep), VerifyCurrentBoard(), "第一關移動後物件、蛇與棋盤仍符合合法格位規則");
    }

    public void ShowSecondLevelReadyScreen()
    {
        if (_level != 2 || _phase != GamePhase.Ready || _lastCompletedLevel != 1)
        {
            throw CreatePhaseViolation(nameof(ShowSecondLevelReadyScreen), "第一關已通關且第二關尚未開始");
        }

        Verify(nameof(ShowSecondLevelReadyScreen), _snakeLength == 3 && _shieldCount == 0 && _objects.Count == 0, "第二關待開始、蛇長為 3、保命為 0 且尚無物件");
    }

    public void StartSecondLevel()
    {
        if (_level != 2 || _phase != GamePhase.Ready)
        {
            return;
        }

        BeginLevelPlay();
        MaintainLevelObjects();
        Verify(nameof(StartSecondLevel), _phase == GamePhase.Playing && _snakeLength == 3 && _shieldCount == 0 && HasRequiredStartingObjects(2), "第二關已開始並產生草莓、藍莓、護盾及火焰");
    }

    public void AdvanceSecondLevelStep()
    {
        if (_level != 2 || _phase != GamePhase.Playing || GetElapsedMilliseconds() < _nextMoveAtMilliseconds)
        {
            return;
        }

        _nextMoveAtMilliseconds += MOVE_INTERVAL_MILLISECONDS;
        AdvanceOneCell();
        Verify(nameof(AdvanceSecondLevelStep), VerifyCurrentBoard(), "第二關移動後物件、蛇與棋盤仍符合合法格位規則");
    }

    public void ShowThirdLevelReadyScreen()
    {
        if (_level != 3 || _phase != GamePhase.Ready || _lastCompletedLevel != 2)
        {
            throw CreatePhaseViolation(nameof(ShowThirdLevelReadyScreen), "第二關已通關且第三關尚未開始");
        }

        Verify(nameof(ShowThirdLevelReadyScreen), _snakeLength == 3 && _shieldCount == 0 && _objects.Count == 0 && HasThirdLevelCapacity(), "第三關待開始、門狀態可辨認、尚無物件且保留至少 50 格連續路線與物件空格");
    }

    public void StartThirdLevel()
    {
        if (_level != 3 || _phase != GamePhase.Ready)
        {
            return;
        }

        BeginLevelPlay();
        MaintainLevelObjects();
        Verify(nameof(StartThirdLevel), _phase == GamePhase.Playing && _snakeLength == 3 && _shieldCount == 0 && HasRequiredStartingObjects(3), "第三關已開始並產生草莓、藍莓、護盾、火焰及炸彈");
    }

    public void AdvanceThirdLevelStep()
    {
        if (_level != 3 || _phase != GamePhase.Playing || GetElapsedMilliseconds() < _nextMoveAtMilliseconds)
        {
            return;
        }

        _nextMoveAtMilliseconds += MOVE_INTERVAL_MILLISECONDS;
        AdvanceOneCell();
        Verify(nameof(AdvanceThirdLevelStep), VerifyCurrentBoard(), "第三關移動後物件、蛇、牆與門仍符合合法格位規則");
    }

    public void UpdateThirdLevelDoor()
    {
        if (_level != 3 || _phase != GamePhase.Playing || GetElapsedMilliseconds() < _nextDoorAtMilliseconds)
        {
            return;
        }

        int openingDoor = 1 - _scheduledClosingDoor;
        int closingDoor = _scheduledClosingDoor;
        _doorsOpen[openingDoor] = true;
        _doorsPendingClose[openingDoor] = false;

        if (SnakeOccupies(_doors[closingDoor]))
        {
            _doorsPendingClose[closingDoor] = true;
            _doorsOpen[closingDoor] = true;
        }
        else
        {
            _doorsPendingClose[closingDoor] = false;
            _doorsOpen[closingDoor] = false;
        }

        _warningDoorIndex = -1;
        _doorWarningTriggered = false;
        _scheduledClosingDoor = openingDoor;
        _nextDoorAtMilliseconds += DOOR_INTERVAL_MILLISECONDS;
        Verify(nameof(UpdateThirdLevelDoor), VerifyDoorState(), "第三關門依交替時序切換；蛇仍占格時關門延後");
    }

    public void RescueWithShield(DeathCause cause)
    {
        if (!Enum.IsDefined(cause))
        {
            throw CreateInputViolation(nameof(RescueWithShield), "死亡原因只接受蛇身、牆、關閉的門、炸彈或致命火焰", cause.ToString());
        }

        if (cause == DeathCause.BodyCollision || cause == DeathCause.WallCollision || cause == DeathCause.ClosedDoor || cause == DeathCause.Bomb || cause == DeathCause.FatalFire)
        {
        }
        else
        {
            throw CreateInputViolation(nameof(RescueWithShield), "死亡原因不在允許範圍", cause.ToString());
        }

        if (_phase != GamePhase.DeathPending || _level < 2 || _level > 3 || _shieldCount <= 0 || _lastDeathCause != cause)
        {
            throw CreatePhaseViolation(nameof(RescueWithShield), "第二或第三關已有可保命的死亡事件且保命次數大於 0");
        }

        int previousShieldCount = _shieldCount;
        int restoredLength = _lengthBeforeDeath;
        List<GameObjectType> displacedObjects = new List<GameObjectType>();
        HashSet<Point> safeCells = new HashSet<Point>(_safePath.Take(restoredLength));
        foreach (KeyValuePair<Point, GameObjectType> entry in _objects.ToArray())
        {
            if (safeCells.Contains(entry.Key))
            {
                displacedObjects.Add(entry.Value);
                _objects.Remove(entry.Key);
            }
        }

        _shieldCount--;
        SetSnakeFromSafePath(restoredLength);
        _phase = GamePhase.Playing;
        foreach (GameObjectType objectType in displacedObjects)
        {
            PlaceObject(objectType);
        }

        _nextMoveAtMilliseconds = GetElapsedMilliseconds() + MOVE_INTERVAL_MILLISECONDS;
        MaintainLevelObjects();
        Verify(nameof(RescueWithShield), _phase == GamePhase.Playing && _snakeLength == restoredLength && _shieldCount == previousShieldCount - 1 && VerifyCurrentBoard(), "消耗一次保命、恢復事件前蛇長並從安全起點繼續本關");
    }

    public void UpdateLevelStatus(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > 3)
        {
            throw CreateInputViolation(nameof(UpdateLevelStatus), "關卡編號只接受 1、2 或 3", levelNumber.ToString());
        }

        if (_phase == GamePhase.Uninitialized)
        {
            throw CreatePhaseViolation(nameof(UpdateLevelStatus), "遊戲已初始化且指定關卡狀態可讀取");
        }

        int previousLevel = _statusLevel;
        int previousGameLevel = _level;
        int previousSnakeLength = _snakeLength;
        int previousShieldCount = _shieldCount;
        int previousObjectCount = _objects.Count;
        GamePhase previousPhase = _phase;
        long previousNextMoveAt = _nextMoveAtMilliseconds;
        long previousNextDoorAt = _nextDoorAtMilliseconds;
        _statusLevel = levelNumber;
        bool gameStateUnchanged = _level == previousGameLevel && _snakeLength == previousSnakeLength && _shieldCount == previousShieldCount && _objects.Count == previousObjectCount && _phase == previousPhase && _nextMoveAtMilliseconds == previousNextMoveAt && _nextDoorAtMilliseconds == previousNextDoorAt;
        Verify(nameof(UpdateLevelStatus), _statusLevel == levelNumber && gameStateUnchanged, $"狀態顯示關卡 {levelNumber}，且遊戲時間與遊戲狀態不變；原顯示關卡 {previousLevel}");
    }

    public void MaintainLevelObjects()
    {
        if (_phase != GamePhase.Playing)
        {
            return;
        }

        List<GameObjectType> allowedObjects = new List<GameObjectType> { GameObjectType.Strawberry };
        if (_level >= 2)
        {
            allowedObjects.Add(GameObjectType.Blueberry);
            allowedObjects.Add(GameObjectType.Fire);
        }

        if (_level == 3)
        {
            allowedObjects.Add(GameObjectType.Bomb);
        }

        foreach (GameObjectType objectType in allowedObjects)
        {
            if (!ContainsObject(objectType))
            {
                PlaceObject(objectType);
            }
        }

        if (_level >= 2 && _shieldPickupsSpawned < 2 && !ContainsObject(GameObjectType.Shield) && (_shieldPickupsSpawned == 0 || _shieldPickupsCollected >= _shieldPickupsSpawned))
        {
            if (PlaceObject(GameObjectType.Shield))
            {
                _shieldPickupsSpawned++;
            }
        }

        Verify(nameof(MaintainLevelObjects), VerifyCurrentBoard(), "只產生本關允許物件；各類型最多一個且都位於合法空格");
    }

    public void ShowGameOverScreen()
    {
        if (_phase != GamePhase.DeathPending)
        {
            throw CreatePhaseViolation(nameof(ShowGameOverScreen), "任一關已死亡且沒有剩餘護盾");
        }

        _phase = GamePhase.GameOver;
        Verify(nameof(ShowGameOverScreen), _phase == GamePhase.GameOver && _failedLevel == _level, "顯示 Game Over、失敗關卡、最後蛇長與重玩按鈕；本輪停止");
    }

    public void ShowThirdLevelSuccessScreen()
    {
        if (_phase != GamePhase.SuccessPending || _level != 3 || _snakeLength < TARGET_LENGTH)
        {
            throw CreatePhaseViolation(nameof(ShowThirdLevelSuccessScreen), "第三關蛇長已達 50 格以上");
        }

        _phase = GamePhase.Success;
        Verify(nameof(ShowThirdLevelSuccessScreen), _phase == GamePhase.Success && ProgressPercent == 100 && _snakeLength >= TARGET_LENGTH, "保留第三關最後畫面、顯示成功、100% 與重玩按鈕");
    }

    public void RestartGame()
    {
        if (_phase != GamePhase.GameOver && _phase != GamePhase.Success)
        {
            return;
        }

        ClearRunHistory();
        PrepareLevel(1);
        BeginLevelPlay();
        MaintainLevelObjects();
        Verify(nameof(RestartGame), _level == 1 && _phase == GamePhase.Playing && _snakeLength == 3 && _shieldCount == 0 && ContainsObject(GameObjectType.Strawberry), "清除上一輪資料，第一關立即以 3 格蛇與草莓開始移動");
    }

    internal void UpdateGameTime()
    {
        if (_phase != GamePhase.Playing)
        {
            return;
        }

        long elapsedMilliseconds = GetElapsedMilliseconds();
        int processedEvents = 0;
        while (_phase == GamePhase.Playing && processedEvents < 10000)
        {
            long nextMovementEvent = _nextMoveAtMilliseconds;
            long nextWarningEvent = _level == 3 && !_doorWarningTriggered ? _nextDoorAtMilliseconds - DOOR_WARNING_MILLISECONDS : long.MaxValue;
            long nextDoorEvent = _level == 3 ? _nextDoorAtMilliseconds : long.MaxValue;
            long nextEvent = Math.Min(nextMovementEvent, Math.Min(nextWarningEvent, nextDoorEvent));
            if (nextEvent > elapsedMilliseconds)
            {
                return;
            }

            if (nextEvent == nextWarningEvent)
            {
                _doorWarningTriggered = true;
                _warningDoorIndex = _scheduledClosingDoor;
                processedEvents++;
                continue;
            }

            if (nextEvent == nextDoorEvent)
            {
                UpdateThirdLevelDoor();
                processedEvents++;
                continue;
            }

            if (_level == 1)
            {
                AdvanceFirstLevelStep();
            }
            else if (_level == 2)
            {
                AdvanceSecondLevelStep();
            }
            else if (_level == 3)
            {
                AdvanceThirdLevelStep();
            }

            processedEvents++;
        }
    }

    private long GetElapsedMilliseconds()
    {
        return _testElapsedMilliseconds ?? _clock.ElapsedMilliseconds;
    }

    private void ClearRunHistory()
    {
        _lastCompletedLevel = 0;
        _lastCompletedLength = 0;
        _failedLevel = 0;
        _finalLength = 0;
        _failedByFire = false;
    }

    private void PrepareLevel(int levelNumber)
    {
        _clock.Stop();
        _clock.Reset();
        _level = levelNumber;
        _statusLevel = levelNumber;
        _snakeLength = 3;
        _shieldCount = 0;
        _shieldPickupsSpawned = 0;
        _shieldPickupsCollected = 0;
        _objects.Clear();
        _walls.Clear();
        _safePath = BuildSafePath(levelNumber);
        _snake = new List<Point>();
        _doors = new Point[] { new Point(7, 1), new Point(8, 1) };
        _doorsOpen = new bool[] { true, false };
        _doorsPendingClose = new bool[] { false, false };
        _scheduledClosingDoor = 0;
        _warningDoorIndex = -1;
        _doorWarningTriggered = false;
        _nextMoveAtMilliseconds = MOVE_INTERVAL_MILLISECONDS;
        _nextDoorAtMilliseconds = DOOR_INTERVAL_MILLISECONDS;
        _direction = Direction.Right;
        _queuedDirection = null;
        _failedLevel = 0;
        _finalLength = 0;
        _lengthBeforeDeath = 3;
        _failedByFire = false;
        _phase = GamePhase.Ready;

        if (levelNumber == 3)
        {
            for (int x = 1; x <= 6; x++)
            {
                _walls.Add(new Point(x, 1));
            }

            for (int coordinate = 0; coordinate < BOARD_SIZE; coordinate++)
            {
                _walls.Add(new Point(coordinate, 0));
                _walls.Add(new Point(coordinate, BOARD_SIZE - 1));
                _walls.Add(new Point(0, coordinate));
                _walls.Add(new Point(BOARD_SIZE - 1, coordinate));
            }
        }

        SetSnakeFromSafePath(3);
        Verify(nameof(PrepareLevel), VerifyCurrentBoard(), $"第 {levelNumber} 關安全起點、牆格與合法路線已建立");
    }

    private List<Point> BuildSafePath(int levelNumber)
    {
        List<Point> path = new List<Point>();
        int firstRow = levelNumber == 3 ? 2 : 1;
        int lastRow = 8;
        for (int y = firstRow; y <= lastRow; y++)
        {
            if ((y - firstRow) % 2 == 0)
            {
                for (int x = 1; x <= 8; x++)
                {
                    path.Add(new Point(x, y));
                }
            }
            else
            {
                for (int x = 8; x >= 1; x--)
                {
                    path.Add(new Point(x, y));
                }
            }
        }

        return path;
    }

    private void SetSnakeFromSafePath(int length)
    {
        if (length < 1 || length >= _safePath.Count)
        {
            throw new OutputContractViolationException(nameof(SetSnakeFromSafePath), "安全起點路線必須容納蛇身與下一個前進格", $"length={length}, route={_safePath.Count}");
        }

        _snakeLength = length;
        _snake = new List<Point>();
        for (int index = length - 1; index >= 0; index--)
        {
            _snake.Add(_safePath[index]);
        }

        Point previousCell = _safePath[length - 1];
        Point nextCell = _safePath[length];
        _direction = DirectionFromDelta(new Point(nextCell.X - previousCell.X, nextCell.Y - previousCell.Y));
        _queuedDirection = null;
    }

    private void BeginLevelPlay()
    {
        _phase = GamePhase.Playing;
        _clock.Restart();
        _nextMoveAtMilliseconds = MOVE_INTERVAL_MILLISECONDS;
        _nextDoorAtMilliseconds = DOOR_INTERVAL_MILLISECONDS;
        _scheduledClosingDoor = 0;
        _warningDoorIndex = -1;
        _doorWarningTriggered = false;
        _doorsOpen = _level == 3 ? new bool[] { true, false } : new bool[] { false, false };
        _doorsPendingClose = new bool[] { false, false };
    }

    private void AdvanceOneCell()
    {
        if (_snake.Count == 0 || _snakeLength <= 0)
        {
            TriggerDeath(DeathCause.BodyCollision);
            return;
        }

        if (_queuedDirection.HasValue)
        {
            _direction = _queuedDirection.Value;
            _queuedDirection = null;
        }

        Point nextHead = GetNextPoint(_snake[0]);
        if (_level < 3)
        {
            nextHead = new Point((nextHead.X + BOARD_SIZE) % BOARD_SIZE, (nextHead.Y + BOARD_SIZE) % BOARD_SIZE);
        }
        else if (IsWall(nextHead))
        {
            TriggerDeath(DeathCause.WallCollision);
            return;
        }

        int doorIndex = GetDoorIndex(nextHead);
        if (doorIndex >= 0 && !_doorsOpen[doorIndex])
        {
            TriggerDeath(DeathCause.ClosedDoor);
            return;
        }

        GameObjectType? objectAtNext = _objects.TryGetValue(nextHead, out GameObjectType foundObject) ? foundObject : null;
        bool grows = objectAtNext == GameObjectType.Strawberry || objectAtNext == GameObjectType.Blueberry;
        int bodyCountToCheck = _snake.Count - (grows ? 0 : 1);
        for (int index = 0; index < bodyCountToCheck; index++)
        {
            if (_snake[index] == nextHead)
            {
                TriggerDeath(DeathCause.BodyCollision);
                return;
            }
        }

        int lengthBeforeEvent = _snakeLength;
        _snake.Insert(0, nextHead);
        if (!grows)
        {
            _snake.RemoveAt(_snake.Count - 1);
        }
        else if (objectAtNext == GameObjectType.Blueberry && !ExtendTail())
        {
            string contractMethod = _level == 2 ? nameof(AdvanceSecondLevelStep) : nameof(AdvanceThirdLevelStep);
            throw new OutputContractViolationException(contractMethod, "藍莓接觸後蛇身須立即增加 2 格", DescribeState());
        }

        if (objectAtNext.HasValue)
        {
            _objects.Remove(nextHead);
            ApplyObject(objectAtNext.Value, lengthBeforeEvent);
        }

        RefreshPendingDoorClosures();
        if (_phase == GamePhase.Playing && _snakeLength >= TargetLength)
        {
            CompleteCurrentLevel();
            return;
        }

        if (_phase == GamePhase.Playing)
        {
            MaintainLevelObjects();
        }
    }

    private void ApplyObject(GameObjectType objectType, int lengthBeforeEvent)
    {
        if (objectType == GameObjectType.Strawberry)
        {
            _snakeLength++;
        }
        else if (objectType == GameObjectType.Blueberry)
        {
            _snakeLength += 2;
        }
        else if (objectType == GameObjectType.Shield)
        {
            _shieldCount++;
            _shieldPickupsCollected++;
        }
        else if (objectType == GameObjectType.Fire)
        {
            _snakeLength -= 2;
            int visibleLength = Math.Max(0, _snakeLength);
            while (_snake.Count > visibleLength)
            {
                _snake.RemoveAt(_snake.Count - 1);
            }

            if (_snakeLength <= 0)
            {
                TriggerDeath(DeathCause.FatalFire, lengthBeforeEvent);
            }
        }
        else if (objectType == GameObjectType.Bomb)
        {
            TriggerDeath(DeathCause.Bomb, lengthBeforeEvent);
        }
    }

    private void TriggerDeath(DeathCause cause, int? lengthBeforeEvent = null)
    {
        _lastDeathCause = cause;
        _lengthBeforeDeath = lengthBeforeEvent ?? _snakeLength;
        _failedByFire = cause == DeathCause.FatalFire;

        if (_level >= 2 && _shieldCount > 0)
        {
            _phase = GamePhase.DeathPending;
            RescueWithShield(cause);
            return;
        }

        _failedLevel = _level;
        _finalLength = _snakeLength;
        _phase = GamePhase.DeathPending;
        _clock.Stop();
    }

    private void CompleteCurrentLevel()
    {
        _lastCompletedLevel = _level;
        _lastCompletedLength = _snakeLength;
        int nextLevel = _level + 1;
        if (nextLevel > 3)
        {
            _phase = GamePhase.SuccessPending;
            _clock.Stop();
            return;
        }

        PrepareLevel(nextLevel);
    }

    private bool PlaceObject(GameObjectType objectType)
    {
        List<Point> candidates = new List<Point>();
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                Point candidate = new Point(x, y);
                if (IsLegalObjectCell(candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        Point selected = candidates[_random.Next(candidates.Count)];
        _objects[selected] = objectType;
        return true;
    }

    private bool ExtendTail()
    {
        if (_snake.Count == 0)
        {
            return false;
        }

        Point tail = _snake[_snake.Count - 1];
        List<Point> candidates = new List<Point>
        {
            new Point(tail.X + 1, tail.Y),
            new Point(tail.X - 1, tail.Y),
            new Point(tail.X, tail.Y + 1),
            new Point(tail.X, tail.Y - 1)
        };

        foreach (Point candidate in candidates)
        {
            Point normalized = candidate;
            if (_level < 3)
            {
                normalized = new Point((candidate.X + BOARD_SIZE) % BOARD_SIZE, (candidate.Y + BOARD_SIZE) % BOARD_SIZE);
            }

            if (IsInsideBoard(normalized) && !_snake.Contains(normalized) && !_walls.Contains(normalized) && GetDoorIndex(normalized) < 0 && !_objects.ContainsKey(normalized))
            {
                _snake.Add(normalized);
                return true;
            }
        }

        return false;
    }

    private bool IsLegalObjectCell(Point point)
    {
        return IsInsideBoard(point) && !_snake.Contains(point) && !_walls.Contains(point) && GetDoorIndex(point) < 0 && !_objects.ContainsKey(point);
    }

    private bool IsWall(Point point)
    {
        return !IsInsideBoard(point) || _walls.Contains(point);
    }

    private bool IsInsideBoard(Point point)
    {
        return point.X >= 0 && point.X < BOARD_SIZE && point.Y >= 0 && point.Y < BOARD_SIZE;
    }

    private bool ContainsObject(GameObjectType objectType)
    {
        return _objects.Values.Contains(objectType);
    }

    private bool HasRequiredStartingObjects(int levelNumber)
    {
        bool basicObjects = ContainsObject(GameObjectType.Strawberry) && ContainsObject(GameObjectType.Blueberry) && ContainsObject(GameObjectType.Shield) && ContainsObject(GameObjectType.Fire);
        return levelNumber == 2 ? basicObjects && !ContainsObject(GameObjectType.Bomb) : basicObjects && ContainsObject(GameObjectType.Bomb);
    }

    private bool VerifyCurrentBoard()
    {
        if (_level < 1 || _level > 3 || _safePath.Count < TARGET_LENGTH || _snakeLength > 0 && _snake.Count != _snakeLength)
        {
            return false;
        }

        if (_phase == GamePhase.Playing)
        {
            HashSet<Point> uniqueSnakeCells = new HashSet<Point>();
            foreach (Point segment in _snake)
            {
                if (!IsInsideBoard(segment) || _walls.Contains(segment) || uniqueSnakeCells.Contains(segment))
                {
                    return false;
                }

                int doorIndex = GetDoorIndex(segment);
                if (doorIndex >= 0 && !_doorsOpen[doorIndex])
                {
                    return false;
                }

                uniqueSnakeCells.Add(segment);
            }
        }

        HashSet<GameObjectType> objectTypes = new HashSet<GameObjectType>();
        foreach (KeyValuePair<Point, GameObjectType> entry in _objects)
        {
            if (!IsLegalObjectCellForExisting(entry.Key) || objectTypes.Contains(entry.Value) || !IsObjectAllowed(entry.Value, _level))
            {
                return false;
            }

            objectTypes.Add(entry.Value);
        }

        if (_level == 3 && !HasThirdLevelCapacity())
        {
            return false;
        }

        return true;
    }

    private bool IsLegalObjectCellForExisting(Point point)
    {
        return IsInsideBoard(point) && !_snake.Contains(point) && !_walls.Contains(point) && GetDoorIndex(point) < 0;
    }

    private bool IsObjectAllowed(GameObjectType objectType, int levelNumber)
    {
        if (levelNumber == 1)
        {
            return objectType == GameObjectType.Strawberry;
        }

        if (levelNumber == 2)
        {
            return objectType == GameObjectType.Strawberry || objectType == GameObjectType.Blueberry || objectType == GameObjectType.Shield || objectType == GameObjectType.Fire;
        }

        return objectType == GameObjectType.Strawberry || objectType == GameObjectType.Blueberry || objectType == GameObjectType.Shield || objectType == GameObjectType.Fire || objectType == GameObjectType.Bomb;
    }

    private bool HasThirdLevelCapacity()
    {
        if (_safePath.Count < TARGET_LENGTH)
        {
            return false;
        }

        int walkableCells = 0;
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                Point point = new Point(x, y);
                if (!_walls.Contains(point) && GetDoorIndex(point) < 0)
                {
                    walkableCells++;
                }
            }
        }

        return walkableCells >= TARGET_LENGTH + 5;
    }

    private bool VerifyDoorState()
    {
        if (_doors.Length != 2 || _doorsOpen.Length != 2 || _doorsPendingClose.Length != 2 || _doors[0] == _doors[1])
        {
            return false;
        }

        for (int index = 0; index < _doors.Length; index++)
        {
            if (_doorsPendingClose[index] && !_doorsOpen[index])
            {
                return false;
            }

            if (!_doorsOpen[index] && SnakeOccupies(_doors[index]))
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshPendingDoorClosures()
    {
        for (int index = 0; index < _doors.Length; index++)
        {
            if (_doorsPendingClose[index] && !SnakeOccupies(_doors[index]))
            {
                _doorsPendingClose[index] = false;
                _doorsOpen[index] = false;
                if (_warningDoorIndex == index)
                {
                    _warningDoorIndex = -1;
                }
            }
        }
    }

    private bool SnakeOccupies(Point point)
    {
        return _snake.Contains(point);
    }

    private int GetDoorIndex(Point point)
    {
        for (int index = 0; index < _doors.Length; index++)
        {
            if (_doors[index] == point && _level == 3)
            {
                return index;
            }
        }

        return -1;
    }

    private Point GetNextPoint(Point head)
    {
        if (_direction == Direction.Up)
        {
            return new Point(head.X, head.Y - 1);
        }

        if (_direction == Direction.Down)
        {
            return new Point(head.X, head.Y + 1);
        }

        if (_direction == Direction.Left)
        {
            return new Point(head.X - 1, head.Y);
        }

        return new Point(head.X + 1, head.Y);
    }

    private bool IsDirectReverse(Direction current, Direction requested)
    {
        return (current == Direction.Up && requested == Direction.Down) ||
            (current == Direction.Down && requested == Direction.Up) ||
            (current == Direction.Left && requested == Direction.Right) ||
            (current == Direction.Right && requested == Direction.Left);
    }

    private Direction DirectionFromDelta(Point delta)
    {
        if (delta.X > 0)
        {
            return Direction.Right;
        }

        if (delta.X < 0)
        {
            return Direction.Left;
        }

        if (delta.Y > 0)
        {
            return Direction.Down;
        }

        return Direction.Up;
    }

    private ArgumentOutOfRangeException CreateInputViolation(string contractMethod, string guarantee, string actual)
    {
        ArgumentOutOfRangeException exception = new ArgumentOutOfRangeException("input", actual, $"{contractMethod}: {guarantee}");
        exception.Data["Snake.ContractViolation"] = true;
        exception.Data["Snake.ContractMethod"] = contractMethod;
        return exception;
    }

    private InvalidOperationException CreatePhaseViolation(string contractMethod, string precondition)
    {
        InvalidOperationException exception = new InvalidOperationException($"{contractMethod}: 前置條件不成立；要求：{precondition}；目前狀態：{DescribeState()}");
        exception.Data["Snake.ContractViolation"] = true;
        exception.Data["Snake.ContractMethod"] = contractMethod;
        return exception;
    }

    private void Verify(string contractMethod, bool guaranteeHolds, string guarantee)
    {
        if (!guaranteeHolds)
        {
            throw new OutputContractViolationException(contractMethod, guarantee, DescribeState());
        }
    }

    private string DescribeState()
    {
        return $"level={_level}, phase={_phase}, length={_snakeLength}, shields={_shieldCount}, objects={_objects.Count}, doors={string.Join(',', _doorsOpen.Select((isOpen, index) => $"{index + 1}:{(isOpen ? "open" : "closed")}"))}";
    }
}
