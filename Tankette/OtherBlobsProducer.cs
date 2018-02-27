using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    class OtherBlobsProducer
    {
        private static readonly RNGCryptoServiceProvider RngCsp = new RNGCryptoServiceProvider();

        public static ISourceBlock<byte[]> CreateAndStartBlobsSourceBlock(
            Func<byte[]> task,
            int count,
            int boundedCapacity,
            int parallelism,
            CancellationToken cancellationToken)
        {
            return BlobsSourceBlock(() => task(), count, boundedCapacity, parallelism, cancellationToken);
        }

        private static ISourceBlock<byte[]> BlobsSourceBlock(
            Func<byte[]> task,
            int count,
            int boundedCapacity,
            int parallelism,
            CancellationToken cancellationToken)
        {
            var engine = new Engine(boundedCapacity);
            var block = new TransformBlock<int, byte[]>(
                i => task(),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = parallelism,
                });
            engine.SourceBlock.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });
            engine.StartEngine(count, cancellationToken);
            return block;
        }

        private static byte[] CreateBlob(int size)
        {
            var buffer = new byte[size];
            RngCsp.GetBytes(buffer);
            return buffer;
        }
    }
}
