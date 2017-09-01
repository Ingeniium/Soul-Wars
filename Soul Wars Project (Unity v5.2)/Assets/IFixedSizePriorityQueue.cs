using System;
using System.Collections.Generic;
using System.Text;
/*I WAS IN NO WAY INVOLVED IN THE CREATION OF THIS CODE.
 ALL CREDIT GOES TO BlueRaja
 http://www.blueraja.com/blog/356
 https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
 */
namespace Priority_Queue
{
    /// <summary>
    /// A helper-interface only needed to make writing unit tests a bit easier (hence the 'internal' access modifier)
    /// </summary>
    internal interface IFixedSizePriorityQueue<TItem, in TPriority> : IPriorityQueue<TItem, TPriority>
        where TPriority : IComparable<TPriority>
    {
        /// <summary>
        /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
        /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
        /// </summary>
        void Resize(int maxNodes);

        /// <summary>
        /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
        /// attempting to enqueue another item will cause undefined behavior.
        /// </summary>
        int MaxSize { get; }
    }
}
