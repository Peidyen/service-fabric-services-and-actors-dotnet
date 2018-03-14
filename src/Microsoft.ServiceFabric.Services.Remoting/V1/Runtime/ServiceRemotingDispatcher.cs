﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V1.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Common;
    using System.Globalization;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting.Description;
    using Microsoft.ServiceFabric.Services.Remoting.Diagnostic;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Builder;

    /// <summary>
    ///     Provides an implementation of <see cref="IServiceRemotingMessageHandler" /> that can dispatch
    ///     messages to the service implementing <see cref="IService" /> interface.
    /// </summary>
    public class ServiceRemotingDispatcher : IServiceRemotingMessageHandler, IDisposable
    {
        private readonly IService service;
        private readonly ServiceRemotingCancellationHelper cancellationHelper;
        private readonly Dictionary<int, ServiceMethodDispatcherBase> methodDispatcherMap;
        private readonly ServicePerformanceCounterProvider servicePerformanceCounterProvider;

        /// <summary>
        ///     Instantiates the ServiceRemotingDispatcher that uses the given service context and
        ///     dispatches messages to the given service implementation.
        /// </summary>
        /// <param name="serviceContext">Service context</param>
        /// <param name="service">Service implementation that implements interfaces of type <see cref="IService" /></param>
        public ServiceRemotingDispatcher(ServiceContext serviceContext, IService service)
        {
            serviceContext.ThrowIfNull("serviceContext");

            this.cancellationHelper = new ServiceRemotingCancellationHelper(serviceContext.TraceId);

            this.methodDispatcherMap = new Dictionary<int, ServiceMethodDispatcherBase>();
            this.service = service;

            if (service != null)
            {
                var serviceTypeInformation = ServiceTypeInformation.Get(service.GetType());
                var interfaceDescriptions = new List<ServiceInterfaceDescription>();

                foreach (var interfaceType in serviceTypeInformation.InterfaceTypes)
                {
                    var methodDispatcher = ServiceCodeBuilder.GetOrCreateMethodDispatcher(interfaceType);
                    this.methodDispatcherMap.Add(methodDispatcher.InterfaceId, methodDispatcher);
                    interfaceDescriptions.Add(ServiceInterfaceDescription.Create(interfaceType));
                }

                this.servicePerformanceCounterProvider =
                    new ServicePerformanceCounterProvider(
                        serviceContext.PartitionId,
                        serviceContext.ReplicaOrInstanceId,
                        interfaceDescriptions);
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (this.servicePerformanceCounterProvider != null)
            {
                this.servicePerformanceCounterProvider.Dispose();
            }
        }

        /// <summary>
        ///     Handles a message from the client that requires a response from the service.
        /// </summary>
        /// <param name="requestContext">Request context - contains additional information about the request</param>
        /// <param name="messageHeaders">Request message headers</param>
        /// <param name="requestBody">Request message body</param>
        /// <returns>Response body</returns>
        public virtual async Task<byte[]> RequestResponseAsync(
            IServiceRemotingRequestContext requestContext,
            ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody)
        {
            if (this.IsCancellationRequest(messageHeaders))
            {
                await
                    this.cancellationHelper.CancelRequestAsync(
                        messageHeaders.InterfaceId,
                        messageHeaders.MethodId,
                        messageHeaders.InvocationId);
                return null;
            }

            if (null != this.servicePerformanceCounterProvider.serviceOutstandingRequestsCounterWriter)
            {
                this.servicePerformanceCounterProvider.serviceOutstandingRequestsCounterWriter.UpdateCounterValue(1);
            }

            var requestStopWatch = Stopwatch.StartNew();
            byte[] retval = null;
            try
            {
                retval = await this.cancellationHelper.DispatchRequest(
                    messageHeaders.InterfaceId,
                    messageHeaders.MethodId,
                    messageHeaders.InvocationId,
                    cancellationToken => this.OnDispatch(messageHeaders, requestBody, cancellationToken));
            }
            finally
            {
                if (null != this.servicePerformanceCounterProvider.serviceOutstandingRequestsCounterWriter)
                {
                    this.servicePerformanceCounterProvider.serviceOutstandingRequestsCounterWriter
                        .UpdateCounterValue(-1);
                }

                if (null != this.servicePerformanceCounterProvider.serviceRequestProcessingTimeCounterWriter)
                {
                    this.servicePerformanceCounterProvider.serviceRequestProcessingTimeCounterWriter
                        .UpdateCounterValue(
                            requestStopWatch.ElapsedMilliseconds);
                }
            }

            return retval;
        }

        /// <summary>
        ///     Handles a one way message from the client.
        /// </summary>
        /// <param name="requestContext">Request context - contains additional information about the request</param>
        /// <param name="messageHeaders">Request message headers</param>
        /// <param name="requestBody">Request message body</param>
        public virtual void HandleOneWay(
            IServiceRemotingRequestContext requestContext,
            ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            throw new NotImplementedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    SR.ErrorMethodNotImplemented,
                    this.GetType().Name,
                    "HandleOneWay"));
        }

        internal bool IsCancellationRequest(ServiceRemotingMessageHeaders messageHeaders)
        {
            if (messageHeaders.InvocationId != null &&
                messageHeaders.TryGetHeaderValue(ServiceRemotingMessageHeaders.CancellationHeaderName, out var headerValue))
            {
                return true;
            }

            return false;
        }

        private Task<byte[]> OnDispatch(
            ServiceRemotingMessageHeaders headers, byte[] requestBodyBytes,
            CancellationToken cancellationToken)
        {
            if (!this.methodDispatcherMap.TryGetValue(headers.InterfaceId, out var methodDispatcher))
            {
                throw new NotImplementedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        SR.ErrorInterfaceNotImplemented,
                        headers.InterfaceId,
                        this.service));
            }

            Task<object> dispatchTask = null;
            var stopwatch = Stopwatch.StartNew();
            var requestBody = methodDispatcher.DeserializeRequestMessageBody(requestBodyBytes);

            if (this.servicePerformanceCounterProvider.serviceRequestDeserializationTimeCounterWriter != null)
            {
                this.servicePerformanceCounterProvider.serviceRequestDeserializationTimeCounterWriter.UpdateCounterValue
                (
                    stopwatch.ElapsedMilliseconds);
            }

            stopwatch.Restart();
            try
            {
                dispatchTask = methodDispatcher.DispatchAsync(
                    this.service,
                    headers.MethodId,
                    requestBody,
                    cancellationToken);
            }
            catch (Exception e)
            {
                var info = ExceptionDispatchInfo.Capture(e);
                this.servicePerformanceCounterProvider.OnServiceMethodFinish(
                    headers.InterfaceId,
                    headers.MethodId,
                    stopwatch.Elapsed,
                    e);
                info.Throw();
            }

            return dispatchTask.ContinueWith(
                t =>
                {
                    object responseBody = null;
                    try
                    {
                        responseBody = t.GetAwaiter().GetResult();
                    }
                    catch (Exception e)
                    {
                        var info = ExceptionDispatchInfo.Capture(e);

                        this.servicePerformanceCounterProvider.OnServiceMethodFinish(
                            headers.InterfaceId,
                            headers.MethodId,
                            stopwatch.Elapsed,
                            e);
                        info.Throw();
                    }

                    this.servicePerformanceCounterProvider.OnServiceMethodFinish(
                        headers.InterfaceId,
                        headers.MethodId,
                        stopwatch.Elapsed);

                    stopwatch.Restart();
                    var response = methodDispatcher.SerializeResponseMessageBody(responseBody);
                    if (this.servicePerformanceCounterProvider.serviceResponseSerializationTimeCounterWriter != null)
                    {
                        this.servicePerformanceCounterProvider.serviceResponseSerializationTimeCounterWriter
                            .UpdateCounterValue(stopwatch.ElapsedMilliseconds);
                    }

                    return response;
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}