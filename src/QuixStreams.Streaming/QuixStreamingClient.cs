using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuixStreams.Kafka;
using QuixStreams.Kafka.Transport;
using QuixStreams.Streaming.Configuration;
using QuixStreams.Streaming.Exceptions;
using QuixStreams.Streaming.Models;
using QuixStreams.Streaming.QuixApi;
using QuixStreams.Streaming.QuixApi.Portal;
using QuixStreams.Streaming.QuixApi.Portal.Requests;
using QuixStreams.Streaming.Raw;
using QuixStreams.Streaming.Utils;
using AutoOffsetReset = QuixStreams.Telemetry.Kafka.AutoOffsetReset;
using SaslMechanism = QuixStreams.Streaming.Configuration.SaslMechanism;

namespace QuixStreams.Streaming
{
    /// <summary>
    /// Represents a streaming client for Kafka configured automatically using Environment Variables and Quix Cloud endpoints.
    /// Use this Client when you use this library together with Quix Cloud.
    /// </summary>
    public interface IQuixStreamingClient : IQuixStreamingClientAsync
    {
        /// <summary>
        /// Gets a topic consumer capable of subscribing to receive incoming streams.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="options">The settings to use for committing</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="partitions">The partitions to subscribe to. If not provided, All partitions are subscribed to according to other configuration such as consumer group.</param>
        /// <returns>Instance of <see cref="ITopicConsumer"/></returns>
        ITopicConsumer GetTopicConsumer(string topicIdOrName, string consumerGroup = null, CommitOptions options = null, AutoOffsetReset autoOffset = AutoOffsetReset.Latest, ICollection<Partition> partitions = null);

        /// <summary>
        /// Gets a topic consumer capable of subscribing to receive incoming streams.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partitionOffset">The partition offset to start reading from</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="options">The settings to use for committing</param>
        /// <returns>Instance of <see cref="ITopicConsumer"/></returns>
        ITopicConsumer GetTopicConsumer(string topicIdOrName, PartitionOffset partitionOffset, string consumerGroup = null, CommitOptions options = null);

        /// <summary>
        /// Gets a topic consumer capable of subscribing to receive non-quixstreams incoming messages. 
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="partitions">The partitions to subscribe to. If not provided, All partitions are subscribed to according to other configuration such as consumer group.</param>
        /// <returns>Instance of <see cref="IRawTopicConsumer"/></returns>
        IRawTopicConsumer GetRawTopicConsumer(string topicIdOrName, string consumerGroup = null, AutoOffsetReset? autoOffset = null, ICollection<Partition> partitions = null);

        /// <summary>
        /// Gets a topic producer capable of publishing non-quixstreams messages. 
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <returns>Instance of <see cref="IRawTopicProducer"/></returns>
        IRawTopicProducer GetRawTopicProducer(string topicIdOrName);

        /// <summary>
        /// Gets a topic producer capable of publishing non-quixstreams messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partition">The partition to produce to.</param>
        /// <returns>Instance of <see cref="IRawTopicProducer"/></returns>
        IRawTopicProducer GetRawTopicProducer(string topicIdOrName, Partition partition);
        
        /// <summary>
        /// Gets a topic producer capable of publishing non-quixstreams messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partitioner">The partitioner to produce with.</param>
        /// <returns>Instance of <see cref="IRawTopicProducer"/></returns>
        IRawTopicProducer GetRawTopicProducer(string topicIdOrName, QuixPartitionerDelegate partitioner);

        /// <summary>
        /// Gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <returns>Instance of <see cref="ITopicProducer"/></returns>
        ITopicProducer GetTopicProducer(string topicIdOrName);

        /// <summary>
        /// Gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partition">The partition to produce to.</param>
        /// <returns>Instance of <see cref="ITopicProducer"/></returns>
        ITopicProducer GetTopicProducer(string topicIdOrName, Partition partition);
        
        /// <summary>
        /// Gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partitioner">The partitioner to produce with.</param>
        /// <returns>Instance of <see cref="ITopicProducer"/></returns>
        ITopicProducer GetTopicProducer(string topicIdOrName, StreamPartitionerDelegate partitioner);
    }

    /// <summary>
    /// Represents an asynchronous streaming client for Kafka configured automatically using Environment Variables and Quix Cloud endpoints.
    /// Use this Client when you use this library together with Quix Cloud.
    /// </summary>
    public interface IQuixStreamingClientAsync
    {
        /// <summary>
        /// Asynchronously gets a topic consumer capable of subscribing to receive incoming streams.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="options">The settings to use for committing</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="partitions">The partitions to subscribe to. If not provided, All partitions are subscribed to according to other configuration such as consumer group.</param>
        /// <returns>A task returning an instance of <see cref="ITopicConsumer"/></returns>
        Task<ITopicConsumer> GetTopicConsumerAsync(string topicIdOrName, string consumerGroup = null, CommitOptions options = null, AutoOffsetReset autoOffset = AutoOffsetReset.Latest, ICollection<Partition> partitions = null);

