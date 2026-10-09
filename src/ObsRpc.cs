using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EvaClipper2 {
// One authenticated connection per encoder. Polling no longer reconnects twice per tick.
public sealed class ObsRpc:IDisposable {
 readonly int port;readonly string password;
 readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
 ClientWebSocket socket;bool disposed;
 public ObsRpc(int p,string pw){port=p;password=pw;}
 static string Hash(string text){using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(text)));}
 async Task<Dictionary<string,object>> Read(CancellationToken token){
  byte[] bytes=new byte[8192];using(var memory=new MemoryStream()){
   WebSocketReceiveResult r;
   do{r=await socket.ReceiveAsync(new ArraySegment<byte>(bytes),token).ConfigureAwait(false);if(r.MessageType==WebSocketMessageType.Close)throw new IOException("Ядро записи закрыло соединение.");memory.Write(bytes,0,r.Count);if(memory.Length>16000000)throw new IOException("Слишком большой ответ ядра.");}while(!r.EndOfMessage);
   return J.Parse(Encoding.UTF8.GetString(memory.ToArray()));
  }
 }
 async Task Send(object data,CancellationToken token){byte[] bytes=Encoding.UTF8.GetBytes(J.Encode(data));await socket.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,token).ConfigureAwait(false);}
 async Task Connect(CancellationToken token){
  if(socket!=null&&socket.State==WebSocketState.Open)return;
  Reset();socket=new ClientWebSocket();socket.Options.Proxy=null;
  await socket.ConnectAsync(new Uri("ws://127.0.0.1:"+port),token).ConfigureAwait(false);
  var hello=J.D((await Read(token).ConfigureAwait(false))["d"]);var identify=J.Obj("rpcVersion",1,"eventSubscriptions",0);
  if(hello.ContainsKey("authentication")){var auth=J.D(hello["authentication"]);identify["authentication"]=Hash(Hash(password+J.S(auth,"salt"))+J.S(auth,"challenge"));}
  await Send(J.Obj("op",1,"d",identify),token).ConfigureAwait(false);
  if(J.N(await Read(token).ConfigureAwait(false),"op")!=2)throw new IOException("Ядро не подтвердило подключение.");
 }
 public static Dictionary<string,object> Request(string type,object data=null){return J.Obj("requestType",type,"requestData",data??J.Obj());}
 static Dictionary<string,object> Result(Dictionary<string,object> reply){var status=J.D(reply["requestStatus"]);if(!J.B(status,"result"))throw new IOException(J.S(reply,"requestType")+": "+J.S(status,"comment",J.S(status,"code")));return reply.ContainsKey("responseData")?J.D(reply["responseData"]):J.Obj();}
 async Task<Dictionary<string,object>> Exchange(int op,object data,int expected,string id){
  await gate.WaitAsync().ConfigureAwait(false);
  try{
   if(disposed)throw new ObjectDisposedException("ObsRpc");
   using(var timeout=new CancellationTokenSource(15000)){
    await Connect(timeout.Token).ConfigureAwait(false);await Send(J.Obj("op",op,"d",data),timeout.Token).ConfigureAwait(false);
    while(true){var packet=await Read(timeout.Token).ConfigureAwait(false);if(J.N(packet,"op")!=expected)continue;var d=J.D(packet["d"]);if(J.S(d,"requestId")==id)return d;}
   }
  }catch{Reset();throw;}finally{gate.Release();}
 }
 public async Task<Dictionary<string,object>> Call(string type,object data=null){var id=Guid.NewGuid().ToString("N");return Result(await Exchange(6,J.Obj("requestType",type,"requestId",id,"requestData",data??J.Obj()),7,id).ConfigureAwait(false));}
 public async Task<List<Dictionary<string,object>>> Batch(IEnumerable<Dictionary<string,object>> requests){
  var id=Guid.NewGuid().ToString("N");var reply=await Exchange(8,J.Obj("requestId",id,"haltOnFailure",false,"executionType",0,"requests",requests),9,id).ConfigureAwait(false);
  var results=new List<Dictionary<string,object>>();foreach(var row in J.Rows(reply["results"]))results.Add(Result(row));return results;
 }
 void Reset(){if(socket!=null){socket.Dispose();socket=null;}}
 public void Dispose(){disposed=true;Reset();}
}
}
