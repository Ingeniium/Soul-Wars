﻿/*I WAS IN NO WAY INVOLVED IN THE CREATION OF THIS CODE.
 ALL CREDIT GOES TO BlueRaja
 http://www.blueraja.com/blog/356
 https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
 */
namespace Priority_Queue
{
    public class FastPriorityQueueNode
    {
        /// <summary>
        /// The Priority to insert this node at.  Must be set BEFORE adding a node to the queue (ideally just once, in the node's constructor).
        /// Should not be manually edited once the node has been enqueued - use queue.UpdatePriority() instead
        /// </summary>
        public float Priority { get; protected internal set; }

        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        public int QueueIndex { get; internal set; }
    }
}
