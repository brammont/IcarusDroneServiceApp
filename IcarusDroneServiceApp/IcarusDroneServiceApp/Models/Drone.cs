using System.Globalization;

namespace IcarusDroneServiceApp.Models
{
    public class Drone
    {
        private string clientName = string.Empty;
        private string droneModel = string.Empty;
        private string serviceProblem = string.Empty;
        private double cost;
        private int serviceTag;

        public string ClientName
        {
            get => clientName;
            set => clientName = CultureInfo.CurrentCulture.TextInfo
                                   .ToTitleCase(value.Trim());
        }

        public string DroneModel
        {
            get => droneModel;
            set => droneModel = value.Trim();
        }

        public string ServiceProblem
        {
            get => serviceProblem;
            set
            {
                string s = value.Trim();
                if (s.Length > 0)
                    serviceProblem = char.ToUpper(s[0]) + s.Substring(1).ToLower();
            }
        }

        public double Cost
        {
            get => cost;
            set => cost = value;
        }

        public int ServiceTag
        {
            get => serviceTag;
            set => serviceTag = value;
        }

        public string Display() => $"{ClientName} – ${Cost:F2}";
    }
}
