using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    public class LoaderBlockNew<T>
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly CancellationToken _cancellationToken;
        private readonly int _boundedCapacity;
        private readonly ILogger _logger;
        private readonly int _countsPerSecond;
        private readonly int _countsPerBatch;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="jobAction">Работа, которую необходимо совершать.</param>
        public LoaderBlockNew(
            int countsPerSecond,
            int countsPerBatch,
            int boundedCapacity,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            _logger = logger;
            _countsPerSecond = countsPerSecond;
            _countsPerBatch = countsPerBatch;
            _boundedCapacity = boundedCapacity;
            _cancellationToken = cancellationToken;
        }

        public ITargetBlock<T> GetLoaderBlock<TOut>(
            Func<T, Task<TOut>> asyncAction,
            Func<TOut, string> logAction)
        {
            var timeInterval = TimeSpan.FromMilliseconds(1000 * _countsPerBatch / _countsPerSecond);
            var count = 0;

            _stopwatch.Start();
            var previous = _stopwatch.Elapsed;
            var current = previous;

            return new ActionBlock<T>(
                command =>
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        //_actionBlock.Complete();
                        return;
                    }

                    if (++count > _countsPerBatch)
                    {
                        do
                        {
                            current = _stopwatch.Elapsed;
                        } while ((current - previous) < timeInterval);

                        previous = current;
                        count = 1;
                    }

                    DoAsyncAction(command, asyncAction, logAction);
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 1,
                    BoundedCapacity = _boundedCapacity,
                });
        }

        private void DoAction<TOut>(T command, Func<T, TOut> action, Func<TOut, string> logAction)
        {
            var startTicks = _stopwatch.ElapsedTicks;

            //_logger.LogTrace($"Sent {startTicks / (double)Stopwatch.Frequency}");
            Console.WriteLine($"Sent {startTicks / (double)Stopwatch.Frequency}");

            var task = Task.Run(() => action(command));
            task.ContinueWith(t => Log(t, logAction, startTicks));
        }

        private void DoAsyncAction<TOut>(T command, Func<T, Task<TOut>> asyncAction, Func<TOut, string> logAction)
        {
            var startTicks = _stopwatch.ElapsedTicks;

            //_logger.LogTrace($"Sent {startTicks / (double)Stopwatch.Frequency}");
            Console.WriteLine($"Sent {startTicks / (double)Stopwatch.Frequency}");

            var task = asyncAction(command);
            task.ContinueWith(t => Log(t, logAction, startTicks));
        }

        private void Log<TOut>(Task<TOut> task, Func<TOut, string> logAction, long startTicks)
        {
            var finishTicks = _stopwatch.ElapsedTicks;
            var timeStamp = finishTicks / (double)Stopwatch.Frequency;

            var logMessage = logAction != null ? $": {logAction(task.Result)}" : "";

            //_logger.LogTrace($"Received at {timeStamp} duration {(finishTicks - startTicks) / (double)Stopwatch.Frequency}");
            Console.WriteLine($"Received at {timeStamp} duration {(finishTicks - startTicks) / (double)Stopwatch.Frequency} {logMessage}");

            //    if (targetBlock != null && transform != null)
            //        await targetBlock.SendAsync(transform(command), _cancellationToken).ConfigureAwait(false);

            // TODO: fix this
            //    //_actionBlock.Complete();
        }
    }
}
