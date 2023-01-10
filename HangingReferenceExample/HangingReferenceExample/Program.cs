using System.Net;
using System.Runtime.Loader;
using System.Text.Json;

namespace HangingReferenceExample;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a new HTTP listener
        var listener = new HttpListener();

        // Add a prefix to the listener
        listener.Prefixes.Add("http://localhost:8080/");

        // Start the listener
        listener.Start();

        while (true)
        {
            // Wait for a request
            var context = await listener.GetContextAsync();

            // Get the request and response objects
            var request = context.Request;
            var response = context.Response;

            // Get the path parameter from the URL
            var pathParam = request.Url.Segments[1].TrimEnd('/');

            var responseString = "";

            if (pathParam == "Load")
            {
                var assemblyLoadContext = new AssemblyLoadContext("GithubExample.Testing", true);
                assemblyLoadContext.LoadFromAssemblyPath(Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.FullName, "GithubExample.dll"));
                responseString = "Loaded";
            }

            if (pathParam == "Unload")
            {
                var contexts = AssemblyLoadContext.All.Reverse();
                foreach (var c in contexts)
                {
                    if (string.Equals(c.Name, "GithubExample.Testing", StringComparison.OrdinalIgnoreCase) && c != AssemblyLoadContext.Default)
                    {
                        var weakReference = new WeakReference(context, true);
                        c.Unload();
                        for (int i = 0; weakReference.IsAlive && i < 10; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                }
                responseString = "Unloaded";
            }

            if (pathParam == "TestToString")
            {
                responseString = GetData().ToString();
            }

            if (pathParam == "TestToJson")
            {
                responseString = JsonSerializer.Serialize(GetData());
            }

            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer);

            // Close the response
            responseOutput.Close();
        }
    }

    private static object GetData()
    {
        Type? foundType = null;
        foreach (var asm in AssemblyLoadContext.All.Reverse().SelectMany(c => c.Assemblies))
        {
            if (asm.IsDynamic) continue;
            var type = asm.GetType("GithubExample.Testing", false, true);
            if (type is not null)
                foundType = type;
        }
        if (foundType is null) return null;
        var result = (SomeBase)Activator.CreateInstance(foundType);
        return result.GetData();
    }
}