        /// <summary>
        /// Asynchronously gets a topic consumer capable of subscribing to receive incoming streams.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partitionOffset">The partition offset to start reading from</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="options">The settings to use for committing</param>
        /// <returns>A task returning an instance of <see cref="ITopicConsumer"/></returns>
        Task<ITopicConsumer> GetTopicConsumerAsync(string topicIdOrName, PartitionOffset partitionOffset, string consumerGroup = null, CommitOptions options = null);
        
        /// <summary>
        /// Asynchronously gets a topic consumer capable of subscribing to receive non-quixstreams incoming messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="partitions">The partitions to subscribe to. If not provided, All partitions are subscribed to according to other configuration such as consumer group.</param>
        /// <returns>A task returning an instance of <see cref="IRawTopicConsumer"/></returns>
        Task<IRawTopicConsumer> GetRawTopicConsumerAsync(string topicIdOrName, string consumerGroup = null, AutoOffsetReset? autoOffset = null, ICollection<Partition> partitions = null);

        /// <summary>
        /// Asynchronously gets a topic producer capable of publishing non-quixstreams messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <returns>A task returning an instance of <see cref="IRawTopicProducer"/></returns>
        Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName);

        /// <summary>
        /// Asynchronously gets a topic producer capable of publishing non-quixstreams messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partition">The partition to produce to.</param>
        /// <returns>A task returning an instance of <see cref="IRawTopicProducer"/></returns>
        Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName, Partition partition);
        
        /// <summary>
        /// Asynchronously gets a topic producer capable of publishing non-quixstreams messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partitioner">The partitioner to produce with.</param>
        /// <returns>A task returning an instance of <see cref="IRawTopicProducer"/></returns>
        Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName, QuixPartitionerDelegate partitioner);

        /// <summary>
        /// Asynchronously gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <returns>A task returning an instance of <see cref="ITopicProducer"/></returns>
        Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName);

        /// <summary>
        /// Asynchronously gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partition">The partition to produce to.</param>
        /// <returns>A task returning an instance of <see cref="ITopicProducer"/></returns>
        Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName, Partition partition);
        
        /// <summary>
        /// Asynchronously gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topicIdOrName">Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order</param>
        /// <param name="partitioner">The partitioner to produce with.</param>
        /// <returns>A task returning an instance of <see cref="ITopicProducer"/></returns>
        Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName, StreamPartitionerDelegate partitioner);
    }

    /// <summary>
    /// Streaming client for Kafka configured automatically using Environment Variables and Quix Cloud endpoints.
    /// Use this Client when you use this library together with Quix Cloud.
    /// </summary>
    public class QuixStreamingClient : IQuixStreamingClient
    {
        private readonly ILogger logger = Logging.CreateLogger<QuixStreamingClient>();
        private readonly IDictionary<string, string> brokerProperties;
        private readonly string token;
        private readonly string workspaceId;
        private readonly bool autoCreateTopics;
        private readonly bool debug;
        private readonly ConcurrentDictionary<string, KafkaStreamingClient> wsToStreamingClientDict = new ConcurrentDictionary<string, KafkaStreamingClient>();
        private readonly ConcurrentDictionary<string, Workspace> topicToWorkspaceDict = new ConcurrentDictionary<string, Workspace>();
        private Lazy<HttpClient> httpClient;
        private HttpClientHandler handler;
        private const string WorkspaceIdEnvironmentKey = "Quix__Workspace__Id";
        private const string PortalApiEnvironmentKey = "Quix__Portal__Api";
        private const string SdkTokenKey = "Quix__Sdk__Token";
        private static readonly ConcurrentDictionary<string, object> workspaceLocks = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// The base API uri. Defaults to <c>https://portal-api.platform.quix.io</c>, or environment variable <c>Quix__Portal__Api</c> if available.
        /// </summary>
        public Uri ApiUrl = new Uri("https://portal-api.platform.quix.io");
        
        /// <summary>
        /// The period for which some API responses will be cached to avoid excessive amount of calls. Defaults to 1 minute.
        /// </summary>
        public TimeSpan CachePeriod = TimeSpan.FromMinutes(1);

        private bool tokenChecked;

        private static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>()
            {
                new SafeEnumConverter()
            }
        };

        /// <summary>
        /// Initializes a new instance of <see cref="KafkaStreamingClient"/> that is capable of creating topic consumer and producers
        /// </summary>
        /// <param name="token">The token to use when talking to Quix. When not provided, Quix__Sdk__Token environment variable will be used</param>
        /// <param name="autoCreateTopics">Whether topics should be auto created if they don't exist yet</param>
        /// <param name="properties">Additional broker properties</param>
        /// <param name="debug">Whether debugging should be enabled</param>
        /// <param name="httpClient">The http client to use</param>
        /// <param name="workspaceId">The optional workspace id to use. When not provided, Quix__Workspace__Id environment variable will be used</param>
        /// <param name="apiUrl">The optional api url to use. When not provided, Quix__Portal__Api environment variable is used if specified else defaults to https://portal-api.platform.quix.io</param>
        public QuixStreamingClient(string token = null, bool autoCreateTopics = true, IDictionary<string, string> properties = null, bool debug = false, HttpClient httpClient = null, string workspaceId = null, Uri apiUrl = null)
        {
            this.token = token;
            if (string.IsNullOrWhiteSpace(this.token))
            {
                this.token = Environment.GetEnvironmentVariable(SdkTokenKey);
                if (string.IsNullOrWhiteSpace(this.token))
                {
                    throw new InvalidConfigurationException($"Token must be given as an argument or set in {SdkTokenKey} environment variable.");
                }
                this.logger.LogTrace("Using token from environment variable {0}", SdkTokenKey);
            }

            this.workspaceId = workspaceId;
            if (string.IsNullOrWhiteSpace(this.workspaceId))
            {
                this.workspaceId = Environment.GetEnvironmentVariable(WorkspaceIdEnvironmentKey);
                if (string.IsNullOrWhiteSpace(this.workspaceId))
                {
                    this.logger.LogTrace("Workspace Id will be picked from token if available, as not provided in {0}", WorkspaceIdEnvironmentKey);
                }
                else
                {
                    this.logger.LogTrace("Using token from environment variable {0}", WorkspaceIdEnvironmentKey);
                }
            }
            
            this.autoCreateTopics = autoCreateTopics;
            this.brokerProperties = properties ?? new Dictionary<string, string>();
            this.debug = debug;
            if (httpClient == null)
            {
                this.handler = new HttpClientHandler();
                
                httpClient = new HttpClient(handler);
            }

            this.httpClient = new Lazy<HttpClient>(() => httpClient);

            if (apiUrl != null)
            {
                ApiUrl = apiUrl;
            }
            else
            {
                var envUri = Environment.GetEnvironmentVariable(PortalApiEnvironmentKey)?
                    .ToLowerInvariant().TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(envUri))
                {
                    if (envUri != ApiUrl.AbsoluteUri.ToLowerInvariant().TrimEnd('/'))
                    {
                        this.logger.LogInformation("Using {0} endpoint for portal, configured from env var {1}", envUri, PortalApiEnvironmentKey);
                        ApiUrl = new Uri(envUri);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public ITopicConsumer GetTopicConsumer(string topicIdOrName, string consumerGroup = null, CommitOptions options = null, AutoOffsetReset autoOffset = AutoOffsetReset.Latest, ICollection<Partition> partitions = null)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));
            
            var (client, topicId, ws) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();
            (consumerGroup, options) = GetValidConsumerGroup(topicIdOrName, consumerGroup, options).ConfigureAwait(false).GetAwaiter().GetResult();
            
            return client.GetTopicConsumer(topicId, consumerGroup, options, autoOffset, partitions);
        }

        /// <inheritdoc/>
        public ITopicConsumer GetTopicConsumer(string topicIdOrName, PartitionOffset partitionOffset, string consumerGroup = null, CommitOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));
            
            var (client, topicId, ws) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();
            (consumerGroup, options) = GetValidConsumerGroup(topicIdOrName, consumerGroup, options).ConfigureAwait(false).GetAwaiter().GetResult();
            
            return client.GetTopicConsumer(topicId, partitionOffset, consumerGroup, options);
        }

        /// <inheritdoc/>
        public async Task<ITopicConsumer> GetTopicConsumerAsync(string topicIdOrName, string consumerGroup = null, CommitOptions options = null, AutoOffsetReset autoOffset = AutoOffsetReset.Latest, ICollection<Partition> partitions = null)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, ws) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);
            (consumerGroup, options) = await GetValidConsumerGroup(topicIdOrName, consumerGroup, options).ConfigureAwait(false);

            return client.GetTopicConsumer(topicId, consumerGroup, options, autoOffset, partitions);
        }

        /// <inheritdoc/>
        public async Task<ITopicConsumer> GetTopicConsumerAsync(string topicIdOrName, PartitionOffset partitionOffset, string consumerGroup = null, CommitOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, ws) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);
            (consumerGroup, options) = await GetValidConsumerGroup(topicIdOrName, consumerGroup, options).ConfigureAwait(false);

            return client.GetTopicConsumer(topicId, partitionOffset, consumerGroup, options);
        }

        /// <inheritdoc/>
        public IRawTopicConsumer GetRawTopicConsumer(string topicIdOrName, string consumerGroup = null, AutoOffsetReset? autoOffset = null, ICollection<Partition> partitions = null)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();
            (consumerGroup, _) = GetValidConsumerGroup(topicIdOrName, consumerGroup, null).ConfigureAwait(false).GetAwaiter().GetResult();

            return client.GetRawTopicConsumer(topicId, consumerGroup, autoOffset, partitions);
        }

        /// <inheritdoc/>
        public async Task<IRawTopicConsumer> GetRawTopicConsumerAsync(string topicIdOrName, string consumerGroup = null, AutoOffsetReset? autoOffset = null, ICollection<Partition> partitions = null)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);
            (consumerGroup, _) = await GetValidConsumerGroup(topicIdOrName, consumerGroup, null).ConfigureAwait(false);

            return client.GetRawTopicConsumer(topicId, consumerGroup, autoOffset, partitions);
        }

        /// <inheritdoc/>
        public IRawTopicProducer GetRawTopicProducer(string topicIdOrName)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();

            return client.GetRawTopicProducer(topicId);
        }

        /// <inheritdoc/>
        public IRawTopicProducer GetRawTopicProducer(string topicIdOrName, Partition partition)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();

            return client.GetRawTopicProducer(topicId, partition);
        }

        /// <inheritdoc/>
        public IRawTopicProducer GetRawTopicProducer(string topicIdOrName, QuixPartitionerDelegate partitioner)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();

            return client.GetRawTopicProducer(topicId, partitioner);
        }

        /// <inheritdoc/>
        public async Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);

            return client.GetRawTopicProducer(topicId);
        }

        /// <inheritdoc/>
        public async Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName, Partition partition)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);

            return client.GetRawTopicProducer(topicId, partition);
        }

        /// <inheritdoc/>
        public async Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName, QuixPartitionerDelegate partitioner)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);

            return client.GetRawTopicProducer(topicId, partitioner);
        }

        /// <inheritdoc/>
        public ITopicProducer GetTopicProducer(string topicIdOrName)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();

            return client.GetTopicProducer(topicId);
        }

        /// <inheritdoc/>
        public ITopicProducer GetTopicProducer(string topicIdOrName, Partition partition)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();

            return client.GetTopicProducer(topicId, partition);
        }

        /// <inheritdoc/>
        public ITopicProducer GetTopicProducer(string topicIdOrName, StreamPartitionerDelegate partitioner)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false).GetAwaiter().GetResult();

            return client.GetTopicProducer(topicId, partitioner);
        }

        /// <inheritdoc/>
        public async Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);

            return client.GetTopicProducer(topicId);
        }

        /// <inheritdoc/>
        public async Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName, Partition partition)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);

            return client.GetTopicProducer(topicId, partition);
        }

        /// <inheritdoc/>
        public async Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName, StreamPartitionerDelegate partitioner)
        {
            if (string.IsNullOrWhiteSpace(topicIdOrName)) throw new ArgumentNullException(nameof(topicIdOrName));

            var (client, topicId, _) = await this.ValidateTopicAndCreateClient(topicIdOrName).ConfigureAwait(false);

            return client.GetTopicProducer(topicId, partitioner);
        }

        private async Task<(string, CommitOptions)> GetValidConsumerGroup(string topicIdOrName, string originalConsumerGroup, CommitOptions commitOptions)
        {
            topicIdOrName = topicIdOrName.Trim();
            originalConsumerGroup = originalConsumerGroup?.Trim();
            var newCommitOptions = commitOptions;
            var consumerGroup = originalConsumerGroup;
            
            var ws = await GetWorkspaceFromConfiguration(topicIdOrName).ConfigureAwait(false);
            if (originalConsumerGroup == null)
            {
                if (commitOptions?.AutoCommitEnabled == true)
                {
                    this.logger.LogWarning("Disabling commit options as no consumer group is set. To remove this warning, set consumer group or disable auto commit.");
                }

                newCommitOptions = new CommitOptions()
                {
                    AutoCommitEnabled = false
                };
                
                // Hacky workaround to an issue that Kafka client can't be left with no GroupId, but it still uses it for ACL checks.
                ConsumerConfiguration.ConsumerGroupIdWhenNotSet = ws.WorkspaceId + "-" + Guid.NewGuid().ToString("N").Substring(0, 10);
                return (null, newCommitOptions);
            }
            
            if (!consumerGroup.StartsWith(ws.WorkspaceId))
            {
                this.logger.LogDebug("Updating consumer group to have workspace id prefix to avoid being invalid.");
                consumerGroup = ws.WorkspaceId + "-" + consumerGroup;
            }

            return (consumerGroup, newCommitOptions);
        }

        private async Task<(KafkaStreamingClient client, string topicId, Workspace ws)> ValidateTopicAndCreateClient(string topicIdOrName)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));
            topicIdOrName = topicIdOrName.Trim();
            var sw = Stopwatch.StartNew();
            var ws = await GetWorkspaceFromConfiguration(topicIdOrName).ConfigureAwait(false);
            var client = await this.CreateStreamingClientForWorkspace(ws).ConfigureAwait(false);
            sw.Stop();
            this.logger.LogTrace("Created streaming client for workspace {0} in {1}.", ws.WorkspaceId, sw.Elapsed);

            sw = Stopwatch.StartNew();
            var topicId = await this.ValidateTopicExistence(ws, topicIdOrName).ConfigureAwait(false);
            sw.Stop();
            this.logger.LogTrace("Validated topic {0} in {1}.", topicIdOrName, sw.Elapsed);
            return (client, topicId, ws);
        }

        /// <summary>
        /// Validate that topic exists within the workspace and create it if it doesn't
        /// </summary>
        /// <param name="workspace">Workspace</param>
        /// <param name="topicIdOrName">Topic Id or Topic Name</param>
        /// <returns>Topic Id</returns>
        private async Task<string> ValidateTopicExistence(Workspace workspace, string topicIdOrName)
        {
            this.logger.LogTrace("Checking if topic {0} is already created.", topicIdOrName);
            var topics = await this.GetTopics(workspace, true).ConfigureAwait(false);
            var existingTopic = topics.FirstOrDefault(y => y.Id.Equals(topicIdOrName, StringComparison.InvariantCulture)) ?? topics.FirstOrDefault(y=> y.Name.Equals(topicIdOrName, StringComparison.InvariantCulture)); // id prio
            var topicName = existingTopic?.Name;
            if (topicName == null)
            {
                if (topicIdOrName.StartsWith(workspace.WorkspaceId, StringComparison.InvariantCultureIgnoreCase))
                {
                    var before = topicIdOrName;
                    topicName = new Regex($"^{workspace.WorkspaceId}\\-", RegexOptions.IgnoreCase).Replace(topicIdOrName, "");
                    if (topicName == before) throw new ArgumentException($"The specified topic id {topicIdOrName} is malformed.");
                    if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException($"The specified topic id {topicIdOrName} is missing topic name part.");
                }
                else topicName = topicIdOrName;
            }
            
            if (existingTopic == null)
            {
                if (!this.autoCreateTopics)
                {
                    throw new InvalidConfigurationException($"Topic {topicIdOrName} does not exist and configuration is set to not automatically create.");
                }
                this.logger.LogInformation("Topic {0} is not yet created, creating in workspace {1} due to active settings.", topicName, workspace.WorkspaceId);
                existingTopic = await CreateTopic(workspace, topicName).ConfigureAwait(false);
                this.logger.LogTrace("Created topic {0}.", topicName);
            }
            else
            {
                this.logger.LogTrace("Topic {0} is already created.", topicName);
            }
            
            while (existingTopic.Status == TopicStatus.Creating)
            {
                this.logger.LogInformation("Topic {0} is still creating.", topicName);
                await Task.Delay(1000).ConfigureAwait(false);
                existingTopic = await this.GetTopic(workspace, topicName, false).ConfigureAwait(false);
                if (existingTopic.Status == TopicStatus.Ready) this.logger.LogInformation("Topic {0} created.", topicName); // will break out by itself
            }

            switch (existingTopic.Status)
            {
                case TopicStatus.Deleting:
                    throw new InvalidConfigurationException("Topic {0} is being deleted, not able to use.");
                case TopicStatus.Ready:
                    this.logger.LogDebug("Topic {0} is available for streaming.", topicName);
                    break;
                default:
                    this.logger.LogWarning("Topic {0} is in state {1}, but expected {2}.", topicName, existingTopic.Status, TopicStatus.Ready);
                    break;
            }

            return existingTopic.Id;
        }

        private void ValidateAgainstConfiguredWorkspace(string workspaceId)
        {
            if (string.IsNullOrWhiteSpace(this.workspaceId)) return;
            if (string.Equals(this.workspaceId, workspaceId)) return;
            throw new InvalidConfigurationException(
                $"The client is configured to use '{this.workspaceId}' but topic or token points to '{workspaceId}'.");
        }

        private async Task<Workspace> GetWorkspaceFromConfiguration(string topicIdOrName)
        {
            var workspaces = await this.GetWorkspaces().ConfigureAwait(false);
            
            // Assume it is an ID
            if (topicToWorkspaceDict.TryGetValue(topicIdOrName, out var ws))
            {
                this.logger.LogTrace("Retrieving workspace for topic {0} from cache", topicIdOrName);
                ValidateAgainstConfiguredWorkspace(ws.WorkspaceId);
                return ws;
            }

            this.logger.LogTrace("Checking if workspace matching topic {0} exists", topicIdOrName);
            var matchingWorkspace = workspaces.OrderBy(y => y.WorkspaceId.Length).FirstOrDefault(y => topicIdOrName.StartsWith(y.WorkspaceId + '-'));
            if (matchingWorkspace != null)
            {
                this.logger.LogTrace("Found workspace using topic id where topic {0} can be present, called {1}.", topicIdOrName, matchingWorkspace.Name);
                ValidateAgainstConfiguredWorkspace(matchingWorkspace.WorkspaceId);
                return topicToWorkspaceDict.GetOrAdd(topicIdOrName, matchingWorkspace);
            }
            
            // If a single workspace is available, then lets use that
            if (workspaces.Count == 1)
            {
                matchingWorkspace = workspaces.First();
                ValidateAgainstConfiguredWorkspace(matchingWorkspace.WorkspaceId);
                this.logger.LogTrace("Found workspace using token where topic {0} can be present, called {1}.", topicIdOrName, matchingWorkspace.Name);
                return topicToWorkspaceDict.GetOrAdd(topicIdOrName, matchingWorkspace);
            }
            
            // Assume it is a name, in which case the workspace comes from environment variables or token
            // Environment variable check
            var envWs = this.workspaceId;
            if (!string.IsNullOrWhiteSpace(envWs))
            {
                matchingWorkspace = workspaces.FirstOrDefault(y => y.WorkspaceId == envWs);
                if (matchingWorkspace != null)
                {
                    this.logger.LogTrace("Found workspace using environment variable where topic {0} can be present, called {1}.", topicIdOrName, matchingWorkspace.Name);
                    return topicToWorkspaceDict.GetOrAdd(topicIdOrName, matchingWorkspace);
                }
                
                var almostWorkspaceByEnv = workspaces.FirstOrDefault(y => y.WorkspaceId.GetLevenshteinDistance(envWs) < 2);
                if (almostWorkspaceByEnv != null)
                {
                    throw new InvalidConfigurationException($"The workspace id specified ({envWs}) provided (env var or constructor) is similar to {almostWorkspaceByEnv.WorkspaceId}, but not exact. Typo or token without access to it?");
                }
                throw new InvalidConfigurationException($"The workspace id specified ({envWs}) provided (env var or constructor) is not available. Typo or token without access to it?");
            }
            
            // Not able to find workspace, lets figure out what kind of exception we throw back. These exceptions are about topic id/name being similar/invalid, other methods have their exception above
            var exactWorkspace = workspaces.FirstOrDefault(y => y.WorkspaceId == topicIdOrName);
            if (exactWorkspace != null)
            {
                throw new InvalidConfigurationException($"The specified topic id {topicIdOrName} is missing the topic name. Found workspace {exactWorkspace.WorkspaceId}.");
            }
            
            var almostWorkspace = workspaces.FirstOrDefault(y => y.WorkspaceId.GetLevenshteinDistance(topicIdOrName) < 2);
            if (almostWorkspace != null)
            {
                throw new InvalidConfigurationException($"The specified topic id {topicIdOrName} is missing the topic name. Workspace not found either, did you mean {almostWorkspace.WorkspaceId}?");
            }
            
            List<string> possibleWorkspaces;
            if (QuixUtils.TryGetWorkspaceIdPrefix(topicIdOrName, out var wsId))
            {
                possibleWorkspaces = wsId.GetLevenshteinDistance(workspaces.Select(y => y.WorkspaceId), 3).ToList();
            }
            else
            {
                var lastDash = topicIdOrName.LastIndexOf('-');
                var possibleTopicName = topicIdOrName.Substring(lastDash > -1 ? lastDash : topicIdOrName.Length);
                var possibleWsId = topicIdOrName.Substring(0, topicIdOrName.Length - possibleTopicName.Length);
                possibleWorkspaces = possibleWsId.GetLevenshteinDistance(workspaces.Select(y => y.WorkspaceId), 5).ToList();
            }

            if (possibleWorkspaces.Count > 0)
            {
                throw new InvalidConfigurationException($"Could not find the workspace for topic {topicIdOrName}. Verify that the workspace exists, token provided has access to it or use {nameof(KafkaStreamingClient)} instead of {nameof(QuixStreamingClient)}. Similar workspaces found: {string.Join(", ", possibleWorkspaces)}");

            }

            throw new InvalidConfigurationException($"No workspace could be identified for topic {topicIdOrName}. Verify the topic id or name is correct. If name is provided then {WorkspaceIdEnvironmentKey} environment variable, constructor variable or token with access to 1 workspace only must be provided. Current token has access to {workspaces.Count} workspaces and env var is unset. Alternatively use {nameof(KafkaStreamingClient)} instead of {nameof(QuixStreamingClient)}.");
        }

        private async Task<KafkaStreamingClient> CreateStreamingClientForWorkspace(Workspace ws)
        {
            if (this.wsToStreamingClientDict.TryGetValue(ws.WorkspaceId, out var sc))
            {
                return sc;
            }
            
            if (ws.Broker == null)
            {
                throw new InvalidConfigurationException("Unable to configure broker due missing credentials.");
            }
            
            if (ws.Status != WorkspaceStatus.Ready)
            {
                if (ws.Status == WorkspaceStatus.Deleting)
                {
                    throw new InvalidConfigurationException($"Workspace {ws.WorkspaceId} is being deleted.");
                }
                logger.LogWarning("Workspace {0} is in state {1} instead of {2}.", ws.WorkspaceId, ws.Status, WorkspaceStatus.Ready);
            }
            
            var securityOptions = new SecurityOptions();

            if (ws.Broker.SecurityMode == BrokerSecurityMode.Ssl || ws.Broker.SecurityMode == BrokerSecurityMode.SaslSsl)
            {
                var librdKafkaConfig = await GetWorkspaceLibrdKafkaConfig(ws.WorkspaceId);
                securityOptions.UseSsl = true;
                if (librdKafkaConfig.TryGetValue("ssl.ca.cert", out var sslcacert))
                {
                    byte[] data = Convert.FromBase64String(sslcacert);
                    string decodedString = System.Text.Encoding.UTF8.GetString(data);
                    securityOptions.SslCaContent = decodedString;
                }
                if (!brokerProperties.ContainsKey("ssl.endpoint.identification.algorithm"))
                {
                    brokerProperties["ssl.endpoint.identification.algorithm"] = "none"; // default back to None
                }
            }
            else
            {
                securityOptions.UseSsl = false;
            }

            if (ws.Broker.SecurityMode == BrokerSecurityMode.Sasl || ws.Broker.SecurityMode == BrokerSecurityMode.SaslSsl)
            {
                if (!Enum.TryParse(ws.Broker.SaslMechanism.ToString(), true, out SaslMechanism parsed))
                {
                    throw new ArgumentOutOfRangeException(nameof(ws.Broker.SaslMechanism), "Unsupported sasl mechanism " + ws.Broker.SaslMechanism);
                }

                securityOptions.UseSasl = true;
                securityOptions.SaslMechanism = parsed;
                securityOptions.Username = ws.Broker.Username;
                securityOptions.Password = ws.Broker.Password;
            } 
            else
            {
                securityOptions.UseSasl = false;
            }

            try
            {
                if (ws.BrokerSettings?.BrokerType == WorkspaceBrokerType.ConfluentCloud &&
                    !string.IsNullOrWhiteSpace(ws.BrokerSettings.ConfluentCloudSettings.ClientID))
                {
                    brokerProperties["client.id"] = ws.BrokerSettings.ConfluentCloudSettings.ClientID;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogTrace(ex, "Failed to set Confluent client id");
            }

            var client = new KafkaStreamingClient(ws.Broker.Address, securityOptions, brokerProperties, debug);
            return wsToStreamingClientDict.GetOrAdd(ws.WorkspaceId, client);
        }
        
        private async Task<List<Workspace>> GetWorkspaces()
        {
            var result = await GetModelFromApi<List<Workspace>>("workspaces", true, true).ConfigureAwait(false);
            if (result.Count == 0) throw new InvalidTokenException("Could not find any workspaces for this token.");
            return result;
        }
        
        private async Task<IDictionary<string, string>> GetWorkspaceLibrdKafkaConfig(string workspaceId)
        {
            var result = await GetModelFromApi<IDictionary<string, string>>($"workspaces/{workspaceId}/broker/librdkafka", true, true).ConfigureAwait(false);
            return result;
        }
        
        private Task<List<Topic>> GetTopics(Workspace workspace, bool useCache)
        {
            return GetModelFromApi<List<Topic>>($"{workspace.WorkspaceId}/topics", true, useCache);
        }
        
        private Task<Topic> GetTopic(Workspace workspace, string topicName, bool useCache)
        {
            return GetModelFromApi<Topic>($"{workspace.WorkspaceId}/topics/{topicName}", true, useCache);
        }
        
        private async Task<Topic> CreateTopic(Workspace workspace, string topicName)
        {
            var uri = new Uri(ApiUrl, $"{workspace.WorkspaceId}/topics");
            
            
            HttpResponseMessage response;
            try
            {
                response = await SendRequestToApi(HttpMethod.Post, uri, new TopicCreateRequest() {Name = topicName}).ConfigureAwait(false);
            }
            catch (QuixApiException ex) when (ex.Message.Contains("already exists"))
            {
                this.logger.LogDebug("Another process created topic {0}, retrieving it from workspace.", topicName);
                return await this.GetTopic(workspace, topicName, false).ConfigureAwait(false);
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                var converted = JsonConvert.DeserializeObject<Topic>(content, jsonSerializerSettings);
                return converted;
            }
            catch
            {
                throw new QuixApiException(uri.AbsolutePath, $"Failed to serialize response while creating topic {topicName}.", String.Empty, HttpStatusCode.OK);
            }
        }

        private async Task<HttpResponseMessage> SendRequestToApi(HttpMethod method, Uri uri, object bodyModel = null)
        {
            var httpRequest = new HttpRequestMessage(method, uri);
            if (bodyModel != null)
            {
                httpRequest.Content = new StringContent(JsonConvert.SerializeObject(bodyModel), Encoding.UTF8, "application/json");
            }
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Headers.Add("X-Version", "2.0");
            try
            {
                var response =
                    await this.httpClient.Value.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.logger.LogTrace("Request {0} had {1} response:{2}{3}", uri.AbsoluteUri, response.StatusCode,
                        Environment.NewLine, responseContent);
                    string msg;
                    var cid = String.Empty;
                    try
                    {
                        var error = JsonConvert.DeserializeObject<PortalException>(responseContent, jsonSerializerSettings);
                        msg = error.Message;
                        cid = error.CorrelationId;
                    }
                    catch
                    {
                        msg = responseContent;
                    }

                    throw new QuixApiException(uri.AbsoluteUri, msg, cid, response.StatusCode);
                }

                return response;
            }
            catch (HttpRequestException ex) when (ex.InnerException?.Message.Contains("Could not create SSL/TLS secure channel") == true &&
                                                  this.handler?.SslProtocols == SslProtocols.None)
            {
                // This is here for rare compatibility issue when for some reason the library is unable to pick an SSL protocol based on OS configuration
                this.logger.LogDebug("Could not create SSL/TLS secure channel for request to {0}, Forcing TLS 1.2", uri.AbsoluteUri);
                this.handler = new HttpClientHandler()
                {
                    SslProtocols = SslProtocols.Tls12
                };
                var prevClient = this.httpClient;
                this.httpClient = new Lazy<HttpClient>(() => new HttpClient(this.handler));
                prevClient.Value.Dispose();
                return await SendRequestToApi(method, uri, bodyModel).ConfigureAwait(false);
            }
        }

        private async Task<T> GetModelFromApi<T>(string path, bool saveToCache, bool readFromCache) where T : class
        {
            var uri = new Uri(ApiUrl, path);
            var cacheKey = $"API:GET:{uri.AbsolutePath}";

            var response = await SendRequestToApi(HttpMethod.Get, uri).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            this.logger.LogTrace("Request {0} had {1} response:{2}{3}", uri.AbsolutePath, response.StatusCode, Environment.NewLine, responseContent);
            try
            {
                var converted = JsonConvert.DeserializeObject<T>(responseContent, jsonSerializerSettings);

                return converted;
            }
            catch
            {
                throw new QuixApiException(uri.AbsolutePath, "Failed to serialize response.", String.Empty, HttpStatusCode.OK);
            }
        }
        
        private class SafeEnumConverter: StringEnumConverter
        {
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                try
                {
                    if (reader.TokenType == JsonToken.Null) return null;
                    var enumText = reader.Value.ToString();
                    if (string.IsNullOrEmpty(enumText)) return null;

                    return base.ReadJson(reader, objectType, existingValue, serializer);
                }
                catch
                {
                    // Return the default 'Unknown' value when an exception occurs
                    if (Enum.GetNames(objectType).Contains("Unknown", StringComparer.InvariantCultureIgnoreCase))
                    {
                        return Enum.Parse(objectType, "Unknown", true);
                    }

                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Quix Streaming Client extensions
    /// </summary>
    public static class QuixStreamingClientExtensions
    {
        /// <summary>
        /// Gets a topic consumer capable of subscribing to receive streams in the specified topic
        /// </summary>
        /// <param name="client">Quix Streaming client instance</param>
        /// <param name="topicId">Id of the topic. Should look like: myvery-myworkspace-mytopic</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="commitMode">The commit strategy to use for this topic</param>
        /// <returns>Instance of <see cref="ITopicConsumer"/></returns>
        public static ITopicConsumer GetTopicConsumer(this IQuixStreamingClient client, string topicId, string consumerGroup = null, CommitMode commitMode = CommitMode.Automatic, AutoOffsetReset autoOffset =  AutoOffsetReset.Latest)
        {
            switch (commitMode)
            {
                case CommitMode.Automatic:
                    return client.GetTopicConsumer(topicId, consumerGroup, null, autoOffset);
                case CommitMode.Manual:
                    var commitOptions = new CommitOptions()
                    {
                        AutoCommitEnabled = false
                    };
                    return client.GetTopicConsumer(topicId, consumerGroup, commitOptions, autoOffset);
                default:
                    throw new ArgumentOutOfRangeException(nameof(commitMode), commitMode, null);
            }
        }
    }
}
