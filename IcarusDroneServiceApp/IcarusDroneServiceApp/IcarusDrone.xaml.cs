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
    public partial class IcarusDrone : Window
    {
        // ─── Data Structures ─────────────────────────────────────────────────────

        /// <summary>
        /// Queue of regular‐priority Drone jobs         [Programming Criteria]
        /// </summary>
        private readonly Queue<Drone> RegularService = new();

        /// <summary>
        /// Queue of express‐priority Drone jobs         [Programming Criteria]
        /// </summary>
        private readonly Queue<Drone> ExpressService = new();

        /// <summary>
        /// List of completed Drone jobs                [Programming Criteria]
        /// </summary>
        private readonly List<Drone> FinishedList = new();

        // ─── Constructor ─────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes components, sets up ListView columns,
        /// and populates initial queue and finished lists.
        ///                                             [Programming Criteria]
        /// </summary>
        public IcarusDrone()
        {
            InitializeComponent();
            Trace.TraceInformation("[Startup] Main window initialized");
            SetupListViewColumns();
            RefreshQueues();
            RefreshFinished();
        }

        // ─── GUI SETUP ────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds GridView columns for Regular and Express ListViews.
        /// Cannot assign Columns property directly, so Add() is used.
        ///                                             [Programming Criteria]
        /// </summary>
        private void SetupListViewColumns()
        {
            Trace.TraceInformation("[Setup] Building ListView columns");
            var columns = new (string header, string path)[]
            {
                ("Tag","ServiceTag"),
                ("Client","ClientName"),
                ("Model","DroneModel"),
                ("Problem","ServiceProblem"),
                ("Cost","Cost")
            };

            // Regular queue columns
            var gvReg = new GridView();
            foreach (var (h, p) in columns)
                gvReg.Columns.Add(new GridViewColumn
                {
                    Header = h,
                    DisplayMemberBinding = new Binding(p)
                });
            lvRegular.View = gvReg;

            // Express queue columns
            var gvExpr = new GridView();
            foreach (var (h, p) in columns)
                gvExpr.Columns.Add(new GridViewColumn
                {
                    Header = h,
                    DisplayMemberBinding = new Binding(p)
                });
            lvExpress.View = gvExpr;
        }

        // ─── TEST CASES 1–3: ADD NEW ITEM ────────────────────────────────────────

        /// <summary>
        /// Adds a new Drone job to the appropriate queue.
        /// TC1: Add Regular Job
        /// TC2: Add Express Job & surcharge
        /// TC3: Invalid cost validation
        ///                                             [Programming Criteria]
        /// </summary>
        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            Trace.TraceInformation("[AddNewItem] invoked");

            // Validate cost: only two decimals, positive
            if (!double.TryParse(txtCost.Text, out double cost) || cost <= 0)
            {
                Trace.TraceWarning($"[AddNewItem] Invalid cost '{txtCost.Text}'");
                MessageBox.Show("Enter a valid positive cost (up to two decimals).",
                                "Invalid Cost", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Grab tag and increment for next
            int tag = numTag.Value ?? 100;
            Trace.TraceInformation($"[AddNewItem] Tag={tag}");
            IncrementTag();

            // Determine priority and apply express surcharge
            var priority = GetServicePriority();
            Trace.TraceInformation($"[AddNewItem] Priority={priority}");
            if (priority == "Express")
            {
                cost *= 1.15;
                Trace.TraceInformation($"[AddNewItem] Surcharge applied → Cost={cost:F2}");
            }

            // Construct and enqueue Drone
            var drone = new Drone
            {
                ClientName = txtClient.Text,
                DroneModel = txtModel.Text,
                ServiceProblem = txtProblem.Text,
                Cost = cost,
                ServiceTag = tag
            };

            if (priority == "Express")
                ExpressService.Enqueue(drone);
            else
                RegularService.Enqueue(drone);

            Trace.TraceInformation(
                $"[AddNewItem] Enqueued. RegularCount={RegularService.Count}, ExpressCount={ExpressService.Count}");

            // Refresh UI and clear inputs
            RefreshQueues();
            ClearForm();
            txtStatus.Text = $"Job #{tag} enqueued to {priority}.";
        }

        /// <summary>
        /// Returns the selected priority radio button value.
        ///                                             [Programming Criteria]
        /// </summary>
        private string GetServicePriority()
            => rbExpress.IsChecked == true ? "Express" : "Regular";

        /// <summary>
        /// Increments the numeric ServiceTag by 10, wrapping at 900 → 100.
        ///                                             [Programming Criteria]
        /// </summary>
        private void IncrementTag()
        {
            int next = (numTag.Value ?? 100) + 10;
            if (next > 900) next = 100;
            numTag.Value = next;
            Trace.TraceInformation($"[IncrementTag] NextTag={next}");
        }

        /// <summary>
        /// Refreshes both Regular and Express ListViews and process buttons.
        ///                                             [Programming Criteria]
        /// </summary>
        private void RefreshQueues()
        {
            lvRegular.ItemsSource = RegularService.ToList();
            lvExpress.ItemsSource = ExpressService.ToList();
            btnProcessReg.IsEnabled = RegularService.Count > 0;
            btnProcessExpr.IsEnabled = ExpressService.Count > 0;
        }

        // ─── TEST CASE 4: PROCESS REGULAR ────────────────────────────────────────

        /// <summary>
        /// Dequeues from RegularService and adds to FinishedList.
        ///                                             [Programming Criteria]
        /// </summary>
        private void ProcessReg_Click(object sender, RoutedEventArgs e)
        {
            if (RegularService.Count == 0) return;
            var d = RegularService.Dequeue();
            Trace.TraceInformation($"[ProcessReg] Dequeued Tag={d.ServiceTag}");
            FinishedList.Add(d);
            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Regular job #{d.ServiceTag}.";
        }

        // ─── TEST CASE 5: PROCESS EXPRESS ────────────────────────────────────────

        /// <summary>
        /// Dequeues from ExpressService and adds to FinishedList.
        ///                                             [Programming Criteria]
        /// </summary>
        private void ProcessExpr_Click(object sender, RoutedEventArgs e)
        {
            if (ExpressService.Count == 0) return;
            var d = ExpressService.Dequeue();
            Trace.TraceInformation($"[ProcessExpr] Dequeued Tag={d.ServiceTag}");
            FinishedList.Add(d);
            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Express job #{d.ServiceTag}.";
        }

        /// <summary>
        /// Updates the Finished ListBox from FinishedList.
        ///                                             [Programming Criteria]
        /// </summary>
        private void RefreshFinished()
        {
            lbFinished.ItemsSource = FinishedList.Select(d => d.Display());
        }

        // ─── TEST CASE 6: REMOVE FINISHED ────────────────────────────────────────

        /// <summary>
        /// Double-click to remove a finished job from the list.
        ///                                             [Programming Criteria]
        /// </summary>
        private void OnRemoveFinished(object sender, MouseButtonEventArgs e)
        {
            int idx = lbFinished.SelectedIndex;
            if (idx < 0) return;
            Trace.TraceWarning($"[OnRemoveFinished] Removing index={idx}");
            if (MessageBox.Show("Remove this finished job?", "Confirm",
                   MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            FinishedList.RemoveAt(idx);
            RefreshFinished();
            txtStatus.Text = "Finished job removed.";
        }

        // ─── LISTVIEW SELECTION HANDLERS ─────────────────────────────────────────

        /// <summary>
        /// Populates form fields when selecting a Regular job.
        ///                                             [Programming Criteria]
        /// </summary>
        private void LvRegular_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (lvRegular.SelectedItem is Drone d)
            {
                txtClient.Text = d.ClientName;
                txtProblem.Text = d.ServiceProblem;
                Trace.TraceInformation($"[LvRegular] Selected Tag={d.ServiceTag}");
            }
        }

        /// <summary>
        /// Populates form fields when selecting an Express job.
        ///                                             [Programming Criteria]
        /// </summary>
        private void LvExpress_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (lvExpress.SelectedItem is Drone d)
            {
                txtClient.Text = d.ClientName;
                txtProblem.Text = d.ServiceProblem;
                Trace.TraceInformation($"[LvExpress] Selected Tag={d.ServiceTag}");
            }
        }

        // ─── COST INPUT VALIDATION ──────────────────────────────────────────────

        /// <summary>
        /// Ensures cost textbox only accepts up to two decimal places.
        ///                                             [Programming Criteria]
        /// </summary>
        private void TxtCost_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;  // safe null check
            string full = tb.Text.Insert(tb.SelectionStart, e.Text);
            bool blocked = !Regex.IsMatch(full, @"^\d*\.?\d{0,2}$");
            e.Handled = blocked;
            Trace.TraceInformation($"[TxtCost] Input='{full}', Blocked={blocked}");
        }

        // ─── FORM RESET ─────────────────────────────────────────────────────────

        /// <summary>
        /// Clears all inputs and resets priority to Regular.
        ///                                             [Programming Criteria]
        /// </summary>
        private void ClearForm()
        {
            txtClient.Clear();
            txtModel.Clear();
            txtCost.Clear();
            txtProblem.Clear();
            rbRegular.IsChecked = true;
            Trace.TraceInformation("[ClearForm] Inputs reset");
        }

        private void TabQueues_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // no operation needed; columns already configured
        }
    }
}
