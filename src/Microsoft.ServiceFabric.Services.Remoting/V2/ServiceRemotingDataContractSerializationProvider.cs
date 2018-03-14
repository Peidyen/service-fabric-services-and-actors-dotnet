// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V2
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;
    using Microsoft.ServiceFabric.Services.Remoting.V2.Messaging;

    /// <summary>
    ///     <para>
    ///         This is the default implmentation  for <see cref="IServiceRemotingMessageSerializationProvider" />used by
    ///         remoting
    ///         service and client during request/response serialization. It uses DataContractSerializer for serialization of
    ///         request and responses.
    ///     </para>
    /// </summary>
    public class ServiceRemotingDataContractSerializationProvider : IServiceRemotingMessageSerializationProvider
    {
        private readonly IBufferPoolManager bodyBufferPoolManager;

        /// <summary>
        ///     Creates a ServiceRemotingDataContractSerializationProvider with default IBufferPoolManager
        /// </summary>
        public ServiceRemotingDataContractSerializationProvider()
            : this(new BufferPoolManager(Constants.DefaultMessageBufferSize, Constants.DefaultMaxBufferCount))
        {
        }

        /// <summary>
        ///     Creates a ServiceRemotingDataContractSerializationProvider with user specified IBufferPoolManager.
        ///     If the specified buffer pool manager is null, the buffer pooling will be turned off.
        /// </summary>
        /// <param name="bodyBufferPoolManager"></param>
        public ServiceRemotingDataContractSerializationProvider(
            IBufferPoolManager bodyBufferPoolManager)
        {
            this.bodyBufferPoolManager = bodyBufferPoolManager;
        }

        /// <summary>
        ///     Creates IServiceRemotingRequestMessageBodySerializer for a serviceInterface using DataContract implementation
        /// </summary>
        /// <param name="serviceInterfaceType">The remoted service interface.</param>
        /// <param name="methodParameterTypes">The union of parameter types of all of the methods of the specified interface.</param>
        /// <returns>
        ///     An instance of the <see cref="IServiceRemotingRequestMessageBodySerializer" /> that can serialize the service
        ///     remoting request message body to a messaging body for transferring over the transport.
        /// </returns>
        public IServiceRemotingRequestMessageBodySerializer CreateRequestMessageBodySerializer(
            Type serviceInterfaceType,
            IEnumerable<Type> methodParameterTypes)
        {
            var serializer = new DataContractSerializer(
                typeof(ServiceRemotingRequestMessageBody),
                this.GetResponseMessageBodySerializerSettings(serviceInterfaceType, methodParameterTypes));

            if (this.bodyBufferPoolManager != null)
            {
                return new PooledBufferMessageBodySerializer(
                    this,
                    this.bodyBufferPoolManager,
                    serializer);
            }

            return new MemoryStreamMessageBodySerializer(
                this,
                serializer);
        }


        /// <summary>
        ///     Creates IServiceRemotingResponseMessageBodySerializer for a serviceInterface using DataContract implementation
        /// </summary>
        /// <param name="serviceInterfaceType">The remoted service interface.</param>
        /// <param name="methodReturnTypes">The return types of all of the methods of the specified interface.</param>
        /// <returns>
        ///     An instance of the <see cref="IServiceRemotingResponseMessageBodySerializer" /> that can serialize the service
        ///     remoting response message body to a messaging body for transferring over the transport.
        /// </returns>
        public IServiceRemotingResponseMessageBodySerializer CreateResponseMessageBodySerializer(
            Type serviceInterfaceType,
            IEnumerable<Type> methodReturnTypes)
        {
            var serializer = new DataContractSerializer(
                typeof(ServiceRemotingResponseMessageBody),
                this.GetResponseMessageBodySerializerSettings(serviceInterfaceType, methodReturnTypes));

            if (this.bodyBufferPoolManager != null)
            {
                return new PooledBufferMessageBodySerializer(
                    this,
                    this.bodyBufferPoolManager,
                    serializer);
            }

            return new MemoryStreamMessageBodySerializer(
                this,
                serializer);
        }

        /// <summary>
        ///     Creates a MessageFactory for DataContract Remoting Types. This is used to create Remoting Request/Response objects.
        /// </summary>
        /// <returns>
        ///     <see cref="IServiceRemotingMessageBodyFactory" /> that provides an instance of the factory for creating
        ///     remoting request and response message bodies.
        /// </returns>
        public IServiceRemotingMessageBodyFactory CreateMessageBodyFactory()
        {
            return new DataContractRemotingMessageFactory();
        }

        /// <summary>
        ///     Gets the settings used to create DataContractSerializer for serializing and de-serializing request message body.
        /// </summary>
        /// <param name="serviceInterfaceType">The remoted service interface.</param>
        /// <param name="methodReturnTypes">The return types of all of the methods of the specified interface.</param>
        /// <returns><see cref="DataContractSerializerSettings" /> for serializing and de-serializing request message body.</returns>
        protected virtual DataContractSerializerSettings GetRequestMessageBodySerializerSettings(
            Type serviceInterfaceType,
            IEnumerable<Type> methodReturnTypes)
        {
            return new DataContractSerializerSettings
            {
                MaxItemsInObjectGraph = int.MaxValue,
                KnownTypes = methodReturnTypes
            };
        }

        /// <summary>
        ///     Gets the settings used to create DataContractSerializer for serializing and de-serializing response message body.
        /// </summary>
        /// <param name="serviceInterfaceType">The remoted service interface.</param>
        /// <param name="methodParameterTypes">The union of parameter types of all of the methods of the specified interface.</param>
        /// <returns><see cref="DataContractSerializerSettings" /> for serializing and de-serializing response message body.</returns>
        protected virtual DataContractSerializerSettings GetResponseMessageBodySerializerSettings(
            Type serviceInterfaceType,
            IEnumerable<Type> methodParameterTypes)
        {
            return new DataContractSerializerSettings
            {
                MaxItemsInObjectGraph = int.MaxValue,
                KnownTypes = methodParameterTypes
            };
        }

        /// <summary>
        ///     Create the writer to write to the stream. Use this method to customize how the serialized contents are written to
        ///     the stream.
        /// </summary>
        /// <param name="outputStream">The stream on which to write the serialized contents.</param>
        /// <returns>
        ///     An <see cref="System.Xml.XmlDictionaryWriter" /> using which the serializer will write the object on the
        ///     stream.
        /// </returns>
        protected virtual XmlDictionaryWriter CreateXmlDictionaryWriter(Stream outputStream)
        {
            return XmlDictionaryWriter.CreateBinaryWriter(outputStream);
        }

        /// <summary>
        ///     Create the reader to read from the input stream. Use this method to customize how the serialized contents are read
        ///     from the stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the serialized contents.</param>
        /// <returns>
        ///     An <see cref="System.Xml.XmlDictionaryReader" /> using which the serializer will read the object from the
        ///     stream.
        /// </returns>
        protected virtual XmlDictionaryReader CreateXmlDictionaryReader(Stream inputStream)
        {
            return XmlDictionaryReader.CreateBinaryReader(inputStream, XmlDictionaryReaderQuotas.Max);
        }

        /// <summary>
        ///     Default serializdr for service remoting request and response message body that uses the
        ///     memory stream to create outgoing message buffers.
        /// </summary>
        private class MemoryStreamMessageBodySerializer :
            IServiceRemotingRequestMessageBodySerializer,
            IServiceRemotingResponseMessageBodySerializer
        {
            private readonly ServiceRemotingDataContractSerializationProvider serializationProvider;
            private readonly DataContractSerializer serializer;

            public MemoryStreamMessageBodySerializer(
                ServiceRemotingDataContractSerializationProvider serializationProvider,
                DataContractSerializer serializer)
            {
                this.serializationProvider = serializationProvider;
                this.serializer = serializer;
            }

            IMessageBody IServiceRemotingRequestMessageBodySerializer.Serialize(
                IServiceRemotingRequestMessageBody serviceRemotingRequestMessageBody)
            {
                if (serviceRemotingRequestMessageBody == null)
                {
                    return null;
                }

                using (var stream = new MemoryStream())
                {
                    using (var writer = this.CreateXmlDictionaryWriter(stream))
                    {
                        this.serializer.WriteObject(writer, serviceRemotingRequestMessageBody);
                        writer.Flush();
                        return new OutgoingMessageBody(
                            new[]
                                {new ArraySegment<byte>(stream.ToArray())});
                    }
                }
            }

            IServiceRemotingRequestMessageBody IServiceRemotingRequestMessageBodySerializer.Deserialize(
                IMessageBody messageBody)
            {
                if (messageBody == null || messageBody.GetReceivedBuffer() == null || messageBody.GetReceivedBuffer().Length == 0)
                {
                    return null;
                }

                using (var stream = new DisposableStream(messageBody.GetReceivedBuffer()))
                {
                    using (var reader = this.CreateXmlDictionaryReader(stream))
                    {
                        return (ServiceRemotingRequestMessageBody) this.serializer.ReadObject(reader);
                    }
                }
            }

            IMessageBody IServiceRemotingResponseMessageBodySerializer.Serialize(
                IServiceRemotingResponseMessageBody serviceRemotingResponseMessageBody)
            {
                if (serviceRemotingResponseMessageBody == null)
                {
                    return null;
                }

                using (var stream = new MemoryStream())
                {
                    using (var writer = this.CreateXmlDictionaryWriter(stream))
                    {
                        this.serializer.WriteObject(writer, serviceRemotingResponseMessageBody);
                        writer.Flush();
                        return new OutgoingMessageBody(
                            new[]
                                {new ArraySegment<byte>(stream.ToArray())});
                    }
                }
            }

            IServiceRemotingResponseMessageBody IServiceRemotingResponseMessageBodySerializer.Deserialize(
                IMessageBody messageBody)
            {
                if (messageBody?.GetReceivedBuffer() == null || messageBody.GetReceivedBuffer().Length == 0)
                {
                    return null;
                }

                using (var stream = new DisposableStream(messageBody.GetReceivedBuffer()))
                {
                    using (var reader = this.CreateXmlDictionaryReader(stream))
                    {
                        return (ServiceRemotingResponseMessageBody) this.serializer.ReadObject(reader);
                    }
                }
            }

            /// <summary>
            ///     Create the writer to write to the stream. Use this method to customize how the serialized contents are written to
            ///     the stream.
            /// </summary>
            /// <param name="outputStream">The stream on which to write the serialized contents.</param>
            /// <returns>
            ///     An <see cref="System.Xml.XmlDictionaryWriter" /> using which the serializer will write the object on the
            ///     stream.
            /// </returns>
            private XmlDictionaryWriter CreateXmlDictionaryWriter(Stream outputStream)
            {
                return this.serializationProvider.CreateXmlDictionaryWriter(outputStream);
            }

            /// <summary>
            ///     Create the reader to read from the input stream. Use this method to customize how the serialized contents are read
            ///     from the stream.
            /// </summary>
            /// <param name="inputStream">The stream from which to read the serialized contents.</param>
            /// <returns>
            ///     An <see cref="System.Xml.XmlDictionaryReader" /> using which the serializer will read the object from the
            ///     stream.
            /// </returns>
            private XmlDictionaryReader CreateXmlDictionaryReader(Stream inputStream)
            {
                return this.serializationProvider.CreateXmlDictionaryReader(inputStream);
            }
        }

        /// <summary>
        ///     Default serializdr for service remoting request and response message body that uses the
        ///     buffer pool manager to create outgoing message buffers.
        /// </summary>
        private class PooledBufferMessageBodySerializer :
            IServiceRemotingRequestMessageBodySerializer,
            IServiceRemotingResponseMessageBodySerializer
        {
            private readonly ServiceRemotingDataContractSerializationProvider serializationProvider;
            private readonly IBufferPoolManager bufferPoolManager;
            private readonly DataContractSerializer serializer;

            public PooledBufferMessageBodySerializer(
                ServiceRemotingDataContractSerializationProvider serializationProvider,
                IBufferPoolManager bufferPoolManager,
                DataContractSerializer serializer)
            {
                this.serializationProvider = serializationProvider;
                this.bufferPoolManager = bufferPoolManager;
                this.serializer = serializer;
            }

            IMessageBody IServiceRemotingRequestMessageBodySerializer.Serialize(
                IServiceRemotingRequestMessageBody serviceRemotingRequestMessageBody)
            {
                if (serviceRemotingRequestMessageBody == null)
                {
                    return null;
                }

                using (var stream = new SegmentedPoolMemoryStream(this.bufferPoolManager))
                {
                    using (var writer = this.CreateXmlDictionaryWriter(stream))
                    {
                        this.serializer.WriteObject(writer, serviceRemotingRequestMessageBody);
                        writer.Flush();
                        return new OutgoingMessageBody(stream.GetBuffers());
                    }
                }
            }

            IServiceRemotingRequestMessageBody IServiceRemotingRequestMessageBodySerializer.Deserialize(
                IMessageBody messageBody)
            {
                if (messageBody?.GetReceivedBuffer() == null || messageBody.GetReceivedBuffer().Length == 0)
                {
                    return null;
                }

                using (var stream = new DisposableStream(messageBody.GetReceivedBuffer()))
                {
                    using (var reader = this.CreateXmlDictionaryReader(stream))
                    {
                        return (ServiceRemotingRequestMessageBody) this.serializer.ReadObject(reader);
                    }
                }
            }

            IMessageBody IServiceRemotingResponseMessageBodySerializer.Serialize(
                IServiceRemotingResponseMessageBody serviceRemotingResponseMessageBody)
            {
                if (serviceRemotingResponseMessageBody == null)
                {
                    return null;
                }

                using (var stream = new SegmentedPoolMemoryStream(this.bufferPoolManager))
                {
                    using (var writer = this.CreateXmlDictionaryWriter(stream))
                    {
                        this.serializer.WriteObject(writer, serviceRemotingResponseMessageBody);
                        writer.Flush();
                        return new OutgoingMessageBody(stream.GetBuffers());
                    }
                }
            }

            IServiceRemotingResponseMessageBody IServiceRemotingResponseMessageBodySerializer.Deserialize(
                IMessageBody messageBody)
            {
                if (messageBody?.GetReceivedBuffer() == null || messageBody.GetReceivedBuffer().Length == 0)
                {
                    return null;
                }

                using (var stream = new DisposableStream(messageBody.GetReceivedBuffer()))
                {
                    using (var reader = this.CreateXmlDictionaryReader(stream))
                    {
                        return (ServiceRemotingResponseMessageBody) this.serializer.ReadObject(reader);
                    }
                }
            }

            /// <summary>
            ///     Create the writer to write to the stream. Use this method to customize how the serialized contents are written to
            ///     the stream.
            /// </summary>
            /// <param name="outputStream">The stream on which to write the serialized contents.</param>
            /// <returns>
            ///     An <see cref="System.Xml.XmlDictionaryWriter" /> using which the serializer will write the object on the
            ///     stream.
            /// </returns>
            private XmlDictionaryWriter CreateXmlDictionaryWriter(Stream outputStream)
            {
                return this.serializationProvider.CreateXmlDictionaryWriter(outputStream);
            }

            /// <summary>
            ///     Create the reader to read from the input stream. Use this method to customize how the serialized contents are read
            ///     from the stream.
            /// </summary>
            /// <param name="inputStream">The stream from which to read the serialized contents.</param>
            /// <returns>
            ///     An <see cref="System.Xml.XmlDictionaryReader" /> using which the serializer will read the object from the
            ///     stream.
            /// </returns>
            private XmlDictionaryReader CreateXmlDictionaryReader(Stream inputStream)
            {
                return this.serializationProvider.CreateXmlDictionaryReader(inputStream);
            }
        }
    }
}