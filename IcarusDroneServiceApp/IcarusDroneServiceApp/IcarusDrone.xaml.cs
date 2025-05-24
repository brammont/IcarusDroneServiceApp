using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using IcarusDroneServiceApp.Models;

namespace IcarusDroneServiceApp
{
    public partial class IcarusDrone : Window
    {
        private readonly Queue<Drone> RegularService = new();
        private readonly Queue<Drone> ExpressService = new();
        private readonly List<Drone> FinishedList = new();

        public IcarusDrone()
        {
            InitializeComponent();
            SetupListViewColumns();
            RefreshQueues();
            RefreshFinished();
        }

        /// <summary>
        /// Builds GridView columns programmatically (cannot assign Columns property directly)
        /// </summary>
        private void SetupListViewColumns()
        {
            // Define headers and bindings
            var columns = new (string header, string path)[]
            {
                ("Tag", "ServiceTag"),
                ("Client", "ClientName"),
                ("Model", "DroneModel"),
                ("Problem", "ServiceProblem"),
                ("Cost", "Cost")
            };

            // Regular queue
            var gvReg = new GridView();
            foreach (var (h, p) in columns)
                gvReg.Columns.Add(new GridViewColumn
                {
                    Header = h,
                    DisplayMemberBinding = new Binding(p)
                });
            lvRegular.View = gvReg;

            // Express queue
            var gvExpr = new GridView();
            foreach (var (h, p) in columns)
                gvExpr.Columns.Add(new GridViewColumn
                {
                    Header = h,
                    DisplayMemberBinding = new Binding(p)
                });
            lvExpress.View = gvExpr;
        }

        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            // Validate cost
            if (!double.TryParse(txtCost.Text, out double cost) || cost <= 0)
            {
                MessageBox.Show("Enter a valid positive cost (up to two decimals).",
                                "Invalid Cost", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tag
            int tag = numTag.Value ?? 100;
            IncrementTag();

            // Priority & surcharge
            var priority = GetServicePriority();
            if (priority == "Express") cost *= 1.15;

            // Construct Drone
            var drone = new Drone
            {
                ClientName = txtClient.Text,
                DroneModel = txtModel.Text,
                ServiceProblem = txtProblem.Text,
                Cost = cost,
                ServiceTag = tag
            };

            // Enqueue
            if (priority == "Express") ExpressService.Enqueue(drone);
            else RegularService.Enqueue(drone);

            RefreshQueues();
            ClearForm();
            txtStatus.Text = $"Job #{tag} enqueued to {priority}.";
        }

        private string GetServicePriority()
            => rbExpress.IsChecked == true ? "Express" : "Regular";

        private void IncrementTag()
        {
            int next = (numTag.Value ?? 100) + 10;
            if (next > 900) next = 100;
            numTag.Value = next;
        }

        private void RefreshQueues()
        {
            lvRegular.ItemsSource = RegularService.ToList();
            lvExpress.ItemsSource = ExpressService.ToList();
            btnProcessReg.IsEnabled = RegularService.Count > 0;
            btnProcessExpr.IsEnabled = ExpressService.Count > 0;
        }

        private void ProcessReg_Click(object sender, RoutedEventArgs e)
        {
            if (RegularService.Count == 0) return;
            var d = RegularService.Dequeue();
            FinishedList.Add(d);
            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Regular job #{d.ServiceTag}.";
        }

        private void ProcessExpr_Click(object sender, RoutedEventArgs e)
        {
            if (ExpressService.Count == 0) return;
            var d = ExpressService.Dequeue();
            FinishedList.Add(d);
            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Express job #{d.ServiceTag}.";
        }

        private void RefreshFinished()
        {
            lbFinished.ItemsSource = FinishedList.Select(d => d.Display());
        }

        private void OnRemoveFinished(object sender, MouseButtonEventArgs e)
        {
            if (lbFinished.SelectedIndex < 0) return;
            if (MessageBox.Show("Remove this finished job?", "Confirm",
                   MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            FinishedList.RemoveAt(lbFinished.SelectedIndex);
            RefreshFinished();
            txtStatus.Text = "Finished job removed.";
        }

        private void LvRegular_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (lvRegular.SelectedItem is Drone d)
            {
                txtClient.Text = d.ClientName;
                txtProblem.Text = d.ServiceProblem;
            }
        }

        private void LvExpress_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (lvExpress.SelectedItem is Drone d)
            {
                txtClient.Text = d.ClientName;
                txtProblem.Text = d.ServiceProblem;
            }
        }

        private void TxtCost_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string full = (sender as TextBox).Text
                          .Insert(((TextBox)sender).SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(full, @"^\d*\.?\d{0,2}$");
        }

        private void ClearForm()
        {
            txtClient.Clear();
            txtModel.Clear();
            txtCost.Clear();
            txtProblem.Clear();
            rbRegular.IsChecked = true;
        }

        private void TabQueues_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // no-op; columns already built
        }
    }
}
