using System.Globalization;

namespace IcarusDroneServiceApp.Models
{
    /// <summary>
    /// Represents a drone service job request.
    /// </summary>
    public class Drone
    {
        private string clientName = string.Empty;
        private string droneModel = string.Empty;
        private string serviceProblem = string.Empty;
        private double cost;
        private int serviceTag;
        private string priority = "Regular";

        /// <summary>
        /// Gets or sets the client’s name in Title Case.
        /// </summary>
        public string ClientName
        {
            get => clientName;
            set => clientName = CultureInfo
                .CurrentCulture
                .TextInfo
                .ToTitleCase(value?.Trim() ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets the drone model identifier.
        /// </summary>
        public string DroneModel
        {
            get => droneModel;
            set => droneModel = (value ?? string.Empty).Trim();
        }

        /// <summary>
        /// Gets or sets the problem description in Sentence Case.
        /// </summary>
        public string ServiceProblem
        {
            get => serviceProblem;
            set
            {
                var s = (value ?? string.Empty).Trim();
                if (s.Length > 0)
                    serviceProblem = char.ToUpper(s[0]) + s.Substring(1).ToLower();
            }
        }

        /// <summary>
        /// Gets or sets the service cost. Always stored as a positive double.
        /// </summary>
        public double Cost
        {
            get => cost;
            set => cost = value;
        }

        /// <summary>
        /// Gets or sets the unique service tag (100–900).
        /// </summary>
        public int ServiceTag
        {
            get => serviceTag;
            set => serviceTag = value;
        }

        /// <summary>
        /// Gets or sets the service priority: "Regular" or "Express".
        /// </summary>
        public string Priority
        {
            get => priority;
            set
            {
                var v = (value ?? "Regular").Trim();
                priority = v == "Express" ? "Express" : "Regular";
            }
        }

        /// <summary>
        /// Returns a display string combining tag, priority, client and cost.
        /// </summary>
        /// <returns>Formatted string like "#100 [Express] Acme Corp – $115.00".</returns>
        public string Display()
            => $"#{ServiceTag} [{Priority}] {ClientName} – ${Cost:F2}";
    }
}
