// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V1.Wcf.Client
{
    using System;
    using System.Fabric;
    using System.Globalization;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Communication;
    using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;
    using Microsoft.ServiceFabric.Services.Wcf;

    internal class WcfServiceRemotingClient : IServiceRemotingClient
    {
        public WcfServiceRemotingClient(WcfCommunicationClient<IServiceRemotingContract> wcfClient)
        {
            this.WcfClient = wcfClient;
        }

        public WcfCommunicationClient<IServiceRemotingContract> WcfClient { get; }

        /// <summary>
        ///     Gets or Sets the Resolved service partition which was used when this client was created.
        /// </summary>
        /// <value><see cref="System.Fabric.ResolvedServicePartition" /> object</value>
        public ResolvedServicePartition ResolvedServicePartition
        {
            get { return this.WcfClient.ResolvedServicePartition; }

            set { this.WcfClient.ResolvedServicePartition = value; }
        }

        /// <summary>
        ///     Gets or Sets the name of the listener in the replica or instance to which the client is
        ///     connected to.
        /// </summary>
        public string ListenerName
        {
            get { return this.WcfClient.ListenerName; }

            set { this.WcfClient.ListenerName = value; }
        }

        /// <summary>
        ///     Gets or Sets the service endpoint to which the client is connected to.
        /// </summary>
        /// <value>
        ///     <see cref="System.Fabric.ResolvedServiceEndpoint" />
        /// </value>
        public ResolvedServiceEndpoint Endpoint
        {
            get { return this.WcfClient.Endpoint; }

            set { this.WcfClient.Endpoint = value; }
        }

        public async Task<byte[]> RequestResponseAsync(ServiceRemotingMessageHeaders headers, byte[] requestBody)
        {
            try
            {
                return await this.WcfClient.Channel.RequestResponseAsync(headers, requestBody).ContinueWith(
                    t => t.GetAwaiter().GetResult(),
                    TaskScheduler.Default);

                // the code above (TaskScheduler.Default) for dispatches the responses on different thread
                // so that if the user code blocks, we do not stop the response receive pump in WCF
            }
            catch (FaultException<RemoteExceptionInformation> faultException)
            {
                if (RemoteExceptionInformation.ToException(faultException.Detail, out var remoteException))
                {
                    throw new AggregateException(remoteException);
                }

                throw new ServiceException(
                    remoteException.GetType().FullName,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        SR.ErrorDeserializationFailure,
                        remoteException.ToString()));
            }
        }

        public void SendOneWay(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            this.WcfClient.Channel.OneWayMessage(messageHeaders, requestBody);
        }
    }
}