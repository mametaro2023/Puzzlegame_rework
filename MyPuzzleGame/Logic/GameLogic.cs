using System;
using System.Collections.Generic;
using System.Linq;
using MyPuzzleGame.Core;
using MyPuzzleGame.Entities;
using MyPuzzleGame.SystemUtils;

namespace MyPuzzleGame.Logic
{
    public class GameLogic : IDisposable
    {
        public enum GameState
        {
            MinoFalling,
            MinoLocked,
            MatchCheck, 
            BlocksAnimating,
            Spawning,
            GameOver
        }

        private readonly GameField _gameField;
        private readonly SoundManager? _soundManager;
        private readonly Random _random = new();
        private readonly object _stateLock = new();
        private readonly List<AnimatingBlock> _fallingBlocks = new();
        
        private GameState _currentState = GameState.Spawning;
        private double _stateTimer = 0.0;

        private Mino? _currentMino;
        private double _fallTimer = Core.GameConfig.FallIntervalMs;
        private bool _isSoftDropping = false;
        private readonly List<Mino> _nextMinos = new();
        private const int NextMinoCount = 2;
        private float _nextMinoSlideAnimation = 1.0f; // 0.0 (start) to 1.0 (end)
        public float NextMinoSlideProgress => _nextMinoSlideAnimation;
        
        private bool _disposed = false;

        private static readonly BlockType[] _validBlockTypes = ((BlockType[])Enum.GetValues(typeof(BlockType))).Where(t => t != BlockType.None).ToArray();

        public GameLogic(GameField gameField, SoundManager? soundManager)
        {
            _gameField = gameField ?? throw new ArgumentNullException(nameof(gameField));
            _soundManager = soundManager;
        }

