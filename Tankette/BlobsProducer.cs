using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    public static class BlobsProducer
    {
        private static readonly RNGCryptoServiceProvider RngCsp = new RNGCryptoServiceProvider();

        public static ISourceBlock<byte[]> CreateAndStartBlobsSourceBlock(int count, int size, int boundedCapacity, CancellationToken cancellationToken)
        {
            var producer = new Producer<byte[]>(() => CreateBlob(size), boundedCapacity);
            if (count == 0)
                producer.StartEngine(cancellationToken);
            else
                producer.StartEngine(count, cancellationToken);

            return producer.SourceBlock;
        }

        public static ISourceBlock<byte[]> CreateAndStartBlobsSourceBlock(int count, Func<byte[]> produce, int boundedCapacity, CancellationToken cancellationToken)
        {
            var producer = new Producer<byte[]>(() => produce(), boundedCapacity);
            if (count == 0)
                producer.StartEngine(cancellationToken);
            else
                producer.StartEngine(count, cancellationToken);

            return producer.SourceBlock;
        }

        private static byte[] CreateBlob(int size)
        {
            var buffer = new byte[size];
            RngCsp.GetBytes(buffer);
            return buffer;
        }
    }
}
