using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataFlowBenchmark
{
    /// <summary>
    /// Блок, создающий некую нагрузку с заданной частотой.
    /// </summary>
    /// <typeparam name="T">Входящий тип.</typeparam>
    public class LoaderBlock<T> : ITargetBlock<T>
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly int _boundedCapacity;
        private readonly ILogger _logger;
        private readonly int _countsPerSecond;
        private readonly int _countsPerBatch;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<T> _jobAction;

        private ITargetBlock<T> _actionBlock;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="jobAction">Работа, которую необходимо совершать.</param>
        public LoaderBlock(
            Action<T> jobAction,
            int countsPerSecond,
            int countsPerBatch,
            int boundedCapacity,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            _logger = logger;
            _jobAction = jobAction;
            _countsPerSecond = countsPerSecond;
            _countsPerBatch = countsPerBatch;
            _boundedCapacity = boundedCapacity;
            _cancellationToken = cancellationToken;

            _actionBlock = GetLoaderBlock<object>(arg => Task.Run(() => _jobAction(arg), _cancellationToken));
        }

        /// <inheritdoc/>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
        {
            return _actionBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        /// <inheritdoc/>
        public void Complete()
        {
        }

        /// <inheritdoc/>
        public void Fault(Exception exception)
        {
            _actionBlock.Fault(exception);
        }

        /// <inheritdoc/>
        public Task Completion => _actionBlock.Completion;

        public void LinkTo<TOut>(ITargetBlock<TOut> targetBlock, Func<T, TOut> transform)
        {
            _actionBlock = GetLoaderBlock(arg => Task.Run(() => _jobAction(arg), _cancellationToken), targetBlock, transform);
            _actionBlock.Completion.ContinueWith((task) => targetBlock.Complete(), _cancellationToken);
        }

        private ITargetBlock<T> GetLoaderBlock<TOut>(
            Func<T, Task> action,
            ITargetBlock<TOut> targetBlock = null,
            Func<T, TOut> transform = null)
        {
            var dt = DateTime.MinValue;
            var started = 0;
            var count = 0;
            var timeInterval = 1000 * _countsPerBatch / _countsPerSecond;

            return new ActionBlock<T>(
                command =>
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        _actionBlock.Complete();
                        return;
                    }

                    if (started == 0)
                    {
                        dt = DateTime.UtcNow;
                        _stopwatch.Start();
                        started = 1;
                    }

                    if (count++ < _countsPerBatch)
                    {
                        DoAndLog(command);
                    }
                    else
                    {
                        while ((DateTime.UtcNow - dt).TotalMilliseconds < timeInterval)
                        {
                        }
                        dt = DateTime.UtcNow;
                        count = 1;

                        DoAndLog(command);
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 1,
                    BoundedCapacity = _boundedCapacity,
                });

            void DoAndLog(T command)
            {
                var startTicks = _stopwatch.ElapsedTicks;
                //_logger.LogTrace($"Sent {startTicks / (double)Stopwatch.Frequency}");
                Console.WriteLine($"Sent {startTicks / (double)Stopwatch.Frequency}");
                var t = action(command);
                t.ContinueWith(task => ContinuationAction<TOut>(command, startTicks, targetBlock, transform), _cancellationToken);
            }
        }

        private async Task ContinuationAction<TOut>(
            T command,
            long startTicks,
            ITargetBlock<TOut> targetBlock = null,
            Func<T, TOut> transform = null)
        {
            var finishTicks = _stopwatch.ElapsedTicks;

            var timeStamp = finishTicks / (double)Stopwatch.Frequency;
            //_logger.LogTrace($"Received at {timeStamp} duration {(finishTicks - startTicks) / (double)Stopwatch.Frequency}");
            Console.WriteLine($"Received at {timeStamp} duration {(finishTicks - startTicks) / (double)Stopwatch.Frequency}");

            if (targetBlock != null && transform != null)
                await targetBlock.SendAsync(transform(command), _cancellationToken).ConfigureAwait(false);

            // TODO: пока не придумал, как сделать красиво и правильно. Возможно, что никак.
            //_actionBlock.Complete();
        }
    }
}
