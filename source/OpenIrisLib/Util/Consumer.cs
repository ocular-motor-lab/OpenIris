//-----------------------------------------------------------------------
// <copyright file="Consumer.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Class to create a background task that consumes items with an associated order number.
    /// This is used to make sure several consumers finish consuming the same item number.
    /// </summary>
    public class Consumer<T>
    {
        private readonly Func<T, bool> consumeItem;
        private BlockingCollection<T>? buffer;
        private long lastItemNumberToConsume;
        private bool started;

        /// <summary>
        /// Initializes an instance of the Consumer class.Need to iniitialize the buffer here just
        /// in case TryAdd is called right after initializing the consumer but before it is started.
        /// The idea is that start means start consuming. But items can be added to the buffer as
        /// soon as possible. This will be more robust to errors.
        /// </summary>
        /// <param name="consumeItemFunction">Method that consumes the items.</param>
        /// <param name="bufferSize">Size of the item buffer.</param>
        public Consumer(Func<T, bool> consumeItemFunction, int bufferSize = 0)
        {
            buffer = (bufferSize > 0) ? new BlockingCollection<T>(bufferSize) : new BlockingCollection<T>();
            consumeItem = consumeItemFunction;
            lastItemNumberToConsume = long.MaxValue;
        }

        /// <summary>
        /// Gets the number of items that have been attempted to be added to the consumer.
        /// </summary>
        public int TryAddedCount { get; private set; }

        /// <summary>
        /// Gets the number of items that have been added to the consumer.
        /// </summary>
        public int AddedCount { get; private set; }

        /// <summary>
        /// Gets the number of items that were successfully consumed.
        /// </summary>
        public int ConsumedCount { get; private set; }

        /// <summary>
        /// Gets the number of items that have been dropped.
        /// </summary>
        public int DroppedCount { get; private set; }

        /// <summary>
        /// Gets the number of the first item added to the consumer.
        /// </summary>
        public long FirstItemAdded { get; private set; }

        /// <summary>
        /// Gets the number of the last item added to the consumer.
        /// </summary>
        public long LastItemAdded { get; private set; }

        /// <summary>
        /// Starts the consumer.
        /// </summary>
        public async Task Start()
        {
            if (started) throw new InvalidOperationException("Consumer already started.");
            if ( buffer is null) throw new InvalidOperationException("Buffer not ready.");
            started = true;

            try
            {
                using var cancellation = new CancellationTokenSource();
                using var consumerTask = Task.Factory.StartNew(() =>
                  {
                      foreach (T item in buffer.GetConsumingEnumerable(cancellation.Token))
                      {
                          if (consumeItem(item)) ConsumedCount++;
                      }
                  }, TaskCreationOptions.LongRunning);

                await consumerTask;
            }
            finally
            {
                buffer = null;
            }
        }

        /// <summary>
        /// Stops the consumer. Stopping when the last item added until this point is consumed.
        /// </summary>
        public void Stop()
        {
            Stop(LastItemAdded);
        }

        /// <summary>
        /// Stops the consumer, specifying the exact number of frame that should be consumed. Which
        /// may arrive in the future. So, the consumer will wait for it, within a timeout.
        /// </summary>
        /// <param name="lastItemToConsume">
        /// Number of the last frame that should be consumed. This could be some number in the
        /// future. Then the consumer will keep going until a frame with this number or larger is added.
        /// </param>
        /// <param name="timeoutMilliseconds">Number of miliseconds the consumer will wait for the last
        /// item to consume. If it doesn't come it will stop anyway.</param>
        public void Stop(long lastItemToConsume, int timeoutMilliseconds = 5000)
        {
            // Save the number of frame that should be the last to be consumed. For processing videos
            // this will be the last frame of the video or of the range that is being postprocessed.
            // For cameras this will be the current frame at the time the stop recording is received.
            this.lastItemNumberToConsume = lastItemToConsume;

            // If the last item added is the same as the desired last item to consume we can mark the
            // buffer as completed and the consumer loop will stop when it finishes consuming the items.
            if (LastItemAdded >= lastItemToConsume) buffer?.CompleteAdding();

            // If the last item to be consumed has not
            // been added yet we have to wait for it to arrive and tryAdd will mark the buffer
            // as completed. There is the posibility that it never arrives because it was dropped
            // at processing so we have a timeout for that case.
            Task.Delay(timeoutMilliseconds).ContinueWith((t) =>
            {
                // If we have a timeout we will asume that last item to be consumed will never
                // arrive and we mark the buffer as completed and wait for the consumer loop to
                // finish consuming whatever it can.
                buffer?.CompleteAdding();
            });
        }

        /// <summary>
        /// Tries adding an item to the queue be consumed. If the queue is full it returns inmidiately.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemNumber">Order number of the items. Should be always increasing and continuos.</param>
        /// <returns>True if the item could be added.</returns>
        public bool TryAdd(T item, long itemNumber)
        {
            if (buffer is null) throw new InvalidOperationException("Buffer is not initialized yet.");

            TryAddedCount++;

            var itemAdded = false;

            if (itemNumber <= lastItemNumberToConsume && !buffer.IsAddingCompleted)
            {
                itemAdded = buffer.TryAdd(item);
            }

            // If the frame number is equal or past the number of the last frame that should be
            // consumed then complete adding to the buffer.
            if (itemNumber >= lastItemNumberToConsume && !buffer.IsAddingCompleted)
            {
                buffer.CompleteAdding();
            }

            if (itemAdded)
            {
                if (AddedCount == 0) FirstItemAdded = itemNumber;
                LastItemAdded = itemNumber;
                AddedCount++;
            }
            else
            {
                DroppedCount++;
            }

            return itemAdded;
        }
    }
}