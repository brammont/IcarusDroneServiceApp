using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using IcarusDroneServiceApp.Models;


namespace IcarusDroneServiceApp
{
    /// <summary>
    /// Main window (code-behind) for the Icarus Drone Service application.
    /// Handles user input, queue management, updates, and finished-job display.
    /// </summary>
    public partial class IcarusDrone : Window
    {
        /*────────────────── Queues & finished list ──────────────────*/
        private readonly Queue<Drone> _regularService = new();
        private readonly Queue<Drone> _expressService = new();
        private readonly List<Drone> _finished = new();
        
        /*────────────────── Constructor ─────────────────────────────*/
        public IcarusDrone()
        {
            InitializeComponent();
            Trace.TraceInformation("[Startup] Window initialised");

            BuildQueueColumns();
            RefreshQueues();
            RefreshFinished();
        }

        /*────────────────── GUI initialisation ──────────────────────*/
        private void BuildQueueColumns()
        {
            (string Head, string Path)[] cols =
            {
                ("Tag","ServiceTag"), ("Client","ClientName"),
                ("Model","DroneModel"), ("Problem","ServiceProblem"), ("Cost","Cost")
            };

            static GridView MakeGrid(IEnumerable<(string H, string P)> spec)
            {
                var gv = new GridView();
                foreach (var (h, p) in spec)
                    gv.Columns.Add(new GridViewColumn
                    {
                        Header = h,
                        DisplayMemberBinding = new Binding(p)
                    });
                return gv;
            }

            lvRegular.View = MakeGrid(cols);
            lvExpress.View = MakeGrid(cols);
        }

        private void TabQueues_SelectionChanged(object? _, SelectionChangedEventArgs __)
        {
            lvRegular.SelectedItem = lvExpress.SelectedItem = null;
            SetCostEnabled(true);
            RefreshQueues();
        }

        /*────────────────── ADD  (Q6) ───────────────────────────────*/
        /// <summary>
        /// Adds a new job to the selected queue (Regular/Express).
        /// Cost validated, duplicate tag checked, express surcharge applied.
        /// </summary>
        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            // validate cost
            if (!double.TryParse(txtCost.Text, out double raw) || raw <= 0)
            {
                MessageBox.Show("Enter a positive cost (max two decimals).",
                                "Invalid Cost", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // allocate tag and ensure uniqueness
            int tag = numTag.Value ?? 100;
            IncrementTag();
            if (IsTagDuplicate(tag))
            {
                MessageBox.Show($"Service Tag #{tag} already exists.",
                                "Duplicate Tag", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // priority
            string prio = GetServicePriority();
            double cost = prio == "Express" ? Math.Round(raw * 1.15, 2) : Math.Round(raw, 2);

            // create & enqueue
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

            // UI refresh
            RefreshQueues();
            RefreshFinished();
            ClearForm();
            txtStatus.Text = $"Job #{tag} enqueued as {prio}.";
        }

        private string GetServicePriority() =>
            rbExpress.IsChecked == true ? "Express" : "Regular";

        private void IncrementTag()
        {
            numTag.Value = (numTag.Value ?? 100) + 10 > 900 ? 100
                          : (numTag.Value ?? 100) + 10;
        }

        private bool IsTagDuplicate(int tag) =>
            _regularService.Any(d => d.ServiceTag == tag) ||
            _expressService.Any(d => d.ServiceTag == tag) ||
            _finished.Any(d => d.ServiceTag == tag);

        /*────────────────── PROCESS buttons ─────────────────────────*/
        private void ProcessReg_Click(object sender, RoutedEventArgs e)
        {
            if (_regularService.Count == 0)
            {
                MessageBox.Show("No Regular jobs in the queue.", "Queue Empty",
                                MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("No Express jobs in the queue.", "Queue Empty",
                                MessageBoxButton.OK, MessageBoxImage.Information);
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

        /*────────────────── List refresh helpers ───────────────────*/
        private void RefreshQueues()
        {
            lvRegular.ItemsSource = _regularService.ToList();
            lvExpress.ItemsSource = _expressService.ToList();

            btnProcessReg.IsEnabled = tabQueues.SelectedIndex == 0 && _regularService.Count > 0;
            btnProcessExpr.IsEnabled = tabQueues.SelectedIndex == 1 && _expressService.Count > 0;

            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }

        private void RefreshFinished()
        {
            lvFinished.ItemsSource = _finished.Select(d => new
            {
                d.ServiceTag,
                d.ClientName,
                Cost = $"{d.Cost:F2}",
                d.Priority
            }).ToList();
        }

        /*────────────────── Selection → form fill ──────────────────*/
        private void LvRegular_SelectionChanged(object? _, SelectionChangedEventArgs __)
        {
            if (lvRegular.SelectedItem is Drone d) { 
                PopulateForm(d, "Regular"); SetCostEnabled(false);
                SetCostEnabled(false);
                btnUpdate.IsEnabled = true;
            }
            else if (lvExpress.SelectedItem == null) SetCostEnabled(true);

            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }

        private void LvExpress_SelectionChanged(object? _, SelectionChangedEventArgs __)
        {
            if (lvExpress.SelectedItem is Drone d) { 
                PopulateForm(d, "Express"); SetCostEnabled(false);
                SetCostEnabled(false);
                btnUpdate.IsEnabled = true;
            }
            else if (lvRegular.SelectedItem == null) SetCostEnabled(true);

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

        /*────────────────── UPDATE (cost locked) ───────────────────*/
        private void UpdateSelectedJob_Click(object sender, RoutedEventArgs e)
        {
            ListView active = tabQueues.SelectedIndex == 0 ? lvRegular : lvExpress;
            if (active.SelectedItem is not Drone sel) return;

            sel.ClientName = txtClient.Text;
            sel.DroneModel = txtModel.Text;
            sel.ServiceProblem = txtProblem.Text;
            // Cost unchanged (txtCost disabled)

            active.SelectedItem = null;
            SetCostEnabled(true);
            btnUpdate.IsEnabled = false;

            RefreshQueues();
            txtStatus.Text = $"Updated job #{sel.ServiceTag}.";
            ClearForm();
        }

        /*────────────────── Finished-list double-click ─────────────*/
        private void RemoveFinished(object sender, MouseButtonEventArgs e)
        {
            int idx = lvFinished.SelectedIndex;
            if (idx < 0) return;

            if (MessageBox.Show("Remove this finished job?",
                                "Confirm", MessageBoxButton.YesNo,
                                MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            _finished.RemoveAt(idx);
            RefreshFinished();
            txtStatus.Text = "Finished job removed.";
        }

        /*────────────────── Validation & misc ──────────────────────*/
        private void TxtCost_PreviewTextInput(object? s, TextCompositionEventArgs e)
        {
            if (s is not TextBox tb) return;
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
