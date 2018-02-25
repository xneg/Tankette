using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace DataFlowBenchmark
{
    public class BlobsProducer
    {
        private static readonly RNGCryptoServiceProvider RngCsp = new RNGCryptoServiceProvider();

        public static ISourceBlock<byte[]> CreateAndStartBlobsSourceBlock(int count, int size, int boundedCapacity, CancellationToken cancellationToken)
        {
            return BlobsSourceBlock(() => CreateBlob(size), count, boundedCapacity, cancellationToken);
        }

        private static ISourceBlock<byte[]> BlobsSourceBlock(Func<byte[]> task, int count, int boundedCapacity, CancellationToken cancellationToken)
        {
            var driver = new Engine().CreateEngine(count, boundedCapacity, cancellationToken);
            var block = new TransformBlock<int, byte[]>(
                i => task(),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                });
            driver.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true });
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
