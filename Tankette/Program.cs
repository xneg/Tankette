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
            var cancellationTokenSource = new CancellationTokenSource();

            var blobsProducer = BlobsProducer.CreateAndStartBlobsSourceBlock(0, 1024 * 1024, 220, cancellationTokenSource.Token);

            var md5Hash = MD5.Create();

            var loaderBlockNew = new LoaderBlockNew<byte[]>(200, 10, 220, null, cancellationTokenSource.Token);

            var block = loaderBlockNew.GetLoaderBlock<string>(
                async (b) =>
                {
                    await Task.Delay(3000).ConfigureAwait(false);
                    return GetMd5Hash(md5Hash, b);
                },
                hash => hash);

            blobsProducer.LinkTo(block);

            Console.ReadLine();
            cancellationTokenSource.Cancel();
            GC.Collect();
            Console.ReadLine();
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
