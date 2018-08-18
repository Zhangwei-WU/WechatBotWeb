using System;
using System.Collections.Generic;
using System.Text;

namespace WechatBotWeb.Security
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// resource in resource pool 
    /// </summary>
    /// <typeparam name="T">resource type</typeparam>
    public sealed class Resource<T> : IDisposable where T : new()
    {
        /// <summary>
        /// current resource pool
        /// </summary>
        private ResourcePool<T> currentPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="Resource{T}"/> class
        /// </summary>
        /// <param name="data">resource data</param>
        /// <param name="pool">current resource pool</param>
        internal Resource(T data, ResourcePool<T> pool)
        {
            this.Data = data;
            this.currentPool = pool;
        }

        /// <summary>
        /// Gets resource data
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// dispose
        /// when dispose put the data back to pool
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.currentPool.Put(this.Data);
            }
        }
    }

    /// <summary>
    /// allocation pool, reuse heavy objects
    /// </summary>
    /// <typeparam name="T">object type</typeparam>
    public sealed class ResourcePool<T> where T : new()
    {
        /// <summary>
        /// default ctor
        /// </summary>
        public ResourcePool()
            : this(null)
        {
        }

        /// <summary>
        /// ctor with instance initializer
        /// </summary>
        /// <param name="initializer">instance initializer</param>
        public ResourcePool(Action<T> initializer)
        {
            this.initializer = initializer;
        }

        /// <summary>
        /// initializer to initialize T instance
        /// </summary>
        private Action<T> initializer;

        /// <summary>
        /// internal queue for object storing
        /// </summary>
        private Queue<T> internalQueue = new Queue<T>();

        /// <summary>
        /// lock instance
        /// </summary>
        private object locker = new object();

        /// <summary>
        /// Get an object
        /// </summary>
        /// <returns>object instance</returns>
        public Resource<T> Get()
        {
            T data = default(T);

            lock (this.locker)
            {
                if (this.internalQueue.Count != 0)
                {
                    data = this.internalQueue.Dequeue();
                }
            }

            if (data == null)
            {
                data = new T();
                if (this.initializer != null)
                {
                    this.initializer(data);
                }
            }

            return new Resource<T>(data, this);
        }

        /// <summary>
        /// object back
        /// </summary>
        /// <param name="obj">object instance</param>
        internal void Put(T obj)
        {
            if (obj != null)
            {
                lock (this.locker)
                {
                    if (obj != null)
                    {
                        this.internalQueue.Enqueue(obj);
                    }
                }
            }
        }
    }

}
