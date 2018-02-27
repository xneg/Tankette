using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    class OtherProducer<T>
    {
        private TransformBlock<int, T> _transformBlock;
        private bool _isLooping = false;

        public ISourceBlock<T> SourceBlock => _transformBlock;

        public OtherProducer(Func<T> produce, ExecutionDataflowBlockOptions options)
        {
            _transformBlock = new TransformBlock<int, T>(i => produce(), options);
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
                    await _transformBlock.SendAsync(0);
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    counter++;
                }
            }, cancellationToken).ContinueWith((task) =>
            {
                _transformBlock.Complete();
            });

            _isLooping = true;
        }
    }
}
