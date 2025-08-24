using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MyPuzzleGame.SystemUtils
{
    public static class ParallelWorkload
    {
        private static readonly Random _random = new();
        private static volatile bool _isRunning = false;
        private static Task[]? _backgroundTasks;
        private static CancellationTokenSource? _cancellationTokenSource;
        
        // CPU Usage Control
        public static double TargetCpuUsage { get; set; } = 0.5; // 50% by default
        private static volatile int _workIntervalMs = 10; // Work for 10ms
        private static volatile int _sleepIntervalMs = 10; // Then sleep for 10ms
        
        // Statistics
        private static long _totalCalculations = 0;
        public static long TotalCalculations => _totalCalculations;
        public static int ActiveThreads => _backgroundTasks?.Count(t => t.Status == TaskStatus.Running) ?? 0;
        public static double CalculationsPerSecond { get; private set; } = 0.0;
        
        private static readonly object _statsLock = new();
        private static DateTime _lastStatsUpdate = DateTime.Now;
        private static long _lastCalculationCount = 0;

        public static void StartBackgroundWork()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            
            int coreCount = Environment.ProcessorCount;
            _backgroundTasks = new Task[Math.Max(1, coreCount - 2)]; // Leave more cores for main thread
            
            UpdateCpuThrottling();
            
            Console.WriteLine($"Starting parallel workload on {_backgroundTasks.Length} threads (CPU cores: {coreCount}, Target CPU: {TargetCpuUsage:P0})");
            
            for (int i = 0; i < _backgroundTasks.Length; i++)
            {
                int threadId = i;
                _backgroundTasks[i] = Task.Run(() => BackgroundWorkLoop(threadId, token), token);
            }
            
            // Start statistics update task
            Task.Run(() => StatsUpdateLoop(token), token);
        }

        public static void SetCpuUsage(double targetUsage)
        {
            TargetCpuUsage = Math.Clamp(targetUsage, 0.1, 0.9);
            UpdateCpuThrottling();
            Console.WriteLine($"CPU usage target set to {TargetCpuUsage:P0}");
        }

        private static void UpdateCpuThrottling()
        {
            // Calculate work vs sleep intervals based on target CPU usage
            const int totalCycleMs = 50; // Total cycle time
            _workIntervalMs = (int)(totalCycleMs * TargetCpuUsage);
            _sleepIntervalMs = totalCycleMs - _workIntervalMs;
            
            // Ensure minimum values
            _workIntervalMs = Math.Max(1, _workIntervalMs);
            _sleepIntervalMs = Math.Max(1, _sleepIntervalMs);
        }

        public static void StopBackgroundWork()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            
            if (_backgroundTasks != null)
            {
                try
                {
                    Task.WaitAll(_backgroundTasks, TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                    // Tasks were cancelled, this is expected
                }
            }
            
            _cancellationTokenSource?.Dispose();
            _backgroundTasks = null;
            _cancellationTokenSource = null;
            
            Console.WriteLine("Stopped parallel workload");
        }

        private static void BackgroundWorkLoop(int threadId, CancellationToken token)
        {
            var localRandom = new Random(threadId * 1000 + Environment.TickCount);
            long localCalculations = 0;
            
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Work phase - perform calculations
                    var workStartTime = DateTime.Now;
                    var workEndTime = workStartTime.AddMilliseconds(_workIntervalMs);
                    
                    while (DateTime.Now < workEndTime && !token.IsCancellationRequested)
                    {
                        // Perform smaller chunks of work
                        localCalculations += PerformLightweightCalculations(localRandom);
                        
                        // Update global counter periodically
                        if (localCalculations % 1000 == 0)
                        {
                            Interlocked.Add(ref _totalCalculations, 1000);
                            localCalculations = 0;
                        }
                    }
                    
                    // Sleep phase - throttle CPU usage
                    if (_sleepIntervalMs > 0 && !token.IsCancellationRequested)
                    {
                        Thread.Sleep(_sleepIntervalMs);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
            finally
            {
                // Add remaining calculations
                if (localCalculations > 0)
                {
                    Interlocked.Add(ref _totalCalculations, localCalculations);
                }
            }
        }

        private static long PerformLightweightCalculations(Random random)
        {
            long calculationCount = 0;
            
            // Lighter computational tasks
            
            // 1. Simple mathematical operations
            for (int i = 0; i < 100; i++)
            {
                double a = random.NextDouble() * 1000;
                double b = random.NextDouble() * 1000;
                double mathResult = Math.Sin(a) * Math.Cos(b) + Math.Sqrt(a * b);
                calculationCount++;
            }
            
            // 2. Simple prime testing (smaller range)
            int candidate = random.Next(1000, 10000);
            bool isPrime = IsPrimeSimple(candidate);
            calculationCount++;
            
            // 3. Basic array operations
            var smallArray = new int[50];
            for (int i = 0; i < smallArray.Length; i++)
            {
                smallArray[i] = random.Next();
                calculationCount++;
            }
            Array.Sort(smallArray);
            calculationCount += 10; // Simplified counting
            
            // 4. String operations
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                sb.Append((char)('A' + random.Next(26)));
                calculationCount++;
            }
            string stringResult = sb.ToString();
            int hash = stringResult.GetHashCode();
            
            return calculationCount;
        }

        private static long PerformComplexCalculations(Random random)
        {
            long calculationCount = 0;
            
            // 1. Prime number testing
            int candidate = random.Next(100000, 1000000);
            bool isPrime = IsPrime(candidate);
            calculationCount++;
            
            // 2. Matrix multiplication (reduced size)
            var matrix1 = GenerateRandomMatrix(random, 20, 20);
            var matrix2 = GenerateRandomMatrix(random, 20, 20);
            var result = MultiplyMatrices(matrix1, matrix2);
            calculationCount += 20 * 20 * 20; // O(n³) operations
            
            // 3. Fibonacci calculation (smaller range)
            int fibN = random.Next(15, 25);
            long fibResult = Fibonacci(fibN);
            calculationCount++;
            
            // 4. Sorting algorithm (smaller array)
            var array = GenerateRandomArray(random, 500);
            Array.Sort(array);
            calculationCount += 500;
            
            return calculationCount;
        }

        private static bool IsPrimeSimple(int n)
        {
            if (n <= 1) return false;
            if (n <= 3) return true;
            if (n % 2 == 0 || n % 3 == 0) return false;
            
            for (int i = 5; i * i <= n; i += 6)
            {
                if (n % i == 0 || n % (i + 2) == 0) return false;
            }
            return true;
        }

        private static bool IsPrime(int n)
        {
            if (n <= 1) return false;
            if (n <= 3) return true;
            if (n % 2 == 0 || n % 3 == 0) return false;
            
            for (int i = 5; i * i <= n; i += 6)
            {
                if (n % i == 0 || n % (i + 2) == 0) return false;
            }
            return true;
        }

        private static double[,] GenerateRandomMatrix(Random random, int rows, int cols)
        {
            var matrix = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = random.NextDouble() * 100;
                }
            }
            return matrix;
        }

        private static double[,] MultiplyMatrices(double[,] a, double[,] b)
        {
            int rowsA = a.GetLength(0);
            int colsA = a.GetLength(1);
            int colsB = b.GetLength(1);
            
            var result = new double[rowsA, colsB];
            
            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    for (int k = 0; k < colsA; k++)
                    {
                        result[i, j] += a[i, k] * b[k, j];
                    }
                }
            }
            
            return result;
        }

        private static long Fibonacci(int n)
        {
            if (n <= 1) return n;
            return Fibonacci(n - 1) + Fibonacci(n - 2);
        }

        private static int[] GenerateRandomArray(Random random, int size)
        {
            var array = new int[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = random.Next();
            }
            return array;
        }

        private static string GenerateRandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        private static void StatsUpdateLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(1000); // Update every second
                    
                    lock (_statsLock)
                    {
                        var now = DateTime.Now;
                        var elapsed = (now - _lastStatsUpdate).TotalSeconds;
                        var currentCount = TotalCalculations;
                        
                        if (elapsed >= 1.0)
                        {
                            var calculationsDelta = currentCount - _lastCalculationCount;
                            CalculationsPerSecond = calculationsDelta / elapsed;
                            
                            _lastStatsUpdate = now;
                            _lastCalculationCount = currentCount;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
        }
    }
}