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

            var linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };

            GC.Collect();

            var stopwatch = new Stopwatch();

            stopwatch.Reset();
            GC.Collect();

            stopwatch.Start();
            var actionBlock3 = new ActionBlock<byte[]>(b => { }, options);
            var blobsProducer3 = OtherBlobsProducer.CreateAndStartBlobsSourceBlock(() => CreateBlob(size), count, boundedCapacity, CancellationToken.None);
            blobsProducer3.LinkTo(actionBlock3, linkOptions);

            await actionBlock3.Completion.ConfigureAwait(false);
            stopwatch.Stop();

            Console.WriteLine($"Variant 3: {stopwatch.ElapsedTicks} Memory: {GC.GetTotalMemory(false)}");

            stopwatch.Reset();
            GC.Collect();

            stopwatch.Start();
            var actionBlock2 = new ActionBlock<byte[]>(b => { }, options);
            var blobsProducer2 = new OtherProducer<byte[]>(() => CreateBlob(size), options);
            blobsProducer2.SourceBlock.LinkTo(actionBlock2, linkOptions);
            blobsProducer2.StartEngine(count, CancellationToken.None);

            await actionBlock2.Completion.ConfigureAwait(false);
            stopwatch.Stop();

            Console.WriteLine($"Variant 2: {stopwatch.ElapsedTicks} Memory: {GC.GetTotalMemory(false)}");

            //stopwatch.Start();
            //var actionBlock1 = new ActionBlock<byte[]>(b => { }, options);
            //var blobsProducer1 = BlobsProducer.CreateAndStartBlobsSourceBlock(count, size, boundedCapacity, CancellationToken.None);
            //blobsProducer1.LinkTo(actionBlock1, linkOptions);

            //await actionBlock1.Completion.ConfigureAwait(false);
            //stopwatch.Stop();

            //Console.WriteLine($"Variant 1: {stopwatch.ElapsedTicks} Memory: {GC.GetTotalMemory(false)}");

            //stopwatch.Reset();
            //GC.Collect();

            //stopwatch.Start();
            //var actionBlock3 = new ActionBlock<byte[]>(b => { }, options);
            //var blobsProducer3 = OtherBlobsProducer.CreateAndStartBlobsSourceBlock(count, size, boundedCapacity, CancellationToken.None);
            //blobsProducer3.LinkTo(actionBlock3, linkOptions);

            //await actionBlock3.Completion.ConfigureAwait(false);
            //stopwatch.Stop();

            //Console.WriteLine($"Variant 3: {stopwatch.ElapsedTicks} Memory: {GC.GetTotalMemory(false)}");
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
