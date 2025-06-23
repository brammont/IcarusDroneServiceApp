using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IcarusDroneServiceApp.Models;

namespace IcarusDroneServiceApp
{
    /// <summary>
    /// Main window for the Icarus Drone Service application.
    /// Manages job queues, job creation, processing, updating, and finished job records.
    /// </summary>
    public partial class IcarusDrone : Window
    {
        private readonly Queue<Drone> _regularService = new();
        private readonly Queue<Drone> _expressService = new();
        private readonly List<Drone> _finished = new();

        public IcarusDrone()
        {
            InitializeComponent();
            Trace.TraceInformation("[Startup] Window initialised");
            RefreshQueues();
            RefreshFinished();
        }

        private void TabQueues_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            lvRegular.SelectedItem = null;
            lvExpress.SelectedItem = null;
            SetCostEnabled(true);
            RefreshQueues();
        }

        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtCost.Text, out double raw) || raw <= 0)
            {
                MessageBox.Show("Enter a positive cost (max two decimals).", "Invalid Cost", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int tag = numTag.Value ?? 100;
            IncrementTag();

            if (IsTagDuplicate(tag))
            {
                MessageBox.Show($"Service Tag #{tag} already exists.", "Duplicate Tag", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string prio = GetServicePriority();
            double cost = prio == "Express" ? Math.Round(raw * 1.15, 2) : Math.Round(raw, 2);

            var job = new Drone
            {
                ServiceTag = tag,
                ClientName = txtClient.Text,
                DroneModel = txtModel.Text,
                ServiceProblem = txtProblem.Text,
                Cost = cost,
                Priority = prio
            };

            (prio == "Express" ? _expressService : _regularService).Enqueue(job);

            RefreshQueues();
            RefreshFinished();
            ClearForm();
            txtStatus.Text = $"Job #{tag} enqueued as {prio}.";
        }

        private string GetServicePriority() =>
            rbExpress.IsChecked == true ? "Express" : "Regular";

        private void IncrementTag()
        {
            numTag.Value = (numTag.Value ?? 100) + 10 > 900 ? 100 : (numTag.Value ?? 100) + 10;
        }

        private bool IsTagDuplicate(int tag) =>
            _regularService.Any(d => d.ServiceTag == tag) ||
            _expressService.Any(d => d.ServiceTag == tag) ||
            _finished.Any(d => d.ServiceTag == tag);

        private void ProcessReg_Click(object sender, RoutedEventArgs e)
        {
            if (_regularService.Count == 0)
            {
                MessageBox.Show("No Regular jobs in the queue.", "Queue Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var done = _regularService.Dequeue();
            _finished.Add(done);
            PostProcess($"Processed Regular job #{done.ServiceTag}.");
        }

        private void ProcessExpr_Click(object sender, RoutedEventArgs e)
        {
            if (_expressService.Count == 0)
            {
                MessageBox.Show("No Express jobs in the queue.", "Queue Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var done = _expressService.Dequeue();
            _finished.Add(done);
            PostProcess($"Processed Express job #{done.ServiceTag}.");
        }

        private void PostProcess(string msg)
        {
            lvRegular.SelectedItem = lvExpress.SelectedItem = null;
            SetCostEnabled(true);
            ClearForm();
            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = msg;
        }

        private void RefreshQueues()
        {
            // Save current selected tags
            int? regTag = lvRegular.SelectedItem is Drone reg ? reg.ServiceTag : (int?)null;
            int? expTag = lvExpress.SelectedItem is Drone exp ? exp.ServiceTag : (int?)null;

            // Refresh ListViews
            lvRegular.ItemsSource = null;
            lvExpress.ItemsSource = null;
            lvRegular.ItemsSource = _regularService.ToList();
            lvExpress.ItemsSource = _expressService.ToList();

            // Restore selection in Regular
            if (regTag.HasValue)
            {
                var regMatch = lvRegular.Items.OfType<Drone>().FirstOrDefault(x => x.ServiceTag == regTag.Value);
                if (regMatch != null)
                    lvRegular.SelectedItem = regMatch;
            }

            // Restore selection in Express
            if (expTag.HasValue)
            {
                var expMatch = lvExpress.Items.OfType<Drone>().FirstOrDefault(x => x.ServiceTag == expTag.Value);
                if (expMatch != null)
                    lvExpress.SelectedItem = expMatch;
            }

            // Update button states
            btnProcessReg.IsEnabled = tabQueues.SelectedIndex == 0 && _regularService.Count > 0;
            btnProcessExpr.IsEnabled = tabQueues.SelectedIndex == 1 && _expressService.Count > 0;
            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }

        private void RefreshFinished()
        {
            lvFinished.ItemsSource = null;
            lvFinished.ItemsSource = _finished.ToList();
        }

        private void LvRegular_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (lvRegular.SelectedItem is Drone d)
            {
                lvExpress.SelectedItem = null;
                PopulateForm(d, "Regular");
                SetCostEnabled(false);
                btnUpdate.IsEnabled = true;
            }
            else if (lvExpress.SelectedItem == null)
                SetCostEnabled(true);

            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }

        private void LvExpress_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (lvExpress.SelectedItem is Drone d)
            {
                lvRegular.SelectedItem = null;
                PopulateForm(d, "Express");
                SetCostEnabled(false);
                btnUpdate.IsEnabled = true;
            }
            else if (lvRegular.SelectedItem == null)
                SetCostEnabled(true);

            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }

        private void PopulateForm(Drone d, string pr)
        {
            txtClient.Text = d.ClientName;
            txtModel.Text = d.DroneModel;
            txtProblem.Text = d.ServiceProblem;
            txtCost.Text = d.Cost.ToString("F2");
            numTag.Value = d.ServiceTag;
            rbRegular.IsChecked = pr == "Regular";
            rbExpress.IsChecked = pr == "Express";
        }

        private void SetCostEnabled(bool enable) => txtCost.IsEnabled = enable;

        private void UpdateSelectedJob_Click(object sender, RoutedEventArgs e)
        {
            ListView active = tabQueues.SelectedIndex == 0 ? lvRegular : lvExpress;
            if (active.SelectedItem is not Drone sel) return;

            sel.ClientName = txtClient.Text;
            sel.DroneModel = txtModel.Text;
            sel.ServiceProblem = txtProblem.Text;

            active.SelectedItem = null;
            SetCostEnabled(true);
            btnUpdate.IsEnabled = false;

            RefreshQueues();
            txtStatus.Text = $"Updated job #{sel.ServiceTag}.";
            ClearForm();
        }

        private void RemoveFinished(object sender, MouseButtonEventArgs e)
        {
            int idx = lvFinished.SelectedIndex;
            if (idx < 0) return;

            if (MessageBox.Show("Remove this finished job?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            _finished.RemoveAt(idx);
            RefreshFinished();
            txtStatus.Text = "Finished job removed.";
        }

        private void TxtCost_PreviewTextInput(object? sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;
            string next = tb.Text.Insert(tb.SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(next, @"^\d*\.?\d{0,2}$");
        }

        private void ClearForm()
        {
            txtClient.Clear();
            txtModel.Clear();
            txtProblem.Clear();
            txtCost.Clear();
            rbRegular.IsChecked = true;
        }
    }
}
