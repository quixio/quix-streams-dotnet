#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming').[QuixStreamingClient](QuixStreamingClient.md 'QuixStreams.Streaming.QuixStreamingClient')

## QuixStreamingClient(string, bool, IDictionary<string,string>, bool, HttpClient, string, Uri) Constructor

Initializes a new instance of [KafkaStreamingClient](KafkaStreamingClient.md 'QuixStreams.Streaming.KafkaStreamingClient') that is capable of creating topic consumer and producers

```csharp
public QuixStreamingClient(string token=null, bool autoCreateTopics=true, System.Collections.Generic.IDictionary<string,string> properties=null, bool debug=false, System.Net.Http.HttpClient httpClient=null, string workspaceId=null, System.Uri apiUrl=null);
```
#### Parameters

<a name='QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string,bool,System.Collections.Generic.IDictionary_string,string_,bool,System.Net.Http.HttpClient,string,System.Uri).token'></a>

`token` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The token to use when talking to Quix. When not provided, Quix__Sdk__Token environment variable will be used

<a name='QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string,bool,System.Collections.Generic.IDictionary_string,string_,bool,System.Net.Http.HttpClient,string,System.Uri).autoCreateTopics'></a>

`autoCreateTopics` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

Whether topics should be auto created if they don't exist yet

<a name='QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string,bool,System.Collections.Generic.IDictionary_string,string_,bool,System.Net.Http.HttpClient,string,System.Uri).properties'></a>

`properties` [System.Collections.Generic.IDictionary&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IDictionary-2 'System.Collections.Generic.IDictionary`2')[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')[,](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IDictionary-2 'System.Collections.Generic.IDictionary`2')[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IDictionary-2 'System.Collections.Generic.IDictionary`2')

Additional broker properties

<a name='QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string,bool,System.Collections.Generic.IDictionary_string,string_,bool,System.Net.Http.HttpClient,string,System.Uri).debug'></a>

`debug` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

Whether debugging should be enabled

<a name='QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string,bool,System.Collections.Generic.IDictionary_string,string_,bool,System.Net.Http.HttpClient,string,System.Uri).httpClient'></a>

`httpClient` [System.Net.Http.HttpClient](https://docs.microsoft.com/en-us/dotnet/api/System.Net.Http.HttpClient 'System.Net.Http.HttpClient')

The http client to use

<a name='QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string,bool,System.Collections.Generic.IDictionary_string,string_,bool,System.Net.Http.HttpClient,string,System.Uri).workspaceId'></a>

`workspaceId` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The optional workspace id to use. When not provided, Quix__Workspace__Id environment variable will be used

<a name='QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string,bool,System.Collections.Generic.IDictionary_string,string_,bool,System.Net.Http.HttpClient,string,System.Uri).apiUrl'></a>

`apiUrl` [System.Uri](https://docs.microsoft.com/en-us/dotnet/api/System.Uri 'System.Uri')

The optional api url to use. When not provided, Quix__Portal__Api environment variable is used if specified else defaults to https://portal-api.platform.quix.io