        public GameState CurrentState
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentState;
                }
            }
        }

        public void Start()
        {
            lock (_stateLock)
            {
                _nextMinos.Clear();
                // Pre-populate the queue with the current mino + next minos
                for (int i = 0; i < NextMinoCount + 1; i++)
                {
                    _nextMinos.Add(CreateNewMino());
                }
                
                _stateTimer = 200.0;
                _currentState = GameState.Spawning;
            }
        }

        public Mino? GetCurrentMino()
        {
            lock (_stateLock)
            {
                return _currentMino;
            }
        }
        public List<Mino> GetNextMinos()
        {
            lock (_stateLock)
            {
                // Return a copy to avoid threading issues
                return new List<Mino>(_nextMinos);
            }
        }


        public IEnumerable<AnimatingBlock> GetFallingBlocks()
        {
            lock (_stateLock)
            {
                // Return a copy to prevent modification outside of the lock
                return new List<AnimatingBlock>(_fallingBlocks);

            }
        }

        public void Update(double deltaTime)
        {
            lock (_stateLock)
            {
                _stateTimer -= deltaTime;

                                switch (_currentState)
                {
                    case GameState.MinoFalling:
                        UpdateMinoFall(deltaTime);
                        break;
                    case GameState.MinoLocked:
                        _stateTimer = 200.0;
                        _currentState = GameState.MatchCheck;
                        break;
                    case GameState.MatchCheck:
                        if (_stateTimer <= 0)
                        {
                            var matches = FindMatches();
                            if (matches.Count > 0)
                            {
                                ClearBlocks(matches);
                                StartGravityAnimation();
                                if (_fallingBlocks.Count > 0)
                                {
                                    _currentState = GameState.BlocksAnimating;
                                }
                                else
                                {
                                    _stateTimer = 500.0;
                                    _currentState = GameState.MatchCheck;
                                }
                            }
                            else
                            {
                                _stateTimer = 200.0; 
                                _currentState = GameState.Spawning;
                            }
                        }
                        break;
                    case GameState.BlocksAnimating:
                        UpdateFallingBlocks((float)deltaTime / 1000f);
                        if (_fallingBlocks.Count == 0)
                        {
                            _stateTimer = 500.0;
                            _currentState = GameState.MatchCheck;
                        }
                        break;
                    case GameState.Spawning:
                        if (_stateTimer <= 0)
                        {
                            if (SpawnMino())
                            {
                                _currentState = GameState.MinoFalling;
                            }
                        }
                        break;
                    case GameState.GameOver:
                        // Do nothing
                        break;
                }

                UpdateNextMinoAnimation(deltaTime);
                UpdateVisuals(deltaTime);
            }
        }

        private void UpdateMinoFall(double deltaTime)
        {
            if (_currentMino == null) 
            {
                _currentState = GameState.Spawning;
                return;
            }

            double currentFallInterval = _isSoftDropping ? Core.GameConfig.SoftDropIntervalMs : Core.GameConfig.FallIntervalMs;
            _fallTimer -= deltaTime;
            
            while (_fallTimer <= 0)
            {
                if (TryMoveMino(0, 1))
                {
                    _fallTimer += currentFallInterval;
                }
                else
                {
                    PlaceMino();
                    break; 
                }
            }
        }

        private void UpdateNextMinoAnimation(double deltaTime)
        {
            if (_nextMinoSlideAnimation < 1.0f)
            {
                _nextMinoSlideAnimation += (float)(deltaTime / 200.0); // 200ms for animation
                if (_nextMinoSlideAnimation > 1.0f)
                {
                    _nextMinoSlideAnimation = 1.0f;
                }
            }
        }

        private void UpdateVisuals(double deltaTime)
        {
            if (_currentMino == null) return;
            double currentFallInterval = _isSoftDropping ? Core.GameConfig.SoftDropIntervalMs : Core.GameConfig.FallIntervalMs;
            if (_currentState == GameState.MinoFalling && currentFallInterval > 0)
            {
                double progress = 1.0 - (_fallTimer / currentFallInterval);
                _currentMino.VisualY = (_currentMino.LogicalY - 1) + (float)Math.Clamp(progress, 0.0, 1.0);
            }
            else
            {
                _currentMino.VisualY = _currentMino.LogicalY;
            }
        }

        private HashSet<(int, int)> FindMatches()
        {
            var matches = new HashSet<(int, int)>();
            var line = new List<(int, int)>();

            // Horizontal
            for (int y = 0; y < Core.GameConfig.FieldHeight; y++)
            {
                for (int x = 0; x < Core.GameConfig.FieldWidth; x++)
                {
                    var b = _gameField.GetBlock(x, y);
                    if (b != null && line.Count > 0 && _gameField.GetBlock(line.Last().Item1, line.Last().Item2)?.Type == b.Type)
                    {
                        line.Add((x, y));
                    }
                    else
                    {
                        if (line.Count >= 3) matches.UnionWith(line);
                        line.Clear();
                        if (b != null) line.Add((x, y));
                    }
                }
                if (line.Count >= 3) matches.UnionWith(line);
                line.Clear();
            }

            // Vertical
            for (int x = 0; x < Core.GameConfig.FieldWidth; x++)
            {
                for (int y = 0; y < Core.GameConfig.FieldHeight; y++)
                {
                    var b = _gameField.GetBlock(x, y);
                    if (b != null && line.Count > 0 && _gameField.GetBlock(line.Last().Item1, line.Last().Item2)?.Type == b.Type)
                    {
                        line.Add((x, y));
                    }
                    else
                    {
                        if (line.Count >= 3) matches.UnionWith(line);
                        line.Clear();
                        if (b != null) line.Add((x, y));
                    }
                }
                if (line.Count >= 3) matches.UnionWith(line);
                line.Clear();
            }

            // Diagonals
            for (int k = 0; k <= Core.GameConfig.FieldWidth + Core.GameConfig.FieldHeight - 2; k++)
            {
                // Down-Right
                for (int j = 0; j <= k; j++)
                {
                    int i = k - j;
                    if (i < Core.GameConfig.FieldHeight && j < Core.GameConfig.FieldWidth) {
                        var b = _gameField.GetBlock(j, i);
                        if (b != null && line.Count > 0 && _gameField.GetBlock(line.Last().Item1, line.Last().Item2)?.Type == b.Type) line.Add((j, i));
                        else { if (line.Count >= 3) matches.UnionWith(line); line.Clear(); if (b != null) line.Add((j, i)); }
                    }
                }
                if (line.Count >= 3) matches.UnionWith(line); line.Clear();

                // Down-Left
                for (int j = 0; j <= k; j++)
                {
                    int i = k - j;
                    if (i < Core.GameConfig.FieldHeight && j < Core.GameConfig.FieldWidth) {
                        int x = Core.GameConfig.FieldWidth - 1 - j;
                        var b = _gameField.GetBlock(x, i);
                        if (b != null && line.Count > 0 && _gameField.GetBlock(line.Last().Item1, line.Last().Item2)?.Type == b.Type) line.Add((x, i));
                        else { if (line.Count >= 3) matches.UnionWith(line); line.Clear(); if (b != null) line.Add((x, i)); }
                    }
                }
                if (line.Count >= 3) matches.UnionWith(line); line.Clear();
            }

            return matches;
        }

        private void ClearBlocks(HashSet<(int, int)> blocksToClear)
        {
            if (blocksToClear.Count > 0)
            {
                _soundManager?.PlaySound("clear");
            }

            foreach (var (x, y) in blocksToClear)
            {
                _gameField.SetBlock(x, y, null);
            }
        }

        private void UpdateFallingBlocks(float deltaTimeInSeconds)
        {
            for (int i = _fallingBlocks.Count - 1; i >= 0; i--)
            {
                var block = _fallingBlocks[i];
                if (block.Update(deltaTimeInSeconds))
                {
                    // Animation finished, place block in grid
                    _gameField.SetBlock(block.X, (int)block.EndY, block.Block);
                    _fallingBlocks.RemoveAt(i);
                }
            }
        }

        private void StartGravityAnimation()
        {
            const float animationDuration = 0.5f; // 500ms animation

            for (int x = 0; x < Core.GameConfig.FieldWidth; x++)
            {
                int emptyRow = Core.GameConfig.FieldHeight - 1;
                for (int y = Core.GameConfig.FieldHeight - 1; y >= 0; y--)
                {
                    var block = _gameField.GetBlock(x, y);
                    if (block != null)
                    {
                        if (y != emptyRow)
                        {
                            // This block needs to fall
                            _fallingBlocks.Add(new AnimatingBlock(block, x, y, emptyRow, animationDuration));
                            _gameField.SetBlock(x, y, null); // Remove from original position
                        }
                        emptyRow--;
                    }
                }
            }
        }

        

        private bool SpawnMino()
        {
            var minoToSpawn = _nextMinos[0];
            if (CheckCollision(minoToSpawn, minoToSpawn.X, minoToSpawn.LogicalY))
            {
                _currentState = GameState.GameOver;
                _currentMino = null;
                return false; // Game over
            }

            _currentMino = minoToSpawn;
            _nextMinos.RemoveAt(0);
            _nextMinos.Add(CreateNewMino());
            _nextMinoSlideAnimation = 0.0f;

            _fallTimer = Core.GameConfig.FallIntervalMs;
            _isSoftDropping = false;
            return true; // Success
        }

        private Mino CreateNewMino()
        {
            // Center the new mino horizontally.
            int spawnX = Core.GameConfig.FieldWidth / 2;
            return new Mino(spawnX, 0, 
                _validBlockTypes[_random.Next(_validBlockTypes.Length)], 
                _validBlockTypes[_random.Next(_validBlockTypes.Length)], 
                _validBlockTypes[_random.Next(_validBlockTypes.Length)]);
        }

        private bool CheckCollision(Mino mino, int x, int y)
        {
            if (mino == null) return true;
            for (int i = 0; i < mino.Blocks.Length; i++)
            {
                if (_gameField.IsCollision(x, y - (mino.Blocks.Length - 1 - i))) return true;
            }
            return false;
        }

        private bool CheckCollision(int x, int y)
        {
            return CheckCollision(_currentMino, x, y);
        }

        public void MoveMino(int deltaX, int deltaY)
        {
            lock (_stateLock)
            {
                if (_currentState != GameState.MinoFalling) return;
                TryMoveMino(deltaX, deltaY);
            }
        }

        public void RotateMino()
        {
            lock (_stateLock)
            {
                if (_currentState != GameState.MinoFalling || _currentMino == null) return;
                _currentMino.RotateUp();
            }
        }

        public void RotateNextMinoUp()
        {
            lock (_stateLock)
            {
                if (_currentState == GameState.MinoLocked || _currentState == GameState.MatchCheck)
                {
                    if (_nextMinos.Count > 0)
                    {
                        _nextMinos[0].RotateUp();
                    }
                }
            }
        }

        public void RotateNextMinoDown()
        {
            lock (_stateLock)
            {
                if (_currentState == GameState.MinoLocked || _currentState == GameState.MatchCheck)
                {
                    if (_nextMinos.Count > 0)
                    {
                        _nextMinos[0].RotateDown();
                    }
                }
            }
        }

        public void StartSoftDrop()
        {
            lock (_stateLock)
            {
                if (_currentState != GameState.MinoFalling) return;
                if (!_isSoftDropping && _currentMino != null)
                {
                    _isSoftDropping = true;
                    double progress = 1.0 - (_fallTimer / Core.GameConfig.FallIntervalMs);
                    _fallTimer = (1.0 - progress) * Core.GameConfig.SoftDropIntervalMs;
                }
            }
        }

        public void StopSoftDrop()
        {
            lock (_stateLock)
            {
                if (_currentState != GameState.MinoFalling) return;
                if (_isSoftDropping && _currentMino != null)
                {
                    _isSoftDropping = false;
                    double progress = 1.0 - (_fallTimer / Core.GameConfig.SoftDropIntervalMs);
                    _fallTimer = (1.0 - progress) * Core.GameConfig.FallIntervalMs;
                }
            }
        }

        public void HardDrop()
        {
            lock (_stateLock)
            {
                if (_currentState != GameState.MinoFalling || _currentMino == null) return;
                while (TryMoveMino(0, 1)) { }
                PlaceMino();
                _stateTimer = 200.0; // Hard drop uses its own lock delay
                _currentState = GameState.MatchCheck;
            }
        }

        private bool TryMoveMino(int deltaX, int deltaY)
        {
            if (_currentMino == null) return false;
            int newX = _currentMino.X + deltaX;
            int newY = _currentMino.LogicalY + deltaY;
            if (!CheckCollision(newX, newY))
            {
                _currentMino.X = newX;
                _currentMino.LogicalY = newY;
                if (deltaX != 0) _soundManager?.PlaySound("move");
                return true;
            }
            return false;
        }

        private void PlaceMino()
        {
            if (_currentMino == null) return;

            _soundManager?.PlaySound("lock");

            for (int i = 0; i < _currentMino.Blocks.Length; i++)
            {
                int gridX = _currentMino.X;
                int gridY = _currentMino.LogicalY - (_currentMino.Blocks.Length - 1 - i);
                if (gridY >= 0) _gameField.SetBlock(gridX, gridY, new Block(_currentMino.Blocks[i].Type));
            }
            _currentMino = null;
            _currentState = GameState.MinoLocked;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}