using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting;
using System.Text;

public class JsonRpcRequest
{
    public string jsonrpc = "2.0";
    [JsonProperty("params")]
    public dynamic Params { get; set; }
    [JsonProperty("method")]
    public string Method{ get; set; }
    [JsonProperty("id")]
    public string Id { get; set; }
}

public class JsonRpcResponse
{
    [JsonProperty("result", NullValueHandling=NullValueHandling.Ignore)]
    public dynamic Result { get; set; }
    [JsonProperty("error", NullValueHandling=NullValueHandling.Ignore)]
    public dynamic Error { get; set; }
    [JsonProperty("id")]
    public string Id { get; set; }
}

public class JsonRpcError
{
    [JsonProperty("code")]
    public int Code;

    [JsonProperty("message")]
    public string Message;
}

public class RpcRunner
{
    public Dictionary<string, Func<object, string, dynamic>> Methods = new Dictionary<string, Func<object, string, dynamic>>();

    public void SetHandler(string methodName, Func<object, string, dynamic> run)
    {
        Methods.Add(methodName, run);
    }

    public bool HasHandler(string methodName)
    {
        return Methods.ContainsKey(methodName);
    }

    public dynamic Run(string methodName, object rpcParams, string rpcId)
    {
        return Methods[methodName](rpcParams, rpcId);
    }
}

public class RpcFailedException : Exception
{
    public int ErrorCode{ get; set; }
    public RpcFailedException(string message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}

namespace dotnet_hello_json_rpc
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            TODO: read RPCs from STDIN
            Stream s = Console.OpenStandardInput();

            byte[] bytes = new byte[s.Length + 10];
            int numBytesToRead = (int)s.Length;
            int numBytesRead = 0;
            do
            {
                // Read may return anything from 0 to 10.
                int n = s.Read(bytes, numBytesRead, 10);
                numBytesRead += n;
                numBytesToRead -= n;
            } while (numBytesToRead > 0);
            s.Close();
            char[] chars = Encoding.UTF8.GetChars(bytes, 0, numBytesRead);
            */

            string reqJson = @"{'jsonrpc': '2.0', 'method': 'MyCompany.MyApp.Foo', 'params': { 'arg1' : 3 }, 'id': 'deadbeefcafebabe'}";
            JsonRpcRequest req = JsonConvert.DeserializeObject<JsonRpcRequest>(reqJson);
            Console.WriteLine($"REQUEST ID IS {req.Id}");

            RpcRunner r = new RpcRunner();
            r.SetHandler("MyCompany.MyApp.Foo", (dynamic rpcParams, string rpcId) =>
            {
                Console.WriteLine("Got a Foo!");
                return rpcParams.arg1 + 42;
            });

            JsonRpcResponse resp = new JsonRpcResponse();
            if (r.HasHandler(req.Method))
            {
               resp.Id = req.Id;
               try
               {
                   resp.Result = r.Run(req.Method, req.Params, req.Id);
               }
               catch (RpcFailedException rfe)
               {
                   resp.Error = new JsonRpcError()
                   {
                       Code = rfe.ErrorCode,
                       Message = rfe.Message
                   };
               }
            }
            else
            {
                resp.Id = req.Id;
                resp.Error = new JsonRpcError()
                {
                    Code = 501,
                    Message = $"Unknown method {req.Method}"
                };
            }

            Console.WriteLine(JsonConvert.SerializeObject(resp, Formatting.Indented));
        }
    }
}
