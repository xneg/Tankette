using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    // TODO: Add Stop method.

    internal class Producer<T>
    {
        private BufferBlock<T> _bufferBlock;
        private Func<T> _produce;
        private bool _isLooping = false;

        public ISourceBlock<T> SourceBlock => _bufferBlock;

        public Producer(Func<T> produce, int boundedCapacity)
        {
            _produce = produce;
            _bufferBlock = new BufferBlock<T>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });
        }

        /// <summary>
        /// Starts engine for <paramref name="tactsCount"/> cycles.
        /// </summary>
        /// <param name="tactsCount">Number of cycles.</param>
        /// <param name="cancellationToken">Cancellation token to stop loop.</param>
        public void StartEngine(int tactsCount, CancellationToken cancellationToken)
        {
            StartEngineInternal(false, tactsCount, cancellationToken);
        }

        /// <summary>
        /// Start infinite engine.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop loop.</param>
        public void StartEngine(CancellationToken cancellationToken)
        {
            StartEngineInternal(true, 0, cancellationToken);
        }

        private void StartEngineInternal(bool isInfintite, int tactsCount, CancellationToken cancellationToken)
        {
            if (_isLooping)
                return;

            Task.Run(async () =>
            {
                var counter = 0;
                while (isInfintite || counter < tactsCount)
                {
                    await _bufferBlock.SendAsync(_produce());
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    counter++;
                }
            }, cancellationToken).ContinueWith((task) =>
            {
                _bufferBlock.Complete();
            });

            _isLooping = true;
        }
    }
}
