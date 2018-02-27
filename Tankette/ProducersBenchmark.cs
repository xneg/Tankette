using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    internal static class ProducersBenchmark
    {
        private static readonly RNGCryptoServiceProvider RngCsp = new RNGCryptoServiceProvider();

        public static async Task Do()
        {
            const int count = 10000;
            const int size = 1024 * 1024;
            const int boundedCapacity = 100;

            var options = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = boundedCapacity,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            };

            var blobsProducer3 = BlobsProducer.CreateAndStartBlobsSourceBlock(count, () => CreateBlob(size), boundedCapacity, CancellationToken.None);
            await DoTest("One thread first version", blobsProducer3, options).ConfigureAwait(false);

            var blobsProducer1 = OtherBlobsProducer
                .CreateAndStartBlobsSourceBlock(() => CreateBlob(size), count, boundedCapacity, Environment.ProcessorCount, CancellationToken.None);
            await DoTest("OtherBlobsProducer 4 parallels", blobsProducer1, options).ConfigureAwait(false);

            var blobsProducer2 = new OtherProducer<byte[]>(() => CreateBlob(size), options);
            blobsProducer2.StartEngine(count, CancellationToken.None);
            await DoTest("Simple effective producer 4 parallels", blobsProducer2.SourceBlock, options).ConfigureAwait(false);

            var blobsProducer4 = OtherBlobsProducer
                .CreateAndStartBlobsSourceBlock(() => CreateBlob(size), count, boundedCapacity, 2, CancellationToken.None);
            await DoTest("OtherBlobsProducer 2 parallels", blobsProducer4, options).ConfigureAwait(false);

            var blobsProducer6 = new OtherProducer<byte[]>(() => CreateBlob(size), options);
            blobsProducer6.StartEngine(count, CancellationToken.None);
            var options6 = options;
            options6.MaxDegreeOfParallelism = 1;
            await DoTest("Simple effective producer 1 parallels", blobsProducer6.SourceBlock, options6).ConfigureAwait(false);

            var blobsProducer7 = OtherBlobsProducer
               .CreateAndStartBlobsSourceBlock(() => CreateBlob(size), count, boundedCapacity, 1, CancellationToken.None);
            await DoTest("OtherBlobsProducer 1 parallels", blobsProducer7, options).ConfigureAwait(false);

            var blobsProducer5 = new OtherProducer<byte[]>(() => CreateBlob(size), options);
            blobsProducer5.StartEngine(count, CancellationToken.None);
            var options5 = options;
            options5.MaxDegreeOfParallelism = 2;
            await DoTest("Simple effective producer 2 parallels", blobsProducer5.SourceBlock, options5).ConfigureAwait(false);
        }

        public static async Task TestSimpleEffectiveProducer()
        {
            const int count = 10000;
            const int size = 1024 * 1024;
            const int boundedCapacity = 100;

            for (var i = Environment.ProcessorCount; i >= 1; i--)
            {
                var options = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = i,
                };
                var blobsProducer = new OtherProducer<byte[]>(() => CreateBlob(size), options);
                blobsProducer.StartEngine(count, CancellationToken.None);
                await DoTest($"Benchmark {i}", blobsProducer.SourceBlock, options).ConfigureAwait(false);
            }
        }

        private static async Task DoTest(string comment, ISourceBlock<byte[]> sourceBlock, ExecutionDataflowBlockOptions options)
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            var stopwatch = new Stopwatch();
            var actionBlock = new ActionBlock<byte[]>(b => { }, options);
            sourceBlock.LinkTo(actionBlock, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
            stopwatch.Start();
            await actionBlock.Completion.ConfigureAwait(false);
            stopwatch.Stop();
            Console.WriteLine($"{comment, -50} Elapsed ticks: {stopwatch.ElapsedTicks} Memory: {GC.GetTotalMemory(false)}");
        }

        private static byte[] CreateBlob(int size)
        {
            var buffer = new byte[size];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0x20;
            }
            //RngCsp.GetBytes(buffer);
            return buffer;
        }
    }
}
