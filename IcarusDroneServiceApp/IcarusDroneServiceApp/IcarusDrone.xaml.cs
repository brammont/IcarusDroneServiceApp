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
    /// Interaction logic for IcarusDrone.xaml
    /// Main window that manages job creation, editing, processing and display.
    /// </summary>
    public partial class IcarusDrone : Window
    {
        #region Fields

        private readonly Queue<Drone> RegularService = new();
        private readonly Queue<Drone> ExpressService = new();
        private readonly List<Drone> FinishedList = new();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the window, sets up all ListViews and loads initial data.
        /// </summary>
        public IcarusDrone()
        {
            InitializeComponent();
            Trace.TraceInformation("[Startup] Window initialized");

            SetupQueueColumns();
            SetupFinishedColumns();
            RefreshQueues();
            RefreshFinished();
            UpdateProcessButtons();
        }

        #endregion

        #region Setup Columns

        /// <summary>
        /// Dynamically builds the GridView for Regular & Express queues.
        /// </summary>
        private void SetupQueueColumns()
        {
            Trace.TraceInformation("[Setup] Building queue columns");

            var cols = new (string Header, string Path)[]
            {
                ("Tag",     "ServiceTag"),
                ("Client",  "ClientName"),
                ("Model",   "DroneModel"),
                ("Problem", "ServiceProblem"),
                ("Cost",    "Cost")
            };

            var gvReg = new GridView();
            foreach (var (h, p) in cols)
                gvReg.Columns.Add(new GridViewColumn { Header = h, DisplayMemberBinding = new Binding(p) });
            lvRegular.View = gvReg;

            var gvExpr = new GridView();
            foreach (var (h, p) in cols)
                gvExpr.Columns.Add(new GridViewColumn { Header = h, DisplayMemberBinding = new Binding(p) });
            lvExpress.View = gvExpr;
        }

        /// <summary>
        /// Builds the GridView for the Finished‐Jobs ListView.
        /// </summary>
        private void SetupFinishedColumns()
        {
            Trace.TraceInformation("[Setup] Building finished columns");

            var gvFin = new GridView();
            gvFin.Columns.Add(new GridViewColumn { Header = "Tag", DisplayMemberBinding = new Binding("ServiceTag") });
            gvFin.Columns.Add(new GridViewColumn { Header = "Client", DisplayMemberBinding = new Binding("ClientName") });
            gvFin.Columns.Add(new GridViewColumn { Header = "Cost", DisplayMemberBinding = new Binding("Cost") });
            gvFin.Columns.Add(new GridViewColumn { Header = "Priority", DisplayMemberBinding = new Binding("Priority") });
            lvFinished.View = gvFin;
        }

        #endregion

        #region Add & Update

        /// <summary>
        /// Adds a new job based on form inputs.
        /// </summary>
        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            Trace.TraceInformation("[Add] Invoked");

            // Cost validation
            if (!double.TryParse(txtCost.Text, out double cost) || cost <= 0)
            {
                Trace.TraceWarning($"[Add] Bad cost '{txtCost.Text}'");
                MessageBox.Show(
                    "Please enter a valid positive cost (max two decimals).",
                    "Invalid Cost", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Read & validate tag
            int tag = numTag.Value ?? 100;
            if (IsTagDuplicate(tag))
            {
                Trace.TraceWarning($"[Add] Duplicate tag {tag}");
                MessageBox.Show(
                    $"Tag #{tag} already used. Choose another.",
                    "Duplicate Tag", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prepare next tag
            IncrementTag();

            // Priority & surcharge
            var priority = GetServicePriority();
            if (priority == "Express")
            {
                cost *= 1.15;
                Trace.TraceInformation($"[Add] Express surcharge applied → {cost:F2}");
            }

            // Construct and enqueue
            var job = new Drone
            {
                ClientName = txtClient.Text,
                DroneModel = txtModel.Text,
                ServiceProblem = txtProblem.Text,
                Cost = Math.Round(cost, 2),
                ServiceTag = tag,
                Priority = priority
            };

            if (priority == "Express") ExpressService.Enqueue(job);
            else RegularService.Enqueue(job);

            Trace.TraceInformation($"[Add] Enqueued #{tag} ({priority})");

            RefreshQueues();
            ClearForm();
            txtStatus.Text = $"Job #{tag} added to {priority}.";
        }

        /// <summary>
        /// Updates the currently selected job’s fields.
        /// </summary>
        private void UpdateSelectedJob_Click(object sender, RoutedEventArgs e)
        {
            Trace.TraceInformation("[Update] Invoked");

            // Determine which queue and which job
            Drone current = (tabQueues.SelectedIndex == 0
                ? lvRegular.SelectedItem as Drone
                : lvExpress.SelectedItem as Drone);

            if (current == null) return;

            // Apply edits
            current.ClientName = txtClient.Text;
            current.DroneModel = txtModel.Text;
            current.ServiceProblem = txtProblem.Text;

            if (double.TryParse(txtCost.Text, out double c) && c > 0)
                current.Cost = Math.Round(c, 2);

            current.Priority = GetServicePriority();
            current.ServiceTag = numTag.Value ?? current.ServiceTag;

            Trace.TraceInformation($"[Update] Updated #{current.ServiceTag}");

            RefreshQueues();
            txtStatus.Text = $"Job #{current.ServiceTag} updated.";
        }

        #endregion

        #region Process Jobs

        /// <summary>
        /// Processes (dequeues) the selected Regular job.
        /// </summary>
        private void ProcessReg_Click(object sender, RoutedEventArgs e)
        {
            var d = lvRegular.SelectedItem as Drone;
            if (d == null) return;

            RegularService.Dequeue();
            FinishedList.Add(d);
            Trace.TraceInformation($"[ProcReg] Processed #{d.ServiceTag}");

            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Regular #{d.ServiceTag}.";
        }

        /// <summary>
        /// Processes (dequeues) the selected Express job.
        /// </summary>
        private void ProcessExpr_Click(object sender, RoutedEventArgs e)
        {
            var d = lvExpress.SelectedItem as Drone;
            if (d == null) return;

            ExpressService.Dequeue();
            FinishedList.Add(d);
            Trace.TraceInformation($"[ProcExpr] Processed #{d.ServiceTag}");

            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Express #{d.ServiceTag}.";
        }

        #endregion

        #region Refresh Displays

        /// <summary>
        /// Reloads the queue ListViews.
        /// </summary>
        private void RefreshQueues()
        {
            lvRegular.ItemsSource = RegularService.ToList();
            lvExpress.ItemsSource = ExpressService.ToList();
            UpdateProcessButtons();
        }

        /// <summary>
        /// Reloads the Finished-Jobs ListView.
        /// </summary>
        private void RefreshFinished()
        {
            lvFinished.ItemsSource = FinishedList.ToList();
        }

        #endregion

        #region Selection Handlers

        /// <summary>
        /// When you click a Regular item, populate the form.
        /// </summary>
        private void LvRegular_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvRegular.SelectedItem is Drone d)
                PopulateForm(d);
            UpdateProcessButtons();
        }

        /// <summary>
        /// When you click an Express item, populate the form.
        /// </summary>
        private void LvExpress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvExpress.SelectedItem is Drone d)
                PopulateForm(d);
            UpdateProcessButtons();
        }

        /// <summary>
        /// Writes every field of a Drone into the input form.
        /// </summary>
        /// <param name="d">The selected Drone job.</param>
        private void PopulateForm(Drone d)
        {
            txtClient.Text = d.ClientName;
            txtModel.Text = d.DroneModel;
            txtProblem.Text = d.ServiceProblem;
            txtCost.Text = d.Cost.ToString("F2");
            numTag.Value = d.ServiceTag;
            rbRegular.IsChecked = d.Priority == "Regular";
            rbExpress.IsChecked = d.Priority == "Express";
        }

        #endregion

        #region Input Validation

        /// <summary>
        /// Ensures cost textbox only accepts up to two decimals.
        /// </summary>
        private void TxtCost_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;
            string all = tb.Text.Insert(tb.SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(all, @"^\d*\.?\d{0,2}$");
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Reads which Priority radio is checked.
        /// </summary>
        /// <returns>"Regular" or "Express".</returns>
        private string GetServicePriority()
            => rbExpress.IsChecked == true ? "Express" : "Regular";

        /// <summary>
        /// Moves the numeric tag up by 10, wraps >900 → 100.
        /// </summary>
        private void IncrementTag()
        {
            int next = (numTag.Value ?? 100) + 10;
            if (next > 900) next = 100;
            numTag.Value = next;
        }

        /// <summary>
        /// Checks across all queues and finished list for a duplicate tag.
        /// </summary>
        /// <param name="tag">The tag to verify.</param>
        /// <returns>True if already in use.</returns>
        private bool IsTagDuplicate(int tag)
            => RegularService.Any(x => x.ServiceTag == tag)
            || ExpressService.Any(x => x.ServiceTag == tag)
            || FinishedList.Any(x => x.ServiceTag == tag);

        /// <summary>
        /// Clears the form fields and resets priority & selection.
        /// </summary>
        private void ClearForm()
        {
            txtClient.Clear();
            txtModel.Clear();
            txtCost.Clear();
            txtProblem.Clear();
            numTag.Value = 100;
            rbRegular.IsChecked = true;
            lvRegular.SelectedItem = null;
            lvExpress.SelectedItem = null;
            UpdateProcessButtons();
        }

        #endregion

        #region TabControl & Button Logic

        /// <summary>
        /// Fires whenever you switch tabs. Ensures the correct Process
        /// button is active and that Update is enabled if anything is selected.
        /// </summary>
        private void TabQueues_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateProcessButtons();

        /// <summary>
        /// Enables/disables the Process and Update buttons based on:
        /// • Which tab is active
        /// • Whether an item in that queue is selected
        /// </summary>
        private void UpdateProcessButtons()
        {
            bool onReg = (tabQueues.SelectedIndex == 0);
            bool hasReg = (lvRegular.SelectedItem != null);
            bool hasExpr = (lvExpress.SelectedItem != null);

            btnProcessReg.IsEnabled = onReg && hasReg;
            btnProcessExpr.IsEnabled = !onReg && hasExpr;
            btnUpdate.IsEnabled = hasReg || hasExpr;
        }

        #endregion

        #region Finished Removal

        /// <summary>
        /// Double‐click to remove a finished job after confirmation.
        /// </summary>
        private void OnRemoveFinished(object sender, MouseButtonEventArgs e)
        {
            if (lvFinished.SelectedItem is not Drone) return;

            if (MessageBox.Show(
                "Remove this finished job?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            FinishedList.RemoveAt(lvFinished.SelectedIndex);
            RefreshFinished();
            txtStatus.Text = "Finished job removed.";
        }

        #endregion
    }
}
