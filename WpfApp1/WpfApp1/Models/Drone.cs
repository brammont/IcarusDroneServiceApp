using System;
using System.Globalization;

namespace WpfApp1.Models
{
    /// <summary>
    /// Represents a single drone service request.
    /// </summary>
    public class Drone
    {
        private string? _clientName;
        private string? _droneModel;
        private int _serviceTag;
        private string? _serviceProblem;
        private double _serviceCost;
        private string? _servicePriority;

        public string ClientName
        {
            get => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_clientName ?? string.Empty);
            set => _clientName = value?.Trim() ?? string.Empty;
        }

        public string DroneModel
        {
            get => _droneModel ?? string.Empty;
            set => _droneModel = value?.Trim() ?? string.Empty;
        }

        public int ServiceTag
        {
            get => _serviceTag;
            set => _serviceTag = value;
        }

        public string ServiceProblem
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_serviceProblem))
                    return string.Empty;
                var s = _serviceProblem.Trim();
                return char.ToUpper(s[0]) + s.Substring(1).ToLower();
            }
            set => _serviceProblem = value?.Trim() ?? string.Empty;
        }

        public double ServiceCost
        {
            get => _serviceCost;
            set => _serviceCost = Math.Round(value, 2);
        }

        public string ServicePriority
        {
            get => _servicePriority ?? string.Empty;
            set => _servicePriority = value;
        }

        public string Display() =>
            $"Tag: {ServiceTag}, Client: {ClientName}, Model: {DroneModel}, " +
            $"Problem: {ServiceProblem}, Cost: ${ServiceCost:F2}, Priority: {ServicePriority}";
    }
}
