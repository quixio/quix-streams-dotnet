using System;

namespace QuixStreams.Streaming.Configuration
{
    /// <summary>
    /// A class representing security options for configuring SSL encryption with SASL authentication in Kafka.
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>
        /// The SASL mechanism to use.
        /// </summary>
        public SaslMechanism? SaslMechanism { get; set; }
        
        /// <summary>
        /// The username for SASL authentication.
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// The password for SASL authentication
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The path to the folder or file containing the certificate authority certificate(s) to validate the ssl connection.
        /// </summary>
        [Obsolete("Use SslCaContent instead")]
        public string SslCertificates { get; set; }
        
        /// <summary>
        /// The content of the SSL certificate authority to use.
        /// This is the same as ssl.ca.pem in librdkafka.
        /// If specified, <see cref="SslCertificates"/> is ignored
        /// </summary>
        public string SslCaContent { get; set; }

        /// <summary>
        /// Use SSL
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Use authentication
        /// </summary>
        public bool UseSasl { get; set; }

        /// <summary>
        /// For deserialization when binding to Configurations like Appsettings
        /// </summary>
        public SecurityOptions()
        {
        }
    }
}
