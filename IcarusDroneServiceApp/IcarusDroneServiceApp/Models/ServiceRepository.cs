using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IcarusDroneServiceApp.Models
{
    /// <summary>
    /// Manages the Regular, Express and Finished collections of <see cref="Drone"/> items.
    /// Provides add, process, duplicate-check, removal and retrieval operations,
    /// logging every action via <see cref="Trace"/>.
    /// </summary>
    public class ServiceRepository
    {
        private readonly Queue<Drone> _regularQueue;
        private readonly Queue<Drone> _expressQueue;
        private readonly List<Drone> _finishedList;

        /// <summary>
        /// Initializes empty queues for regular/express service and an empty finished list.
        /// </summary>
        public ServiceRepository()
        {
            _regularQueue = new Queue<Drone>();
            _expressQueue = new Queue<Drone>();
            _finishedList = new List<Drone>();

            Trace.TraceInformation("[ServiceRepository] Initialized repository with empty collections.");
        }

        /// <summary>
        /// Gets a snapshot of all items in the regular service queue.
        /// </summary>
        public IEnumerable<Drone> RegularItems => _regularQueue.ToList();

        /// <summary>
        /// Gets a snapshot of all items in the express service queue.
        /// </summary>
        public IEnumerable<Drone> ExpressItems => _expressQueue.ToList();

        /// <summary>
        /// Gets a snapshot of all items that have been processed.
        /// </summary>
        public IEnumerable<Drone> FinishedItems => _finishedList.ToList();

        /// <summary>
        /// Returns true if the given tag already exists in any queue or finished list.
        /// </summary>
        /// <param name="tag">ServiceTag to check.</param>
        public bool IsTagDuplicate(int tag)
        {
            bool dup = _regularQueue.Any(d => d.ServiceTag == tag)
                    || _expressQueue.Any(d => d.ServiceTag == tag)
                    || _finishedList.Any(d => d.ServiceTag == tag);

            Trace.TraceInformation($"[ServiceRepository] IsTagDuplicate({tag}) => {dup}");
            return dup;
        }

        /// <summary>
        /// Enqueues a new <see cref="Drone"/> into Regular or Express based on priority.
        /// Throws <see cref="InvalidOperationException"/> for duplicate tags.
        /// </summary>
        /// <param name="drone">The drone to add.</param>
        /// <param name="priority">"Regular" or "Express".</param>
        public void AddItem(Drone drone, string priority)
        {
            if (IsTagDuplicate(drone.ServiceTag))
            {
                throw new InvalidOperationException(
                    $"Cannot add duplicate ServiceTag {drone.ServiceTag}");
            }

            if (string.Equals(priority, "Express", StringComparison.OrdinalIgnoreCase))
            {
                _expressQueue.Enqueue(drone);
                Trace.TraceInformation(
                    $"[ServiceRepository] Enqueued Express: {drone.Display()}");
            }
            else
            {
                _regularQueue.Enqueue(drone);
                Trace.TraceInformation(
                    $"[ServiceRepository] Enqueued Regular: {drone.Display()}");
            }
        }

        /// <summary>
        /// Dequeues the next regular‐priority drone, moves it to Finished, and returns it.
        /// Returns null if no item is available.
        /// </summary>
        public Drone? ProcessRegular()
        {
            if (_regularQueue.Count == 0)
            {
                Trace.TraceInformation("[ServiceRepository] ProcessRegular: queue empty");
                return null;
            }

            var drone = _regularQueue.Dequeue();
            _finishedList.Add(drone);
            Trace.TraceInformation(
                $"[ServiceRepository] ProcessRegular => Moved to Finished: {drone.Display()}");
            return drone;
        }

        /// <summary>
        /// Dequeues the next express‐priority drone, moves it to Finished, and returns it.
        /// Returns null if no item is available.
        /// </summary>
        public Drone? ProcessExpress()
        {
            if (_expressQueue.Count == 0)
            {
                Trace.TraceInformation("[ServiceRepository] ProcessExpress: queue empty");
                return null;
            }

            var drone = _expressQueue.Dequeue();
            _finishedList.Add(drone);
            Trace.TraceInformation(
                $"[ServiceRepository] ProcessExpress => Moved to Finished: {drone.Display()}");
            return drone;
        }

        /// <summary>
        /// Removes a finished drone by ServiceTag from the finished list.
        /// Returns true if removal succeeded.
        /// </summary>
        /// <param name="serviceTag">Tag of the drone to remove.</param>
        public bool RemoveFinished(int serviceTag)
        {
            var drone = _finishedList.FirstOrDefault(d => d.ServiceTag == serviceTag);
            if (drone == null)
            {
                Trace.TraceInformation(
                    $"[ServiceRepository] RemoveFinished: tag {serviceTag} not found");
                return false;
            }

            _finishedList.Remove(drone);
            Trace.TraceInformation(
                $"[ServiceRepository] Removed from Finished: {drone.Display()}");
            return true;
        }

        /// <summary>
        /// Clears all queues and the finished list.
        /// </summary>
        public void ClearAll()
        {
            _regularQueue.Clear();
            _expressQueue.Clear();
            _finishedList.Clear();
            Trace.TraceInformation("[ServiceRepository] Cleared all collections.");
        }
    }
}
