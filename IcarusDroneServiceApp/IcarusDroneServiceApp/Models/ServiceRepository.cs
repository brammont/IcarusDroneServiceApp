using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IcarusDroneServiceApp.Models
{
    /// <summary>
    /// Holds the three collections for drone jobs.
    /// </summary>
    public class ServiceRepository
    {
        /// <summary>Queue of regular‐priority Drone jobs.</summary>
        public Queue<Drone> RegularService { get; } = new Queue<Drone>();

        /// <summary>Queue of express‐priority Drone jobs.</summary>
        public Queue<Drone> ExpressService { get; } = new Queue<Drone>();

        /// <summary>List of completed Drone jobs.</summary>
        public List<Drone> FinishedList { get; } = new List<Drone>();
    }
}
