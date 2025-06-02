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
    /// Main window for the Icarus Drone Service Application.
    /// Manages user input, queue operations, job updates, and finished-jobs display.
    /// Logs all actions via System.Diagnostics.Trace for debugging & auditing.
    /// </summary>
    public partial class IcarusDrone : Window
    {
        // ───────────────────────────────────────────────────────────────────────────
        // Private Data Structures
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Queue for regular-priority Drone jobs.
        /// Enqueue when “Add New Item” + “Regular”; dequeue when “Process Regular” is clicked.
        /// </summary>
        private readonly Queue<Drone> RegularService = new();

        /// <summary>
        /// Queue for express-priority Drone jobs.
        /// Enqueue when “Add New Item” + “Express” (with 15% surcharge); dequeue when “Process Express” is clicked.
        /// </summary>
        private readonly Queue<Drone> ExpressService = new();

        /// <summary>
        /// List of all completed drone jobs (in the order processed).
        /// Each entry appears in the Finished-Jobs ListView with Tag, Client, Cost, and Type.
        /// </summary>
        private readonly List<Drone> FinishedList = new();

        // ───────────────────────────────────────────────────────────────────────────
        // Constructor
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes UI components, builds columns, and loads any existing data.
        /// Logs a startup trace entry.
        /// </summary>
        public IcarusDrone()
        {
            InitializeComponent();

            // Log that the window has been created
            Trace.TraceInformation("[Startup] IcarusDrone main window initialized.");

            // Build the GridView columns for lvRegular, lvExpress, and lvFinished
            SetupListViewColumns();

            // Populate all three UI lists (all start empty)
            RefreshQueues();
            RefreshFinished();
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ GUI SETUP: COLUMNS & TAB HANDLING ════════════════════════════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures the columns for:
        ///   • lvRegular  (Regular queue)
        ///   • lvExpress  (Express queue)
        /// These both show: Tag, ClientName, DroneModel, ServiceProblem, Cost.
        ///
        /// lvFinished is already set up in XAML with its own GridView (Tag, Client, Cost, Type).
        /// </summary>
        private void SetupListViewColumns()
        {
            Trace.TraceInformation("[Setup] Building ListView columns for Regular & Express.");

            // Define which columns to create on the queue ListViews
            var columns = new (string header, string path)[]
            {
                ("Tag",     "ServiceTag"),
                ("Client",  "ClientName"),
                ("Model",   "DroneModel"),
                ("Problem", "ServiceProblem"),
                ("Cost",    "Cost")
            };

            // Build GridView for Regular queue
            var gvRegular = new GridView();
            foreach (var (header, path) in columns)
            {
                gvRegular.Columns.Add(new GridViewColumn
                {
                    Header = header,
                    DisplayMemberBinding = new Binding(path)
                });
            }
            lvRegular.View = gvRegular;

            // Build GridView for Express queue
            var gvExpress = new GridView();
            foreach (var (header, path) in columns)
            {
                gvExpress.Columns.Add(new GridViewColumn
                {
                    Header = header,
                    DisplayMemberBinding = new Binding(path)
                });
            }
            lvExpress.View = gvExpress;
        }

        /// <summary>
        /// Handles when the user switches between the “Regular” and “Express” tabs.
        /// If a tab has at least one item, auto-select the first one.
        /// Then refresh button states.
        /// </summary>
        /// <param name="sender">The TabControl “tabQueues.”</param>
        /// <param name="e">SelectionChangedEventArgs (unused).</param>
        private void TabQueues_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If user selected the Regular tab (index 0) and there are items, select index=0
            if (tabQueues.SelectedIndex == 0 && lvRegular.Items.Count > 0)
            {
                lvRegular.SelectedIndex = 0;
            }
            // If user selected the Express tab (index 1) and there are items, select index=0
            else if (tabQueues.SelectedIndex == 1 && lvExpress.Items.Count > 0)
            {
                lvExpress.SelectedIndex = 0;
            }

            // Always refresh process-button enabling/disabling
            RefreshQueues();
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ ADD NEW ITEM (Test Cases 1–3 + Auto-Increment Tag) ═══════════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// “Add New Item” button click handler.
        /// 1. Validates Cost (positive double, ≤2 decimals).
        /// 2. Reads current ServiceTag from numTag.
        /// 3. Immediately auto-increments numTag by 10 (wrapping 900→100).
        /// 4. Checks for duplicate Tag (in RegularService, ExpressService, FinishedList).
        /// 5. Applies 15% surcharge if priority=Express.
        /// 6. Constructs a new Drone object with all fields (Client, Model, Problem, Cost, Tag, Priority).
        /// 7. Enqueues into RegularService or ExpressService.
        /// 8. Refreshes UI lists, enables/disables process buttons, and clears the form.
        /// 9. Logs each major step to Trace.
        /// </summary>
        /// <param name="sender">Button “btnAdd.”</param>
        /// <param name="e">RoutedEventArgs (unused).</param>
        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            Trace.TraceInformation("[AddNewItem] Invoked.");

            // (1) VALIDATE COST
            if (!double.TryParse(txtCost.Text, out double enteredCost) || enteredCost <= 0)
            {
                Trace.TraceWarning($"[AddNewItem] Invalid cost entry '{txtCost.Text}'.");
                MessageBox.Show(
                    "Enter a valid positive cost (up to two decimal places).",
                    "Invalid Cost",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // (2) READ AND STORE CURRENT TAG
            int tagToUse = numTag.Value ?? 100;
            Trace.TraceInformation($"[AddNewItem] Current ServiceTag to use = {tagToUse}");

            // (3) AUTO-INCREMENT numTag FOR NEXT JOB
            IncrementTag();
            Trace.TraceInformation($"[AddNewItem] Auto-incremented numTag → {numTag.Value}");

            // (4) CHECK DUPLICATE TAG
            if (IsTagDuplicate(tagToUse))
            {
                Trace.TraceWarning($"[AddNewItem] Duplicate tag #{tagToUse} detected.");
                MessageBox.Show(
                    $"Service Tag #{tagToUse} is already in use. Please choose a different tag.",
                    "Duplicate Tag",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // (5) DETERMINE PRIORITY
            string priority = GetServicePriority();
            Trace.TraceInformation($"[AddNewItem] Priority selected = {priority}");

            // (6) APPLY SURCHARGE IF EXPRESS
            double finalCost = Math.Round(enteredCost, 2);
            if (priority == "Express")
            {
                finalCost = Math.Round(enteredCost * 1.15, 2);
                Trace.TraceInformation($"[AddNewItem] Applied 15% surcharge → Cost = {finalCost:F2}");
            }

            // (7) CONSTRUCT NEW Drone OBJECT
            var newJob = new Drone
            {
                ClientName = txtClient.Text,
                DroneModel = txtModel.Text,
                ServiceProblem = txtProblem.Text,
                Cost = finalCost,
                ServiceTag = tagToUse,
                Priority = priority
            };

            // (8) ENQUEUE INTO APPROPRIATE QUEUE
            if (priority == "Express")
            {
                ExpressService.Enqueue(newJob);
                Trace.TraceInformation($"[AddNewItem] Enqueued Express job #{tagToUse} (Cost=${finalCost:F2}).");
            }
            else
            {
                RegularService.Enqueue(newJob);
                Trace.TraceInformation($"[AddNewItem] Enqueued Regular job #{tagToUse} (Cost=${finalCost:F2}).");
            }

            // (9) REFRESH UI LISTS, CLEAR FORM, UPDATE STATUS BAR
            RefreshQueues();
            RefreshFinished();
            ClearForm();
            txtStatus.Text = $"Job #{tagToUse} enqueued as {priority}.";
        }

        /// <summary>
        /// Helper method to read which radio button (Regular/Express) is checked.
        /// </summary>
        /// <returns>“Regular” if rbRegular.IsChecked, otherwise “Express.”</returns>
        private string GetServicePriority()
        {
            return (rbExpress.IsChecked == true) ? "Express" : "Regular";
        }

        /// <summary>
        /// Increments the ServiceTag numeric control (numTag) by 10.
        /// If the new value exceeds 900, wraps back to 100.
        /// Called immediately after consuming the old tag in AddNewItem().
        /// </summary>
        private void IncrementTag()
        {
            int next = (numTag.Value ?? 100) + 10;
            if (next > 900)
            {
                next = 100;
            }
            numTag.Value = next;
            Trace.TraceInformation($"[IncrementTag] Next ServiceTag set to {next}.");
        }

        /// <summary>
        /// Checks if a given ServiceTag already exists in:
        ///   • RegularService queue,
        ///   • ExpressService queue,
        ///   • FinishedList.
        /// </summary>
        /// <param name="tag">The ServiceTag to check.</param>
        /// <returns>True if that tag already appears anywhere; otherwise false.</returns>
        private bool IsTagDuplicate(int tag)
        {
            bool inRegular = RegularService.Any(d => d.ServiceTag == tag);
            bool inExpress = ExpressService.Any(d => d.ServiceTag == tag);
            bool inFinished = FinishedList.Any(d => d.ServiceTag == tag);
            return inRegular || inExpress || inFinished;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ PROCESS JOBS (Test Cases 4 & 5 + “only selected item”) ═══════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// “Process Regular” button click handler.
        /// Only processes if:
        ///   • The Regular tab (index 0) is active,
        ///   • A job is selected in lvRegular,
        ///   • That job is at the front of the RegularService queue.
        /// Dequeues the job, adds it to FinishedList, and refreshes all UI.
        /// </summary>
        /// <param name="sender">Button “btnProcessReg.”</param>
        /// <param name="e">RoutedEventArgs (unused).</param>
        private void ProcessReg_Click(object sender, RoutedEventArgs e)
        {
            // 1) Ensure Regular tab is active
            if (tabQueues.SelectedIndex != 0)
            {
                Trace.TraceWarning("[ProcessReg_Click] Ignored because Regular tab is not active.");
                return;
            }

            // 2) Ensure something is actually selected
            if (!(lvRegular.SelectedItem is Drone selectedJob))
            {
                Trace.TraceWarning("[ProcessReg_Click] No Regular job selected.");
                MessageBox.Show(
                    "Please select a Regular job to process.",
                    "No Job Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 3) Ensure the selected job is at the front of the queue
            if (RegularService.Count == 0 || RegularService.Peek().ServiceTag != selectedJob.ServiceTag)
            {
                Trace.TraceWarning(
                    $"[ProcessReg_Click] Selected tag #{selectedJob.ServiceTag} is not at the front of the Regular queue.");
                MessageBox.Show(
                    "You can only process the job at the front of the Regular queue.",
                    "Cannot Process",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 4) Dequeue it
            var dequeued = RegularService.Dequeue();
            Trace.TraceInformation($"[ProcessReg_Click] Dequeued Regular job #{dequeued.ServiceTag}.");

            // 5) Add to finished list
            FinishedList.Add(dequeued);
            Trace.TraceInformation($"[ProcessReg_Click] Added #{dequeued.ServiceTag} to FinishedList.");

            // 6) Refresh UI
            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Regular job #{dequeued.ServiceTag}.";
        }

        /// <summary>
        /// “Process Express” button click handler.
        /// Only processes if:
        ///   • The Express tab (index 1) is active,
        ///   • A job is selected in lvExpress,
        ///   • That job is at the front of the ExpressService queue.
        /// Dequeues it, adds it to FinishedList, and refreshes UI.
        /// </summary>
        /// <param name="sender">Button “btnProcessExpr.”</param>
        /// <param name="e">RoutedEventArgs (unused).</param>
        private void ProcessExpr_Click(object sender, RoutedEventArgs e)
        {
            // 1) Ensure Express tab is active
            if (tabQueues.SelectedIndex != 1)
            {
                Trace.TraceWarning("[ProcessExpr_Click] Ignored because Express tab is not active.");
                return;
            }

            // 2) Ensure a job is selected
            if (!(lvExpress.SelectedItem is Drone selectedJob))
            {
                Trace.TraceWarning("[ProcessExpr_Click] No Express job selected.");
                MessageBox.Show(
                    "Please select an Express job to process.",
                    "No Job Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 3) Ensure selected job is at the front of Express queue
            if (ExpressService.Count == 0 || ExpressService.Peek().ServiceTag != selectedJob.ServiceTag)
            {
                Trace.TraceWarning(
                    $"[ProcessExpr_Click] Selected tag #{selectedJob.ServiceTag} is not at the front of the Express queue.");
                MessageBox.Show(
                    "You can only process the job at the front of the Express queue.",
                    "Cannot Process",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 4) Dequeue it
            var dequeued = ExpressService.Dequeue();
            Trace.TraceInformation($"[ProcessExpr_Click] Dequeued Express job #{dequeued.ServiceTag}.");

            // 5) Add to finished list
            FinishedList.Add(dequeued);
            Trace.TraceInformation($"[ProcessExpr_Click] Added #{dequeued.ServiceTag} to FinishedList.");

            // 6) Refresh UI
            RefreshQueues();
            RefreshFinished();
            txtStatus.Text = $"Processed Express job #{dequeued.ServiceTag}.";
        }

        /// <summary>
        /// Refreshes lvRegular & lvExpress to show the current queue contents,
        /// and toggles the enabled state of btnProcessReg/btnProcessExpr accordingly:
        ///   • “Process Regular” enabled only when tab0 is active and RegularService.Count>0
        ///   • “Process Express” enabled only when tab1 is active and ExpressService.Count>0
        /// Also disables btnUpdate until a new selection is made.
        /// </summary>
        private void RefreshQueues()
        {
            lvRegular.ItemsSource = RegularService.ToList();
            lvExpress.ItemsSource = ExpressService.ToList();

            btnProcessReg.IsEnabled = (tabQueues.SelectedIndex == 0 && RegularService.Count > 0);
            btnProcessExpr.IsEnabled = (tabQueues.SelectedIndex == 1 && ExpressService.Count > 0);

            // Also disable the Update button, because we only allow updates after a selection
            btnUpdate.IsEnabled = false;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ FINISHED-JOBS LIST (Test Case 6 + Columns) ═══════════════════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the Finished-Jobs ListView (lvFinished) by binding it to an
        /// anonymous object list with:
        ///   • Tag    (int)
        ///   • Client (string)
        ///   • Cost   (string formatted to two decimals, with $)
        ///   • Type   (string; either “Regular” or “Express”)
        /// </summary>
        private void RefreshFinished()
        {
            var toDisplay = FinishedList
                .Select(d => new
                {
                    Tag = d.ServiceTag,
                    Client = d.ClientName,
                    Cost = $"${d.Cost:F2}",
                    Type = d.Priority
                })
                .ToList();

            lvFinished.ItemsSource = toDisplay;
        }

        /// <summary>
        /// Handles double-click on a finished job. Prompts to confirm removal.
        /// If user confirms, removes that job from FinishedList and refreshes UI.
        /// </summary>
        /// <param name="sender">The ListView “lvFinished.”</param>
        /// <param name="e">MouseButtonEventArgs (unused).</param>
        private void OnRemoveFinished(object sender, MouseButtonEventArgs e)
        {
            int idx = lvFinished.SelectedIndex;
            if (idx < 0)
            {
                // No item selected
                return;
            }

            Trace.TraceInformation($"[OnRemoveFinished] Attempting to remove FinishedList index={idx}.");

            var result = MessageBox.Show(
                "Remove this finished job from the list?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            FinishedList.RemoveAt(idx);
            Trace.TraceInformation($"[OnRemoveFinished] Removed job at index {idx} from FinishedList.");

            RefreshFinished();
            txtStatus.Text = "Finished job removed.";
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ SELECTION-CHANGED: POPULATE FORM + ENABLE “Update” BUTTON ════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// When a Regular-queue item is clicked, populates all form fields so they can be updated.
        /// Also enables btnUpdate so the user can click “Update Selected Job.”
        /// </summary>
        /// <param name="sender">The ListView “lvRegular.”</param>
        /// <param name="e">SelectionChangedEventArgs (unused).</param>
        private void LvRegular_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvRegular.SelectedItem is Drone d)
            {
                PopulateFormWithDrone(d, "Regular");
                btnUpdate.IsEnabled = true;
                Trace.TraceInformation($"[LvRegular] Selected job #{d.ServiceTag} (Regular). Populated form.");
            }
        }

        /// <summary>
        /// When an Express-queue item is clicked, populates all form fields so they can be updated.
        /// Also enables btnUpdate so the user can click “Update Selected Job.”
        /// </summary>
        /// <param name="sender">The ListView “lvExpress.”</param>
        /// <param name="e">SelectionChangedEventArgs (unused).</param>
        private void LvExpress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvExpress.SelectedItem is Drone d)
            {
                PopulateFormWithDrone(d, "Express");
                btnUpdate.IsEnabled = true;
                Trace.TraceInformation($"[LvExpress] Selected job #{d.ServiceTag} (Express). Populated form.");
            }
        }

        /// <summary>
        /// Populates the form controls (Client, Model, Problem, Cost, Priority, ServiceTag) 
        /// from the given Drone object. 
        /// </summary>
        /// <param name="d">The Drone object whose fields we want to show.</param>
        /// <param name="priority">“Regular” or “Express” (to check the correct RadioButton).</param>
        private void PopulateFormWithDrone(Drone d, string priority)
        {
            txtClient.Text = d.ClientName;
            txtModel.Text = d.DroneModel;
            txtProblem.Text = d.ServiceProblem;
            txtCost.Text = d.Cost.ToString("F2"); // Always two decimals
            numTag.Value = d.ServiceTag;

            if (priority == "Regular")
                rbRegular.IsChecked = true;
            else
                rbExpress.IsChecked = true;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ UPDATE SELECTED JOB FEATURE ═════════════════════════════════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// “Update Selected Job” button click handler.
        /// Allows the user to modify the ClientName, DroneModel, ServiceProblem, and Cost
        /// of the currently-selected job in either lvRegular or lvExpress—whichever tab is active.
        /// 
        /// Steps:
        ///   1. Ensure the Regular or Express tab is active.
        ///   2. Ensure a job is actually selected in that ListView.
        ///   3. Validate the new Cost.
        ///   4. Update the fields on the Drone object.
        ///   5. Refresh the ListView to show updated values.
        ///   6. Clear the form and disable the Update button.
        ///   7. Log to Trace and update status bar.
        /// </summary>
        /// <param name="sender">Button “btnUpdate.”</param>
        /// <param name="e">RoutedEventArgs (unused).</param>
        private void UpdateSelectedJob_Click(object sender, RoutedEventArgs e)
        {
            // 1) Which tab is active?
            if (tabQueues.SelectedIndex == 0)
            {
                // Regular tab
                if (!(lvRegular.SelectedItem is Drone selectedRegular))
                {
                    MessageBox.Show(
                        "No Regular job is selected for update.",
                        "No Selection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 2) Validate Cost
                if (!double.TryParse(txtCost.Text, out double newCost) || newCost <= 0)
                {
                    Trace.TraceWarning($"[UpdateSelectedJob_Click] Invalid cost '{txtCost.Text}'.");
                    MessageBox.Show(
                        "Enter a valid positive cost (up to two decimal places).",
                        "Invalid Cost",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 3) Update fields on the Drone object
                selectedRegular.ClientName = txtClient.Text;
                selectedRegular.DroneModel = txtModel.Text;
                selectedRegular.ServiceProblem = txtProblem.Text;
                selectedRegular.Cost = Math.Round(newCost, 2);
                Trace.TraceInformation($"[UpdateSelectedJob_Click] Updated Regular job #{selectedRegular.ServiceTag}.");

                // 4) Refresh the UI for queues
                RefreshQueues();
                txtStatus.Text = $"Updated Regular job #{selectedRegular.ServiceTag}.";
            }
            else if (tabQueues.SelectedIndex == 1)
            {
                // Express tab
                if (!(lvExpress.SelectedItem is Drone selectedExpress))
                {
                    MessageBox.Show(
                        "No Express job is selected for update.",
                        "No Selection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 2) Validate Cost
                if (!double.TryParse(txtCost.Text, out double newCost) || newCost <= 0)
                {
                    Trace.TraceWarning($"[UpdateSelectedJob_Click] Invalid cost '{txtCost.Text}'.");
                    MessageBox.Show(
                        "Enter a valid positive cost (up to two decimal places).",
                        "Invalid Cost",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 3) Update fields on the Drone object
                selectedExpress.ClientName = txtClient.Text;
                selectedExpress.DroneModel = txtModel.Text;
                selectedExpress.ServiceProblem = txtProblem.Text;
                selectedExpress.Cost = Math.Round(newCost, 2);
                Trace.TraceInformation($"[UpdateSelectedJob_Click] Updated Express job #{selectedExpress.ServiceTag}.");

                // 4) Refresh the UI for queues
                RefreshQueues();
                txtStatus.Text = $"Updated Express job #{selectedExpress.ServiceTag}.";
            }
            else
            {
                // Should never happen, but just in case
                return;
            }

            // 5) After updating, clear the form and disable the Update button
            ClearForm();
            btnUpdate.IsEnabled = false;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ INPUT VALIDATION: COST (≤2 decimals) ═════════════════════════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Prevents any non-numeric or >2-decimal input in the Cost textbox.
        /// Checks the prospective string (inserted char into existing text) against:
        ///   ^\d*\.?\d{0,2}$ 
        /// Blocks the keystroke if it doesn’t match.
        /// </summary>
        /// <param name="sender">The TextBox “txtCost.”</param>
        /// <param name="e">TextCompositionEventArgs with the typed character.</param>
        private void TxtCost_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;

            // Build the prospective text if the new char is inserted
            string proposed = tb.Text.Insert(tb.SelectionStart, e.Text);

            // If it does NOT match digits + optional dot + up to two decimals, block it
            bool isInvalid = !Regex.IsMatch(proposed, @"^\d*\.?\d{0,2}$");
            e.Handled = isInvalid;

            Trace.TraceInformation($"[TxtCost] Proposed='{proposed}', Blocked={isInvalid}");
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ═════ FORM RESET (After Add or Update) ═════════════════════════════════════
        // ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Clears all input fields (Client, Model, Cost, Problem) and resets the Priority radio button to “Regular.”
        /// Does NOT touch the numTag control (it was already advanced).
        /// </summary>
        private void ClearForm()
        {
            txtClient.Clear();
            txtModel.Clear();
            txtCost.Clear();
            txtProblem.Clear();
            rbRegular.IsChecked = true;
            Trace.TraceInformation("[ClearForm] Cleared inputs and set priority to Regular.");
        }
    }
}
