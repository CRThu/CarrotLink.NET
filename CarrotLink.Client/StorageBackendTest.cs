using CarrotLink.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Client
{
    public static class StorageBackendTest
    {
        public static void StorageBackendSyncTest()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var backend = new ListStorageBackend<int>(null, cancellationTokenSource.Token);
            int iter = 100000000;
            for (int i = 0; i < iter; i++)
            {
                backend.Write(i);
                if (i % 10000000 == 0)
                {
                    Console.WriteLine($"WRITE:{i}/{iter}");
                }
            }
            Console.WriteLine($"WRITE done.");
            while (backend.Count < iter)
            {
                Console.WriteLine($"Wait for List:{backend.Count}/{iter}");
                Thread.Sleep(50);
            }
            Console.WriteLine($"List stored.");
            Console.WriteLine($"checking.");
            for (int i = 0; i < iter; i++)
            {
                if (i != backend[i])
                {
                    Console.WriteLine($"ERROR:{i} != {backend[i]}");
                    break;
                }
                if (i % 10000000 == 0)
                {
                    Console.WriteLine($"READ:{i}/{iter}");
                }
            }
            Console.WriteLine("Check done.");
            cancellationTokenSource.Cancel();
            Console.WriteLine("Cancelled.");
            Thread.Sleep(100);
            backend.Dispose();
            Console.WriteLine("Disposed.");

            return;
        }

        public static void StorageBackendAsyncTest()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var backend = new ListStorageBackend<int>(null, cancellationTokenSource.Token);
            int iter = 100000000;
            Task w = Task.Run(() =>
            {
                for (int i = 0; i < iter; i++)
                {
                    backend.Write(i);
                    if (i % 10000000 == 0)
                    {
                        Console.WriteLine($"WRITE:{i}/{iter}");
                    }
                }
                Console.WriteLine($"WRITE done.");
            });

            Task r = Task.Run(() =>
            {
                for (int i = 0; i < iter; i++)
                {
                    while (backend.Count - 1 < i)
                        ;
                    if (i % 10000000 == 0)
                    {
                        Console.WriteLine($"READ:{i}/{iter}");
                    }
                    if (i != backend[i])
                    {
                        Console.WriteLine($"ERROR:{i} != {backend[i]}");
                        break;
                    }
                }
                Console.WriteLine("Check done.");
            });

            Task.WaitAll(w, r);
            Console.WriteLine("Task Exit.");
            cancellationTokenSource.Cancel();
            Console.WriteLine("Cancelled.");
            Thread.Sleep(100);
            backend.Dispose();
            Console.WriteLine("Disposed.");

            return;
        }
    }
}
