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
    /// <summary>Main window for the Icarus Drone Service application.</summary>
    public partial class IcarusDrone : Window
    {
        // ─── Queues ───────────────────────────────────────────────────────────────
        private readonly Queue<Drone> RegularService = new();
        private readonly Queue<Drone> ExpressService = new();
        private readonly List<Drone> FinishedList = new();

        // ─── Construction ─────────────────────────────────────────────────────────
        public IcarusDrone()
        {
            InitializeComponent();
            Trace.TraceInformation("[Startup] IcarusDrone main window initialised");

            BuildQueueColumns();
            RefreshQueues();
            RefreshFinished();
        }

        // ─── Build GridView columns programmatically ─────────────────────────────
        private void BuildQueueColumns()
        {
            var cols = new (string H, string P)[]{
                ("Tag","ServiceTag"), ("Client","ClientName"),
                ("Model","DroneModel"), ("Problem","ServiceProblem"), ("Cost","Cost")
            };

            static GridView Make((string H, string P)[] src)
            {
                var gv = new GridView();
                foreach (var (h, p) in src)
                    gv.Columns.Add(new GridViewColumn { Header = h, DisplayMemberBinding = new Binding(p) });
                return gv;
            }
            lvRegular.View = Make(cols);
            lvExpress.View = Make(cols);
        }

        // ─── Tab change – clear selections & reset UI ────────────────────────────
        private void TabQueues_SelectionChanged(object? _, SelectionChangedEventArgs __)
        {
            lvRegular.SelectedItem = lvExpress.SelectedItem = null;
            SetCostEnabled(true);
            RefreshQueues();
        }

        // ─── Add-new-job handler ─────────────────────────────────────────────────
        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtCost.Text, out double raw) || raw <= 0)
            {
                MessageBox.Show("Enter a positive cost (≤ 2 decimals).",
                                "Invalid Cost", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int tag = numTag.Value ?? 100;
            IncrementTag();

            if (IsTagDuplicate(tag))
            {
                MessageBox.Show($"Service Tag #{tag} already exists.",
                                "Duplicate Tag", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string prio = rbExpress.IsChecked == true ? "Express" : "Regular";
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

            (prio == "Express" ? ExpressService : RegularService).Enqueue(job);

            RefreshQueues();
            RefreshFinished();
            ClearForm();
            txtStatus.Text = $"Job #{tag} enqueued as {prio}.";
        }

        private void IncrementTag()
        {
            int next = (numTag.Value ?? 100) + 10; if (next > 900) next = 100; numTag.Value = next;
        }
        private bool IsTagDuplicate(int t) =>
            RegularService.Any(d => d.ServiceTag == t) ||
            ExpressService.Any(d => d.ServiceTag == t) ||
            FinishedList.Any(d => d.ServiceTag == t);

        // ─── PROCESS REGULAR  (acts on queue front) ──────────────────────────────
        private void ProcessReg_Click(object sender, RoutedEventArgs e)
        {
            if (RegularService.Count == 0)
            {
                MessageBox.Show("No Regular jobs in the queue.", "Queue Empty",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Drone done = RegularService.Dequeue();
            FinishedList.Add(done);

            lvRegular.SelectedItem = null;
            PostProcessRefresh($"Processed Regular job #{done.ServiceTag}.");
        }

        // ─── PROCESS EXPRESS  (acts on queue front) ──────────────────────────────
        private void ProcessExpr_Click(object sender, RoutedEventArgs e)
        {
            if (ExpressService.Count == 0)
            {
                MessageBox.Show("No Express jobs in the queue.", "Queue Empty",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Drone done = ExpressService.Dequeue();
            FinishedList.Add(done);

            lvExpress.SelectedItem = null;
            PostProcessRefresh($"Processed Express job #{done.ServiceTag}.");
        }

        private void PostProcessRefresh(string status)
        {
            RefreshQueues();
            RefreshFinished();
            ClearForm();
            txtStatus.Text = status;
        }

        // ─── Refresh helpers ─────────────────────────────────────────────────────
        private void RefreshQueues()
        {
            lvRegular.ItemsSource = RegularService.ToList();
            lvExpress.ItemsSource = ExpressService.ToList();

            btnProcessReg.IsEnabled = tabQueues.SelectedIndex == 0 && RegularService.Count > 0;
            btnProcessExpr.IsEnabled = tabQueues.SelectedIndex == 1 && ExpressService.Count > 0;

            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }

        private void RefreshFinished()
        {
            lvFinished.ItemsSource = FinishedList.Select(d => new {
                Tag = d.ServiceTag,
                Client = d.ClientName,
                Cost = $"{d.Cost:F2}",
                Type = d.Priority
            }).ToList();
        }

        private void OnRemoveFinished(object? _, MouseButtonEventArgs __)
        {
            int idx = lvFinished.SelectedIndex; if (idx < 0) return;
            if (MessageBox.Show("Remove this finished job?", "Confirm", MessageBoxButton.YesNo,
                               MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            FinishedList.RemoveAt(idx);
            RefreshFinished();
            txtStatus.Text = "Finished job removed.";
        }

        // ─── Selection-changed → form populate / cost disable ────────────────────
        private void LvRegular_SelectionChanged(object? _, SelectionChangedEventArgs __)
        {
            if (lvRegular.SelectedItem is Drone d) { FillForm(d, "Regular"); SetCostEnabled(false); }
            else SetCostEnabled(lvExpress.SelectedItem == null);
            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }
        private void LvExpress_SelectionChanged(object? _, SelectionChangedEventArgs __)
        {
            if (lvExpress.SelectedItem is Drone d) { FillForm(d, "Express"); SetCostEnabled(false); }
            else SetCostEnabled(lvRegular.SelectedItem == null);
            btnUpdate.IsEnabled = lvRegular.SelectedItem != null || lvExpress.SelectedItem != null;
        }

        private void FillForm(Drone d, string pr)
        {
            txtClient.Text = d.ClientName; txtModel.Text = d.DroneModel;
            txtProblem.Text = d.ServiceProblem; txtCost.Text = d.Cost.ToString("F2");
            numTag.Value = d.ServiceTag;
            rbRegular.IsChecked = pr == "Regular"; rbExpress.IsChecked = pr == "Express";
        }
        private void SetCostEnabled(bool enable) => txtCost.IsEnabled = enable;

        // ─── UPDATE selected job ─────────────────────────────────────────────────
        private void UpdateSelectedJob_Click(object sender, RoutedEventArgs e)
        {
            ListView active = tabQueues.SelectedIndex == 0 ? lvRegular : lvExpress;
            if (active.SelectedItem is not Drone sel) return;

            if (!double.TryParse(txtCost.Text, out double newCost) || newCost <= 0)
            {
                MessageBox.Show("Enter a valid positive cost.", "Invalid Cost",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            sel.ClientName = txtClient.Text; sel.DroneModel = txtModel.Text;
            sel.ServiceProblem = txtProblem.Text; sel.Cost = Math.Round(newCost, 2);

            lvRegular.SelectedItem = lvExpress.SelectedItem = null;  // clear
            SetCostEnabled(true);
            btnUpdate.IsEnabled = false;

            RefreshQueues();
            txtStatus.Text = $"Updated job #{sel.ServiceTag}.";
            ClearForm();
        }

        // ─── Validation & helpers ────────────────────────────────────────────────
        private void TxtCost_PreviewTextInput(object? s, TextCompositionEventArgs e)
        {
            if (s is not TextBox tb) return;
            string next = tb.Text.Insert(tb.SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(next, @"^\d*\.?\d{0,2}$");
        }
        private void ClearForm()
        {
            txtClient.Clear(); txtModel.Clear(); txtProblem.Clear(); txtCost.Clear();
            rbRegular.IsChecked = true;
        }
    }
}
