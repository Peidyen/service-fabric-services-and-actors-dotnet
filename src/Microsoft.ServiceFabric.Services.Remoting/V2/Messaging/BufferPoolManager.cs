﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V2.Messaging
{
    using System;
    using System.Fabric.Common;

    /// <summary>
    ///     You can use the BufferManager class to manage a buffer pool.
    ///     The pool is created when you instantiate this class . Buffer is instantiated when there are no unused buffers in
    ///     the pool.
    ///     Destroyed when the buffer pool is reclaimed by garbage collection.
    ///     Every time you need to use a buffer, you take one from the pool, use it, and return it to the pool when done.
    ///     This process is much faster than creating and destroying a buffer every time you need to use one.
    /// </summary>
    public sealed class BufferPoolManager : IBufferPoolManager
    {
        private const int DefaultSegmentSize = 4 * 1024;

        private const int DefaultBufferLimit = 100;

        //Not using SynchonizedBufferPool as it has shrink buffer logic , which we don't need yet
        private readonly SynchronizedPool<PooledBuffer> bufferPool;

        private readonly Allocator allocator;

        private readonly int limit;

        /// <summary>
        ///     Initializes a new instance of the BufferPoolManager class.
        /// </summary>
        /// <param name="segmentSize"></param>
        /// <param name="bufferLimit"></param>
        public BufferPoolManager(int segmentSize = DefaultSegmentSize, int bufferLimit = DefaultBufferLimit)
        {
            this.limit = bufferLimit;
            this.bufferPool = new SynchronizedPool<PooledBuffer>(this.limit);
            this.allocator = new Allocator(segmentSize);
            AppTrace.TraceSource.WriteInfo(
                "BufferPoolManager",
                "BufferMessageSize {0} ,BufferMacCount {1}",
                segmentSize,
                bufferLimit);
        }

        /// <summary>
        ///     Gets a buffer from the pool.
        ///     if it doesn't find any unused buffer , it instantiate new buffer.
        /// </summary>
        /// <returns></returns>
        public IPooledBuffer TakeBuffer()
        {
            PooledBuffer segment = this.bufferPool.Take();
            if (segment == null)
            {
                ArraySegment<byte> seg1 = this.CreateSegment();
                return new PooledBuffer(this, seg1, 0);
            }

            segment.ResetBuffer();
            return segment;
        }

        /// <summary>
        ///     Returns a buffer to the pool.
        ///     if limit crosses, buffer won't be returned to the Pool.
        ///     It return false , if buffer is not returned.
        /// </summary>
        /// <param name="buffer"></param>
        public bool ReturnBuffer(IPooledBuffer buffer)
        {
            var segment = (PooledBuffer) buffer;
            //Return the buffer
            return this.bufferPool.Return(segment);
        }

        private ArraySegment<byte> CreateSegment()
        {
            ArraySegment<byte> segment = this.allocator.GetSegment();
            return segment;
        }
    }
}