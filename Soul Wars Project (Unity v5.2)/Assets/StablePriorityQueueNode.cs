/*I WAS IN NO WAY INVOLVED IN THE CREATION OF THIS CODE.
 ALL CREDIT GOES TO BlueRaja
 http://www.blueraja.com/blog/356
 https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
 */
namespace Priority_Queue
{
    public class StablePriorityQueueNode : FastPriorityQueueNode
    {
        /// <summary>
        /// Represents the order the node was inserted in
        /// </summary>
        public long InsertionIndex { get; internal set; }
    }
}
