using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tankette
{
    class Program
    {
        static void Main(string[] args)
        {
            //var cancellationTokenSource = new CancellationTokenSource();

            //var blobsProducer = BlobsProducer.CreateAndStartBlobsSourceBlock(0, 1024 * 1024, 200, cancellationTokenSource.Token);

            //var md5Hash = MD5.Create();

            //var loaderBlock = new LoaderBlock<byte[]>(
            //    async (b) =>
            //    {
            //        await Task.Delay(500);
            //        Console.WriteLine(GetMd5Hash(md5Hash, b));
            //     },
            //    200,
            //    10,
            //    200,
            //    null,
            //    CancellationToken.None);

            ////var actionBlock = new ActionBlock<byte[]>(b => 
            ////{
            ////    Console.WriteLine(GetMd5Hash(md5Hash, b));
            ////},
            ////new ExecutionDataflowBlockOptions() { BoundedCapacity = 10 });

            ////blobsProducer.LinkTo(actionBlock);
            //blobsProducer.LinkTo(loaderBlock);
            //ProducersBenchmark.Do().Wait();
            ProducersBenchmark.TestSimpleEffectiveProducer().Wait();

            Console.ReadLine();
            //Console.WriteLine("Hello World!");
        }

        static string GetMd5Hash(MD5 md5Hash, byte[] input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
