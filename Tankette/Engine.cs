using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    // TODO: Add Stop method.

    /// <summary>
    /// Engine-class (like a car engine) that produced a lot count (or infinite) of actions.
    /// </summary>
    public class Engine
    {
        private BufferBlock<int> _bufferBlock;
        private bool _isLooping = false;

        public ISourceBlock<int> SourceBlock => _bufferBlock;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="boundedCapacity">Bounded capacity (throttling).</param>
        public Engine(int boundedCapacity)
        {
            _bufferBlock = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });
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
                    await _bufferBlock.SendAsync(0);
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
